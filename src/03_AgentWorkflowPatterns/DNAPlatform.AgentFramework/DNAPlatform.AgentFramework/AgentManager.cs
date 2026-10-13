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

        public Task<IEnumerable<AgentSummary>> ListAgents()
    {
        var summaries = _agents.Select(kvp => new AgentSummary
        {
            Id = kvp.Key,
            Name = (kvp.Value as DefaultAgent)?.Name ?? kvp.Value.AgentId,
            Type = (kvp.Value as DefaultAgent)?.AgentType ?? kvp.Value.GetType().Name,
            Prompt = (kvp.Value as DefaultAgent)?.SystemPrompt
        }).ToList();
        return Task.FromResult<IEnumerable<AgentSummary>>(summaries);
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
    public string Name { get; private set; }
    public string AgentType { get; private set; }
    public string? SystemPrompt { get; private set; }

    public DefaultAgent(string agentId, AgentConfig config, ILogger<AgentManager> logger)
    {
        AgentId = agentId;
        Name = config.Name;
        AgentType = config.AgentType;
        SystemPrompt = config.SystemPrompt;
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


