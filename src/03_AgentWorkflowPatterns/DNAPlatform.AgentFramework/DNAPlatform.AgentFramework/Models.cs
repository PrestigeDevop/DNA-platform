using System.Collections.Concurrent;

namespace DNAPlatform.AgentFramework;

/// <summary>
/// Enumeration of node types in a workflow
/// </summary>
public enum NodeType
{
    /// <summary>Data input node (sources)</summary>
    Input,
    
    /// <summary>Node that holds a static string value (user-configurable)</summary>
    Value,
    
    /// <summary>Agent-based processing node with AI reasoning</summary>
    Agent,
    
    /// <summary>Deterministic skill-based processing</summary>
    ProcessingSkill,
    
    /// <summary>Node that receives input and writes it to the platform log buffer</summary>
    Log,
    
    /// <summary>Conditional branching logic</summary>
    Conditional,
    
    /// <summary>Data output node (sinks)</summary>
    Output,
    
    /// <summary>Loop/iteration control</summary>
    Loop,
    
    /// <summary>Subworkflow reference</summary>
    SubWorkflow
}

/// <summary>
/// Enumeration of workflow execution statuses
/// </summary>
public enum WorkflowStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Enumeration of node execution statuses
/// </summary>
public enum NodeStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    Waiting
}

/// <summary>
/// Represents a single workflow node in the execution graph
/// </summary>
public class WorkflowNode
{
    /// <summary>Unique identifier for the node</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>Display name for the node</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of processing this node performs. Bound from the string form
    /// ("Input", "ProcessingSkill", ...) by the JsonStringEnumConverter the API
    /// registers, which is what the designer persists.
    /// </summary>
    public NodeType NodeType { get; set; } = NodeType.ProcessingSkill;
    
    /// <summary>Human-readable description</summary>
    public string? Description { get; set; }
    
    /// <summary>Configuration parameters as key-value pairs</summary>
    public Dictionary<string, object>? Config { get; set; }
    
    /// <summary>Input schema defining expected inputs</summary>
    public Dictionary<string, string>? InputSchema { get; set; }
    
    /// <summary>Output schema defining produced outputs</summary>
    public Dictionary<string, string>? OutputSchema { get; set; }
    
    /// <summary>Retry policy for this node</summary>
    public RetryPolicy? RetryPolicy { get; set; }
    
    /// <summary>Timeout for node execution in milliseconds</summary>
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Canvas position, persisted by the workflow designer alongside the node in
    /// the stored definition. Carried here so one <see cref="Workflow"/> type is
    /// the single representation of a workflow definition (docs/refactor.md
    /// section 15); the engine itself ignores it.
    /// </summary>
    public double X { get; set; }

    /// <summary>Canvas top position, in pixels. See <see cref="X"/>.</summary>
    public double Y { get; set; }
}

/// <summary>
/// Represents a connection between two workflow nodes
/// </summary>
public class NodeConnection
{
    /// <summary>
    /// Source node identifier. Not <c>required</c>: a definition that arrives with
    /// an empty or missing id must reach the validation endpoint and be reported
    /// as an error, not rejected by the JSON deserializer.
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;
    
    /// <summary>Target node identifier. See <see cref="SourceNodeId"/>.</summary>
    public string TargetNodeId { get; set; } = string.Empty;
    
    /// <summary>Output key from source node to pass</summary>
    public string? OutputKey { get; set; }
    
    /// <summary>Input key on target node to receive value</summary>
    public string? InputKey { get; set; }
    
    /// <summary>Condition for edge activation (optional)</summary>
    public string? Condition { get; set; }
}

/// <summary>
/// Represents a complete workflow definition
/// </summary>
public class Workflow
{
    /// <summary>
    /// Unique identifier for the workflow. Defaults to empty rather than being
    /// <c>required</c> so a nameless/Id-less definition reaches
    /// <c>POST /api/workflows/validate</c> and is reported as invalid instead of
    /// failing deserialization.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>Display name for the workflow. See <see cref="Id"/>.</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Human-readable description</summary>
    public string? Description { get; set; }
    
    /// <summary>Version of this workflow</summary>
    public string? Version { get; set; }
    
    /// <summary>Nodes comprising this workflow</summary>
    public IEnumerable<WorkflowNode>? Nodes { get; set; }
    
    /// <summary>Connections between nodes</summary>
    public IEnumerable<NodeConnection>? Connections { get; set; }
    
    /// <summary>Workflow-level configuration and variables</summary>
    public Dictionary<string, object>? GlobalConfig { get; set; }
    
    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Last modification timestamp</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Whether this workflow is published/immutable</summary>
    public bool IsPublished { get; set; }
}

/// <summary>
/// Represents a single execution of a workflow
/// </summary>
public class WorkflowExecution
{
    /// <summary>Unique execution identifier</summary>
    public required string ExecutionId { get; set; }
    
    /// <summary>Reference to the workflow being executed</summary>
    public string WorkflowId { get; set; } = string.Empty;
    
    /// <summary>Current status of this execution</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    
    /// <summary>Execution start time</summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>Execution end time</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Total execution duration</summary>
    public TimeSpan Duration => (CompletedAt ?? DateTime.UtcNow) - StartedAt;
    
    /// <summary>
    /// Node executions tracked individually. A ConcurrentDictionary because the
    /// orchestrator runs each ready node through Task.WhenAll and they all write
    /// here; a plain Dictionary throws on the concurrent writes.
    /// </summary>
    public ConcurrentDictionary<string, NodeExecution>? NodeExecutions { get; set; }
    
    /// <summary>Workflow-level execution context/state</summary>
    public Dictionary<string, object>? ExecutionContext { get; set; }
    
    /// <summary>Errors that occurred during execution</summary>
    public List<ExecutionError>? Errors { get; set; }
}

/// <summary>
/// Represents execution of a single node
/// </summary>
public class NodeExecution
{
    /// <summary>Node identifier</summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>Current status of node execution</summary>
    public NodeStatus Status { get; set; } = NodeStatus.Pending;
    
    /// <summary>When node started executing</summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>When node finished executing</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Node execution duration</summary>
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - (StartedAt ?? DateTime.UtcNow) : null;
    
    /// <summary>Outputs produced by this node</summary>
    public Dictionary<string, object>? Outputs { get; set; }

    /// <summary>
    /// Inputs this node actually received, gathered from its incoming connections.
    /// Persisted so the designer can render a real execution journal (per-node IO
    /// values) instead of inventing them, and so the execution test suite can prove
    /// data is propagated along edges.
    /// </summary>
    public Dictionary<string, object>? Inputs { get; set; }
    
    /// <summary>Number of retries performed</summary>
    public int RetryCount { get; set; }
    
    /// <summary>Error information if execution failed</summary>
    public ExecutionError? Error { get; set; }
}

/// <summary>
/// Represents an error that occurred during execution
/// </summary>
public class ExecutionError
{
    /// <summary>Message from the failure</summary>
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Retry policy for node execution
/// </summary>
public class RetryPolicy
{
    /// <summary>Maximum number of retry attempts</summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>Initial backoff delay in milliseconds</summary>
    public int InitialDelayMs { get; set; } = 1000;
    
    /// <summary>Backoff multiplier (exponential)</summary>
    public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>Maximum backoff delay in milliseconds</summary>
    public int MaxDelayMs { get; set; } = 60000;
}

/// <summary>
/// Result of a workflow execution
/// </summary>
public class WorkflowExecutionResult
{
    /// <summary>Unique execution ID</summary>
    public string ExecutionId { get; set; } = string.Empty;
    
    /// <summary>Final status of the workflow</summary>
    public WorkflowStatus Status { get; set; }
    
    /// <summary>Total execution time</summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>Final output data</summary>
    public Dictionary<string, object>? Outputs { get; set; }
    
    /// <summary>Any errors that occurred</summary>
    public List<ExecutionError>? Errors { get; set; }
    
    /// <summary>Full execution details</summary>
    public WorkflowExecution? FullExecution { get; set; }
}
