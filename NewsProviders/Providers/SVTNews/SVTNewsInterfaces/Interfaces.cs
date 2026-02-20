namespace NewsProviders.Providers.SVTNews.SVTNewsInterfaces;

public interface ISvtrssFeed
{
    Task<IReadOnlyList<Uri>> FetchLinksAsync(string category, CancellationToken ct = default);
}

public interface ISvtNewsScraper
{
    Task<string> ScrapeNewsAsync(string url, CancellationToken ct = default);
}