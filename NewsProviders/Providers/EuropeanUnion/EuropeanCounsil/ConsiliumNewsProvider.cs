namespace NewsProviders.Providers.EuropeanUnion.EuropeanCounsil;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsProviders.Providers.Common;


public sealed class ConsiliumNewsProvider : NewsProviderBase
{
    private readonly ConsiliumOptions _opt;
    private readonly ConsiliumRssClient _rss;
    private readonly ConsiliumNewsScraper _scraper;
    private readonly ILogger<ConsiliumNewsProvider> _logger;

    public ConsiliumNewsProvider(
        IOptions<ConsiliumOptions> options,
        ConsiliumRssClient rssClient,
        ConsiliumNewsScraper scraper,
        ILogger<ConsiliumNewsProvider> logger)
        : base("ConsiliumProvider")
    {
        _opt     = options.Value;
        _rss     = rssClient;
        _scraper = scraper;
        _logger  = logger;

        if (string.IsNullOrWhiteSpace(_opt.RssUrl))
            throw new InvalidOperationException("ConsiliumOptions.RssUrl is not configured.");
    }

    public string ProviderName { get; } = "Consilium";

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

            _logger.LogInformation("Fetching RSS items from {FeedUri}", feedUri);

            var rssItems = await _rss.FetchAsync(feedUri, _opt.MaxItems, ct);
    
            _logger.LogInformation("{Count} items fetched from Consilium feed", rssItems.Count);

            var list = new List<NewsItem>();
            await Task.Delay(400, ct);
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
                    await Task.Delay(2000, ct);
                    var (title, body) = await _scraper.ScrapeArticleAsync(item.Link, ct);
                    Console.WriteLine($"{title} - {body}");
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
                    await Task.Delay(250, ct);
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