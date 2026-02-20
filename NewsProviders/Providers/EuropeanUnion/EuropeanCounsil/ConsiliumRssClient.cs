namespace NewsProviders.Providers.EuropeanUnion.EuropeanCounsil;

using System.ServiceModel.Syndication;
using System.Xml;

public sealed record ConsiliumRssItem(
    string Title,
    string Link,
    DateTimeOffset PublishedAt,
    string? Summary
);

public sealed class ConsiliumRssClient
{
    private readonly HttpClient _http;

    public ConsiliumRssClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<ConsiliumRssItem>> FetchAsync(
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

                return new ConsiliumRssItem(
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