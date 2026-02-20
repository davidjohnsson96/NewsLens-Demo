namespace NewsProviders.Providers.Common;

public sealed record NewsItem(
    string Title,
    string Url,
    string Body,
    DateTimeOffset? PublishedAt,
    string Source
);

public static class NewsItemValidator
{
    public static void ValidateNewsItem(NewsItem item)
    {
        if (item == null)
        {
            throw new InvalidDataException("NewsItem is null.");
        }

        if (string.IsNullOrEmpty(item.Title))
        {
            throw new InvalidDataException("NewsItem.Title is null or empty.");
        }
        if (string.IsNullOrEmpty(item.Url))
        {
            throw new InvalidDataException("NewsItem.Url is null or empty.");
        }
        if (string.IsNullOrEmpty(item.Body))
        {
            throw new InvalidDataException("NewsItem.Body is null or empty.");
        }
        if (string.IsNullOrEmpty(item.Source))
        {
            throw new InvalidDataException("NewsItem.Source is null or empty.");
        }


    }
    
}