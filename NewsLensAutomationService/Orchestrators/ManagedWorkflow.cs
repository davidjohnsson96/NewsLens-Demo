using NewsLensAutomationService.Interfaces;

namespace NewsLensAutomationService.Orchestrators;

public sealed class ManagedWorkflow
{
    public IWorkflow Workflow { get; }
    public TimeSpan Interval { get; }

    public OrchestratorState State { get; set; } = OrchestratorState.Stopped;
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
    public int LastItemsProcessed { get; set; }
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public ManagedWorkflow(IWorkflow wf, TimeSpan interval)
    {
        Workflow = wf;
        Interval = interval;
    }
}

public enum OrchestratorState { Stopped, Running, Paused, Error }