using System.Text.Json.Serialization;

namespace LLMIntegration.Utilities;

public sealed record SystemInstructionsOptions
{
    public IReadOnlyList<SystemInstruction> Instructions { get; init; }
        = Array.Empty<SystemInstruction>();
}

public sealed record SystemInstruction
{
    public string Path { get; init; } = string.Empty;
    public bool UseArticleContent { get; init; }
    [JsonIgnore]
    public string Instruction { get; init; } = string.Empty;
}