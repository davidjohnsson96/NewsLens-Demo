using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsProviders.Providers.Common;

namespace NewsProviders.Providers.Regeringen;

public sealed class RegeringenNewsProvider : NewsProviderBase
{
    private readonly RegeringenOptions _opt;
    private readonly RegeringenRssClient _rss;
    private readonly RegeringenNewsScraper _scraper;
    private readonly ILogger<RegeringenNewsProvider> _logger;
    public RegeringenNewsProvider(
        IOptions<RegeringenOptions> options,
        RegeringenRssClient rssClient,
        RegeringenNewsScraper scraper,
        ILogger<RegeringenNewsProvider> logger) : base("RegeringenProvider")
    {
        _opt     = options.Value;
        _rss     = rssClient;
        _scraper = scraper;
        _logger = logger;
        BaseUrl      = new Uri(_opt.PressReleasesRssUrl).GetLeftPart(UriPartial.Authority);
    }
    
    public string BaseUrl { get; }
    public string ApiKey => string.Empty; // not used

    public override async Task<IReadOnlyList<NewsItem>> GetNewsAsync(Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null, CancellationToken ct = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["serviceName"] = base.ProviderName
               }))
        {
            dedupCheck ??= DefaultDedup;
            var feedUri = new Uri(_opt.PressReleasesRssUrl);
            _logger.LogInformation("Fetching RSS items");
            var rssItems = await _rss.FetchAsync(feedUri, _opt.MaxItems, ct);
            _logger.LogInformation("Number of rssItems fetched: {items}",  rssItems.Count);
            var list = new List<NewsItem>();

            foreach (var item in rssItems)
            {
                try
                {
                    if (!await dedupCheck(ProviderName, item.Link, ct))
                    {
                        _logger.LogInformation("Skipping already processed article: {Url}", item.Link);
                        continue;
                    }
                    
                    _logger.LogInformation("Scraping article: {Url}", item.Link);

                    var (title, body) = await _scraper.ScrapeArticleAsync(item.Link, ct);

                    var news = new NewsItem(
                        Title: title,
                        Url: item.Link,
                        Body: body,
                        PublishedAt: item.PublishedAt,
                        Source: ProviderName
                    );
                    NewsItemValidator.ValidateNewsItem(news);
                    list.Add(news);

                    _logger.LogInformation("Finished article: {Url}", item.Link);
                }
                catch (Exception ex)
                {
                    UpdateScheduleAfterRun(DateTimeOffset.UtcNow);
                    _logger.LogError(ex, "Failed to process article: {Url}", item.Link);
                }
            }
            UpdateScheduleAfterRun(DateTimeOffset.UtcNow);
            return list;
        }
    }
}