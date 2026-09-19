using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DNAPlatform.AgentFramework
{
    public class Workflow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<WorkflowNode> Nodes { get; set; } = new();
        public List<NodeConnection> Connections { get; set; } = new();
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WorkflowNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int NodeType { get; set; }
        public Dictionary<string, object> Config { get; set; } = new();
    }

    public class NodeConnection
    {
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public string OutputKey { get; set; } = string.Empty;
        public string InputKey { get; set; } = string.Empty;
    }

    public interface IWorkflowOrchestrator
    {
        Task<WorkflowExecutionResult> ExecuteWorkflow(Workflow workflow, Dictionary<string, object>? context = null);
        Task<WorkflowExecutionResult> GetExecution(string executionId);
        List<WorkflowExecutionResult> ListExecutions();
        Task PauseExecution(string executionId);
        Task ResumeExecution(string executionId);
        Task CancelExecution(string executionId);
    }

    public class WorkflowExecutionResult
    {
        public string Status { get; set; } = "Completed";
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Outputs { get; set; } = new();
    }

    public interface IAgentManager
    {
        Task<IEnumerable<AgentSummary>> ListAgents();
        Task<IAgent?> GetAgent(string agentId);
        Task<IAgent> CreateAgent(string agentId, AgentConfig config);
        Task DeleteAgent(string agentId);
    }

    public interface IAgent
    {
        string AgentId { get; }
        IEnumerable<string> GetAvailableSkills();
        Task<AgentResponse> Run(string prompt, Dictionary<string, object>? context = null);
        Task BindSkill(string skillId, ISkill skill);
    }

    public class AgentSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class AgentConfig
    {
        public string AgentType { get; set; } = "StandardAgent";
        public string? SystemPrompt { get; set; }
        public string Model { get; set; } = "gpt-4";
        public float Temperature { get; set; } = 0.7f;
        public int? MaxTokens { get; set; }
        public Dictionary<string, object>? CustomConfig { get; set; }
    }

    public class AgentResponse
    {
        public string Response { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
    }

    public interface ISkillRegistry
    {
        Task<ISkill?> GetSkill(string skillId);
        Task<IEnumerable<ISkill>> ListSkills();
        Task RegisterSkill(string skillId, ISkill skill);
    }

    public interface ISkill
    {
        string SkillId { get; }
        string Name { get; }
        string Description { get; }
        Dictionary<string, string>? GetInputSchema();
        Dictionary<string, string>? GetOutputSchema();
        Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null);
    }

    public class SkillOutput
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public enum NodeType
    {
        Input = 0,
        Agent = 1,
        ProcessingSkill = 2,
        Conditional = 3,
        Output = 4
    }

    public enum WorkflowStatus
    {
        Pending = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum NodeStatus
    {
        Pending = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Skipped = 4
    }

    public interface INodeExecutor
    {
        List<NodeType> GetSupportedNodeTypes();
    }
}