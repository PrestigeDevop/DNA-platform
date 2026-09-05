using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DNAPlatform.AgentFramework;

/// <summary>
/// Default implementation of agent management
/// </summary>
public class AgentManager : IAgentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _agents;
    private readonly ILogger<AgentManager> _logger;

    public AgentManager(ILogger<AgentManager> logger)
    {
        _agents = new ConcurrentDictionary<string, IAgent>();
        _logger = logger;
    }

    public Task<IAgent> CreateAgent(string agentId, AgentConfig config)
    {
        var agent = new DefaultAgent(agentId, config, _logger);
        _agents.TryAdd(agentId, agent);
        _logger.LogInformation("Agent created: {AgentId} ({AgentType})", agentId, config.AgentType);
        return Task.FromResult<IAgent>(agent);
    }

    public Task<IAgent?> GetAgent(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task DeleteAgent(string agentId)
    {
        _agents.TryRemove(agentId, out _);
        _logger.LogInformation("Agent deleted: {AgentId}", agentId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> ListAgents()
    {
        return Task.FromResult<IEnumerable<string>>(_agents.Keys);
    }
}

/// <summary>
/// Default implementation of an AI agent
/// </summary>
public class DefaultAgent : IAgent
{
    private readonly AgentConfig _config;
    private readonly ILogger<AgentManager> _logger;
    private readonly Dictionary<string, ISkill> _skills;

    public string AgentId { get; }

    public DefaultAgent(string agentId, AgentConfig config, ILogger<AgentManager> logger)
    {
        AgentId = agentId;
        _config = config;
        _logger = logger;
        _skills = new Dictionary<string, ISkill>();
    }

    public async Task<AgentResponse> Run(string prompt, Dictionary<string, object>? context = null)
    {
        _logger.LogInformation("Agent {AgentId} running with prompt: {Prompt}", AgentId, prompt);

        // Simulate agent reasoning and response
        await Task.Delay(50);

        return new AgentResponse
        {
            Response = $"Agent {AgentId} processed: {prompt}",
            Reasoning = new Dictionary<string, object>
            {
                { "model", _config.Model ?? "default" },
                { "temperature", _config.Temperature }
            },
            ToolCalls = new Dictionary<string, object>(),
            TokensUsed = 150
        };
    }

    public IEnumerable<string> GetAvailableSkills()
    {
        return _skills.Keys;
    }

    public Task BindSkill(string skillId, ISkill skill)
    {
        _skills[skillId] = skill;
        _logger.LogInformation("Skill bound to agent: {AgentId} <- {SkillId}", AgentId, skillId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Registry for managing available skills
/// </summary>
public class SkillRegistry : ISkillRegistry
{
    private readonly ConcurrentDictionary<string, ISkill> _skills;
    private readonly ILogger<SkillRegistry> _logger;

    public SkillRegistry(ILogger<SkillRegistry> logger)
    {
        _skills = new ConcurrentDictionary<string, ISkill>();
        _logger = logger;
        
        // Register built-in skills
        RegisterBuiltInSkills();
    }

    public Task RegisterSkill(string skillId, ISkill skill)
    {
        _skills.TryAdd(skillId, skill);
        _logger.LogInformation("Skill registered: {SkillId} - {SkillName}", skillId, skill.Name);
        return Task.CompletedTask;
    }

    public Task<ISkill?> GetSkill(string skillId)
    {
        _skills.TryGetValue(skillId, out var skill);
        return Task.FromResult(skill);
    }

    public Task<IEnumerable<SkillMetadata>> ListSkills()
    {
        var metadata = _skills.Values.Select(s => new SkillMetadata
        {
            SkillId = s.SkillId,
            Name = s.Name,
            Description = s.Description,
            Category = "General"
        }).ToList();

        return Task.FromResult<IEnumerable<SkillMetadata>>(metadata);
    }

    public Task<IEnumerable<SkillMetadata>> ListSkillsByCategory(string category)
    {
        var metadata = _skills.Values
            .Where(s => s.SkillId.Contains(category, StringComparison.OrdinalIgnoreCase))
            .Select(s => new SkillMetadata
            {
                SkillId = s.SkillId,
                Name = s.Name,
                Description = s.Description,
                Category = category
            }).ToList();

        return Task.FromResult<IEnumerable<SkillMetadata>>(metadata);
    }

    private void RegisterBuiltInSkills()
    {
        // Register some basic skills
        RegisterSkill("transform-data", new DataTransformSkill());
        RegisterSkill("validate-output", new ValidationSkill());
        RegisterSkill("merge-results", new MergeResultsSkill());
    }
}

/// <summary>
/// Built-in skill for data transformation
/// </summary>
public class DataTransformSkill : ISkill
{
    public string SkillId => "transform-data";
    public string Name => "Data Transform";
    public string Description => "Transforms data according to specified operations";

    public Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        return Task.FromResult(new SkillOutput
        {
            Success = true,
            Data = new Dictionary<string, object>
            {
                { "transformed", true },
                { "recordsProcessed", 1000 }
            }
        });
    }

    public Dictionary<string, string>? GetInputSchema()
    {
        return new Dictionary<string, string>
        {
            { "data", "object" },
            { "operation", "string" }
        };
    }

    public Dictionary<string, string>? GetOutputSchema()
    {
        return new Dictionary<string, string>
        {
            { "transformed", "boolean" },
            { "recordsProcessed", "integer" }
        };
    }
}

/// <summary>
/// Built-in skill for data validation
/// </summary>
public class ValidationSkill : ISkill
{
    public string SkillId => "validate-output";
    public string Name => "Validation";
    public string Description => "Validates data against specified criteria";

    public Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        return Task.FromResult(new SkillOutput
        {
            Success = true,
            Data = new Dictionary<string, object>
            {
                { "isValid", true },
                { "errors", new List<string>() }
            }
        });
    }

    public Dictionary<string, string>? GetInputSchema()
    {
        return new Dictionary<string, string>
        {
            { "data", "object" },
            { "schema", "object" }
        };
    }

    public Dictionary<string, string>? GetOutputSchema()
    {
        return new Dictionary<string, string>
        {
            { "isValid", "boolean" },
            { "errors", "array" }
        };
    }
}

/// <summary>
/// Built-in skill for merging results
/// </summary>
public class MergeResultsSkill : ISkill
{
    public string SkillId => "merge-results";
    public string Name => "Merge Results";
    public string Description => "Merges multiple result sets using specified strategy";

    public Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        return Task.FromResult(new SkillOutput
        {
            Success = true,
            Data = new Dictionary<string, object>
            {
                { "merged", true },
                { "recordsMerged", 2000 }
            }
        });
    }

    public Dictionary<string, string>? GetInputSchema()
    {
        return new Dictionary<string, string>
        {
            { "data1", "object" },
            { "data2", "object" },
            { "mergeStrategy", "string" }
        };
    }

    public Dictionary<string, string>? GetOutputSchema()
    {
        return new Dictionary<string, string>
        {
            { "merged", "boolean" },
            { "recordsMerged", "integer" }
        };
    }
}
