namespace NewsLensAutomationService.Interfaces;

public interface IWorkflow
{
    string Id { get; } 
    
    Task<WorkflowRunResult> RunOnceAsync(CancellationToken ct);
}


public sealed record WorkflowRunResult(
    int ItemsProcessed,
    bool HadErrors = false,
    string? ErrorMessage = null);