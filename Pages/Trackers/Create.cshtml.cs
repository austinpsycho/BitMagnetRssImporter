using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BitMagnetRssImporter.Pages.Trackers;

public sealed class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public HtmlTracker Tracker { get; set; } = new();

    public void OnGet()
    {
        // TPB-ish sane defaults (you can change in UI)
        Tracker.Enabled = true;
        Tracker.PollIntervalMinutes = 5;
        Tracker.MaxPagesPerRun = 5;
        Tracker.StopAfterSeenStreak = 30;
        Tracker.InfoHashRegex = @"(?i)\b([a-f0-9]{40})\b";
        Tracker.BackfillEnabled = false;
        Tracker.BackfillNextUrl = null;
        Tracker.BackfillCompletedAt = null;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        Tracker.UpdatedAt = DateTimeOffset.UtcNow;

        db.HtmlTrackers.Add(Tracker);
        await db.SaveChangesAsync(ct);

        return RedirectToPage("/Trackers/Index");
    }
}