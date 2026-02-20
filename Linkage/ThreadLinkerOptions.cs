namespace Linkage;

public class ThreadLinkerOptions
{
    public int FactBatchSize { get; init; }
    public double ThreadMatchingScoreThreshold { get; init; } = 0.09;
    public double EntitiesScoreWeight { get; init; } = 0.7;
    public double KeywordScoreWeight { get; init; } = 0.3;
}