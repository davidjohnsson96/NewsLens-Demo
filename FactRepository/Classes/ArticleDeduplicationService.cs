using FactRepository.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FactRepository.Classes;

public interface IArticleDeduplicationService
{
    Task<bool> TryMarkProcessedAsync(string providerName, string url, CancellationToken ct = default);
}

public sealed class ArticleDeduplicationService : IArticleDeduplicationService
{
    private readonly string _connectionString;

    public ArticleDeduplicationService(IOptions<FactHandlerOptions> opt)
    {
        _connectionString = opt.Value.FactDbConnectionString;
    }

    public async Task<bool> TryMarkProcessedAsync(string providerName, string url, CancellationToken ct = default)
    {
        var urlHash = ComputeSha256(url);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        try
        {
            await DBHelpers.InsertProcessedArticleAsync(conn, url, urlHash, ct);
            return true;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return false; // already exists
        }
    }
    
    private static byte[] ComputeSha256(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
    }
}