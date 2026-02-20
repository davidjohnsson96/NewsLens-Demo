using System.Text.Json.Serialization;

namespace FactHarvester.Classes;

public sealed class HarvestResult
{
    [JsonPropertyName("article")]
    public ArticleMeta Article { get; set; } = null!;
    
    [JsonPropertyName("categories")]
    public CategoryMeta Categories { get; set; } = null!;

    [JsonPropertyName("facts")]
    public List<FactItem> Facts { get; set; } = new();

    [JsonPropertyName("unknowns")]
    public List<string> Unknowns { get; set; } = new();
}
public sealed class ArticleMeta
{
    [JsonPropertyName("alias")]     public string Alias { get; set; } = "";
    [JsonPropertyName("title")]     public string Title { get; set; } = "";
    [JsonPropertyName("url")]       public string Url { get; set; } = "";
    [JsonPropertyName("published")] public string Published { get; set; } = "";
}

public sealed class CategoryMeta
{
    [JsonPropertyName("entities")]             public List<string> Entities { get; set; } = new List<string>();
    [JsonPropertyName("keywords")]          public List<string> Keywords { get; set; } = new List<string>();
    
}

public sealed class FactItem
{
    [JsonPropertyName("id")]        public string Id { get; set; } = "";
    [JsonPropertyName("statement")] public string Statement { get; set; } = "";
    [JsonPropertyName("sources")]   public List<SourceRef> Sources { get; set; } = new();
}

public sealed class SourceRef
{
    [JsonPropertyName("alias")]      public string Alias { get; set; } = "";
    [JsonPropertyName("paragraphs")] public List<int> Paragraphs { get; set; } = new();
}