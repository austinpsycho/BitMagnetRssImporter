namespace BitMagnetRssImporter.Models;

public sealed class RssFeedRun
{
    public long Id { get; set; }

    public long FeedId { get; set; }
    public RssFeed Feed { get; set; } = null!;

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
    public int DurationMs { get; set; }

    public int? HttpStatus { get; set; }

    public int ItemsParsed { get; set; }
    public int Candidates { get; set; }
    public int NewItems { get; set; }
    public int Imported { get; set; }
    public int SkippedNoInfoHash { get; set; }
    public int SkippedSeen { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}