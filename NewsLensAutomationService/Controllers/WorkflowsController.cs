using Microsoft.AspNetCore.Mvc;
using NewsLensAutomationService.Orchestrators;

[ApiController]
[Route("api/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly WorkflowOrchestrator _orc;
    public WorkflowsController(WorkflowOrchestrator orc) => _orc = orc;

    [HttpPost("{id}/pause")]
    public IActionResult Pause(string id) { _orc.Pause(id); return Ok(); }

    [HttpPost("{id}/resume")]
    public IActionResult Resume(string id) { _orc.Resume(id); return Ok(); }

    [HttpPost("{id}/trigger")]
    public async Task<IActionResult> Trigger(string id, CancellationToken ct)
    {
        await _orc.TriggerOnceAsync(id, ct);
        return Ok();
    }

    [HttpGet]
    public IActionResult List()
    {
        var status = _orc.GetStatus()
            .ToDictionary(
                kv => kv.Key,
                kv => new {
                    kv.Value.State,
                    kv.Value.LastRunAt,
                    kv.Value.LastSuccessAt,
                    kv.Value.LastItemsProcessed,
                    kv.Value.LastError,
                    IntervalSeconds = kv.Value.Interval.TotalSeconds
                });
        return Ok(status);
    }
}