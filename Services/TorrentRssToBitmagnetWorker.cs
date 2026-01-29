using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;

namespace BitMagnetRssImporter.Services;

public sealed class TorrentRssToBitmagnetWorker(
    IHttpClientFactory httpClientFactory,
    ILogger<TorrentRssToBitmagnetWorker> log,
    IServiceScopeFactory scopeFactory,
    IConfiguration config)
    : BackgroundService
{
    private readonly Uri _defaultBitmagnetImport =
        new(config["Bitmagnet:ImportUrl"] ?? "http://localhost:3333/import");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var http = httpClientFactory.CreateClient(nameof(TorrentRssToBitmagnetWorker));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTimeOffset.UtcNow;

                var feeds = await db.RssFeeds.Where(f => f.Enabled).ToListAsync(stoppingToken);

                foreach (var feed in feeds)
                {
                    if (!IsDue(feed, now)) continue;

                    // skip if an active run exists for this source
                    var active = await db.IngestionRuns.AnyAsync(
                        r => r.SourceType == IngestionSourceType.RssFeed && r.SourceId == feed.Id && r.IsActive,
                        stoppingToken);

                    if (active) continue;

                    await PollOneFeedAsync(db, http, feed, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Worker loop failed");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private static bool IsDue(RssFeed feed, DateTimeOffset now)
    {
        if (feed.LastCheckedAt is null) return true;
        var next = feed.LastCheckedAt.Value.AddMinutes(Math.Max(1, feed.PollIntervalMinutes));
        return now >= next;
    }

    private async Task PollOneFeedAsync(AppDbContext db, HttpClient http, RssFeed feed, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;

        var run = new IngestionRun
        {
            SourceType = IngestionSourceType.RssFeed,
            SourceId = feed.Id,
            IsActive = true,
            Phase = "fetching",
            StartedAt = startedAt,
            HeartbeatAt = startedAt,
            CreatedAt = startedAt
        };

        db.IngestionRuns.Add(run);

        try
        {
            feed.LastCheckedAt = DateTimeOffset.UtcNow;
            feed.UpdatedAt = DateTimeOffset.UtcNow;

            var request = new HttpRequestMessage(HttpMethod.Get, feed.Url);
            if (!string.IsNullOrWhiteSpace(feed.LastEtag))
                request.Headers.TryAddWithoutValidation("If-None-Match", feed.LastEtag);

            if (feed.LastModified.HasValue)
                request.Headers.IfModifiedSince = feed.LastModified.Value.UtcDateTime;

            HttpResponseMessage resp;
            try
            {
                resp = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (Exception ex)
            {
                run.Error = $"Fetch failed: {ex.Message}";
                return;
            }

            run.HttpStatus = (int)resp.StatusCode;
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            if (resp.StatusCode == HttpStatusCode.NotModified)
                return;

            resp.EnsureSuccessStatusCode();

            if (resp.Headers.ETag != null)
                feed.LastEtag = resp.Headers.ETag.Tag;

            if (resp.Content.Headers.LastModified.HasValue)
                feed.LastModified = resp.Content.Headers.LastModified.Value;

            run.Phase = "parsing";
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var feedItems = await FeedParser.ReadFeedAsync(stream, ct);

            run.ItemsParsed = feedItems.Count;

            // candidate items keyed by infoHash
            var candidates = new Dictionary<string, BitmagnetImportItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var i in feedItems)
            {
                var itemUrl = FeedParser.GetBestTorrentOrMagnetUrl(i);
                if (string.IsNullOrWhiteSpace(itemUrl)) continue;

                string? infoHash = MagnetInfoHashParser.TryGetInfoHashFromMagnet(itemUrl);

                if (infoHash is null && itemUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    infoHash = await UrlInfoHashParser.TryComputeInfoHashFromTorrentUrlAsync(http, itemUrl, ct);

                if (infoHash is null)
                {
                    run.SkippedNoInfoHash++;
                    continue;
                }

                if (!candidates.ContainsKey(infoHash))
                {
                    candidates[infoHash] = new BitmagnetImportItem(
                        infoHash: infoHash,
                        name: i.Title?.Text ?? infoHash,
                        size: null,
                        source: feed.SourceName,
                        publishedAt: i.PublishDate != DateTimeOffset.MinValue ? i.PublishDate : null
                    );
                }
            }

            run.Candidates = candidates.Count;

            if (candidates.Count == 0) return;

            run.Phase = "deduping";
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            var keys = candidates.Keys.ToArray();

            var seenKeys = await db.RssSeenItems
                .Where(x => x.FeedId == feed.Id && keys.Contains(x.ItemKey))
                .Select(x => x.ItemKey)
                .ToListAsync(ct);

            var seenSet = seenKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            run.SkippedSeen = seenSet.Count;

            var newItems = candidates
                .Where(kvp => !seenSet.Contains(kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();

            run.NewItems = newItems.Count;

            if (newItems.Count == 0) return;

            run.Phase = "importing";
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            await BitmagnetImporter.PostToBitmagnetImportAsync(http, _defaultBitmagnetImport, newItems, ct);
            run.Imported = newItems.Count;

            run.Phase = "saving";
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            foreach (var item in newItems)
            {
                db.RssSeenItems.Add(new RssSeenItem
                {
                    FeedId = feed.Id,
                    ItemKey = item.infoHash,
                    InfoHash = item.infoHash,
                    Title = item.name
                });
            }

            log.LogInformation("Imported {Count} items from {FeedName}", newItems.Count, feed.Name);
        }
        catch (Exception ex)
        {
            run.Error = ex.Message;
            log.LogWarning(ex, "RSS poll failed for {FeedName}", feed.Name);
        }
        finally
        {
            sw.Stop();

            run.IsActive = false;
            run.Phase = "done";
            run.FinishedAt = DateTimeOffset.UtcNow;
            run.DurationMs = (int)sw.ElapsedMilliseconds;
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            try
            {
                await db.SaveChangesAsync(ct);
                feed.LastRunId = run.Id;
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // keep behavior consistent with your current best-effort idempotency
            }
        }
    }
}