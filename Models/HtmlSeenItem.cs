namespace BitMagnetRssImporter.Models;

public sealed class HtmlSeenItem
{
    public long Id { get; set; }
    public long TrackerId { get; set; }

    public string ItemKey { get; set; } = "";     // prefer infoHash, else URL
    public string? InfoHash { get; set; }
    public string? Title { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}