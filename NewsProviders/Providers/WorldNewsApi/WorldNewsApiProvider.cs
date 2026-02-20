using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NewsProviders.Providers.Common;
using NewsProviders.Providers.WorldNewsApi.Configuration;

namespace NewsProviders.Providers.WorldNewsApi;
public class WorldNewsApiProvider : NewsProviderBase
{
    public WorldNewsApiProvider(IOptions<WorldNewsApiOptions> options, HttpClient client) : base("WorldNewsApiProvider")
    {
        _opt = options.Value;
        this.BaseUrl = _opt.BaseUrl ?? throw new InvalidOperationException("Missing BaseUrl.");;
        this.ApiKey = _opt.ApiKey ?? throw new InvalidOperationException("Missing WorldNews API key.");
        this.NumberOfNewsPerRequest = _opt.NumberOfNewsPerRequest;
        this.NumberOfNewsToGetPerCategory = _opt.NumberOfNewsToGetPerCategory;
        this.Categories = _opt.Categories;
        this.Client = client;
        this.InitializeProvider();

    }
    
    public HttpClient Client { get;}
    
    public string BaseUrl { get; }
    public string ApiKey { get; }
    
    private WorldNewsApiOptions _opt;
    private int NumberOfNewsPerRequest { get; }
    
    public List<String> Categories;
    private int NumberOfNewsToGetPerCategory { get; }
    
    public override async Task<IReadOnlyList<NewsItem>> GetNewsAsync(Func<string, string, CancellationToken, Task<bool>>? dedupCheck = null, CancellationToken ct = default)
    {
        //DOES NOT SUPPORT dedupCheck
        var newsBatches = new List<IReadOnlyList<NewsItem>>();
        
        foreach (var category in Categories)
        {
            int batchSize = NumberOfNewsPerRequest;
            for (int offset = 0; offset < NumberOfNewsToGetPerCategory; offset += batchSize)
            {
                int remaining = NumberOfNewsToGetPerCategory - offset;
                int numberOfNews = Math.Min(batchSize, remaining);
                try
                {
                    var newsBatch = await GetCategoryNewsAsync(category, numberOfNews, offset, ct);
                    newsBatches.Add(newsBatch);
                }
                catch (Exception e)
                {
                    UpdateScheduleAfterRun(DateTimeOffset.UtcNow);
                    Console.WriteLine($"Fetching of news failed with error: {e}");
                    Console.WriteLine($"Remaining for category {category}: {remaining}");
                }
            }
        }
        
        var allNews = newsBatches.SelectMany(x => x).ToList();
        UpdateScheduleAfterRun(DateTimeOffset.UtcNow);
        return allNews;
    }

    
    private void InitializeProvider()
    {
        this.Client.DefaultRequestHeaders.Add("x-api-key", this.ApiKey);
    }
    
    private string BuildUri(Dictionary<string, string> parameters, string path = "")
    {
        return QueryHelpers.AddQueryString(
            string.Join("/", new List<string?>{this.BaseUrl, path}),
            parameters
        );
    }
    
    
    public async Task<IReadOnlyList<NewsItem>> GetCategoryNewsAsync(
        string category,
        int numberOfNews,
        int offset,
        CancellationToken ct = default)
    {
        //The main method for fetching news for the apis by utilizing
        //other methods. 
        var envelope = await RequestNumberOfNewsByCategoryAndOffsetAsync(category, numberOfNews, offset, ct);
    
        var items = envelope.News ?? new();
    
        return items.Select(MapArticleToNewsItem).ToList();
    }

    private NewsItem MapArticleToNewsItem(WorldNewsApiArticle item){
        DateTimeOffset? published = null;
        if (!string.IsNullOrWhiteSpace(item.PublishDateRaw) &&
            DateTimeOffset.TryParseExact(
                item.PublishDateRaw,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dto))
        {
            published = dto;
        }
        return new NewsItem(
            Title: item.Title ?? string.Empty,
            Url: item.Url ?? string.Empty,
            Body: item.Text ?? item.Summary ?? string.Empty,
            PublishedAt: published,
            Source: "WorldNewsAPI"
        );
    }

    private async Task<WorldNewsApiSearchNewsResponse> RequestNumberOfNewsByCategoryAndOffsetAsync(string category, int numberOfNews, int offset,
        CancellationToken ct = default)
    {
        //Helper method to fetch news from api,
        //deserialize and convert response to WorldNewsApiSearchNewsResponse object
        //which contains a list of WorldNewsApiSearchItems (see ProviderDTOs file)
    
        var parameters = BuildSearchQueryParameters(category, numberOfNews, offset);
        var url = BuildUri(parameters, "search-news");
        using var resp = await Client.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; 

        var raw = await resp.Content.ReadAsStringAsync(ct);

        var envelope = JsonSerializer.Deserialize<WorldNewsApiSearchNewsResponse>(raw, jsonOptions); 
        
        var validator = new WorldNewsApiValidator();
        var result    = validator.Validate(envelope);

        if (!result.IsValid)
        {
            foreach (var err in result.Errors)
            {
                Console.WriteLine(err);
            }
        }
        else
        {
            Console.WriteLine("WorldNewsApiSearchNewsResponse is valid ✅");
        }
        
        if (envelope is null)
        {
            throw new InvalidOperationException("Json Deserialization in FetchWorldNewsApiEnvelope returned null. Raw: " + raw);
        }
        return envelope;
    }

    private Dictionary<string, string> BuildSearchQueryParameters(string category, int numberOfNews, int offset)
    {
        var now = DateTime.UtcNow;

        var categoryKey = (category ?? string.Empty).Trim();
        var categoryParam = categoryKey.ToLowerInvariant();

        var dict = new Dictionary<string, string>
        {
            ["language"] = _opt.Language,
            ["source-country"] = _opt.SourceCountry,
            ["earliest-publish-date"] = now.Subtract(_opt.LookbackWindow)
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ["latest-publish-date"] = now
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ["sort"] = _opt.Sort,
            ["sort-direction"] = NormalizeSortDirection(_opt.SortDirection),
            ["categories"] = categoryParam,
            ["number"] = numberOfNews.ToString(CultureInfo.InvariantCulture),
            ["offset"] = offset.ToString(CultureInfo.InvariantCulture),
        };
        
        if (_opt.NewsSources is { Count: > 0 })
        {
            dict["news-sources"] = string.Join(",",
                _opt.NewsSources.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)));
        }
        
        var text = BuildConfiguredTextQuery(categoryKey);
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (text.Length > 100)
            {
                throw new InvalidOperationException(
                    $"WorldNewsApi 'text' query exceeds 100 chars for category '{categoryKey}' (len={text.Length}).");
            }

            dict["text"] = text;
            dict["text-match-indexes"] = string.IsNullOrWhiteSpace(_opt.TextMatchIndexes) ? "title" : _opt.TextMatchIndexes;
        }

        return dict;
    }

    private string? BuildConfiguredTextQuery(string categoryKey)
    {
        string? cat = null;
        if (_opt.CategoryTextQueries.TryGetValue(categoryKey, out var cq) && !string.IsNullOrWhiteSpace(cq))
            cat = cq.Trim();
        if (cat is null)
            return null;
        return cat;
        
    }

    private static string NormalizeSortDirection(string? sortDir)
    {
        if (sortDir is null) return "DESC";
        return sortDir.Equals("ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
    }




}