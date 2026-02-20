using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace NewsProviders.Providers.Regeringen;

public sealed class RegeringenNewsScraper
{
    private readonly HttpClient _http;

    public RegeringenNewsScraper(HttpClient http) => _http = http;

    public async Task<(string Title, string Body)> ScrapeArticleAsync(
        string url,
        CancellationToken ct = default)
    {
        var html = await _http.GetStringAsync(url, ct);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html).Address(url), ct);

        var title = HtmlTextUtils.ExtractTitle(document);
        var body  = HtmlTextUtils.ExtractBody(document);

        title = HtmlTextUtils.CleanText(title);
        body  = HtmlTextUtils.CleanText(body);

        return (title, body);
    }
}

public static class HtmlTextUtils
{
    public static string ExtractVisibleText(string html)
    {
        var noTags = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
        var collapsed = System.Text.RegularExpressions.Regex.Replace(noTags, "\\s+", " ");
        return collapsed.Trim();
    }
    
    public static string ExtractTitle(IDocument document)
    {
        var h1 = document.QuerySelector("main h1")
                 ?? document.QuerySelector("h1");

        var title = h1?.TextContent?.Trim();

        if (!string.IsNullOrWhiteSpace(title))
            return title;

        return document.Title?.Trim() ?? string.Empty;
    }

    public static string ExtractBody(IDocument document)
    {
        var root =
            document.QuerySelector("article") ??
            document.QuerySelector("main") ??
            document.Body;

        if (root == null)
            return string.Empty;

        var paragraphs = root
            .QuerySelectorAll("p")
            .Select(p => p.TextContent?.Trim() ?? string.Empty)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .TakeWhile(t =>
                !t.StartsWith("Presskontakt", StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith("Presskontakt", StringComparison.Ordinal) &&
                !t.StartsWith("Presskontakt", StringComparison.CurrentCultureIgnoreCase)
            )
            .ToList();
        if (paragraphs.Count == 0)
        {
            paragraphs = root
                .QuerySelectorAll("p")
                .Select(p => p.TextContent?.Trim() ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }

        return string.Join("\n\n", paragraphs);
    }
    
    public static string CleanText(string text)
    {
        if (text == null) return "";

        text = text.Replace("\u00AD", "");

        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }
    
}