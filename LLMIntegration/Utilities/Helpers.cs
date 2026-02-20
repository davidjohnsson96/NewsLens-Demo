using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FactHarvester.Classes;
using System.Text.RegularExpressions;

namespace LLMIntegration.Utilities;

public class Helpers
{
    public static HarvestResult DeserializeLlmResponseToHarvestResult(string llmResponse)
    {

        llmResponse = CleanLlmResponse(llmResponse);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        try
        {
            return JsonSerializer.Deserialize<HarvestResult>(llmResponse, jsonOptions)
                   ?? throw new InvalidOperationException(
                       "Deserialization returned null. Sanitized response from LLM:\n" + llmResponse);
        }
        catch (JsonException e)
        {
            // Include sanitized JSON in the error so you can see what broke
            Console.WriteLine($"Deserialization failed:\n{e}\n\nSanitized JSON:\n{llmResponse}");
            throw;
        }
    }

    private static string CleanLlmResponse(string llmResponse)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
            return string.Empty;

        var cleaned = llmResponse.Trim();

        // 1) Remove ``` fences
        cleaned = StripMarkdownCodeFences(cleaned);

        // 2) Remove JS-style comments (// and /* */)
        cleaned = StripJsonComments(cleaned);

        // 3) Extract first JSON object (guards against extra leading/trailing text)
        cleaned = ExtractFirstJsonObject(cleaned);

        return cleaned.Trim();
    }

    private static string StripMarkdownCodeFences(string s)
    {
        var originalS = s; // ToDo: remove
        var t = s.Trim();

        if (!t.StartsWith("```", StringComparison.Ordinal))
            return s;

        // Remove first fence line (``` or ```json etc)
        var firstNewline = t.IndexOf('\n');
        if (firstNewline < 0) return s;

        t = t[(firstNewline + 1)..];

        // Remove trailing ```
        var lastFence = t.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence >= 0)
            t = t[..lastFence];

        return t.Trim();
    }

    private static string StripJsonComments(string s)
    {
        var originalS = s; // ToDo: remove
        // Remove /* ... */ comments
        s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Remove //... comments.
        // Best-effort: avoids removing http:// by not matching when preceded by ':'
        s = Regex.Replace(s, @"(?<!:)//.*?$", "", RegexOptions.Multiline);

        return s;
    }

    private static string ExtractFirstJsonObject(string s)
    {
        var originalS = s; // ToDo: remove
        int start = s.IndexOf('{');
        if (start < 0)
            throw new InvalidDataException("No '{' found in LLM response.");

        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = start; i < s.Length; i++)
        {
            char c = s[i];

            if (inString)
            {
                if (escape) { escape = false; continue; }
                if (c == '\\') { escape = true; continue; }
                if (c == '"') inString = false;
                continue;
            }

            if (c == '"') { inString = true; continue; }

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return s.Substring(start, i - start + 1);
            }
        }

        throw new InvalidDataException("JSON object was not balanced (missing closing '}').");
    }
    


    public static void MergeHarvestResults(HarvestResult mergeTo, HarvestResult mergeFrom)
    {
        if (mergeTo is null) throw new ArgumentNullException(nameof(mergeTo));
        if (mergeFrom is null) return;

        // ---- Article (overwrite non-default strings) ----
        if (mergeFrom.Article != null)
        {
            mergeTo.Article ??= new ArticleMeta();
            OverwriteArticleMeta(mergeTo.Article, mergeFrom.Article);
        }

        // ---- Categories (replace per-list; allow entities/keywords to be updated in different calls) ----
        if (mergeFrom.Categories != null)
        {
            mergeTo.Categories ??= new CategoryMeta();
            MergeCategoriesReplacePerList(mergeTo.Categories, mergeFrom.Categories);
        }

        // ---- Facts (REPLACE list if incoming has any non-default facts) ----
        if (mergeFrom.Facts != null && mergeFrom.Facts.Count > 0)
        {
            var incomingFacts = mergeFrom.Facts
                .Where(f => f != null && !IsDefaultFact(f))
                .Select(CloneFact)
                .ToList();

            if (incomingFacts.Count > 0)
                mergeTo.Facts = incomingFacts;
        }

        // ---- Unknowns (REPLACE list if incoming has any non-default strings) ----
        if (mergeFrom.Unknowns != null && mergeFrom.Unknowns.Count > 0)
        {
            var incomingUnknowns = mergeFrom.Unknowns
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            if (incomingUnknowns.Count > 0)
                mergeTo.Unknowns = incomingUnknowns;
        }
    }

    private static void OverwriteArticleMeta(ArticleMeta to, ArticleMeta from)
    {
        if (!string.IsNullOrWhiteSpace(from.Alias))     to.Alias = from.Alias.Trim();
        if (!string.IsNullOrWhiteSpace(from.Title))     to.Title = from.Title.Trim();
        if (!string.IsNullOrWhiteSpace(from.Url))       to.Url = from.Url.Trim();
        if (!string.IsNullOrWhiteSpace(from.Published)) to.Published = from.Published.Trim();
    }

    private static void MergeCategoriesReplacePerList(CategoryMeta to, CategoryMeta from)
    {
        // Replace Entities only if incoming has any non-default values
        var incomingEntities = CleanStringListOrNull(from.Entities);
        if (incomingEntities != null)
            to.Entities = incomingEntities;

        // Replace Keywords only if incoming has any non-default values
        var incomingKeywords = CleanStringListOrNull(from.Keywords);
        if (incomingKeywords != null)
            to.Keywords = incomingKeywords;

        // If a list is null/empty/whitespace-only in 'from', we do NOTHING for that list,
        // so another call can update the other list without wiping it.
    }

    private static List<string>? CleanStringListOrNull(List<string>? list)
    {
        if (list == null) return null;

        var cleaned = list
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.ToLower())
            .ToList();

        return cleaned.Count == 0 ? null : cleaned;
    }

    private static bool IsDefaultFact(FactItem f)
    {
        var hasId = !string.IsNullOrWhiteSpace(f.Id);
        var hasStatement = !string.IsNullOrWhiteSpace(f.Statement);
        var hasSources = f.Sources != null && f.Sources.Count > 0 &&
                         f.Sources.Any(s => s != null && !IsDefaultSource(s));
        return !(hasId || hasStatement || hasSources);
    }

    private static bool IsDefaultSource(SourceRef s)
    {
        var hasAlias = !string.IsNullOrWhiteSpace(s.Alias);
        var hasParagraphs = s.Paragraphs != null && s.Paragraphs.Count > 0 && s.Paragraphs.Any(p => p > 0);
        return !(hasAlias || hasParagraphs);
    }

    private static FactItem CloneFact(FactItem f) => new FactItem
    {
        Id = f.Id?.Trim() ?? "",
        Statement = f.Statement ?? "",
        Sources = (f.Sources ?? new List<SourceRef>())
            .Where(s => s != null && !IsDefaultSource(s))
            .Select(CloneSource)
            .ToList()
    };

    private static SourceRef CloneSource(SourceRef s) => new SourceRef
    {
        Alias = s.Alias?.Trim() ?? "",
        Paragraphs = (s.Paragraphs ?? new List<int>())
            .Where(p => p > 0)
            .Distinct()
            .ToList()
    };


    public static IReadOnlyList<SystemInstruction> LoadSystemInstructions(
        SystemInstructionsOptions options)
    {
        return options.Instructions
            .Select(i => new SystemInstruction
            {
                Path = i.Path,
                UseArticleContent = i.UseArticleContent,
                Instruction = File.ReadAllText(i.Path)
            })
            .ToList();
    }


// -----------------------------------------Nedan är nytt från chatGPT-----------------------------------------

    private static readonly JsonSerializerOptions _harvestResultPromptJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Creates a compact JSON snapshot of the current HarvestResult to be used as input for the next LLM call.
    /// Omits default/empty fields to keep prompts small and avoid accidental "clear" signals.
    /// </summary>
    public static string SerializeHarvestResultForNextCall(HarvestResult hr)
    {
        if (hr is null) return "{}";

        var root = new Dictionary<string, object?>();

        // article
        var articleObj = BuildArticleObjectOrNull(hr.Article);
        if (articleObj != null) root["article"] = articleObj;

        // categories
        var categoriesObj = BuildCategoriesObjectOrNull(hr.Categories);
        if (categoriesObj != null) root["categories"] = categoriesObj;

        // facts
        var factsArr = BuildFactsArrayOrNull(hr.Facts);
        if (factsArr != null) root["facts"] = factsArr;

        // unknowns
        var unknownsArr = CleanStringListOrNull(hr.Unknowns);
        if (unknownsArr != null) root["unknowns"] = unknownsArr;

        return root.Count == 0
            ? "{}"
            : JsonSerializer.Serialize(root, _harvestResultPromptJsonOptions);
    }

    private static Dictionary<string, object?>? BuildArticleObjectOrNull(ArticleMeta? a)
    {
        if (a == null) return null;

        var obj = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(a.Alias)) obj["alias"] = a.Alias.Trim();
        if (!string.IsNullOrWhiteSpace(a.Title)) obj["title"] = a.Title.Trim();
        if (!string.IsNullOrWhiteSpace(a.Url)) obj["url"] = a.Url.Trim();
        if (!string.IsNullOrWhiteSpace(a.Published)) obj["published"] = a.Published.Trim();

        return obj.Count == 0 ? null : obj;
    }

    private static Dictionary<string, object?>? BuildCategoriesObjectOrNull(CategoryMeta? c)
    {
        if (c == null) return null;

        var entities = CleanStringListOrNull(c.Entities);
        var keywords = CleanStringListOrNull(c.Keywords);

        // only include if at least one list has content
        if (entities == null && keywords == null) return null;

        var obj = new Dictionary<string, object?>();

        if (entities != null) obj["entities"] = entities;
        if (keywords != null) obj["keywords"] = keywords;

        return obj;
    }

    private static List<Dictionary<string, object?>>? BuildFactsArrayOrNull(List<FactItem>? facts)
    {
        if (facts == null || facts.Count == 0) return null;

        var arr = new List<Dictionary<string, object?>>();

        foreach (var f in facts)
        {
            if (f == null) continue;

            // skip fully-default facts
            if (string.IsNullOrWhiteSpace(f.Id) &&
                string.IsNullOrWhiteSpace(f.Statement) &&
                (f.Sources == null || f.Sources.Count == 0))
                continue;

            var factObj = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(f.Id)) factObj["id"] = f.Id.Trim();
            if (!string.IsNullOrWhiteSpace(f.Statement)) factObj["statement"] = f.Statement.Trim();

            var sourcesArr = BuildSourcesArrayOrNull(f.Sources);
            if (sourcesArr != null) factObj["sources"] = sourcesArr;

            if (factObj.Count > 0)
                arr.Add(factObj);
        }

        return arr.Count == 0 ? null : arr;
    }

    private static List<Dictionary<string, object?>>? BuildSourcesArrayOrNull(List<SourceRef>? sources)
    {
        if (sources == null || sources.Count == 0) return null;

        var arr = new List<Dictionary<string, object?>>();

        foreach (var s in sources)
        {
            if (s == null) continue;

            var srcObj = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(s.Alias))
                srcObj["alias"] = s.Alias.Trim();

            var paragraphs = (s.Paragraphs ?? new List<int>())
                .Where(p => p > 0)
                .Distinct()
                .ToList();

            if (paragraphs.Count > 0)
                srcObj["paragraphs"] = paragraphs;

            if (srcObj.Count > 0)
                arr.Add(srcObj);
        }

        return arr.Count == 0 ? null : arr;
    }
    public static ArticleMeta CreateArticleMeta(
        string? title,
        string? url,
        DateTimeOffset? publishedUtc = null,
        string? alias = null)
    {
        static string BuildAliasFromUrl(string url)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(url));
            return Convert.ToHexString(bytes)
                .ToLowerInvariant()
                .Substring(0, 16);
        }

        var safeUrl = url ?? "";
        var resolvedAlias =
            !string.IsNullOrWhiteSpace(alias)
                ? alias
                : (!string.IsNullOrWhiteSpace(safeUrl)
                    ? BuildAliasFromUrl(safeUrl)
                    : "");

        return new ArticleMeta
        {
            Alias = resolvedAlias,
            Title = title ?? "",
            Url = safeUrl,
            Published = publishedUtc?.UtcDateTime.ToString("O") ?? ""
        };
    }


}