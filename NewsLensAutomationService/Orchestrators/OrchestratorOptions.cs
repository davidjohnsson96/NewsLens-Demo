namespace NewsLensAutomationService.Orchestrators;

public class OrchestratorOptions
{

    public Dictionary<string, int> WorkflowRunIntervals { get; set; } = new()
    {
        { "FactHarvest", 100 },
        { "ArticleCreation", 100 },
        { "ThreadLinker", 100 },
    };

}