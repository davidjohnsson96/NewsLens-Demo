using LLMIntegration.Configuration;
using LLMIntegration.Interfaces;
using LLMIntegration.Utilities;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace LLMIntegration.Clients;

public sealed class ArticleFactoryLlmClient : ILlmClient
{
    private readonly ChatClient _chat;
    
    private string _FactoryInstruction;
    private ArticleFactoryOptions _options;
    public ArticleFactoryLlmClient(IOptions<ArticleFactoryOptions> options){
        
        _options = options.Value;
        
        InitializeFactoryInstruction();
        
        _chat = new ChatClient(model: _options.Model, apiKey: _options.ApiKey);
    }
    
    private void InitializeFactoryInstruction()
    {
        
        string? instructionsPath = _options.FactoryInstructionPath;
        
        if (!string.IsNullOrWhiteSpace(instructionsPath))
        {
            var path = Path.IsPathRooted(instructionsPath)
                ? instructionsPath
                : Path.Combine(AppContext.BaseDirectory, instructionsPath);

            if (File.Exists(path))
                _FactoryInstruction = File.ReadAllText(path, System.Text.Encoding.UTF8);
            else
                throw new FileNotFoundException($"SystemInstructions not found at '{path}'.");
        }
    }

    public async Task<string> GenerateTextAsync(string articleContent, SystemInstruction systemInstructions, CancellationToken ct = default)

    {
        
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(_FactoryInstruction),
            new UserChatMessage(articleContent) // input till ChatGPT
        };

        var options = new ChatCompletionOptions()
        {
            Temperature = _options.Temperature, // Temperature avgör vilket ord som ska komma härnäst baserat på statistisk sannolikhet
            //MaxOutputTokenCount = _opt.MaxOutputTokens
        };
        
        ChatCompletion completion = await _chat.CompleteChatAsync(messages, options, ct);
        return completion.Content.Count > 0 ? completion.Content[0].Text ?? string.Empty : string.Empty;
        
    }

    public bool ValidateLlmResponse(string response)
    {
        throw new NotImplementedException();
    }

    public bool ValidateOptions<T>(IOptions<T> options) where T : class 
    {
        throw new NotImplementedException();
    }

    public void ConfigureHttpClient()
    {
        throw new NotImplementedException();
    }
}