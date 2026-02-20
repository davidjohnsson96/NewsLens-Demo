using Microsoft.Extensions.Logging;

namespace NewsProviders.Providers.EuropeanUnion.EuCommission;

using NewsProviders.Providers.Common;
using Microsoft.Extensions.Options;


public sealed class EuCommissionNewsProvider : NewsProviderBase
{
    private readonly EuCommissionOptions _opt;
    private readonly EuCommissionRSSClient _rss;
    private readonly EuCommissionNewsScraper _scraper;
    private readonly ILogger<EuCommissionNewsProvider> _logger;

    public EuCommissionNewsProvider(
        IOptions<EuCommissionOptions> options,
        EuCommissionRSSClient rssClient,
        EuCommissionNewsScraper scraper,
        ILogger<EuCommissionNewsProvider> logger) : base("EuCommissionProvider")
    {
        _opt = options.Value;
        _rss = rssClient;
        _scraper = scraper;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_opt.RssUrl))
            throw new InvalidOperationException("EuCommissionOptions.RssUrl is not configured.");
    }

    public string ProviderName { get; } = "EuCommission";
    public string BaseUrl => new Uri(_opt.RssUrl).GetLeftPart(UriPartial.Authority);
    public string ApiKey => string.Empty;
    

    public override async Task<IReadOnlyList<NewsItem>> GetNewsAsync(Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null, CancellationToken ct = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["serviceName"] = ProviderName
               }))
        {
            dedupCheck ??= DefaultDedup;
            var feedUri = new Uri(_opt.RssUrl);
            _logger.LogInformation("Fetching rss items");
            var rssItems = await _rss.FetchAsync(feedUri, _opt.MaxItems, ct);
            _logger.LogInformation($"{rssItems.Count} items fetched");
            var list = new List<NewsItem>();
    
            foreach (var item in rssItems)
            {
                
                if (!await dedupCheck(ProviderName, item.Link, ct))
                {
                    _logger.LogInformation("Skipping already processed article: {Url}", item.Link);
                    continue;
                }
                try
                {
                    _logger.LogInformation("Scraping article: {Url}", item.Link);

                    var (title, body) = await _scraper.ScrapeArticleAsync(item.Link, ct);

                    if (string.IsNullOrWhiteSpace(body))
                    {
                        _logger.LogWarning("Skipped empty article: {Url}", item.Link);
                        continue;
                    }

                    var finalTitle = !string.IsNullOrWhiteSpace(title) ? title : item.Title;

                    var news = new NewsItem(
                        Title: finalTitle,
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
                    _logger.LogError(ex, "Failed to scrape article: {Url}", item.Link);
                }
            }
            UpdateScheduleAfterRun(DateTimeOffset.UtcNow);
            return list;
        }
    }

}