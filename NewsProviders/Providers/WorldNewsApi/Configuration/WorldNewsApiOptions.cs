namespace NewsProviders.Providers.WorldNewsApi.Configuration;

public sealed class WorldNewsApiOptions
{
    public string? ApiKey { get; init; } 
    public string? BaseUrl { get; init; } = "https://api.worldnewsapi.com/";
    public string? ProviderName { get; init; } = "WorldNewsApiProvider";
    public int NumberOfNewsPerRequest { get; set; } = 2;
    public int NumberOfNewsToGetPerCategory { get; set; } = 2;
    public List<string> Categories { get; init; } = new();

    public string Language { get; init; } = "en";

    public string SourceCountry { get; init; } = "us";

    public string Sort { get; init; } = "publish-time";

    public string SortDirection { get; init; } = "desc";

    public TimeSpan LookbackWindow { get; init; } = TimeSpan.FromHours(8);
    
    public List<string> NewsSources { get; init; } = new();
    
    public Dictionary<string, string> CategoryTextQueries { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string TextMatchIndexes { get; init; } = "title";
    
    public string? GlobalTextQuery { get; init; }
    
    public int? MinTitleLength { get; init; } = 30;

}