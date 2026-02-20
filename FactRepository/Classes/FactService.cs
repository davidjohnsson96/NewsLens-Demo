using FactRepository.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FactRepository.Classes;

public class FactService(IOptions<FactHandlerOptions> options)
{
    private FactHandlerOptions Opt { get; } = options.Value;
    
    public async Task InsertNewsFactsAsync(NewsFactsDatabaseBatch recordBatch, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);

        using var check = new SqlCommand("SELECT DB_NAME()", conn);
        var db = (string?)await check.ExecuteScalarAsync(ct);
        Console.WriteLine($"Connected DB: {db}");

        var batchId = Guid.NewGuid();

        foreach (var factRecord in recordBatch.FactStatements)
        {
            await using var cmd = DBHelpers.BuildInsertUnassignedFactCommand(conn, recordBatch, factRecord, batchId);

            var insertedParam = cmd.Parameters["@Inserted"];
            await cmd.ExecuteNonQueryAsync(ct);

            var inserted = insertedParam.Value is bool b && b;
            Console.WriteLine(inserted
                ? $"✅ Inserted {factRecord.FactStatementId}"
                : $"↩️ Duplicate {factRecord.FactStatementId}");
        }
    }
    public async Task InsertWrittenArticleAsync(NewsLensArticle article, CancellationToken ct = default)
    {
        if (article == null) throw new ArgumentNullException(nameof(article));
        if (string.IsNullOrWhiteSpace(article.SourceUrl))
            throw new ArgumentException("SourceUrl is null or empty", nameof(article));
        if (string.IsNullOrWhiteSpace(article.ThreadId))
            throw new ArgumentException("ThreadId is null or empty (required for FK to Threads)", nameof(article));

        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = DBHelpers.BuildInsertWrittenArticleCommand(conn, article);

        var insertedParam = cmd.Parameters["@Inserted"];
        await cmd.ExecuteNonQueryAsync(ct);

        var inserted = insertedParam.Value is bool b && b;
        Console.WriteLine(inserted
            ? $"✅ Inserted processed article for ThreadId={article.ThreadId}"
            : $"↩️ Duplicate processed article for ThreadId={article.ThreadId}");
    }
    
    public async Task<IEnumerable<EventThread>> GetCandidateThreadsAsync(
        int sinceDays = 14,
        bool includeClosed = false,
        int maxResults = 100,
        CancellationToken ct = default)
    {
        var results = new List<EventThread>();

        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = DBHelpers.BuildGetCandidateThreadsCommand(
            conn, sinceDays, includeClosed, maxResults);

        using var reader = await cmd.ExecuteReaderAsync(ct);

        results = await DBHelpers.MapRowsAsync(reader, r => new EventThread
        {
            ThreadId = DBHelpers.GetString(r, "ThreadId"),
            Entities = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Entities")),
            Keywords = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Keywords")),
            FactCount = DBHelpers.GetInt(r, "FactCount"),
            HasBeenUsedInArticle = DBHelpers.GetBool(r, "HasBeenUsedInArticle"),
            LastFactAddedAt = DBHelpers.GetDateTime(r, "LastFactAddedAt")
        }, ct);

        return results;
    }
    
    public async Task<(bool created, bool assigned)> AssignFactBatchAndUpsertThreadAsync(       
        Guid factBatchId,
        string threadId,
        string? entities,
        string? keywords,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(threadId))
            throw new InvalidDataException("ThreadId is null or empty");

        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = DBHelpers.BuildAssignFactBatchAndUpsertThreadCommand(conn, 
            factBatchId, 
            threadId, 
            entities, 
            keywords);

        await cmd.ExecuteNonQueryAsync(ct);

        var createdParam = cmd.Parameters["@Created"];
        var assignedParam = cmd.Parameters["@Assigned"];
        var created = createdParam.Value is bool b && b;
        var assigned = assignedParam.Value is bool a && a;
        return (created, assigned);
        // If you care about created vs updated, you can log or return this.
    }
    
    public async Task<List<NewsFactRow>> GetFactsInThreadAsync(
        string ThreadId,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = DBHelpers.BuildGetFactsInThreadCommand(conn, ThreadId);

        var rows = new List<NewsFactRow>();

        using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            rows.Add(new NewsFactRow
            {
                FactId = r.GetString(r.GetOrdinal("FactId")),
                Statement = r.GetString(r.GetOrdinal("Statement")),
                SourceUrl = r.GetString(r.GetOrdinal("SourceUrl")),
                Entities = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Entities")),
                Keywords = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Keywords")),
                ThreadId = r["ThreadId"] as string,
            });
        }

        return rows;
    }
    
    public async Task<IEnumerable<string>> GetTriggeredThreadIdsAsync(
        int minFacts,
        int sinceDays,
        int maxRows,
        CancellationToken ct = default)
    {
        var results = new List<string>();

        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = DBHelpers.BuildGetTriggeredThreadsCommand(conn, minFacts, sinceDays, maxRows);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(reader.GetString(reader.GetOrdinal("ThreadId")));
        }

        return results;
    }

    public async Task<IEnumerable<Guid>> GetGetUnassignedFactBatchIdsAsync(int batchSize, CancellationToken ct = default)
    {
        var results = new List<Guid>();
        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);
     
        await using var cmd = DBHelpers.BuildGetUnassignedFactBatchIdsCommand(conn, batchSize);
     
        using var reader = await cmd.ExecuteReaderAsync(ct);
     
        while (await reader.ReadAsync(ct))
        {
            results.Add(reader.GetGuid(reader.GetOrdinal("FactBatchId")));
        }
        return results;
     
    }
    
    public async Task<List<NewsFactRow>> GetUnassignedFactsByFactBatchId(
        string batchId,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = DBHelpers.BuildGetUnassignedFactsByFactBatchIdCommand(conn, batchId);

        var rows = new List<NewsFactRow>();

        using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            rows.Add(new NewsFactRow
            {
                FactId = r.GetString(r.GetOrdinal("FactId")),
                Statement = r.GetString(r.GetOrdinal("Statement")),
                SourceUrl = r.GetString(r.GetOrdinal("SourceUrl")),
                Entities = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Entities")),
                Keywords = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Keywords")),
            });
        }
        return rows;
    }
    
    public async Task<(HashSet<string> entities, HashSet<string> keywords)> GetFactBatchKeywordsAndEntities(
        Guid batchId,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(Opt.FactDbConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = DBHelpers.BuildGetFactBatchKeywordsAndEntitiesCommand(conn, batchId);
        var entities = new HashSet<string>();
        var keywords = new HashSet<string>();
        using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            entities = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Entities"));
            keywords = ThreadUtils.ParseKeywordSetFromCsv(DBHelpers.GetNullableString(r, "Keywords"));
        }
        return (entities, keywords);
        
    }
}