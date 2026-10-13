namespace DNAPlatform.AgentFramework;
/// <summary>Main orchestrator for workflow execution.</summary>
public interface IWorkflowOrchestrator
{
 Task<WorkflowExecutionResult> ExecuteWorkflow(Workflow workflow);
 Task<WorkflowExecutionResult> ExecuteWorkflow(Workflow workflow, Dictionary<string, object> executionContext);
 Task PauseExecution(string executionId);
 Task ResumeExecution(string executionId);
 Task CancelExecution(string executionId);
     Task<WorkflowExecution> GetExecution(string executionId);
    IEnumerable<WorkflowExecution> ListExecutions();
}

/// <summary>Executes individual nodes within a workflow.</summary>
public interface INodeExecutor
{
 Task<NodeExecution> ExecuteNode(WorkflowNode node, Dictionary<string, object>? inputs = null);
 Task<bool> ValidateNode(WorkflowNode node);
 IEnumerable<NodeType> GetSupportedNodeTypes();
}

/// <summary>Manages agent lifecycle and execution.</summary>
public interface IAgentManager
{
 /// <summary>Create a new agent.</summary>
 Task<IAgent> CreateAgent(string agentId, AgentConfig config);
 /// <summary>Get an existing agent.</summary>
 Task<IAgent?> GetAgent(string agentId);
 /// <summary>Delete an agent.</summary>
 Task DeleteAgent(string agentId);
 /// <summary>List all agents (summary).</summary>
 Task<IEnumerable<AgentSummary>> ListAgents();
}

/// <summary>Summary information about an agent.</summary>
public class AgentSummary
{
 public required string Id { get; set; }
 public required string Name { get; set; }
 public required string Type { get; set; }
 public string? Prompt { get; set; }
}

/// <summary>Represents an AI agent capable of reasoning.</summary>
public interface IAgent
{
 string AgentId { get; }
 string Name { get; }
 Task<AgentResponse> Run(string prompt, Dictionary<string, object>? context = null);
 IEnumerable<string> GetAvailableSkills();
 Task BindSkill(string skillId, ISkill skill);
}

/// <summary>Registry for available skills/plugins.</summary>
public interface ISkillRegistry
{
 Task RegisterSkill(string skillId, ISkill skill);
 Task<ISkill?> GetSkill(string skillId);
 Task<IEnumerable<SkillMetadata>> ListSkills();
 Task<IEnumerable<SkillMetadata>> ListSkillsByCategory(string category);
}

/// <summary>A reusable skill/plugin.</summary>
public interface ISkill
{
 string SkillId { get; }
 string Name { get; }
 string Description { get; }
 Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null);
 Dictionary<string, string>? GetInputSchema();
 Dictionary<string, string>? GetOutputSchema();
}

/// <summary>Configuration for agent creation.</summary>
public class AgentConfig
{
 public string Name { get; set; } = "";
 public string AgentType { get; set; } = "StandardAgent";
 public string? SystemPrompt { get; set; }
 public string? Model { get; set; } = "gpt-4";
 public float Temperature { get; set; } = 0.7f;
 public int? MaxTokens { get; set; }
 public Dictionary<string, object>? CustomConfig { get; set; }
}

/// <summary>Response from agent execution.</summary>
public class AgentResponse
{
 public required string Response { get; set; }
 public Dictionary<string, object>? Reasoning { get; set; }
 public Dictionary<string, object>? ToolCalls { get; set; }
 public long TokensUsed { get; set; }
 public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Metadata about a registered skill.</summary>
public class SkillMetadata
{
 public required string SkillId { get; set; }
 public required string Name { get; set; }
 public required string Description { get; set; }
 public string? Category { get; set; }
 public string? Version { get; set; }
}

/// <summary>Output from skill execution.</summary>
public class SkillOutput
{
 public required bool Success { get; set; }
 public Dictionary<string, object>? Data { get; set; }
 public string? ErrorMessage { get; set; }
}
