using Microsoft.AspNetCore.Mvc;
using DNAPlatform.AgentFramework;
using DNAPlatform.DevUI.API.Services;

namespace DNAPlatform.DevUI.API.Controllers;

/// <summary>
/// API controller for workflow management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly IWorkflowStore _store;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowOrchestrator orchestrator,
        IWorkflowStore store,
        ILogger<WorkflowsController> logger)
    {
        _orchestrator = orchestrator;
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/workflows - List all workflows
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var workflows = await _store.GetAllAsync();
        return Ok(new { Count = workflows.Count(), Workflows = workflows });
    }

    /// <summary>
    /// GET /api/workflows/{id} - Get workflow by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var workflow = await _store.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound(new { Message = $"Workflow '{id}' not found" });
        }
        return Ok(workflow);
    }

    /// <summary>
    /// POST /api/workflows - Create a new workflow
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Workflow workflow)
    {
        if (string.IsNullOrEmpty(workflow.Name))
        {
            return BadRequest(new { Message = "Workflow name is required" });
        }

        try
        {
            var created = await _store.CreateAsync(workflow);
            _logger.LogInformation("Workflow created: {WorkflowId} ({WorkflowName})", created.Id, created.Name);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/workflows/{id} - Update an existing workflow
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Workflow workflow)
    {
        try
        {
            var updated = await _store.UpdateAsync(id, workflow);
            if (updated == null)
            {
                return NotFound(new { Message = $"Workflow '{id}' not found" });
            }

            _logger.LogInformation("Workflow updated: {WorkflowId}", id);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow: {WorkflowId}", id);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/workflows/{id} - Delete a workflow
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _store.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { Message = $"Workflow '{id}' not found" });
        }

        _logger.LogInformation("Workflow deleted: {WorkflowId}", id);
        return Ok(new { Message = $"Workflow '{id}' deleted" });
    }

    /// <summary>
    /// POST /api/workflows/{id}/execute - Execute a workflow
    /// </summary>
    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(string id, [FromBody] Dictionary<string, object>? context = null)
    {
        var workflow = await _store.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound(new { Message = $"Workflow '{id}' not found" });
        }

        try
        {
            _logger.LogInformation("Executing workflow: {WorkflowId}", id);
            var result = context != null
                ? await _orchestrator.ExecuteWorkflow(workflow, context)
                : await _orchestrator.ExecuteWorkflow(workflow);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed: {WorkflowId}", id);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/workflows/validate - Validate a workflow definition
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] Workflow workflow)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(workflow.Name))
            errors.Add("Workflow name is required");

        if (workflow.Nodes == null || !workflow.Nodes.Any())
            errors.Add("Workflow must have at least one node");

        if (workflow.Nodes != null)
        {
            // Check for duplicate node IDs
            var duplicateIds = workflow.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).Select(g => g.Key);
            errors.AddRange(duplicateIds.Select(id => $"Duplicate node ID: {id}"));

            // Check connections reference valid nodes
            var nodeIds = workflow.Nodes.Select(n => n.Id).ToHashSet();
            if (workflow.Connections != null)
            {
                foreach (var conn in workflow.Connections)
                {
                    if (!nodeIds.Contains(conn.SourceNodeId))
                        errors.Add($"Connection references unknown source node: {conn.SourceNodeId}");
                    if (!nodeIds.Contains(conn.TargetNodeId))
                        errors.Add($"Connection references unknown target node: {conn.TargetNodeId}");
                }
            }
        }

        return Ok(new { IsValid = !errors.Any(), Errors = errors });
    }
}
