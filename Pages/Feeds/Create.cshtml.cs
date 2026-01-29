using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BitMagnetRssImporter.Pages.Feeds;

public sealed class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty]
    public RssFeed Feed { get; set; } = new()
    {
        Name = "",
        Url = "",
        SourceName = "myrss",
        Enabled = true,
        PollIntervalMinutes = 5
    };

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        Feed.CreatedAt = DateTimeOffset.UtcNow;
        Feed.UpdatedAt = DateTimeOffset.UtcNow;

        _db.RssFeeds.Add(Feed);
        await _db.SaveChangesAsync(ct);

        return RedirectToPage("/Index");
    }
}