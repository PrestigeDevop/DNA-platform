using System.Collections.Concurrent;
using DNAPlatform.AgentFramework;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// In-memory node executor for the DevUI API.
/// </summary>
/// <remarks>
/// The API consumes the lightweight contracts in <c>AgentFrameworkStubs.cs</c>
/// rather than the full AgentFramework engine. This provides the missing
/// implementation so the Workflows/Agents/Executions controllers can be
/// activated by dependency injection.
/// </remarks>
public class InMemoryNodeExecutor : INodeExecutor
{
    public List<NodeType> GetSupportedNodeTypes() => Enum.GetValues<NodeType>().ToList();
}

/// <summary>
/// In-memory workflow orchestrator: executes nodes in connection order and
/// records each run so /api/executions can report on it.
/// </summary>
public class InMemoryWorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly ILogger<InMemoryWorkflowOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, WorkflowExecutionResult> _executions = new();

    public InMemoryWorkflowOrchestrator(ILogger<InMemoryWorkflowOrchestrator> logger)
    {
        _logger = logger;
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflow(
        Workflow workflow,
        Dictionary<string, object>? context = null)
    {
        var startedAt = DateTime.UtcNow;
        var outputs = new Dictionary<string, object>();
        var completed = new HashSet<string>();

        foreach (var node in OrderNodes(workflow))
        {
            // Simulate deterministic work; agent/LLM-backed execution arrives in Phase 4.
            await Task.Delay(20);
            outputs[node.Id] = new Dictionary<string, object>
            {
                ["output"] = $"Processed by node {node.Name}",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };
            completed.Add(node.Id);
            _logger.LogInformation("Node executed: {NodeId} ({NodeType})", node.Id, node.NodeType);
        }

        var result = new WorkflowExecutionResult
        {
            Status = "Completed",
            Duration = DateTime.UtcNow - startedAt,
            Outputs = outputs
        };

        _executions[workflow.Id] = result;
        return result;
    }

    // Topological order derived from the connection list, falling back to
    // declaration order for nodes that are not part of the edge graph.
    private static IEnumerable<WorkflowNode> OrderNodes(Workflow workflow)
    {
        var nodes = workflow.Nodes ?? new List<WorkflowNode>();
        var connections = workflow.Connections ?? new List<NodeConnection>();
        var ordered = new List<WorkflowNode>();
        var remaining = new List<WorkflowNode>(nodes);

        while (remaining.Count > 0)
        {
            var ready = remaining
                .Where(n => !connections.Any(c =>
                    c.TargetNodeId == n.Id && remaining.Any(r => r.Id == c.SourceNodeId)))
                .ToList();

            if (ready.Count == 0)
            {
                // Cycle or dangling reference: keep declaration order.
                ordered.AddRange(remaining);
                break;
            }

            ordered.AddRange(ready);
            remaining.RemoveAll(r => ready.Contains(r));
        }

        return ordered;
    }

    public Task<WorkflowExecutionResult> GetExecution(string executionId)
    {
        if (!_executions.TryGetValue(executionId, out var result))
        {
            throw new KeyNotFoundException($"Execution '{executionId}' not found");
        }

        return Task.FromResult(result);
    }

    public List<WorkflowExecutionResult> ListExecutions() => _executions.Values.ToList();

    public Task PauseExecution(string executionId) => Task.CompletedTask;

    public Task ResumeExecution(string executionId) => Task.CompletedTask;

    public Task CancelExecution(string executionId) => Task.CompletedTask;
}

/// <summary>
/// In-memory agent manager for the DevUI API.
/// </summary>
public class InMemoryAgentManager : IAgentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();
    private readonly ILogger<InMemoryAgentManager> _logger;

    public InMemoryAgentManager(ILogger<InMemoryAgentManager> logger)
    {
        _logger = logger;
    }

    public Task<IAgent> CreateAgent(string agentId, AgentConfig config)
    {
        IAgent agent = new InMemoryAgent(agentId, config);
        _agents[agentId] = agent;
        _logger.LogInformation("Agent created: {AgentId} ({AgentType})", agentId, config.AgentType);
        return Task.FromResult(agent);
    }

    public Task<IAgent?> GetAgent(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task DeleteAgent(string agentId)
    {
        _agents.TryRemove(agentId, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AgentSummary>> ListAgents()
    {
        IEnumerable<AgentSummary> summaries = _agents.Values
            .Select(a => new AgentSummary
            {
                Id = a.AgentId,
                Name = a.AgentId,
                Type = "StandardAgent"
            })
            .ToList();
        return Task.FromResult(summaries);
    }
}

/// <summary>
/// Minimal in-memory agent used by <see cref="InMemoryAgentManager"/>.
/// </summary>
public class InMemoryAgent : IAgent
{
    private readonly AgentConfig _config;
    private readonly ConcurrentDictionary<string, ISkill> _skills = new();

    public InMemoryAgent(string agentId, AgentConfig config)
    {
        AgentId = agentId;
        _config = config;
    }

    public string AgentId { get; }

    public IEnumerable<string> GetAvailableSkills() => _skills.Keys.ToList();

    public Task<AgentResponse> Run(string prompt, Dictionary<string, object>? context = null)
    {
        // Deterministic placeholder until Phase 4 wires an LLM provider.
        return Task.FromResult(new AgentResponse
        {
            Response = $"[{_config.AgentType}] echo: {prompt}",
            TokensUsed = prompt.Length
        });
    }

    public Task BindSkill(string skillId, ISkill skill)
    {
        _skills[skillId] = skill;
        return Task.CompletedTask;
    }
}
