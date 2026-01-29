namespace BitMagnetRssImporter.Models;

public sealed class RssSeenItem
{
    public long Id { get; set; }

    public long FeedId { get; set; }
    public RssFeed Feed { get; set; } = null!;

    // A stable key for an item: prefer guid, else enclosure/link, else infoHash
    public required string ItemKey { get; set; }

    public string? InfoHash { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.UtcNow;
}