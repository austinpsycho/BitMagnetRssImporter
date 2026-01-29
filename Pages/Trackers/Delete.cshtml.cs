using BitMagnetRssImporter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BitMagnetRssImporter.Pages.Trackers;

public sealed class DeleteModel(AppDbContext db) : PageModel
{
    public string Name { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        var t = await db.HtmlTrackers.FindAsync([id], ct);
        if (t is null) return NotFound();

        Name = t.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken ct)
    {
        var t = await db.HtmlTrackers.FindAsync([id], ct);
        if (t is null) return RedirectToPage("/Trackers/Index");

        db.HtmlTrackers.Remove(t);
        await db.SaveChangesAsync(ct);

        return RedirectToPage("/Trackers/Index");
    }
}