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
    private readonly INodeExecutor _nodeExecutor;
    private readonly IWorkflowStore _store;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowOrchestrator orchestrator,
        INodeExecutor nodeExecutor,
        IWorkflowStore store,
        ILogger<WorkflowsController> logger)
    {
        _orchestrator = orchestrator;
        _nodeExecutor = nodeExecutor;
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
    /// POST /api/workflows/validate - Structural validation of a workflow definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the Test step of the Configure -> Test -> Run lifecycle
    /// (docs/refactor.md section 4). It checks <b>structure</b> only: node identity
    /// and type support, edge endpoints, and whether the graph is executable (has an
    /// entry node, is acyclic). It does not run skills, does not check the
    /// environment, and makes no attempt at semantic validation — the spec asks for
    /// the UI/API contract first so further checks can be added incrementally rather
    /// than building a speculative validation engine.
    /// </para>
    /// <para>
    /// Always 200: <c>{ isValid, errors, warnings, checks, summary }</c>. A body that
    /// fails to deserialize is a 400 from model binding, which is a different fault
    /// from "the workflow is invalid".
    /// </para>
    /// </remarks>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] Workflow workflow)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var checks = new List<ValidationCheck>();

        // Detail is the failure explanation, so it is only attached when the check
        // fails — otherwise a passing check reads like an error message.
        void Record(string name, bool passed, string? failureDetail = null)
            => checks.Add(new ValidationCheck
            {
                Name = name,
                Passed = passed,
                Detail = passed ? null : failureDetail
            });

        var hasName = !string.IsNullOrWhiteSpace(workflow.Name);
        Record("workflow.name", hasName, "Workflow name is required.");
        if (!hasName)
        {
            errors.Add("Workflow name is required");
        }

        var nodes = workflow.Nodes?.ToList() ?? new List<WorkflowNode>();
        var connections = workflow.Connections?.ToList() ?? new List<NodeConnection>();

        var hasNodes = nodes.Count > 0;
        Record("workflow.hasNodes", hasNodes, "Workflow must have at least one node.");
        if (!hasNodes)
        {
            errors.Add("Workflow must have at least one node");
        }

        // --- Node identity, naming and per-type validity ---
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            var label = string.IsNullOrWhiteSpace(node.Name) ? node.Id : node.Name;

            if (string.IsNullOrWhiteSpace(node.Id))
            {
                errors.Add($"Node '{label}' has no id");
            }
            else if (!ids.Add(node.Id))
            {
                errors.Add($"Duplicate node ID: {node.Id}");
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                errors.Add($"Node '{node.Id}' has no name");
            }

            // Delegate to the engine's own notion of node validity so this endpoint
            // cannot drift from what execution actually requires.
            if (!await _nodeExecutor.ValidateNode(node))
            {
                errors.Add(node.NodeType == NodeType.ProcessingSkill
                    ? $"Node '{label}' is a ProcessingSkill but names no skill id in its config (expected a \"skillId\" key)"
                    : $"Node '{label}' is not valid for node type {node.NodeType}");
            }
        }

        // --- Edge endpoints ---
        foreach (var conn in connections)
        {
            if (!ids.Contains(conn.SourceNodeId))
            {
                errors.Add($"Connection references unknown source node: {conn.SourceNodeId}");
            }

            if (!ids.Contains(conn.TargetNodeId))
            {
                errors.Add($"Connection references unknown target node: {conn.TargetNodeId}");
            }

            if (conn.SourceNodeId == conn.TargetNodeId)
            {
                errors.Add($"Node '{conn.SourceNodeId}' is connected to itself");
            }
        }

        if (nodes.Count > 1 && connections.Count == 0)
        {
            warnings.Add("No connections: every node runs in parallel and no data flows between them.");
        }

        // --- Graph executability: at least one entry node (no incoming edge) ---
        var hasEntryNode = nodes.Any(n => !connections.Any(c => c.TargetNodeId == n.Id));
        Record("graph.hasEntryNode", hasEntryNode,
            "Every node has an incoming connection, so the graph has no starting point.");
        if (!hasEntryNode && hasNodes)
        {
            errors.Add("No entry node: every node has an incoming connection, so the graph cannot start.");
        }

        // --- Graph executability: acyclic ---
        var cycle = FindCycle(ids, connections);
        Record("graph.acyclic", cycle is null, cycle is null ? null : $"Circular dependency: {cycle}.");        if (cycle is not null)
        {
            errors.Add($"Circular dependency: {cycle}");
        }

        var isValid = errors.Count == 0;

        return Ok(new
        {
            isValid,
            errors,
            warnings,
            checks,
            summary = isValid
                ? $"Structural validation passed: {nodes.Count} node(s), {connections.Count} connection(s)."
                : $"Structural validation failed with {errors.Count} error(s)."
        });
    }

    /// <summary>
    /// Depth-first cycle search returning a readable path such as
    /// <c>a -&gt; b -&gt; c -&gt; a</c>, or null when the graph is acyclic. Only
    /// edges between known node ids are followed, so dangling references are left
    /// for the edge checks to report rather than being mistaken for a cycle.
    /// </summary>
    private static string? FindCycle(
        IEnumerable<string> knownIds,
        IReadOnlyCollection<NodeConnection> connections)
    {
        var known = new HashSet<string>(knownIds, StringComparer.Ordinal);

        var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var conn in connections)
        {
            if (!known.Contains(conn.SourceNodeId) || !known.Contains(conn.TargetNodeId))
            {
                continue;
            }

            if (!outgoing.TryGetValue(conn.SourceNodeId, out var targets))
            {
                targets = new List<string>();
                outgoing[conn.SourceNodeId] = targets;
            }

            targets.Add(conn.TargetNodeId);
        }

        var state = new Dictionary<string, int>(StringComparer.Ordinal); // 0 unvisited, 1 on path, 2 done
        var path = new List<string>();

        string? Visit(string node)
        {
            state[node] = 1;
            path.Add(node);

            if (outgoing.TryGetValue(node, out var targets))
            {
                foreach (var target in targets)
                {
                    if (state.TryGetValue(target, out var targetState))
                    {
                        if (targetState == 1)
                        {
                            return string.Join(" -> ", path.Skip(path.IndexOf(target)).Append(target));
                        }

                        if (targetState == 2)
                        {
                            continue;
                        }
                    }

                    var found = Visit(target);
                    if (found is not null)
                    {
                        return found;
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            state[node] = 2;
            return null;
        }

        foreach (var id in known)
        {
            if (!state.ContainsKey(id))
            {
                var found = Visit(id);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }
}

/// <summary>
/// One structural check performed by <c>POST /api/workflows/validate</c>, echoed
/// back so the client can show coverage rather than implying more was verified
/// than actually was.
/// </summary>
public class ValidationCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Detail { get; set; }
}
