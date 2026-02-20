using System.Text;
using FactRepository.Utilities;


public sealed class NewsLensArticle
{
   
    public string SourceUrl { get; init; } = default!;
    public string? ThreadId { get; init; }
    
    public List<string> ArticleMaterial { get; } = new();
    
    public string? ArticleBody { get; set; } = null;

    /// <summary>
    /// Builds a NewsLensArticle from a list of NewsFactRow.
    /// </summary>
    public NewsLensArticle(List<NewsFactRow> rows)
    {
        if (rows == null || rows.Count == 0)
            throw new ArgumentException("rows cannot be null or empty", nameof(rows));
        
        var first = rows[0];

        SourceUrl = first.SourceUrl;
        ThreadId = first.ThreadId;

        foreach (var r in rows)
        {
            if (!string.IsNullOrWhiteSpace(r.Statement))
                ArticleMaterial.Add(r.Statement.Trim());
        }
    }

    /// <summary>
    /// Converts ArticleMaterial into a single string suitable for the LLM.
    /// </summary>
    public string BuildArticleMaterialString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fact Statements:");

        foreach (var statement in ArticleMaterial)
        {
            sb.Append("- ").AppendLine(statement);
        }

        return sb.ToString();
    }
}