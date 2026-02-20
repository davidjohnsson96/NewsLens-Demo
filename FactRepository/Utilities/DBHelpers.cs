using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactHarvester.Classes;

namespace FactRepository.Utilities;

public class DBHelpers
{
    public static SqlParameter AddParam(
        SqlCommand cmd,
        string name,
        SqlDbType type,
        object? value,
        int size = 0)
    {
        var p = size > 0
            ? cmd.Parameters.Add(name, type, size)
            : cmd.Parameters.Add(name, type);

        p.Value = value ?? DBNull.Value;
        return p;
    }

    public static SqlParameter AddOutputParam(
        SqlCommand cmd,
        string name,
        SqlDbType type)
    {
        var p = cmd.Parameters.Add(name, type);
        p.Direction = ParameterDirection.Output;
        return p;
    }

    public static string GetString(SqlDataReader r, string col) =>
        r[col] as string ?? string.Empty;

    public static string? GetNullableString(SqlDataReader r, string col) =>
        r[col] as string;

    public static int GetInt(SqlDataReader r, string col) =>
        r[col] is int v ? v : default;
    
    public static bool GetBool(SqlDataReader r, string col) =>
        !r.IsDBNull(col) && r.GetBoolean(r.GetOrdinal(col));

    public static DateTime GetDateTime(SqlDataReader r, string col) =>
        r[col] is DateTime dt ? dt : default;

    public static DateTime? GetNullableDateTime(SqlDataReader r, string col) =>
        r[col] as DateTime?;

    private static SqlCommand CreateStoredProc(
        SqlConnection conn,
        string procName)
    {
        return new SqlCommand(procName, conn)
        {
            CommandType = CommandType.StoredProcedure
        };
    }

    public static SqlCommand BuildGetCandidateThreadsCommand(
        SqlConnection conn,
        int MaxUpdatedAgeDays,
        bool includeClosed,
        int maxResults)
    {
        var cmd = CreateStoredProc(conn, "dbo.GetCandidateThreads");
        
        AddParam(cmd, "@MaxUpdatedAgeDays", SqlDbType.Int, MaxUpdatedAgeDays);
        AddParam(cmd, "@IncludeClosed", SqlDbType.Bit, includeClosed);
        AddParam(cmd, "@MaxResults", SqlDbType.Int, maxResults);

        return cmd;
    }

    public static SqlCommand BuildGetFactsInThreadCommand(SqlConnection conn, string ThreadId)
    {
        var cmd = CreateStoredProc(conn, "dbo.GetFactsInThread");
        cmd.Parameters.AddWithValue("@ThreadId", ThreadId);
        return cmd;
    }
    
    public static SqlCommand BuildGetUnassignedFactBatchIdsCommand(SqlConnection conn, int batchSize)
    {
        var cmd = CreateStoredProc(conn, "dbo.GetUnassignedFactBatchIds");
        cmd.Parameters.AddWithValue("@Take", batchSize);
        return cmd;
    }
    
    public static SqlCommand BuildGetFactBatchKeywordsAndEntitiesCommand(SqlConnection conn, Guid batchId)
    {
        var cmd = CreateStoredProc(conn, "dbo.GetFactBatchKeywordsAndEntities");
        cmd.Parameters.AddWithValue("@FactBatchId", batchId);
        return cmd;
    }
    
    public static SqlCommand BuildGetUnassignedFactsByFactBatchIdCommand(SqlConnection conn, string batchId)
    {
        var cmd = CreateStoredProc(conn, "dbo.GetUnassignedFactsByFactBatchId");
        cmd.Parameters.AddWithValue("@FactBatchId", batchId);
        return cmd;
    }
    
    public static SqlCommand BuildGetTriggeredThreadsCommand(
        SqlConnection conn,
        int minFacts,
        int sinceDays,
        int maxRows,
        int minArticleCount = 4)
    {
        var cmd = CreateStoredProc(conn, "dbo.GetTriggeredThreads");

        AddParam(cmd, "@MinFacts", SqlDbType.Int, minFacts);
        AddParam(cmd, "@SinceDays", SqlDbType.Int, sinceDays);
        AddParam(cmd, "@MaxRows", SqlDbType.Int, maxRows);
        AddParam(cmd, "@MinArticleCount", SqlDbType.Int, minArticleCount);

        return cmd;
    }
    
    public static SqlCommand BuildAssignFactBatchAndUpsertThreadCommand(
        SqlConnection conn,
        Guid factBatchId,
        string threadId,
        string? entities,
        string? keywords)
    {
        var cmd = CreateStoredProc(conn, "dbo.AssignFactBatchAndUpsertThread");
        AddParam(cmd, "@FactBatchId", SqlDbType.UniqueIdentifier, factBatchId, 64);
        AddParam(cmd, "@ThreadId", SqlDbType.NVarChar, threadId, 64);
        //AddParam(cmd, "@Title", SqlDbType.NVarChar, title, 256);

        AddParam(cmd, "@Entities", SqlDbType.NVarChar, entities, 400);
        AddParam(cmd, "@Keywords", SqlDbType.NVarChar, keywords, 400);

        AddOutputParam(cmd, "@Created", SqlDbType.Bit);
        AddOutputParam(cmd, "@Assigned", SqlDbType.Bit);

        return cmd;
    }

    public static SqlCommand BuildInsertUnassignedFactCommand(
        SqlConnection conn,
        NewsFactsDatabaseBatch recordBatch,
        /* replace this with your actual fact record type */ dynamic factRecord,
        Guid batchId)
    {
        var cmd = CreateStoredProc(conn, "dbo.InsertUnassignedFact");

        cmd.Parameters.AddWithValue("@FactId", factRecord.FactStatementId);
        cmd.Parameters.AddWithValue("@SourceUrl", recordBatch.FactsCentroid?.SourceUrl);
        cmd.Parameters.AddWithValue("@Statement", factRecord.Statement);
        cmd.Parameters.AddWithValue("@Entities", recordBatch.FactsCentroid?.Entities);
        cmd.Parameters.AddWithValue("@Keywords", recordBatch.FactsCentroid?.Keywords);
        cmd.Parameters.AddWithValue("@FactBatchId", batchId);

        AddOutputParam(cmd, "@Inserted", SqlDbType.Bit);

        return cmd;
    }
    
    public static async Task InsertProcessedArticleAsync( //vad gör denna metod?
        SqlConnection conn,
        string url,
        byte[] urlHash,
        CancellationToken ct)
    {
        // language=SQL
        const string sql = @"
        INSERT INTO ProcessedArticle (Url, UrlHash)
        VALUES (@Url, @UrlHash);";

        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add("@Url", SqlDbType.NVarChar, 2048).Value      = url;
        cmd.Parameters.Add("@UrlHash", SqlDbType.Binary, 32).Value      = urlHash;

        await cmd.ExecuteNonQueryAsync(ct);
    }
    public static SqlCommand BuildInsertWrittenArticleCommand(
        SqlConnection conn,
        NewsLensArticle article)
    {
        if (article == null) throw new ArgumentNullException(nameof(article));

        var cmd = CreateStoredProc(conn, "dbo.InsertWrittenArticle");

        var articleMaterialJson = JsonSerializer.Serialize(article.ArticleMaterial ?? new List<string>());
        
        var articleId = BuildStableArticleId(article);

        AddParam(cmd, "@ArticleId", SqlDbType.NVarChar, articleId, 64);
        AddParam(cmd, "@SourceUrl", SqlDbType.NVarChar, article.SourceUrl, 2048);
        AddParam(cmd, "@ThreadId", SqlDbType.NVarChar, article.ThreadId, 64);

        AddParam(cmd, "@ArticleMaterialJson", SqlDbType.NVarChar, articleMaterialJson);
        AddParam(cmd, "@ArticleBody", SqlDbType.NVarChar, article.ArticleBody);

        AddOutputParam(cmd, "@Inserted", SqlDbType.Bit);

        return cmd;
    }
    
    private static string BuildStableArticleId(NewsLensArticle article)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var seed = $"thread:{article.ThreadId}|url:{article.SourceUrl}";
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
    public static async Task<List<T>> MapRowsAsync<T>(
        SqlDataReader reader,
        Func<SqlDataReader, T> map,
        CancellationToken ct)
    {
        var list = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(map(reader));
        }

        return list;
    }
    
    public static string ToCsvString<T>(HashSet<T> set)
    {
        var csvString = string.Join(',', set) ?? "";
        return csvString;
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