using System.Xml;
using System.Xml.Linq;
using NewsProviders.Providers.SVTNews.SVTNewsInterfaces;

namespace NewsProviders.Providers.SVTNews
{
    public class SvtrssFeed : IDisposable, ISvtrssFeed
    {
        private static readonly Uri BaseUri = new("https://www.svt.se/nyheter/");
        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;

        public SvtrssFeed(HttpClient? httpClient = null)
        {
            _ownsClient = httpClient is null;
            _httpClient = httpClient ?? new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NewsLens/1.0 (+https://example.com)");
            }
        }
        
        public async Task<IReadOnlyList<Uri>> FetchLinksAsync(string category, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category must be a non-empty path segment.", nameof(category));
            
            var normalized = category.Trim().Trim('/');

            var feedUri = new Uri(BaseUri, $"{normalized}/rss.xml");

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
                .Select(s => ToAbsoluteUri(s!, new Uri("https://www.svt.se")))
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