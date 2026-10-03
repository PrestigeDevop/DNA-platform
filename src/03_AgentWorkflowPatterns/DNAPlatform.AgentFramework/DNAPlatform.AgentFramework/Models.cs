namespace DNAPlatform.AgentFramework;

/// <summary>
/// Enumeration of node types in a workflow
/// </summary>
public enum NodeType
{
    /// <summary>Data input node (sources)</summary>
    Input,
    
    /// <summary>Agent-based processing node with AI reasoning</summary>
    Agent,
    
    /// <summary>Deterministic skill-based processing</summary>
    ProcessingSkill,
    
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
    public required string Id { get; set; }
    
    /// <summary>Display name for the node</summary>
    public required string Name { get; set; }
    
    /// <summary>Type of processing this node performs</summary>
    public required NodeType NodeType { get; set; }
    
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
}

/// <summary>
/// Represents a connection between two workflow nodes
/// </summary>
public class NodeConnection
{
    /// <summary>Source node identifier</summary>
    public required string SourceNodeId { get; set; }
    
    /// <summary>Target node identifier</summary>
    public required string TargetNodeId { get; set; }
    
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
    /// <summary>Unique identifier for the workflow</summary>
    public required string Id { get; set; }
    
    /// <summary>Display name for the workflow</summary>
    public required string Name { get; set; }
    
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
    public required string WorkflowId { get; set; }
    
    /// <summary>Current status of this execution</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    
    /// <summary>Execution start time</summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>Execution end time</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Total execution duration</summary>
    public TimeSpan Duration => (CompletedAt ?? DateTime.UtcNow) - StartedAt;
    
    /// <summary>Node executions tracked individually</summary>
    public Dictionary<string, NodeExecution>? NodeExecutions { get; set; }
    
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
    public required string NodeId { get; set; }
    
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
    public required string Message { get; set; }
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
    public required string ExecutionId { get; set; }
    
    /// <summary>Final status of the workflow</summary>
    public required WorkflowStatus Status { get; set; }
    
    /// <summary>Total execution time</summary>
    public required TimeSpan Duration { get; set; }
    
    /// <summary>Final output data</summary>
    public Dictionary<string, object>? Outputs { get; set; }
    
    /// <summary>Any errors that occurred</summary>
    public List<ExecutionError>? Errors { get; set; }
    
    /// <summary>Full execution details</summary>
    public WorkflowExecution? FullExecution { get; set; }
}
