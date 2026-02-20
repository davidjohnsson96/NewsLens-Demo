using Microsoft.Extensions.Logging;
using NewsProviders.Providers.Common;
using NewsProviders.Providers.SVTNews.SVTNewsInterfaces;

namespace NewsProviders.Providers.SVTNews;

public class SvtNewsProvider : NewsProviderBase
{
    
    private readonly ISvtrssFeed _svtrssFeed;
    private readonly ISvtNewsScraper _svtNewsScraper;
    private readonly ILogger<SvtNewsProvider> _logger;

    public SvtNewsProvider(ISvtrssFeed rssFeed, ISvtNewsScraper newsScraper, ILogger<SvtNewsProvider> logger ) : base("SVTNewsProvider")
    {
        this._svtrssFeed = rssFeed;
        this._svtNewsScraper = newsScraper;
        this._logger = logger;
    }
    
    public override async Task<IReadOnlyList<NewsItem>> GetNewsAsync(Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null, CancellationToken ct = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["ServiceName"] = ProviderName
               }))
        {
            dedupCheck ??= DefaultDedup;
            
            var newsItems = new List<NewsItem>();

            _logger.LogInformation("Fetching links for category {Category}", "utrikes");

            IReadOnlyList<Uri> newsLinks = await _svtrssFeed.FetchLinksAsync("utrikes");

            _logger.LogInformation("Fetched {Count} RSS links", newsLinks.Count);

            foreach (var link in newsLinks.Take(9))
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                       {
                           ["ArticleUrl"] = link.ToString()
                       }))
                {
                    try
                    {
                        if (!await dedupCheck(ProviderName, link.ToString(), ct))
                        {
                            _logger.LogInformation("Skipping already processed article: {Url}", link);
                            continue;
                        }
                        _logger.LogDebug("Scraping article");

                        var title = SvtNewsProviderUtils.GetHumanTitle(link.ToString());
                        var articleBody = await _svtNewsScraper.ScrapeNewsAsync(link.ToString(), ct);

                        var newsItem = SvtNewsProviderUtils.ToNewsItem(link.ToString(), articleBody, title);
                        NewsItemValidator.ValidateNewsItem(newsItem);
                        newsItems.Add(newsItem);

                        _logger.LogDebug("Article scraped successfully");
                    }
                    catch (Exception ex)
                    {
                        UpdateScheduleAfterRun(DateTimeOffset.UtcNow);
                        _logger.LogError(ex, "Failed to process article");
                    }
                }
            }

            return newsItems;
        }
    }

}