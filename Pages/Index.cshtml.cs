using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitMagnetRssImporter.Pages;

public sealed class IndexModel(AppDbContext db) : PageModel
{
    public List<RssFeedRow> Feeds { get; private set; } = [];

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
        string? BitmagnetImportUrl
    );

    public async Task OnGetAsync(CancellationToken ct)
    {
        var feeds = await db.RssFeeds
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

        Feeds = feeds.Select(f =>
        {
            DateTimeOffset? nextDue = null;
            if (f.LastCheckedAt is not null)
                nextDue = f.LastCheckedAt.Value.AddMinutes(Math.Max(1, f.PollIntervalMinutes));

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
                f.BitmagnetImportUrl
            );
        }).ToList();
    }
}