using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitMagnetRssImporter.Pages.Feeds;

public sealed class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public RssFeed Feed { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        var feed = await _db.RssFeeds.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feed is null) return NotFound();

        Feed = feed;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        var existing = await _db.RssFeeds.FirstOrDefaultAsync(f => f.Id == Feed.Id, ct);
        if (existing is null) return NotFound();

        existing.Name = Feed.Name;
        existing.Url = Feed.Url;
        existing.SourceName = Feed.SourceName;
        existing.PollIntervalMinutes = Feed.PollIntervalMinutes;
        existing.Enabled = Feed.Enabled;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return RedirectToPage("/Index");
    }
}