using FactRepository.Classes;
using LLMIntegration.Clients;
using LLMIntegration.Utilities;
using NewsLensAutomationService.Interfaces;

namespace NewsLensAutomationService.Workflows;

public sealed class ArticleCreationWorkflow : IWorkflow
{
    public string Id { get; } = "ArticleCreation";

    private readonly FactService _factService;
    private readonly ArticleFactoryLlmClient _llmClient;
    private readonly ILogger<ArticleCreationWorkflow> _logger;

    public ArticleCreationWorkflow(FactService factService, ArticleFactoryLlmClient llmClient, ILogger<ArticleCreationWorkflow> logger)
    {
        _factService = factService ?? throw new ArgumentNullException(nameof(factService));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WorkflowRunResult> RunOnceAsync(CancellationToken ct)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["serviceName"] = "ArticleCreationWorkflow"
               }))
        {
            var threadIds = await _factService.GetTriggeredThreadIdsAsync(7, 3, 4, ct).ConfigureAwait(false);

            if (threadIds == null || threadIds.Count() == 0)
            {
                _logger.LogInformation("No triggered threads recieved from database");
                return new WorkflowRunResult(
                    0, false, "No articles created (no facts or empty LLM responses)."
                );
            }
            int created = 0;
            _logger.LogInformation($"Recieved {threadIds.Count()} threads from database");
            foreach (var threadId in threadIds)
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(threadId))
                    continue;

                var facts = await _factService.GetFactsInThreadAsync(threadId, ct).ConfigureAwait(false);
                _logger.LogInformation($"Recieved {facts.Count()} facts from thread with ID: {threadId}");
                if (facts == null || facts.Count == 0)
                    continue;
                var article = new NewsLensArticle(facts);
                _logger.LogInformation($"Article object created");

                var llmInput = article.BuildArticleMaterialString();
                _logger.LogInformation($"LLM input build with BuildArticleMaterialString");
                _logger.LogInformation($"Sending material to LLM client");
                var llmResponse = await _llmClient.GenerateTextAsync(llmInput, new SystemInstruction(), ct)
                    .ConfigureAwait(false); //TODO, fix here

                if (!string.IsNullOrWhiteSpace(llmResponse))
                {
                    _logger.LogInformation($"LLM response NOT null or empty.");
                    article.ArticleBody = llmResponse.Trim();
                    await _factService.InsertWrittenArticleAsync(article, ct).ConfigureAwait(false);
                    _logger.LogInformation($"Inserted new written article for ThreadId={threadId}");

                    created++;
                }
                else { _logger.LogInformation($"LLM response WAS null or empty.");
                }
            }

            return new WorkflowRunResult(
                created, false, $"{created} articles created."
            );
        }
    }
}