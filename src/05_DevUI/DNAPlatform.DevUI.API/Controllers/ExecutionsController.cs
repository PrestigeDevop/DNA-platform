using Microsoft.AspNetCore.Mvc;
using DNAPlatform.AgentFramework;

namespace DNAPlatform.DevUI.API.Controllers;

/// <summary>
/// API controller for workflow execution monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExecutionsController : ControllerBase
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<ExecutionsController> _logger;

    public ExecutionsController(
        IWorkflowOrchestrator orchestrator,
        ILogger<ExecutionsController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/executions/{executionId} - Get execution status
    /// </summary>
    [HttpGet("{executionId}")]
    public async Task<IActionResult> GetExecution(string executionId)
    {
        try
        {
            var execution = await _orchestrator.GetExecution(executionId);
            return Ok(execution);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = $"Execution '{executionId}' not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution: {ExecutionId}", executionId);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/executions/{executionId}/pause - Pause a running execution
    /// </summary>
    [HttpPost("{executionId}/pause")]
    public async Task<IActionResult> PauseExecution(string executionId)
    {
        try
        {
            await _orchestrator.PauseExecution(executionId);
            _logger.LogInformation("Execution paused: {ExecutionId}", executionId);
            return Ok(new { Message = $"Execution '{executionId}' paused" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause execution: {ExecutionId}", executionId);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/executions/{executionId}/resume - Resume a paused execution
    /// </summary>
    [HttpPost("{executionId}/resume")]
    public async Task<IActionResult> ResumeExecution(string executionId)
    {
        try
        {
            await _orchestrator.ResumeExecution(executionId);
            _logger.LogInformation("Execution resumed: {ExecutionId}", executionId);
            return Ok(new { Message = $"Execution '{executionId}' resumed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume execution: {ExecutionId}", executionId);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/executions/{executionId}/cancel - Cancel a running execution
    /// </summary>
    [HttpPost("{executionId}/cancel")]
    public async Task<IActionResult> CancelExecution(string executionId)
    {
        try
        {
            await _orchestrator.CancelExecution(executionId);
            _logger.LogInformation("Execution cancelled: {ExecutionId}", executionId);
            return Ok(new { Message = $"Execution '{executionId}' cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel execution: {ExecutionId}", executionId);
            return BadRequest(new { Message = ex.Message });
        }
    }
}
