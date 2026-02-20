using FactRepository.Classes;
using FactRepository.Utilities;
using FactHarvester.Classes;
using LLMIntegration.Clients;
using LLMIntegration.Utilities;
using NewsLensAutomationService.Interfaces;
using NewsProviders.Providers.Common;

namespace NewsLensAutomationService.Workflows;

public class FactHarvestWorkflow : IWorkflow
{
    public string Id { get; } =  "FactHarvest";
    private FactService FactService;
    private readonly ILogger<FactHarvestWorkflow> _logger;
    private List<INewsProvider>  _providers; 
    private LocalFactExtractionLlmClient _factExtractionClient;
    private ArticleDeduplicationService _articleDeduplicationService;

    public FactHarvestWorkflow(FactService factService, ArticleDeduplicationService deduplicationService,IEnumerable<INewsProvider> newsProviders, LocalFactExtractionLlmClient factExtractionLlmClient, ILogger<FactHarvestWorkflow> logger)
    {
        FactService = factService;
        _logger = logger;
        _providers = newsProviders.ToList();
        _factExtractionClient = factExtractionLlmClient;
        _articleDeduplicationService = deduplicationService;
        
    }

    public async Task<WorkflowRunResult> RunOnceAsync(CancellationToken ct)
    {

        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["serviceName"] = "FactHarvestWorkflow"
               }))
        {
            
            int itemsProcessed = 0;
            try
            {
                _logger.LogInformation("FetchNewsFromAllProviders started");
                var news = await FetchNewsFromAllProvidersAsync(ct);
                _logger.LogInformation($"FetchNewsFromAllProviders finished with {news.Count} items");
                _logger.LogInformation($"Fact harvesting from news starting");
                foreach (var item in news)
                {
                    _logger.LogInformation($"Harvesting facts from item: {item.Title}");
                    try
                    {
                        var harvestResult = await ExtractFacts(item, ct);
                        _logger.LogInformation($"Fact harvesting from item: {item.Title} finished");
                        _logger.LogInformation($"Database insertion STARTED for {item.Title}");
                        var factBatch = HarvestResultToDbMapper.HarvestResultToDbFactsBatch(harvestResult);
                        await FactService.InsertNewsFactsAsync(factBatch, ct);
                        itemsProcessed++;
                        _logger.LogInformation($"Database insertion FINISHED for {harvestResult.Article.Url}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"ERROR for item {item.Title}: {ex}");
                        continue;
                    }
                }
                return new WorkflowRunResult(
                    itemsProcessed
                );
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"ERROR: {ex}");
                return new WorkflowRunResult(
                    itemsProcessed,
                    true,
                    ex.Message
                );
            }
        }
    }

    private async Task<List<NewsItem>> FetchNewsFromAllProvidersAsync(CancellationToken ct)
    {
        var providerTasks = _providers.Select(async provider =>
        {
            try
            {
                if (!provider.IsDueToRun(DateTimeOffset.UtcNow))
                {
                    _logger.LogInformation(
                        "{Provider} not due to run. Next scheduled run at: {NextRun}",
                        provider.ProviderName,
                        provider.NextRunUtc);

                    return new List<NewsItem>();
                }
                return await provider.GetNewsAsync(
                    dedupCheck:  _articleDeduplicationService.TryMarkProcessedAsync,
                    ct : ct);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"GetNewsAsync failed for  {provider.ProviderName} with error: \n {ex}");
                return new List<NewsItem>();
            }
        });

        return (await Task.WhenAll(providerTasks))
            .SelectMany(x => x)
            .ToList();
    }
    
    private async Task<HarvestResult> ExtractFacts(NewsItem newsItem, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // One-by-one LLM call
        try
        {
            ArticleMeta articleMeta = Helpers.CreateArticleMeta(
                title: newsItem.Title,
                url: newsItem.Url,
                publishedUtc: newsItem.PublishedAt
            );
            
            var harvestResult = await _factExtractionClient.HarvestFacts(newsItem.ToString(), articleMeta, ct);
            
            return harvestResult;
        }
        
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while extracting facts for newsItem {newsItem.Title} with url {newsItem.Url}");
            throw;
        }
    }
    
    
}