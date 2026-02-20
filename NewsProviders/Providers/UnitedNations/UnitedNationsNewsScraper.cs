namespace NewsProviders.Providers.UnitedNations;

using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

public sealed class UnitedNationsNewsScraper
{
    private readonly HttpClient _http;

    public UnitedNationsNewsScraper(HttpClient http)
    {
        _http = http;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/123.0.0.0 Safari/537.36");
    }

    public async Task<(string Title, string Body)> ScrapeArticleAsync(
        string url,
        CancellationToken ct = default)
    {
        var html = await _http.GetStringAsync(url, ct);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html).Address(url), ct);

        var title = ExtractTitle(document);
        var body  = ExtractBody(document);

        title = CleanText(title);
        body  = CleanText(body);

        return (title, body);
    }

    private static string ExtractTitle(IDocument document)
    {
        var h1 = document.QuerySelector("main h1") ?? document.QuerySelector("h1");
        if (!string.IsNullOrWhiteSpace(h1?.TextContent))
            return h1.TextContent.Trim();

        return document.Title?.Trim() ?? string.Empty;
    }

    private static string ExtractBody(IDocument document)
    {
        if (document is null)
            return string.Empty;

        var root =
            document.QuerySelector("div.field--name-field-text-column.field__item")
            ?? document.QuerySelector("div.field--name-field-text-column")
            ?? document.QuerySelector("article")
            ?? document.QuerySelector("main")
            ?? document.Body;

        if (root is null)
            return string.Empty;
        
        var blocks = root
            .QuerySelectorAll("p, h2, h3")
            .Select(n => n.TextContent?.Trim() ?? string.Empty)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (blocks.Count == 0)
        {
            var text = root.TextContent?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = Regex.Replace(text, @"\s+", " ");
            return text;
        }

        return string.Join("\n\n", blocks);
    }



    private static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = WebUtility.HtmlDecode(text);

        text = text
            .Replace("\u00AD", "")
            .Replace("\u200B", "")
            .Replace("\u200C", "")
            .Replace("\u200D", "")
            .Replace("\uFEFF", "");

        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }
}