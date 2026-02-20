using System.Threading.Channels;
using Microsoft.Extensions.Options;
using NewsLensAutomationService.Interfaces;
using NewsLensAutomationService.Workflows;

namespace NewsLensAutomationService.Orchestrators;

public sealed class WorkflowOrchestrator : IHostedService, IDisposable
{
    private readonly ILogger<WorkflowOrchestrator> _log;
    private readonly Dictionary<string, ManagedWorkflow> _workflows = new();
    private OrchestratorOptions _orchestratorOptions;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public WorkflowOrchestrator(
        ILogger<WorkflowOrchestrator> log,
        FactHarvestWorkflow factWorkflow,
        ArticleCreationWorkflow articleWorkflow,
        ThreadLinkerWorkflow threadLinkerWorkflow,
        IOptions<OrchestratorOptions> orchestratorOptions)
    {
        _log = log;
        _orchestratorOptions = orchestratorOptions.Value;
        _workflows[factWorkflow.Id] = new ManagedWorkflow(factWorkflow, TimeSpan.FromMinutes(_orchestratorOptions.WorkflowRunIntervals[factWorkflow.Id]));
        _workflows[articleWorkflow.Id] = new ManagedWorkflow(articleWorkflow, TimeSpan.FromMinutes(_orchestratorOptions.WorkflowRunIntervals[articleWorkflow.Id]));
        _workflows[threadLinkerWorkflow.Id] = new ManagedWorkflow(threadLinkerWorkflow, TimeSpan.FromMinutes(_orchestratorOptions.WorkflowRunIntervals[threadLinkerWorkflow.Id]));
    }

    public Task StartAsync(CancellationToken _) 
    {
        if (_cts != null) return Task.CompletedTask;
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => LoopAsync(_cts.Token));
        /*foreach (var wf in _workflows.Values)
            wf.State = OrchestratorState.Running;*/
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken _)
    {
        if (_cts == null) return;
        _cts.Cancel();
        try { if (_loop != null) await _loop; } catch { }
        foreach (var wf in _workflows.Values)
            wf.State = OrchestratorState.Stopped;
        _cts.Dispose();
        _cts = null;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        var nextTick = _workflows.ToDictionary(
            kv => kv.Key,
            kv => DateTimeOffset.UtcNow
        );

        while (!ct.IsCancellationRequested)
        {
            foreach (var (id, mw) in _workflows)
            {
                if (mw.State != OrchestratorState.Running) continue;

                if (DateTimeOffset.UtcNow >= nextTick[id])
                {
                    await SafeRunOnceAsync(mw, ct);
                    nextTick[id] = DateTimeOffset.UtcNow + mw.Interval;
                }
            }

            await Task.Delay(500, ct);
        }
    }

    private async Task SafeRunOnceAsync(ManagedWorkflow mw, CancellationToken ct)
    {
        if (!await mw.Gate.WaitAsync(0, ct)) return; // no overlap

        try
        {
            mw.LastRunAt = DateTimeOffset.UtcNow;
            var result = await mw.Workflow.RunOnceAsync(ct);
            mw.LastItemsProcessed = result.ItemsProcessed;
            mw.LastError = result.HadErrors ? result.ErrorMessage : null;
            if (!result.HadErrors)
            {
                mw.LastSuccessAt = mw.LastRunAt;
                if (mw.State == OrchestratorState.Error)
                    mw.State = OrchestratorState.Running;
            }
            else mw.State = OrchestratorState.Error;

            _log.LogInformation("Workflow {Id} run: items={Items} error={Error}",
                mw.Workflow.Id, result.ItemsProcessed, result.HadErrors);
        }
        catch (Exception ex)
        {
            mw.LastError = ex.Message;
            mw.State = OrchestratorState.Error;
            _log.LogError(ex, "Workflow {Id} failed", mw.Workflow.Id);
        }
        finally
        {
            mw.Gate.Release();
        }
    }
    
    public void Pause(string id)
    {
        if (_workflows.TryGetValue(id, out var mw))
            mw.State = OrchestratorState.Paused;
    }

    public void Resume(string id)
    {
        if (_workflows.TryGetValue(id, out var mw))
            mw.State = OrchestratorState.Running;
    }

    public Task TriggerOnceAsync(string id, CancellationToken ct)
        => _workflows.TryGetValue(id, out var mw)
            ? SafeRunOnceAsync(mw, ct)
            : Task.CompletedTask;

    public IReadOnlyDictionary<string, ManagedWorkflow> GetStatus() => _workflows;

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        foreach (var wf in _workflows.Values)
            wf.Gate.Dispose();
    }
}