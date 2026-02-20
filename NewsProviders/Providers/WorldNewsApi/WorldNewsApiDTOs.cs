using System.Text.Json.Serialization;

namespace NewsProviders.Providers.WorldNewsApi;

public sealed class WorldNewsApiSearchNewsResponse
{
    [JsonPropertyName("offset")]    public int Offset { get; set; }
    [JsonPropertyName("number")]    public int Number { get; set; }
    [JsonPropertyName("available")] public int Available { get; set; } 
    [JsonPropertyName("news")]      public List<WorldNewsApiArticle> News { get; set; } = new();
}

public sealed class WorldNewsApiArticle
{
    [JsonPropertyName("id")]             public long WorldNewsApiArticleId { get; set; }
    [JsonPropertyName("title")]          public string? Title { get; set; }
    [JsonPropertyName("text")]           public string? Text { get; set; }
    [JsonPropertyName("summary")]        public string? Summary { get; set; }
    [JsonPropertyName("url")]            public string? Url { get; set; }
    [JsonPropertyName("publish_date")]   public string? PublishDateRaw { get; set; }
    [JsonPropertyName("author")]         public string? Author { get; set; }
    [JsonPropertyName("authors")]        public List<string>? Authors { get; set; }
    [JsonPropertyName("language")]       public string? Language { get; set; }
    [JsonPropertyName("category")]       public string? Category { get; set; }
    [JsonPropertyName("source_country")] public string? SourceCountry { get; set; }
}