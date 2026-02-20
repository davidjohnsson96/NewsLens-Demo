namespace FactHarvester.Classes;


public static class HarvestResultValidator
{
    public static void ValidateLlmResponseSerialization(HarvestResult result)
    {
        if (result is null)
            throw new InvalidDataException("HarvestResult was null.");

        // ---- ARTICLE ----
        if (result.Article is null)
            throw new InvalidDataException("Article was null.");

        if (string.IsNullOrWhiteSpace(result.Article.Title))
            throw new InvalidDataException("Article.Title was null or empty.");

        if (string.IsNullOrWhiteSpace(result.Article.Url))
            throw new InvalidDataException("Article.Url was null or empty.");

        // Validate URL format
        if (!Uri.TryCreate(result.Article.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidDataException($"Article.Url is not a valid http/https URL: '{result.Article.Url}'");

        // Validate published date (expects yyyy-MM-dd)
        if (string.IsNullOrWhiteSpace(result.Article.Published))
            throw new InvalidDataException("Article.Published was null or empty.");
        
        // ---- CATEGORIES ----
        if (result.Categories is null)
            throw new InvalidDataException("Categories was null.");

        if (result.Categories.Entities is null)
            throw new InvalidDataException("Categories.Entities was null.");

        if (result.Categories.Keywords is null)
            throw new InvalidDataException("Categories.Keywords was null.");

        // Optional: require at least one entity/keyword
        if (result.Categories.Entities.Count == 0 && result.Categories.Keywords.Count == 0)
            throw new InvalidDataException("Categories.Entities and Categories.Keywords were both empty.");

        // Basic normalization checks (avoid tokens like " of " etc)
        EnsureNoNullOrWhitespace(result.Categories.Entities, "Categories.Entities");
        EnsureNoNullOrWhitespace(result.Categories.Keywords, "Categories.Keywords");

        // Dedup checks (case-insensitive)
        EnsureNoDuplicates(result.Categories.Entities, "Categories.Entities");
        EnsureNoDuplicates(result.Categories.Keywords, "Categories.Keywords");

        // ---- FACTS ----
        if (result.Facts is null || result.Facts.Count == 0)
            throw new InvalidDataException("Facts were null or empty.");

        // Validate each fact
        EnsureNoNullObjects(result.Facts, "Facts");

        foreach (var f in result.Facts)
        {
            if (string.IsNullOrWhiteSpace(f.Id))
                throw new InvalidDataException("FactItem.Id was null or empty.");

            if (string.IsNullOrWhiteSpace(f.Statement))
                throw new InvalidDataException($"FactItem.Statement was null or empty (Fact Id: '{f.Id}').");

            // Avoid ridiculously short “facts”
            if (f.Statement.Trim().Length < 10)
                throw new InvalidDataException($"FactItem.Statement too short (Fact Id: '{f.Id}'): '{f.Statement}'");

        }
        
        if (result.Unknowns is not null)
        {
            EnsureNoNullOrWhitespace(result.Unknowns, "Unknowns");
            EnsureNoDuplicates(result.Unknowns, "Unknowns");
        }
    }

    private static void EnsureNoNullOrWhitespace(List<string> items, string path)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is null)
                throw new InvalidDataException($"{path}[{i}] was null.");

            items[i] = items[i].Trim(); // safe normalization

            if (items[i].Length == 0)
                throw new InvalidDataException($"{path}[{i}] was empty/whitespace.");
        }
    }

    private static void EnsureNoNullObjects<T>(List<T> items, string path) where T : class
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i] is null)
                throw new InvalidDataException($"{path}[{i}] was null.");
    }

    private static void EnsureNoDuplicates(List<string> items, string path)
    {
        var dup = items
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (dup != null)
            throw new InvalidDataException($"{path} contained duplicates (example: '{dup.Key}').");
    }
}