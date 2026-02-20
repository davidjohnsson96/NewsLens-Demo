using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FactHarvester.Classes;
using LLMIntegration.Configuration;
using LLMIntegration.Interfaces;
using LLMIntegration.Utilities;
using Microsoft.Extensions.Options;

namespace LLMIntegration.Clients;

public sealed class LocalFactExtractionLlmClient : ILlmClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private readonly LocalFactExtractionOptions _opt;
    private readonly SystemInstructionsOptions _systemInstructionsOptions;
    private IReadOnlyList<SystemInstruction> _systemInstructionsList = new List<SystemInstruction>();
    public const string ServiceName = "LocalFactExtractionLlmClient";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LocalFactExtractionLlmClient(IOptions<LocalFactExtractionOptions> options, IOptions<SystemInstructionsOptions> systemInstructionsOptions, HttpClient? httpClient = null)
    {
        _systemInstructionsOptions = systemInstructionsOptions.Value ?? throw new ArgumentNullException(nameof(systemInstructionsOptions));
        _opt = options.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_opt.BaseUrl))
            throw new InvalidOperationException("BaseUrl must be set for local LLM (e.g., http://localhost:11434).");
        if (string.IsNullOrWhiteSpace(_opt.Model))
            throw new InvalidOperationException("Model must be set for local LLM.");

        _ownsClient = httpClient is null;
        _http = httpClient ?? new HttpClient();

        _http.BaseAddress = new Uri(_opt.BaseUrl, UriKind.Absolute);

        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("NewsLens/1.0 (+https://example.com)");

        _systemInstructionsList = Helpers.LoadSystemInstructions(_systemInstructionsOptions);
        ConfigureHttpClient();
    }



    public async Task<HarvestResult> HarvestFacts(string articleContent, ArticleMeta articleMeta, CancellationToken ct = default)
    {
        var harvestResult = new HarvestResult();
        harvestResult.Article = articleMeta;
        string currentInput = articleContent;

        for (int i = 0; i < _systemInstructionsList.Count; i++)
        {
            if (_systemInstructionsList[i].UseArticleContent) 
            {
                currentInput = articleContent;
            }
            else
            {
                currentInput = Helpers.SerializeHarvestResultForNextCall(harvestResult);
                
            }
            
            var llmResponse = await GenerateTextAsync(currentInput, _systemInstructionsList[i], ct);

            if (string.IsNullOrWhiteSpace(llmResponse))
            {
                throw new InvalidDataException($"LLM returned empty response at step {i} (UseArticleContent={_systemInstructionsList[i].UseArticleContent})");
            }

            Console.WriteLine($"\n--- LLM RESPONSE STEP {i} ---\n{llmResponse}\n--- END ---\n");
            var tempHarvestResult = Helpers.DeserializeLlmResponseToHarvestResult(llmResponse);
            Helpers.MergeHarvestResults(harvestResult, tempHarvestResult);

        }
        HarvestResultValidator.ValidateLlmResponseSerialization(harvestResult);

        return harvestResult;
    }
    
   
    public async Task<string> GenerateTextAsync(string articleContent, SystemInstruction systemInstructions, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(articleContent))
            return string.Empty;

        if (_opt.OpenAICompatible)
            return await CallOpenAICompatAsync(articleContent, systemInstructions.Instruction, ct).ConfigureAwait(false);

        return await CallOllamaAsync(articleContent, systemInstructions.Instruction, ct).ConfigureAwait(false);
    }

    private async Task<string> CallOpenAICompatAsync(string articleContent, string systemInstructions, CancellationToken ct)
    {
        
        var payload = new
        {
            model = _opt.Model,
            temperature = _opt.Temperature,
            top_k = 50, // move to appsettings
            top_p = 1.0, // move to appsettings
            messages = new[]
            {
                new { role = "system", content = systemInstructions },
                new { role = "user",   content = articleContent }
            }
        };

        using var res = await _http.PostAsJsonAsync("/v1/chat/completions", payload, JsonOpts, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        
        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
                return content.GetString() ?? string.Empty;
            
            if (choice.TryGetProperty("text", out var text))
                return text.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private async Task<string> CallOllamaAsync(string articleContent, string systemInstructions, CancellationToken ct)
    {
        var payload = new
        {
            model = _opt.Model,
            stream = false,
            options = new
            {
                temperature = _opt.Temperature
            },
            messages = new[]
            {
                new { role = "system", content = systemInstructions },
                new { role = "user",   content = articleContent }
            }
        };

        using var res = await _http.PostAsJsonAsync("/api/chat", payload, JsonOpts, ct).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        if (doc.RootElement.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content))
            return content.GetString() ?? string.Empty;
        
        if (doc.RootElement.TryGetProperty("response", out var response))
            return response.GetString() ?? string.Empty;

        return string.Empty;
    }
    

    public bool ValidateLlmResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return false;
        return true;
    }

    public bool ValidateOptions<T>(IOptions<T> options) where T : class
    {
        if (options.Value is LocalFactExtractionOptions o)
        {
            if (string.IsNullOrWhiteSpace(o.BaseUrl)) return false;
            if (string.IsNullOrWhiteSpace(o.Model)) return false;
            if (!o.BaseUrl.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) &&
                !o.BaseUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase) &&
                !o.BaseUrl.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                // return false; // uncomment to strictly require localhost
            }
            return true;
        }
        return false;
    }

    public void ConfigureHttpClient()
    {
        _http.Timeout = TimeSpan.FromSeconds(_opt.Timeout);
    }

    public void Dispose()
    {
        if (_ownsClient) _http.Dispose();
    }
}