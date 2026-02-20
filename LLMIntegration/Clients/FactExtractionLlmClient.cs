using LLMIntegration.Configuration;
using LLMIntegration.Interfaces;
using LLMIntegration.Utilities;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace LLMIntegration.Clients;

public sealed class FactExtractionLlmClient : ILlmClient
{
    private readonly ChatClient _chat;
    private readonly FactExtractionOptions _opt;
    private string _systemInstruction;
    private readonly bool _testMode;

    public FactExtractionLlmClient(IOptions<FactExtractionOptions> options)
    {
        
        _opt = options.Value;
        
        var apiKey = _opt.ApiKey ?? throw new InvalidOperationException("Missing OpenAI API key.");
        
        _chat = new ChatClient(model: _opt.Model, apiKey: apiKey);

        InitializeSystemInstruction();

    }

    
    public async Task<string> GenerateTextAsync(string articleContent, SystemInstruction systemInstructions, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(_systemInstruction),
            new UserChatMessage(articleContent)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = _opt.Temperature,
        };
        ChatCompletion completion = await _chat.CompleteChatAsync(messages, options, ct);
        return completion.Content.Count > 0 ? completion.Content[0].Text ?? string.Empty : string.Empty;
    }

    private void InitializeSystemInstruction()
    {
        string? instructionsPath = _opt.SystemInstructionPath;
        
        if (!string.IsNullOrWhiteSpace(instructionsPath))
        {
            var path = Path.IsPathRooted(instructionsPath)
                ? instructionsPath
                : Path.Combine(AppContext.BaseDirectory, instructionsPath);

            if (File.Exists(path))
                _systemInstruction = File.ReadAllText(path, System.Text.Encoding.UTF8);
            else
                throw new FileNotFoundException($"SystemInstructions not found at '{path}'.");
        }
    }
    
    public bool ValidateLlmResponse(string response)
    {
        throw new NotImplementedException();
    }

    public bool ValidateOptions<T>(IOptions<T> options) where T : class //T är en 
    {
        throw new NotImplementedException();
    }

    public void ConfigureHttpClient()
    {
        throw new NotImplementedException();
    }
}