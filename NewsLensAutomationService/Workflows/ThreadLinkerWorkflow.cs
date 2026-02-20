using FactRepository.Classes;
using FactRepository.Utilities;
using Linkage;
using LLMIntegration.Clients;
using NewsLensAutomationService.Interfaces;
using NewsProviders.Providers.Common;

namespace NewsLensAutomationService.Workflows;

public class ThreadLinkerWorkflow : IWorkflow
{
    public string Id { get; } = "ThreadLinker";
    private FactService FactService;
    private readonly ILogger<FactHarvestWorkflow> _logger;
    private IThreadLinker ThreadLinker { get; set; }

    public ThreadLinkerWorkflow(FactService factService, IThreadLinker threadLinker,
        ILogger<FactHarvestWorkflow> logger)
    {
        FactService = factService;
        _logger = logger;
        ThreadLinker = threadLinker;

    }

    public async Task<WorkflowRunResult> RunOnceAsync(CancellationToken ct)
    {
        int itemsProcessed = 0;
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["serviceName"] = "ThreadLinker",
               }))
        {
            while (true)
            {
                _logger.LogInformation("Threadlinker started");
                var unassignedBatchIds = await FactService.GetGetUnassignedFactBatchIdsAsync(100, ct);
                if (unassignedBatchIds.Count() == 0)
                {
                    _logger.LogInformation("No unassigned batch ids was found");
                    break;
                }
                _logger.LogInformation($"{unassignedBatchIds.Count()} unassigned batch ids were found");
                foreach (var batchId in unassignedBatchIds)
                {
                    IEnumerable<EventThread> candidateThreads = await FactService.GetCandidateThreadsAsync(ct: ct);
                    var (entities, keywords) = await FactService.GetFactBatchKeywordsAndEntities(batchId, ct);
                    string threadId = ThreadLinker.LinkToThread(entities, keywords, candidateThreads);
                    var (created, assigned) = await FactService.AssignFactBatchAndUpsertThreadAsync(batchId, 
                        threadId, DBHelpers.ToCsvString(entities), DBHelpers.ToCsvString(keywords), ct);
                    _logger.LogInformation(created ? $"Thread was created with id: {threadId}" 
                        : $"Thread was assigned with id: {threadId}");
                }

                itemsProcessed += unassignedBatchIds.Count();
            }
            _logger.LogInformation("Threadlinker run finished");            
            return new WorkflowRunResult(itemsProcessed);
        }
    }
}