namespace LLMIntegration.Configuration
{
    public class ArticleFactoryOptions
    {
        public float Temperature { get; init; } = 0.2f;
        public string Model { get; init; } = "gpt-4.1";
        public string? ApiKey { get; init; }
        public string? FactoryInstructionPath { get; init; }
        
    }
}