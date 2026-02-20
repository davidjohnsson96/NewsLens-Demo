using NewsProviders.Providers.EuropeanUnion.EuParliament;

namespace NewsProviders.Providers.EuropeanUnion.EUParliament;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsProviders.Providers.Common;

public sealed class EuParliamentNewsProvider : NewsProviderBase
{
    private readonly EuParliamentOptions _opt;
    private readonly EuParliamentRSSClient _rss;
    private readonly EuParliamentNewsScraper _scraper;
    private readonly ILogger<EuParliamentNewsProvider> _logger;

    public EuParliamentNewsProvider(
        IOptions<EuParliamentOptions> options,
        EuParliamentRSSClient rssClient,
        EuParliamentNewsScraper scraper,
        ILogger<EuParliamentNewsProvider> logger)
        : base("EuParliamentProvider")
    {
        _opt     = options.Value;
        _rss     = rssClient;
        _scraper = scraper;
        _logger  = logger;

        if (string.IsNullOrWhiteSpace(_opt.RssUrl))
            throw new InvalidOperationException("EuParliamentOptions.RssUrl is not configured.");
    }

    public string ProviderName { get; } = "EuParliament";

    public override async Task<IReadOnlyList<NewsItem>> GetNewsAsync(Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null, CancellationToken ct = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["serviceName"] = ProviderName
               }))
        {
            dedupCheck ??= DefaultDedup;
            var feedUri = new Uri(_opt.RssUrl);

            _logger.LogInformation("Fetching RSS items from {FeedUri}", feedUri);

            var rssItems = await _rss.FetchAsync(feedUri, _opt.MaxItems, ct);

            _logger.LogInformation("{Count} items fetched from Europarl feed", rssItems.Count);

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

                    var finalTitle = !string.IsNullOrWhiteSpace(title)
                        ? title
                        : item.Title;

                    var news = new NewsItem(
                        Title:       finalTitle,
                        Url:         item.Link,
                        Body:        body,
                        PublishedAt: item.PublishedAt,
                        Source:      ProviderName
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