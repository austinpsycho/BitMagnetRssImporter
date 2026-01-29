using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html.Dom;
using BitMagnetRssImporter.Models;

namespace BitMagnetRssImporter.Services;

public sealed class HtmlTrackerScraper
{
    private readonly IBrowsingContext _ctx = BrowsingContext.New(Configuration.Default);

    public sealed record ListRow(string Title, Uri DetailUrl);

    public async Task<(List<ListRow> Rows, Uri? NextPage)> ReadListPageAsync(HttpClient http, HtmlTracker tracker, Uri pageUrl, CancellationToken ct)
    {
        var html = await http.GetStringAsync(pageUrl, ct);

        var doc = await _ctx.OpenAsync(req => req.Content(html).Address(pageUrl), ct);

        var rows = new List<ListRow>();

        foreach (var row in doc.QuerySelectorAll(tracker.RowSelector))
        {
            var linkEl = row.QuerySelector(tracker.DetailLinkSelector) as IHtmlAnchorElement;
            if (linkEl?.Href is null) continue;

            var detailUrl = new Uri(linkEl.Href);

            string title = linkEl.TextContent?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(tracker.TitleSelector))
            {
                var titleEl = row.QuerySelector(tracker.TitleSelector!);
                if (titleEl is not null)
                {
                    var t = titleEl.TextContent?.Trim();
                    if (!string.IsNullOrWhiteSpace(t)) title = t!;
                }
            }

            if (string.IsNullOrWhiteSpace(title))
                title = detailUrl.ToString();

            rows.Add(new ListRow(title, detailUrl));
        }

        Uri? next = null;
        if (!string.IsNullOrWhiteSpace(tracker.NextPageSelector))
        {
            var nextEl = doc.QuerySelector(tracker.NextPageSelector!) as IHtmlAnchorElement;
            if (nextEl?.Href is not null)
                next = new Uri(nextEl.Href);
        }

        return (rows, next);
    }

    public async Task<string?> TryReadInfoHashFromDetailPageAsync(HttpClient http, HtmlTracker tracker, Uri detailUrl, CancellationToken ct)
    {
        var html = await http.GetStringAsync(detailUrl, ct);

        var rx = new Regex(tracker.InfoHashRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var m = rx.Match(html);
        if (!m.Success) return null;

        // group 1 is typical; fallback to whole match
        var hash = m.Groups.Count > 1 ? m.Groups[1].Value : m.Value;
        hash = hash.Trim();

        // normalize to lower (bitmagnet accepts either; keeping consistent helps)
        return hash.Length == 40 ? hash.ToLowerInvariant() : null;
    }
}