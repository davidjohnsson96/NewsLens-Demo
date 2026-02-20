using System.ServiceModel.Syndication;
using System.Xml;

namespace NewsProviders.Providers.EuropeanUnion.EuParliament;

using System.ServiceModel.Syndication;
using System.Xml;

public sealed record EuParliamentRssItem(
    string Title,
    string Link,
    DateTimeOffset PublishedAt,
    string? Summary
);

public sealed class EuParliamentRSSClient
{
    private readonly HttpClient _http;

    public EuParliamentRSSClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<EuParliamentRssItem>> FetchAsync(
        Uri feedUri,
        int maxItems,
        CancellationToken ct = default)
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

                return new EuParliamentRssItem(
                    Title: i.Title?.Text ?? "(no title)",
                    Link: link ?? string.Empty,
                    PublishedAt: published,
                    Summary: i.Summary?.Text
                );
            })
            .ToList();

        return items;
    }
}