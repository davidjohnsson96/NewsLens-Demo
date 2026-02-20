using System.IO.Compression;
using System.Text;

namespace NewsProviders.Providers.UnitedNations;


using System.ServiceModel.Syndication;
using System.Xml;

public sealed record UnitedNationsRssItem(
    string Title,
    string Link,
    DateTimeOffset PublishedAt,
    string? Summary
);

public sealed class UnitedNationsRssClient
{
    private readonly HttpClient _http;

    public UnitedNationsRssClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<UnitedNationsRssItem>> FetchAsync(
        IEnumerable<Uri> feedUris,
        int maxItemsPerFeed,
        CancellationToken ct = default)
    {
        var allItems = new List<UnitedNationsRssItem>();

        foreach (var feedUri in feedUris)
        {
            try
            {
                var xml = await DownloadAndSanitizeFeedAsync(feedUri, ct);
                if (string.IsNullOrWhiteSpace(xml))
                {
                    continue;
                }

                using var stringReader = new StringReader(xml);
                using var xmlReader = XmlReader.Create(
                    stringReader,
                    new XmlReaderSettings { Async = true });

                var feed = SyndicationFeed.Load(xmlReader);
                if (feed is null)
                    continue;

                var items = feed.Items
                    .OrderByDescending(i => i.PublishDate)
                    .Take(maxItemsPerFeed)
                    .Select(i =>
                    {
                        var link = i.Links.FirstOrDefault()?.Uri?.ToString() ?? i.Id;
                        var published = i.PublishDate != default
                            ? i.PublishDate
                            : (i.LastUpdatedTime != default
                                ? i.LastUpdatedTime
                                : DateTimeOffset.UtcNow);

                        return new UnitedNationsRssItem(
                            Title: i.Title?.Text ?? "(no title)",
                            Link: link ?? string.Empty,
                            PublishedAt: published,
                            Summary: i.Summary?.Text
                        );
                    })
                    .ToList();

                allItems.AddRange(items);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is XmlException)
            {
                //TODO: Implement logging here
                continue;
            }
        }
        var distinct = allItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Link))
            .GroupBy(i => i.Link, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.PublishedAt).First())
            .OrderByDescending(i => i.PublishedAt)
            .ToList();

        return distinct;
    }
    
    private async Task<string?> DownloadAndSanitizeFeedAsync(Uri feedUri, CancellationToken ct)
    {
        var bytes = await _http.GetByteArrayAsync(feedUri, ct);

        if (bytes is null || bytes.Length == 0)
            return null;

        string text;
        
        if (bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
        {
            await using var ms = new MemoryStream(bytes);
            await using var gzip = new GZipStream(ms, CompressionMode.Decompress);
            using var sr = new StreamReader(gzip, Encoding.UTF8);
            text = await sr.ReadToEndAsync();
        }
        else
        {
            text = Encoding.UTF8.GetString(bytes);
        }

        if (string.IsNullOrWhiteSpace(text))
            return null;

        var firstLt = text.IndexOf('<');
        if (firstLt > 0)
        {
            text = text.Substring(firstLt);
        }

        return text.Trim();
    }
}