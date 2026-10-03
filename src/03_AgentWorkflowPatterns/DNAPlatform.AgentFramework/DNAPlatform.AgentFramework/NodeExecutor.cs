using Microsoft.Extensions.Logging;

namespace DNAPlatform.AgentFramework;

/// <summary>
/// Default implementation of node execution
/// </summary>
public class NodeExecutor : INodeExecutor
{
    private readonly ILogger<NodeExecutor> _logger;

    public NodeExecutor(ILogger<NodeExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<NodeExecution> ExecuteNode(WorkflowNode node, Dictionary<string, object>? inputs = null)
    {
        var execution = new NodeExecution
        {
            NodeId = node.Id,
            Status = NodeStatus.Running,
            StartedAt = DateTime.UtcNow,
            Outputs = new Dictionary<string, object>()
        };

        try
        {
            _logger.LogInformation("Node execution started: {NodeId} ({NodeType})", node.Id, node.NodeType);

            // Simulate processing based on node type
            await Task.Delay(100); // Simulate work

            // Generate mock output
            execution.Outputs = inputs ?? new Dictionary<string, object>();
            execution.Outputs["output"] = $"Processed by node {node.Name}";
            execution.Outputs["timestamp"] = DateTime.UtcNow.ToString("O");

            execution.Status = NodeStatus.Completed;
            execution.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Node execution completed: {NodeId}", node.Id);
        }
        catch (Exception ex)
        {
            execution.Status = NodeStatus.Failed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Error = new ExecutionError
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                ErrorCode = "NODE_EXECUTION_ERROR"
            };
            _logger.LogError(ex, "Node execution failed: {NodeId}", node.Id);
        }

        return execution;
    }

    public Task<bool> ValidateNode(WorkflowNode node)
    {
        // Basic validation
        if (string.IsNullOrEmpty(node.Id))
            return Task.FromResult(false);

        if (node.NodeType == NodeType.Input && (node.Config == null || !node.Config.ContainsKey("source")))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public IEnumerable<NodeType> GetSupportedNodeTypes()
    {
        return Enum.GetValues(typeof(NodeType)).Cast<NodeType>();
    }
}
