using System.Text.Json;
using System.Text.RegularExpressions;

namespace FactRepository.Utilities;

public static class FactParsingUtils
{
    // Split a keyword string like "ceasefire,hostage,talks,aid" → ["ceasefire","hostage","talks","aid"]
    public static HashSet<string> SplitTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();
        var tokens = Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(t => t.Length > 1)
            .ToHashSet();
        return tokens;
    }
    
    public static HashSet<string> JsonToSet(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list is null ? new() : list.Select(s => s.ToLowerInvariant().Trim()).ToHashSet();
        }
        catch
        {
            return new(); // fail safe
        }
    }

    // Parse date list JSON like ["2025-10-02","2025-10-03"]
    public static List<DateTime> JsonToDates(string? json)
    {
        var result = new List<DateTime>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            if (list is null) return result;

            foreach (var s in list)
            {
                if (DateTime.TryParse(s, out var dt))
                    result.Add(dt);
            }
        }
        catch
        {
            // ignore
        }

        return result;
    }
}