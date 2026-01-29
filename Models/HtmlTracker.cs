namespace BitMagnetRssImporter.Models;

public sealed class HtmlTracker
{
    public long Id { get; set; }

    public string Name { get; set; } = "";
    public string StartUrl { get; set; } = "";
    public string SourceName { get; set; } = "html";

    public bool Enabled { get; set; } = true;
    public int PollIntervalMinutes { get; set; } = 5;

    public int MaxPagesPerRun { get; set; } = 5;
    public int StopAfterSeenStreak { get; set; } = 30;

    // List page rules
    public string RowSelector { get; set; } = "";
    public string DetailLinkSelector { get; set; } = "";   // within row: link to details page
    public string? TitleSelector { get; set; }             // within row: title text (optional)
    public string? NextPageSelector { get; set; }          // on doc: next page link selector (optional)

    // Detail page rules
    public string InfoHashRegex { get; set; } = @"(?i)\b([a-f0-9]{40})\b";

    // status fields like RSS
    public DateTimeOffset? LastCheckedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public long? LastRunId { get; set; }
    public IngestionRun? LastRun { get; set; }
    
    // Backfill mode: crawl forward over many runs
    public bool BackfillEnabled { get; set; } = false;

    // Cursor: where the next backfill run should start.
    // If null, starts at StartUrl.
    public string? BackfillNextUrl { get; set; }

    // Mark when we reached the end (no next page).
    public DateTimeOffset? BackfillCompletedAt { get; set; }
}