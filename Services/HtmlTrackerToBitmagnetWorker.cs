using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BitMagnetRssImporter.Services;

public sealed class HtmlTrackerToBitmagnetWorker(
    IHttpClientFactory httpClientFactory,
    ILogger<HtmlTrackerToBitmagnetWorker> log,
    IServiceScopeFactory scopeFactory,
    HtmlTrackerScraper scraper,
    IConfiguration config)
    : BackgroundService
{
    private readonly Uri _defaultBitmagnetImport =
        new(config["Bitmagnet:ImportUrl"] ?? "http://localhost:3333/import");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var http = httpClientFactory.CreateClient(nameof(HtmlTrackerToBitmagnetWorker));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTimeOffset.UtcNow;

                var trackers = await db.HtmlTrackers
                    .Where(t => t.Enabled)
                    .ToListAsync(stoppingToken);

                foreach (var t in trackers)
                {
                    if (!IsDue(t, now)) continue;

                    var active = await db.IngestionRuns.AnyAsync(
                        r => r.SourceType == IngestionSourceType.HtmlTracker && r.SourceId == t.Id && r.IsActive,
                        stoppingToken);

                    if (active) continue;

                    await PollOneTrackerAsync(db, http, t, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "HTML worker loop failed");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private static bool IsDue(HtmlTracker t, DateTimeOffset now)
    {
        if (t.LastCheckedAt is null) return true;
        var next = t.LastCheckedAt.Value.AddMinutes(Math.Max(1, t.PollIntervalMinutes));
        return now >= next;
    }

    private async Task PollOneTrackerAsync(AppDbContext db, HttpClient http, HtmlTracker tracker, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;

        var run = new IngestionRun
        {
            SourceType = IngestionSourceType.HtmlTracker,
            SourceId = tracker.Id,
            IsActive = true,
            Phase = "starting",
            StartedAt = startedAt,
            HeartbeatAt = startedAt,
            CreatedAt = startedAt
        };

        db.IngestionRuns.Add(run);

        // Decide mode
        var backfillMode = tracker.BackfillEnabled && tracker.BackfillCompletedAt is null;
        var maxPages = Math.Max(1, tracker.MaxPagesPerRun);
        var stopStreak = Math.Max(1, tracker.StopAfterSeenStreak);

        // In monitor mode we start from StartUrl every time.
        // In backfill mode we start from cursor (or StartUrl if cursor null).
        Uri pageUrl = new Uri(backfillMode
            ? (tracker.BackfillNextUrl ?? tracker.StartUrl)
            : tracker.StartUrl);

        try
        {
            tracker.LastCheckedAt = DateTimeOffset.UtcNow;
            tracker.UpdatedAt = DateTimeOffset.UtcNow;

            // Save early so UI shows we started
            await db.SaveChangesAsync(ct);

            var toImport = new Dictionary<string, BitmagnetImportItem>(StringComparer.OrdinalIgnoreCase);

            for (var page = 0; page < maxPages; page++)
            {
                run.PagesVisited = page + 1;
                run.Phase = backfillMode ? "backfill_fetching_list" : "fetching_list";
                run.HeartbeatAt = DateTimeOffset.UtcNow;

                // Fetch + parse list page
                (List<HtmlTrackerScraper.ListRow> rows, Uri? next) = await scraper.ReadListPageAsync(http, tracker, pageUrl, ct);
                run.ItemsParsed += rows.Count;

                run.Phase = backfillMode ? "backfill_details" : "details";
                run.HeartbeatAt = DateTimeOffset.UtcNow;

                foreach (var r in rows)
                {
                    run.ItemsScanned++;
                    run.HeartbeatAt = DateTimeOffset.UtcNow;

                    var link = r.BestLink.ToString();

                    string? infoHash =
                        link.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase)
                            ? MagnetInfoHashParser.TryGetInfoHashFromMagnet(link)
                            : null;

                    if (infoHash is null && link.StartsWith("http", StringComparison.OrdinalIgnoreCase) && link.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
                        infoHash = await UrlInfoHashParser.TryComputeInfoHashFromTorrentUrlAsync(http, link, ct);

                    // If it wasn't magnet/torrent, treat it as a detail page and regex the hash
                    if (infoHash is null && link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        infoHash = await scraper.TryReadInfoHashFromDetailPageAsync(http, tracker, new Uri(link), ct);

                    if (infoHash is null)
                    {
                        run.SkippedNoInfoHash++;
                        continue;
                    }

                    run.Candidates++;

                    // Dedupe check (v1 per-item; can batch later if needed)
                    var alreadySeen = await db.HtmlSeenItems.AnyAsync(
                        x => x.TrackerId == tracker.Id && x.ItemKey == infoHash,
                        ct);

                    if (alreadySeen)
                    {
                        run.SkippedSeen++;

                        // Only use the seen-streak stop rule in MONITOR mode
                        if (!backfillMode)
                        {
                            run.SeenStreak++;
                            if (run.SeenStreak >= stopStreak)
                            {
                                run.Phase = "stop_seen_streak";
                                break;
                            }
                        }

                        continue;
                    }

                    // new item resets streak in monitor mode
                    if (!backfillMode) run.SeenStreak = 0;

                    if (!toImport.ContainsKey(infoHash))
                    {
                        toImport[infoHash] = new BitmagnetImportItem(
                            infoHash: infoHash,
                            name: r.Title,
                            size: null,
                            source: tracker.SourceName,
                            publishedAt: null
                        );
                    }
                }

                if (run.Phase == "stop_seen_streak")
                    break;

                // BACKFILL cursor update: advance only after this page completed successfully
                if (backfillMode)
                {
                    tracker.BackfillNextUrl = next?.ToString();

                    if (next is null)
                    {
                        tracker.BackfillCompletedAt = DateTimeOffset.UtcNow;
                        run.Phase = "backfill_done";
                        break;
                    }
                }

                // Persist progress once per page
                run.HeartbeatAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                if (next is null) break;
                pageUrl = next;
            }

            run.NewItems = toImport.Count;

            if (toImport.Count == 0)
                return;

            run.Phase = "importing";
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            var items = toImport.Values.ToList();
            await BitmagnetImporter.PostToBitmagnetImportAsync(http, _defaultBitmagnetImport, items, ct);

            run.Imported = items.Count;

            run.Phase = "saving";
            run.HeartbeatAt = DateTimeOffset.UtcNow;

            foreach (var item in items)
            {
                db.HtmlSeenItems.Add(new HtmlSeenItem
                {
                    TrackerId = tracker.Id,
                    ItemKey = item.infoHash,
                    InfoHash = item.infoHash,
                    Title = item.name
                });
            }

            log.LogInformation("Imported {Count} items from HTML tracker {Name} (Backfill={Backfill})",
                items.Count, tracker.Name, backfillMode);
        }
        catch (Exception ex)
        {
            run.Error = ex.Message;
            log.LogWarning(ex, "HTML poll failed for {Name}", tracker.Name);
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
                tracker.LastRunId = run.Id;
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // best-effort
            }
        }
    }
}