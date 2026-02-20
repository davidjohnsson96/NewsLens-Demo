using HtmlAgilityPack;
using NewsProviders.Providers.SVTNews.SVTNewsInterfaces;

namespace NewsProviders.Providers.SVTNews
{
    public class SvtNewsScraper : IDisposable , ISvtNewsScraper
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;

        public SvtNewsScraper(HttpClient? httpClient = null)
        {
            _ownsClient = httpClient is null;
            _httpClient = httpClient ?? new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NewsLens/1.0 (+https://example.com)");
        }
        public async Task<string> ScrapeNewsAsync(string articleUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(articleUrl))
                throw new ArgumentException("Article URL must be provided.", nameof(articleUrl));

            if (!Uri.TryCreate(articleUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException("Invalid URL format.", nameof(articleUrl));

            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var paragraphs = doc.DocumentNode
                .SelectNodes("//div[contains(@class,'nyh__article-body')]//p | //article//p")
                ?.Select(p => p.InnerText.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            if (paragraphs is null || paragraphs.Count == 0)
            {
                paragraphs = doc.DocumentNode
                    .SelectNodes("//p")
                    ?.Select(p => p.InnerText.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }

            return paragraphs is null || paragraphs.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
        }

        public void Dispose()
        {
            if (_ownsClient)
                _httpClient.Dispose();
        }
        
    }
}