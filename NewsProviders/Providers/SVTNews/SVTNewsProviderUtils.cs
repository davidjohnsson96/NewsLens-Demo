using System.Net;
using NewsProviders.Providers.Common;

namespace NewsProviders.Providers.SVTNews
{
    public static class SvtNewsProviderUtils
    {
        public static NewsItem ToNewsItem(
            string articleUrl,
            string articleBody,
            string? title = null,
            DateTimeOffset? publishedAt = null,
            string? imageUrl = null)
        {
            if (string.IsNullOrWhiteSpace(articleUrl))
                throw new ArgumentException("Article URL must be provided.", nameof(articleUrl));

            if (string.IsNullOrWhiteSpace(articleBody))
                throw new ArgumentException("Article body text must be provided.", nameof(articleBody));

            var uri = new Uri(articleUrl);

            return new NewsItem(
                Title: title ?? string.Empty,
                Url: uri.ToString(),
                Body: articleBody.Trim(),
                PublishedAt: publishedAt,
                Source: "SVT"
            );
        }
    
        public static string GetSlug(string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return string.Empty;

            var last = segments[^1];
            if (last.Equals("amp", StringComparison.OrdinalIgnoreCase) && segments.Length > 1)
                last = segments[^2];

            return WebUtility.UrlDecode(last);
        }

        public static string GetHumanTitle(string url)
        {
            var slug = GetSlug(url);
            return slug.Replace('-', ' ');
        }
    }
}