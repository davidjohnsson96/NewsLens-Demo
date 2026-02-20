namespace FactRepository.Utilities;

public sealed class NewsFactsDatabaseBatch
{
    public List<FactStatement>? FactStatements { get; init; }
    
    public NewsFactsCentroid? FactsCentroid { get; set; }
    
    public NewsFactsDatabaseBatch(
        List<FactStatement>? factStatements = null,
        NewsFactsCentroid? factsCentroid = null)
    {
        FactStatements = factStatements;
        FactsCentroid = factsCentroid;
    }
}


public sealed class NewsFactsCentroid
{
    public string SourceUrl { get; init; }
    public string? Entities { get; init; }     // comma-separated array tokens "Gaza","Israel"
    public string? Keywords { get; init; }     // comma-separated tokens, e.g. "ceasefire,hostage,talks"

    public NewsFactsCentroid(
        string sourceUrl,
        string? entities,
        string? keywords)
    {
        SourceUrl = sourceUrl;
        Entities = entities;
        Keywords = keywords;
    }

    // Optional: add this for deserialization or object-initializer syntax
    public NewsFactsCentroid() {}
}

public sealed record FactStatement(
    string FactStatementId,
    string Statement,
    bool HasBeenUsed
);


public sealed class EventThread
{
    public string? ThreadId { get; init; }
    public HashSet<string>? Entities { get; init; }
    public HashSet<string>? Keywords { get; init; }
    public int FactCount { get; init; }
    public bool HasBeenUsedInArticle { get; init; }
    public DateTime LastFactAddedAt { get; init; }

    public EventThread(){}
}


public sealed class NewsFactRow
{
    // Fact row in database
    public string FactId { get; init; } = default!;
    public string Statement { get; init; } = default!;
    
    public bool HasBeenUsed { get; init; }
    public string SourceUrl { get; init; } = default!;
    public HashSet<string> Entities { get; init; }   // CSV
    public HashSet<string> Keywords { get; init; }       // CSV
    public string? ThreadId { get; init; }
    
    public string? ThreadArticleStatus { get; init; }
    
    public DateTime? ThreadLastFactAddedAt { get; init; }
    
    public int? ThreadFactCount { get; init; }
}