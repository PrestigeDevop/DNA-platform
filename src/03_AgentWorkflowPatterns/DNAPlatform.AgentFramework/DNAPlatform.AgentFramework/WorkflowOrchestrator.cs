using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DNAPlatform.AgentFramework;

/// <summary>
/// Default implementation of workflow orchestration
/// </summary>
public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly INodeExecutor _nodeExecutor;
    private readonly ILogger<WorkflowOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, WorkflowExecution> _executions;

    public WorkflowOrchestrator(INodeExecutor nodeExecutor, ILogger<WorkflowOrchestrator> logger)
    {
        _nodeExecutor = nodeExecutor;
        _logger = logger;
        _executions = new ConcurrentDictionary<string, WorkflowExecution>();
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflow(Workflow workflow)
    {
        return await ExecuteWorkflow(workflow, new Dictionary<string, object>());
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflow(
        Workflow workflow, 
        Dictionary<string, object> executionContext)
    {
        var executionId = Guid.NewGuid().ToString();
            var execution = new WorkflowExecution
            {
                ExecutionId = executionId,
                WorkflowId = workflow.Id,
                Status = WorkflowStatus.Running,
                StartedAt = DateTime.UtcNow,
                ExecutionContext = executionContext,
                NodeExecutions = new ConcurrentDictionary<string, NodeExecution>(),
                Errors = new List<ExecutionError>()
            };

        _executions.TryAdd(executionId, execution);

        try
        {
            _logger.LogInformation("Starting workflow execution: {WorkflowId} ({ExecutionId})", 
                workflow.Id, executionId);

            // Build execution graph
            var nodeMap = workflow.Nodes?.ToDictionary(n => n.Id) ?? new Dictionary<string, WorkflowNode>();
            var connectionList = workflow.Connections?.ToList() ?? new List<NodeConnection>();

            // Find entry nodes (nodes with no incoming connections)
            var entryNodeIds = nodeMap.Keys.Where(nodeId => 
                !connectionList.Any(c => c.TargetNodeId == nodeId)).ToList();

            if (!entryNodeIds.Any())
            {
                throw new InvalidOperationException("Workflow has no entry nodes");
            }

            // Topological order is derived from Workflow.Connections. Nodes whose
            // dependencies are satisfied run together; data flows source -> target.
            var completedNodes = new HashSet<string>();
            var nodeOutputs = new ConcurrentDictionary<string, Dictionary<string, object>>();

            // Terminal nodes (no outgoing edge) decide the workflow's outputs; a
            // workflow with no terminal node falls back to the last node executed.
            var terminalNodeIds = nodeMap.Keys
                .Where(id => !connectionList.Any(c => c.SourceNodeId == id))
                .ToHashSet();

            while (completedNodes.Count < nodeMap.Count && execution.Status == WorkflowStatus.Running)
            {
                var executableNodes = nodeMap
                    .Where(kvp => !completedNodes.Contains(kvp.Key))
                    .Where(kvp => {
                        var incomingConnections = connectionList.Where(c => c.TargetNodeId == kvp.Key).ToList();
                        return incomingConnections.Count == 0 || 
                               incomingConnections.All(c => completedNodes.Contains(c.SourceNodeId));
                    })
                    .Select(kvp => kvp.Value)
                    .ToList();

                if (!executableNodes.Any() && completedNodes.Count < nodeMap.Count)
                {
                    throw new InvalidOperationException("Circular dependency or unreachable nodes detected");
                }

                // Execute nodes (can be parallelized for nodes with parallelizable=true)
                var tasks = executableNodes.Select(async node =>
                {
                    // Thread-safe: the while-loop below runs ready nodes through
                    // Task.WhenAll, so several nodes write here concurrently.
                    var inputs = new Dictionary<string, object>();

                    // Gather inputs from connected nodes
                    var incomingConnections = connectionList.Where(c => c.TargetNodeId == node.Id).ToList();
                    foreach (var connection in incomingConnections)
                    {
                        if (nodeOutputs.TryGetValue(connection.SourceNodeId, out var outputs))
                        {
                            var key = string.IsNullOrEmpty(connection.OutputKey) ? "output" : connection.OutputKey;
                            var inputKey = string.IsNullOrEmpty(connection.InputKey) ? key : connection.InputKey;
                            if (outputs.TryGetValue(key, out var value))
                            {
                                inputs[inputKey] = value;
                            }
                        }
                    }

                    _logger.LogInformation("Executing node: {NodeId} ({NodeName})", node.Id, node.Name);

                    var nodeExecution = await _nodeExecutor.ExecuteNode(node, inputs);
                    nodeExecution.Inputs = inputs;

                    execution.NodeExecutions![node.Id] = nodeExecution;
                    if (nodeExecution.Outputs != null)
                    {
                        nodeOutputs[node.Id] = nodeExecution.Outputs;
                    }

                    return (Node: node, Execution: nodeExecution);
                });

                var results = await Task.WhenAll(tasks);

                foreach (var (node, nodeExec) in results)
                {
                    completedNodes.Add(node.Id);

                    if (nodeExec.Status == NodeStatus.Failed)
                    {
                        execution.Status = WorkflowStatus.Failed;
                        if (nodeExec.Error != null)
                        {
                            execution.Errors!.Add(nodeExec.Error);
                        }
                        _logger.LogError("Node execution failed: {NodeId}", node.Id);
                        break;
                    }
                }
            }

            execution.CompletedAt = DateTime.UtcNow;
            execution.Status = execution.Status == WorkflowStatus.Running ? 
                WorkflowStatus.Completed : execution.Status;

            _logger.LogInformation("Workflow execution completed: {WorkflowId} ({ExecutionId}) - Status: {Status}", 
                workflow.Id, executionId, execution.Status);

            // Prefer the terminal nodes' outputs so the result is deterministic
            // regardless of which branch happened to finish last.
            var resultOutputs = new Dictionary<string, object>();
            foreach (var terminalId in terminalNodeIds.Where(nodeOutputs.ContainsKey))
            {
                foreach (var pair in nodeOutputs[terminalId])
                {
                    resultOutputs[pair.Key] = pair.Value;
                }
            }

            if (resultOutputs.Count == 0 && nodeOutputs.Count > 0)
            {
                var last = completedNodes.LastOrDefault();
                if (last != null && nodeOutputs.TryGetValue(last, out var lastOutputs))
                {
                    foreach (var pair in lastOutputs)
                    {
                        resultOutputs[pair.Key] = pair.Value;
                    }
                }
            }

            return new WorkflowExecutionResult
            {
                ExecutionId = executionId,
                Status = execution.Status,
                Duration = execution.Duration,
                Outputs = resultOutputs,
                Errors = execution.Errors,
                FullExecution = execution
            };
        }
        catch (Exception ex)
        {
            execution.Status = WorkflowStatus.Failed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Errors!.Add(new ExecutionError
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                ErrorCode = "EXECUTION_ERROR"
            });

            _logger.LogError(ex, "Workflow execution failed: {WorkflowId} ({ExecutionId})", 
                workflow.Id, executionId);

            return new WorkflowExecutionResult
            {
                ExecutionId = executionId,
                Status = WorkflowStatus.Failed,
                Duration = execution.Duration,
                Errors = execution.Errors,
                FullExecution = execution
            };
        }
    }

    public Task PauseExecution(string executionId)
    {
        if (_executions.TryGetValue(executionId, out var execution))
        {
            execution.Status = WorkflowStatus.Paused;
            _logger.LogInformation("Execution paused: {ExecutionId}", executionId);
        }
        return Task.CompletedTask;
    }

    public Task ResumeExecution(string executionId)
    {
        if (_executions.TryGetValue(executionId, out var execution))
        {
            execution.Status = WorkflowStatus.Running;
            _logger.LogInformation("Execution resumed: {ExecutionId}", executionId);
        }
        return Task.CompletedTask;
    }

    public Task CancelExecution(string executionId)
    {
        if (_executions.TryGetValue(executionId, out var execution))
        {
            execution.Status = WorkflowStatus.Cancelled;
            execution.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("Execution cancelled: {ExecutionId}", executionId);
        }
        return Task.CompletedTask;
    }

    public Task<WorkflowExecution> GetExecution(string executionId)
    {
        if (_executions.TryGetValue(executionId, out var execution))
        {
            return Task.FromResult(execution);
        }
                throw new KeyNotFoundException($"Execution not found: {executionId}");
    }

    public IEnumerable<WorkflowExecution> ListExecutions()
    {
        return _executions.Values;
    }
}
