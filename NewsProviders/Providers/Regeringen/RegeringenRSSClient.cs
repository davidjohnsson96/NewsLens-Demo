using System.ServiceModel.Syndication;
using System.Xml;

namespace NewsProviders.Providers.Regeringen;

public sealed record GovernmentRssItem(
    string Title,
    string Link,
    DateTimeOffset PublishedAt,
    string? Summary);

public sealed class RegeringenRssClient
{
    private readonly HttpClient _http;

    public RegeringenRssClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<GovernmentRssItem>> FetchAsync(
        Uri feedUri,
        int maxItems,
        CancellationToken ct)
    {
        await using var stream = await _http.GetStreamAsync(feedUri, ct);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
        var feed = SyndicationFeed.Load(reader);

        var items = feed.Items
            .OrderByDescending(i => i.PublishDate)
            .Take(maxItems)
            .Select(i =>
            {
                var link = i.Links.FirstOrDefault()?.Uri?.ToString() ?? i.Id;
                var published = i.PublishDate != default
                    ? i.PublishDate
                    : (i.LastUpdatedTime != default ? i.LastUpdatedTime : DateTimeOffset.UtcNow);

                return new GovernmentRssItem(
                    Title: i.Title?.Text ?? "(no title)",
                    Link: link ?? "",
                    PublishedAt: published,
                    Summary: i.Summary?.Text
                );
            })
            .ToList();

        return items;
    }
}