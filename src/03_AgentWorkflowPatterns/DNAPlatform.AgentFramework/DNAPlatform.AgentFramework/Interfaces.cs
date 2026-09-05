namespace DNAPlatform.AgentFramework;

/// <summary>
/// Main orchestrator for workflow execution
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Execute a complete workflow from start to finish
    /// </summary>
    Task<WorkflowExecutionResult> ExecuteWorkflow(Workflow workflow);
    
    /// <summary>
    /// Execute workflow with custom execution context
    /// </summary>
    Task<WorkflowExecutionResult> ExecuteWorkflow(Workflow workflow, Dictionary<string, object> executionContext);
    
    /// <summary>
    /// Pause a running workflow execution
    /// </summary>
    Task PauseExecution(string executionId);
    
    /// <summary>
    /// Resume a paused workflow execution
    /// </summary>
    Task ResumeExecution(string executionId);
    
    /// <summary>
    /// Cancel a running workflow execution
    /// </summary>
    Task CancelExecution(string executionId);
    
    /// <summary>
    /// Get execution status and history
    /// </summary>
    Task<WorkflowExecution> GetExecution(string executionId);
}

/// <summary>
/// Executes individual nodes within a workflow
/// </summary>
public interface INodeExecutor
{
    /// <summary>
    /// Execute a single node with given inputs
    /// </summary>
    Task<NodeExecution> ExecuteNode(WorkflowNode node, Dictionary<string, object>? inputs = null);
    
    /// <summary>
    /// Validate node configuration before execution
    /// </summary>
    Task<bool> ValidateNode(WorkflowNode node);
    
    /// <summary>
    /// Get supported node types
    /// </summary>
    IEnumerable<NodeType> GetSupportedNodeTypes();
}

/// <summary>
/// Manages agent lifecycle and execution
/// </summary>
public interface IAgentManager
{
    /// <summary>
    /// Create and register a new agent
    /// </summary>
    Task<IAgent> CreateAgent(string agentId, AgentConfig config);
    
    /// <summary>
    /// Get an existing agent by ID
    /// </summary>
    Task<IAgent?> GetAgent(string agentId);
    
    /// <summary>
    /// Delete an agent
    /// </summary>
    Task DeleteAgent(string agentId);
    
    /// <summary>
    /// List all available agents
    /// </summary>
    Task<IEnumerable<string>> ListAgents();
}

/// <summary>
/// Represents an AI agent capable of reasoning and acting
/// </summary>
public interface IAgent
{
    string AgentId { get; }
    
    /// <summary>
    /// Run the agent with a given prompt/task
    /// </summary>
    Task<AgentResponse> Run(string prompt, Dictionary<string, object>? context = null);
    
    /// <summary>
    /// Get available tools/skills for this agent
    /// </summary>
    IEnumerable<string> GetAvailableSkills();
    
    /// <summary>
    /// Bind a skill to this agent
    /// </summary>
    Task BindSkill(string skillId, ISkill skill);
}

/// <summary>
/// Registry for available skills/plugins
/// </summary>
public interface ISkillRegistry
{
    /// <summary>
    /// Register a new skill
    /// </summary>
    Task RegisterSkill(string skillId, ISkill skill);
    
    /// <summary>
    /// Get a skill by ID
    /// </summary>
    Task<ISkill?> GetSkill(string skillId);
    
    /// <summary>
    /// List all registered skills
    /// </summary>
    Task<IEnumerable<SkillMetadata>> ListSkills();
    
    /// <summary>
    /// List skills by category/type
    /// </summary>
    Task<IEnumerable<SkillMetadata>> ListSkillsByCategory(string category);
}

/// <summary>
/// A reusable skill/plugin that can be executed
/// </summary>
public interface ISkill
{
    string SkillId { get; }
    string Name { get; }
    string Description { get; }
    
    /// <summary>
    /// Execute the skill with given inputs
    /// </summary>
    Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null);
    
    /// <summary>
    /// Get input schema
    /// </summary>
    Dictionary<string, string>? GetInputSchema();
    
    /// <summary>
    /// Get output schema
    /// </summary>
    Dictionary<string, string>? GetOutputSchema();
}

/// <summary>
/// Configuration for agent creation
/// </summary>
public class AgentConfig
{
    public string AgentType { get; set; } = "StandardAgent";
    public string? SystemPrompt { get; set; }
    public string? Model { get; set; } = "gpt-4";
    public float Temperature { get; set; } = 0.7f;
    public int? MaxTokens { get; set; }
    public Dictionary<string, object>? CustomConfig { get; set; }
}

/// <summary>
/// Response from agent execution
/// </summary>
public class AgentResponse
{
    public required string Response { get; set; }
    public Dictionary<string, object>? Reasoning { get; set; }
    public Dictionary<string, object>? ToolCalls { get; set; }
    public long TokensUsed { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Metadata about a registered skill
/// </summary>
public class SkillMetadata
{
    public required string SkillId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? Category { get; set; }
    public string? Version { get; set; }
}

/// <summary>
/// Output from skill execution
/// </summary>
public class SkillOutput
{
    public required bool Success { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public string? ErrorMessage { get; set; }
}
