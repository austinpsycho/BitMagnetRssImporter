using BitMagnetRssImporter.Data;
using BitMagnetRssImporter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BitMagnetRssImporter.Pages.Trackers;

public sealed class EditModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public HtmlTracker Tracker { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        var t = await db.HtmlTrackers.FindAsync([id], ct);
        if (t is null) return NotFound();

        Tracker = t;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        Tracker.UpdatedAt = DateTimeOffset.UtcNow;

        db.Attach(Tracker).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        await db.SaveChangesAsync(ct);

        return RedirectToPage("/Trackers/Index");
    }
    
    public async Task<IActionResult> OnPostResetBackfillAsync(long id, CancellationToken ct)
    {
        var t = await db.HtmlTrackers.FindAsync([id], ct);
        if (t is null) return NotFound();

        t.BackfillNextUrl = null;
        t.BackfillCompletedAt = null;

        // leave BackfillEnabled as-is (or set true if you want)
        t.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return RedirectToPage("/Trackers/Edit", new { id });
    }
}