namespace NewsProviders.Providers.UnitedNations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsProviders.Providers.Common;


public sealed class UnitedNationsNewsProvider : NewsProviderBase
{
    private readonly UnitedNationsOptions _opt;
    private readonly UnitedNationsRssClient _rss;
    private readonly UnitedNationsNewsScraper _scraper;
    private readonly ILogger<UnitedNationsNewsProvider> _logger;

    public UnitedNationsNewsProvider(
        IOptions<UnitedNationsOptions> options,
        UnitedNationsRssClient rssClient,
        UnitedNationsNewsScraper scraper,
        ILogger<UnitedNationsNewsProvider> logger)
        : base("UnitedNationsProvider")
    {
        _opt    = options.Value;
        _rss    = rssClient;
        _scraper = scraper;
        _logger = logger;

        if (_opt.RssUrls is null || _opt.RssUrls.Count == 0)
            throw new InvalidOperationException("UnitedNationsOptions.RssUrls must contain at least one feed URL.");
    }

    public string ProviderName { get; } = "UnitedNations";

    public string BaseUrl =>
        new Uri(_opt.RssUrls[0]).GetLeftPart(UriPartial.Authority);

    public string ApiKey => string.Empty;

    public override async Task<IReadOnlyList<NewsItem>> GetNewsAsync(
        Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null,
        CancellationToken ct = default)
    {
        dedupCheck ??= static (_, _, _) => Task.FromResult(true);

        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["serviceName"] = ProviderName
               }))
        {
            var feedUris = _opt.RssUrls.Select(u => new Uri(u)).ToList();

            _logger.LogInformation("Fetching UN RSS items from {Count} feeds", feedUris.Count);

            var rssItems = await _rss.FetchAsync(feedUris, _opt.MaxItemsPerFeed, ct);

            _logger.LogInformation("Fetched {Count} unique items from UN feeds", rssItems.Count);

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
                        Title:       finalTitle,
                        Url:         item.Link,
                        Body:        body,
                        PublishedAt: item.PublishedAt,
                        Source:      ProviderName
                    );
                    NewsItemValidator.ValidateNewsItem(news);
                    list.Add(news);

                    _logger.LogInformation("Finished article: {Url}", item.Link);
                    
                    await Task.Delay(100, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to scrape article: {Url}", item.Link);
                }
            }

            UpdateScheduleAfterRun(DateTimeOffset.UtcNow);

            return list;
        }
    }
}