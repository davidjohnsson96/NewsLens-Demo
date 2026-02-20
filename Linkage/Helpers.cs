using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace Linkage;

public static class Helpers
{
    public static string CreateNewThreadId(HashSet<string> entities, HashSet<string> keywords)
    {
        var seed = $"{string.Join(",", keywords)}|{string.Join(",", entities)}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var id = Convert.ToHexString(bytes).ToLowerInvariant()[..16]; // 16 hex = 64-bit
        return id;
    }

    public static (double w_ent, double w_kw) NormalizeScoringWeights(double wEnt, double wKw)
    {
        double[] values = [wEnt, wKw];

        double min = values.Min(), max = values.Max();
        var normalized = Math.Abs(min - max) == 0
            ? values.Select(_ => 0.0).ToArray()
            : values.Select(v => (v - min) / (max - min)).ToArray();

        return (normalized[0], normalized[1]);
    }

    public static string? ListToCsvString(HashSet<string> set)
    {
        return !set.IsNullOrEmpty() ? string.Join(",", set) : null;
    }
}