using LLMIntegration.Utilities;
using Microsoft.Extensions.Options;

namespace LLMIntegration.Interfaces;

public interface ILlmClient
{
    
    Task<string> GenerateTextAsync(string articleContent, SystemInstruction systemInstructions, CancellationToken ct = default);

    bool ValidateLlmResponse(string response);

    bool ValidateOptions<T>(IOptions<T> options) where T : class;
    
    void ConfigureHttpClient();

}