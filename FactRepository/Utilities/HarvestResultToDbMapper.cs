using System.Security.Cryptography;
using System.Text;
using FactHarvester.Classes;

namespace FactRepository.Utilities;

public class HarvestResultToDbMapper
{
    public static NewsFactsDatabaseBatch HarvestResultToDbFactsBatch(HarvestResult harvestResult)
    {
        string? entities = harvestResult.Categories?.Entities is not null
            ? string.Join(',', harvestResult.Categories.Entities)
            : null;

        string? keywords = harvestResult.Categories?.Keywords is not null
            ? string.Join(',', harvestResult.Categories.Keywords)
            : null;

        return new NewsFactsDatabaseBatch(
            harvestResult.Facts.Select(fact =>
                {
                    return new FactStatement(
                        FactStatementId: createStableIdForFact(fact),
                        Statement: fact.Statement,
                        HasBeenUsed: false
                    );
                }
            ).ToList(),
            new NewsFactsCentroid(
                harvestResult.Article.Url,
                entities,
                keywords
            )
        );
    }

    
    private static string createStableIdForFact(FactItem factItem)
    {
        string idInput = $"fact|{factItem.Id}|" +
                         $"{new Random().Next(0,999999)}|" +
                         $"{factItem.Statement.Trim()}";
        
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(idInput));
        return Convert.ToHexString(bytes.AsSpan(0, 16)); // short stable id
    }
}