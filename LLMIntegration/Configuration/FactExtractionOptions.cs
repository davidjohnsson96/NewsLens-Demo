namespace LLMIntegration.Configuration;

public sealed class FactExtractionOptions
{
    public string Model { get; init; } = "gpt-4.1";
    public float Temperature { get; init; } = 0.2f;
    public int MaxOutputTokens { get; init; } = 1500;
    public string? ApiKey { get; init; }
    public string? SystemInstructionPath { get; init; }
    
    
}