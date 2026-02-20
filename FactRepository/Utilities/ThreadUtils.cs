using System.Text.Json;
using System.Text.RegularExpressions;

namespace FactRepository.Utilities;

public static class ThreadUtils
{
    private static IEnumerable<string> NormalizeTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        foreach (var token in Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+"))
        {
            var t = token.Trim();
            if (t.Length > 1)
                yield return t;
        }
    }

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static string GetDateBucket(IEnumerable<string> dates)
    {
        var first = dates
            .Select(d => DateTime.TryParse(d, out var dt) ? dt : (DateTime?)null)
            .Where(dt => dt.HasValue)
            .Select(dt => dt!.Value)
            .OrderBy(dt => dt)
            .FirstOrDefault();

        return first == default ? "none" : first.ToString("yyyy-MM");
    }
    
    
    public static HashSet<string> ParseStringSetFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list is null
                ? []
                : list.Select(s => s?.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.ToLowerInvariant())
                    .ToHashSet();
        }
        catch { return []; }
    }

    public static HashSet<DateTime> ParseDateSetFromJson(string? json)
    {
        var set = new HashSet<DateTime>();
        if (string.IsNullOrWhiteSpace(json)) return set;

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            if (list is null) return set;

            foreach (var s in list)
            {
                if (DateTime.TryParse(s, out var dt))
                    set.Add(dt);
            }
        }
        catch { /* ignore */ }

        return set;
    }

    public static HashSet<string> ParseKeywordSetFromCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        // split on commas; trim; lower; drop empties
        return Regex.Split(csv, @"\s*,\s*")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.ToLowerInvariant())
            .ToHashSet();
    }
}