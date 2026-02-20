namespace NewsProviders.Providers.EuropeanUnion.EuCommission;

using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;


public sealed class EuCommissionNewsScraper
{
    private readonly HttpClient _http;

    public EuCommissionNewsScraper(HttpClient http)
    {
        _http = http;
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
        var h1 = document.QuerySelector("main h1")
                 ?? document.QuerySelector("h1");

        var title = h1?.TextContent?.Trim();
        if (!string.IsNullOrWhiteSpace(title))
            return title;

        return document.Title?.Trim() ?? string.Empty;
    }

    private static string ExtractBody(IDocument document)
    {
        var root =
            document.QuerySelector("article") ??
            document.QuerySelector("main") ??
            document.Body;

        if (root is null)
            return string.Empty;

        var paragraphs = root
            .QuerySelectorAll("p")
            .Select(p => p.TextContent?.Trim() ?? string.Empty)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .TakeWhile(t =>
                !t.StartsWith("Contacts", StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith("Press contacts", StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith("Background", StringComparison.OrdinalIgnoreCase)
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