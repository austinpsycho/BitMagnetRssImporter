namespace BitMagnetRssImporter.Models;

public sealed class IngestionRun
{
    public long Id { get; set; }

    public IngestionSourceType SourceType { get; set; }
    public long SourceId { get; set; }

    public bool IsActive { get; set; }
    public string Phase { get; set; } = "starting"; // fetching, parsing, details, deduping, importing, saving, done

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int? DurationMs { get; set; }

    public DateTimeOffset HeartbeatAt { get; set; }

    public int? HttpStatus { get; set; }

    // common counters
    public int ItemsParsed { get; set; }
    public int Candidates { get; set; }
    public int NewItems { get; set; }
    public int Imported { get; set; }
    public int SkippedNoInfoHash { get; set; }
    public int SkippedSeen { get; set; }

    // progress-y fields (esp. HTML)
    public int PagesVisited { get; set; }
    public int ItemsScanned { get; set; }
    public int SeenStreak { get; set; }

    public string? Error { get; set; } // short message
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}