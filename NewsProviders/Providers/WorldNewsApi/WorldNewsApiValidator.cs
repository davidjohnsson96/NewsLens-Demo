namespace NewsProviders.Providers.WorldNewsApi;

public class WorldNewsApiValidator
{
    
    public sealed class ValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    }
    
    public ValidationResult Validate(WorldNewsApiSearchNewsResponse? response)
    {
        var errors = new List<string>();

        if (response is null)
        {
            errors.Add("Response object is null (deserialization failed?).");
            return Finish(errors);
        }
        
        if (response.Offset < 0)
            errors.Add($"offset must be >= 0, got {response.Offset}.");

        if (response.Number < 0)
            errors.Add($"number must be >= 0, got {response.Number}.");

        if (response.Available < 0)
            errors.Add($"available must be >= 0, got {response.Available}.");

        // --- Top-level News list ---
        if (response.News is null)
        {
            errors.Add("news list is null.");
        }
        else
        {
            for (int i = 0; i < response.News.Count; i++)
            {
                var article = response.News[i];
                ValidateArticle(article, i, errors);
            }
        }

        return Finish(errors);
    }

    private static void ValidateArticle(WorldNewsApiArticle? article, int index, List<string> errors)
    {
        if (article is null)
        {
            errors.Add($"news[{index}] is null.");
            return;
        }
        
        if (article.WorldNewsApiArticleId <= 0)
        {
            errors.Add($"news[{index}].id must be > 0, got {article.WorldNewsApiArticleId}.");
        }
    } 
    private static bool LooksLikeDateTime(string raw)
    {

        return DateTime.TryParse(raw, out _);
    }

    private static ValidationResult Finish(List<string> errors)
        => new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };


}