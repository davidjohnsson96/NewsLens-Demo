namespace LLMIntegration.Configuration;

public class LocalFactExtractionOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "";
    public double Temperature { get; set; } = 0.0;
    public int? MaxOutputTokens { get; set; }
    public List<string> SystemInstructionPaths { get; set; } = new List<string>();
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public bool OpenAICompatible { get; set; } = false;
    
    public int Timeout { get; set; } = 300;
}