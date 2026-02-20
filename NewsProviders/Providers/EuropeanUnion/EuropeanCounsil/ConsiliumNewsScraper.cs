namespace NewsProviders.Providers.EuropeanUnion.EuropeanCounsil;


using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

public sealed class ConsiliumNewsScraper
{
    private readonly HttpClient _http;

    public ConsiliumNewsScraper(HttpClient http)
    {
        _http = http;

        // === STANDARD BROWSER PROFILE ===
        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/123.0.0.0 Safari/537.36");

        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.ParseAdd(
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        
        _http.DefaultRequestHeaders.AcceptLanguage.Clear();
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        if (!_http.DefaultRequestHeaders.Contains("Accept-Encoding"))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");

    }

    public async Task<(string Title, string Body)> ScrapeArticleAsync(
        string url,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Referrer = new Uri("https://www.consilium.europa.eu/en/press/press-releases/");

        using var response = await _http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new HttpRequestException(
                $"Forbidden when requesting {url}",
                null,
                response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(ct);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(
            req => req.Content(html).Address(url),
            ct);

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

        if (!string.IsNullOrWhiteSpace(h1?.TextContent))
            return h1.TextContent.Trim();

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
            .ToList();

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