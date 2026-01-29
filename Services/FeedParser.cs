namespace BitMagnetRssImporter.Services;

using System.ServiceModel.Syndication;
using System.Xml;

public sealed class FeedParser
{
    internal static string? GetBestTorrentOrMagnetUrl(SyndicationItem item)
    {
        // 1) enclosure
        var enclosure = item.Links.FirstOrDefault(l =>
            l.RelationshipType is "enclosure" or null && !string.IsNullOrWhiteSpace(l.Uri?.ToString()));
        if (enclosure?.Uri != null) return enclosure.Uri.ToString();

        // 2) link
        var link = item.Links.FirstOrDefault(l => l.RelationshipType == "alternate")?.Uri
                   ?? item.Links.FirstOrDefault()?.Uri;
        if (link != null) return link.ToString();

        // 3) guid-ish fallback
        if (item.Id is { Length: > 0 }) return item.Id;

        return null;
    }

    internal static async Task<IReadOnlyList<SyndicationItem>> ReadFeedAsync(HttpClient http, string feedUrl,
        CancellationToken ct)
    {
        await using var stream = await http.GetStreamAsync(feedUrl, ct);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
        var feed = SyndicationFeed.Load(reader);
        return feed?.Items?.ToList() ?? [];
    }
    public static async Task<IReadOnlyList<SyndicationItem>> ReadFeedAsync(Stream xmlStream, CancellationToken ct)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = null // IMPORTANT: prevents external entity resolution
        };
        using var reader = XmlReader.Create(xmlStream, settings);
        var feed = SyndicationFeed.Load(reader);
        return feed?.Items?.ToList() ?? [];
    }
}