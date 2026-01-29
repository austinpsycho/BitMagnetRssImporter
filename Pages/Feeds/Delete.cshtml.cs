using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitMagnetRssImporter.Pages.Feeds;

public sealed class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db) => _db = db;

    public RssFeed Feed { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        var feed = await _db.RssFeeds.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feed is null) return NotFound();

        Feed = feed;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken ct)
    {
        var feed = await _db.RssFeeds.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feed is null) return RedirectToPage("/Index");

        _db.RssFeeds.Remove(feed);
        await _db.SaveChangesAsync(ct);

        return RedirectToPage("/Index");
    }
}