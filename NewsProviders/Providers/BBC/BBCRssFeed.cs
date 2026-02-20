using System.Xml;
using System.Xml.Linq;

namespace NewsProviders.Providers.BBC
{
    public sealed class BbciRssFeed : IDisposable
    {
        private static readonly Uri BaseUri = new("https://feeds.bbci.co.uk/news/");
        private static readonly Uri DefaultArticleBase = new("https://www.bbc.com");

        private static readonly HashSet<string> KnownCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            // Common categories
            "world",
            "uk",
            "business",
            "technology",
            "science_and_environment",
            "entertainment_and_arts",
            "health",
            "politics",
            "education",
            "england", "scotland", "wales", "northern_ireland",
            // aliases
            "top"
        };

        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;

        public BbciRssFeed(HttpClient? httpClient = null)
        {
            _ownsClient = httpClient is null;
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NewsLens/1.0 (+https://example.com)");

            if (!_httpClient.DefaultRequestHeaders.Accept.Any())
                _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/xml;q=0.9, */*;q=0.8");
        }

        /// <summary>
        /// Fetches all article links from BBC's RSS for a given category.
        /// Pass "" or "top" for the main Top Stories feed.
        /// </summary>
        public async Task<IReadOnlyList<Uri>> FetchLinksAsync(string category, CancellationToken cancellationToken = default)
        {
            category ??= string.Empty;

            var normalized = category.Trim().Trim('/');
            if (string.Equals(normalized, "top", StringComparison.OrdinalIgnoreCase))
                normalized = string.Empty; 

            Uri feedUri = string.IsNullOrEmpty(normalized)
                ? new Uri(BaseUri, "rss.xml")
                : new Uri(BaseUri, $"{normalized}/rss.xml");

            Console.WriteLine($"[BbciRssFeed] GET {feedUri.AbsoluteUri}");

            using var response = await _httpClient.GetAsync(feedUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var readerSettings = new XmlReaderSettings
            {
                Async = true,
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Ignore
            };

            using var xmlReader = XmlReader.Create(stream, readerSettings);
            var doc = XDocument.Load(xmlReader);

            var rssLinks = doc
                .Descendants("item")
                .Select(i => i.Element("link")?.Value)
                .Where(s => !string.IsNullOrWhiteSpace(s));

            XNamespace atom = "http://www.w3.org/2005/Atom";
            var atomLinks = doc
                .Descendants(atom + "entry")
                .SelectMany(e => e.Elements(atom + "link"))
                .Where(l => (string?)l.Attribute("rel") is null || (string?)l.Attribute("rel") == "alternate")
                .Select(l => (string?)l.Attribute("href"))
                .Where(s => !string.IsNullOrWhiteSpace(s));

            var allLinks = rssLinks.Concat(atomLinks)
                .Select(href => ToAbsoluteUri(href!, DefaultArticleBase))
                .Where(u => u is not null)!
                .Distinct()
                .ToList();

            return allLinks;
        }

        private static Uri? ToAbsoluteUri(string href, Uri baseUri)
        {
            if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
                return absolute;

            return Uri.TryCreate(baseUri, href, out var combined) ? combined : null;
        }

        public void Dispose()
        {
            if (_ownsClient)
                _httpClient.Dispose();
        }
    }
}