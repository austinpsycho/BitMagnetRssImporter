using BitMagnetRssImporter.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitMagnetRssImporter.Pages;

public sealed class IndexModel(AppDbContext db) : PageModel
{
    public List<RssFeedRow> Feeds { get; private set; } = [];

    public sealed record LastRunRow(
        int? HttpStatus,
        int ItemsParsed,
        int Candidates,
        int NewItems,
        int Imported,
        int SkippedNoInfoHash,
        int SkippedSeen,
        int DurationMs,
        DateTimeOffset StartedAt,
        DateTimeOffset FinishedAt,
        string? Error
    );

    public sealed record RssFeedRow(
        long Id,
        string Name,
        string Url,
        string SourceName,
        bool Enabled,
        int PollIntervalMinutes,
        DateTimeOffset? LastCheckedAt,
        DateTimeOffset? NextDueAt,
        string? LastEtag,
        DateTimeOffset? LastModified,
        LastRunRow? LastRun
    );

    public async Task OnGetAsync(CancellationToken ct)
    {
        var feeds = await db.RssFeeds
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Url,
                f.SourceName,
                f.Enabled,
                f.PollIntervalMinutes,
                f.LastCheckedAt,
                f.LastEtag,
                f.LastModified,
                f.LastRunId
            })
            .ToListAsync(ct);

        var lastRunIds = feeds
            .Where(f => f.LastRunId != null)
            .Select(f => f.LastRunId!.Value)
            .Distinct()
            .ToArray();

        var runMap = await db.RssFeedRuns
            .AsNoTracking()
            .Where(r => lastRunIds.Contains(r.Id))
            .ToDictionaryAsync(
                r => r.Id,
                r => new LastRunRow(
                    r.HttpStatus,
                    r.ItemsParsed,
                    r.Candidates,
                    r.NewItems,
                    r.Imported,
                    r.SkippedNoInfoHash,
                    r.SkippedSeen,
                    r.DurationMs,
                    r.StartedAt,
                    r.FinishedAt,
                    r.Error
                ),
                ct);

        Feeds = feeds.Select(f =>
        {
            DateTimeOffset? nextDue = null;
            if (f.LastCheckedAt is not null)
                nextDue = f.LastCheckedAt.Value.AddMinutes(Math.Max(1, f.PollIntervalMinutes));

            runMap.TryGetValue(f.LastRunId ?? -1, out var run);

            return new RssFeedRow(
                f.Id,
                f.Name,
                f.Url,
                f.SourceName,
                f.Enabled,
                f.PollIntervalMinutes,
                f.LastCheckedAt,
                nextDue,
                f.LastEtag,
                f.LastModified,
                run
            );
        }).ToList();
    }
}