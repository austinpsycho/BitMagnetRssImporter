namespace BitMagnetRssImporter.Models;

public sealed record BitmagnetImportItem(
    string infoHash,
    string name,
    long? size,
    string source,
    DateTimeOffset? publishedAt,
    string? contentType = null,
    string? contentSource = null,
    string? contentId = null
);