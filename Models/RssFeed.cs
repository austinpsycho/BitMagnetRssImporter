namespace BitMagnetRssImporter.Models;

public sealed class RssFeed
{
    public long Id { get; set; }

    public required string Name { get; set; }
    public required string Url { get; set; }

    // What shows up in bitmagnet as `source`
    public required string SourceName { get; set; }

    // Optional: allow different import endpoints per feed (or just set one globally)
    public string? BitmagnetImportUrl { get; set; }

    public bool Enabled { get; set; } = true;

    public int PollIntervalMinutes { get; set; } = 5;
    public DateTimeOffset? LastCheckedAt { get; set; }

    // Conditional GET support
    public string? LastEtag { get; set; }
    public DateTimeOffset? LastModified { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}