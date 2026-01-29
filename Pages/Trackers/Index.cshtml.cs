using BitMagnetRssImporter.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitMagnetRssImporter.Pages.Trackers;

public sealed class IndexModel(AppDbContext db) : PageModel
{
    public List<Row> Trackers { get; private set; } = [];

    public sealed record LastRunRow(
        bool IsActive,
        string Phase,
        DateTimeOffset HeartbeatAt,
        int PagesVisited,
        int ItemsScanned,
        int SeenStreak,
        int ItemsParsed,
        int NewItems,
        int Imported,
        int? DurationMs,
        int? HttpStatus,
        string? Error
    );

    public sealed record Row(
        long Id,
        string Name,
        string StartUrl,
        string SourceName,
        bool Enabled,
        int PollIntervalMinutes,
        int MaxPagesPerRun,
        int StopAfterSeenStreak,
        DateTimeOffset? LastCheckedAt,
        LastRunRow? LastRun
    );

    public async Task OnGetAsync(CancellationToken ct)
    {
        var trackers = await db.HtmlTrackers
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.StartUrl,
                t.SourceName,
                t.Enabled,
                t.PollIntervalMinutes,
                t.MaxPagesPerRun,
                t.StopAfterSeenStreak,
                t.LastCheckedAt,
                t.LastRunId
            })
            .ToListAsync(ct);

        var lastRunIds = trackers
            .Where(t => t.LastRunId != null)
            .Select(t => t.LastRunId!.Value)
            .Distinct()
            .ToArray();

        var runMap = await db.IngestionRuns
            .AsNoTracking()
            .Where(r => lastRunIds.Contains(r.Id))
            .ToDictionaryAsync(
                r => r.Id,
                r => new LastRunRow(
                    r.IsActive,
                    r.Phase,
                    r.HeartbeatAt,
                    r.PagesVisited,
                    r.ItemsScanned,
                    r.SeenStreak,
                    r.ItemsParsed,
                    r.NewItems,
                    r.Imported,
                    r.DurationMs,
                    r.HttpStatus,
                    r.Error
                ),
                ct);

        Trackers = trackers.Select(t =>
        {
            runMap.TryGetValue(t.LastRunId ?? -1, out var run);

            return new Row(
                t.Id,
                t.Name,
                t.StartUrl,
                t.SourceName,
                t.Enabled,
                t.PollIntervalMinutes,
                t.MaxPagesPerRun,
                t.StopAfterSeenStreak,
                t.LastCheckedAt,
                run
            );
        }).ToList();
    }
}
