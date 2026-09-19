using Microsoft.AspNetCore.Mvc;
using DNAPlatform.AgentFramework;

namespace DNAPlatform.DevUI.API.Controllers;

/// <summary>
/// API controller for AI agent management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AgentsController : ControllerBase
{
    private readonly IAgentManager _agentManager;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        IAgentManager agentManager,
        ISkillRegistry skillRegistry,
        ILogger<AgentsController> logger)
    {
        _agentManager = agentManager;
        _skillRegistry = skillRegistry;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/agents - List all agents
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var agents = await _agentManager.ListAgents();
        return Ok(new { Count = agents.Count(), AgentIds = agents });
    }

    /// <summary>
    /// GET /api/agents/{agentId} - Get agent details
    /// </summary>
    [HttpGet("{agentId}")]
    public async Task<IActionResult> GetById(string agentId)
    {
        var agent = await _agentManager.GetAgent(agentId);
        if (agent == null)
        {
            return NotFound(new { Message = $"Agent '{agentId}' not found" });
        }

        return Ok(new
        {
            AgentId = agent.AgentId,
            AvailableSkills = agent.GetAvailableSkills()
        });
    }

    /// <summary>
    /// POST /api/agents - Create a new agent
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgentRequest request)
    {
        try
        {
            var config = new AgentConfig
            {
                AgentType = request.AgentType ?? "StandardAgent",
                SystemPrompt = request.SystemPrompt,
                Model = request.Model ?? "gpt-4",
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                CustomConfig = request.CustomConfig
            };

            var agent = await _agentManager.CreateAgent(request.AgentId, config);
            _logger.LogInformation("Agent created: {AgentId} ({AgentType})", request.AgentId, config.AgentType);

            return CreatedAtAction(nameof(GetById), new { agentId = agent.AgentId }, new
            {
                AgentId = agent.AgentId,
                AvailableSkills = agent.GetAvailableSkills()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent");
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/agents/{agentId} - Delete an agent
    /// </summary>
    [HttpDelete("{agentId}")]
    public async Task<IActionResult> Delete(string agentId)
    {
        var agent = await _agentManager.GetAgent(agentId);
        if (agent == null)
        {
            return NotFound(new { Message = $"Agent '{agentId}' not found" });
        }

        await _agentManager.DeleteAgent(agentId);
        _logger.LogInformation("Agent deleted: {AgentId}", agentId);
        return Ok(new { Message = $"Agent '{agentId}' deleted" });
    }

    /// <summary>
    /// POST /api/agents/{agentId}/run - Run an agent with a prompt
    /// </summary>
    [HttpPost("{agentId}/run")]
    public async Task<IActionResult> RunAgent(string agentId, [FromBody] RunAgentRequest request)
    {
        var agent = await _agentManager.GetAgent(agentId);
        if (agent == null)
        {
            return NotFound(new { Message = $"Agent '{agentId}' not found" });
        }

        try
        {
            var response = await agent.Run(request.Prompt, request.Context);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent run failed: {AgentId}", agentId);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/agents/{agentId}/bind-skill - Bind a skill to an agent
    /// </summary>
    [HttpPost("{agentId}/bind-skill")]
    public async Task<IActionResult> BindSkill(string agentId, [FromBody] BindSkillRequest request)
    {
        var agent = await _agentManager.GetAgent(agentId);
        if (agent == null)
        {
            return NotFound(new { Message = $"Agent '{agentId}' not found" });
        }

        var skill = await _skillRegistry.GetSkill(request.SkillId);
        if (skill == null)
        {
            return NotFound(new { Message = $"Skill '{request.SkillId}' not found" });
        }

        await agent.BindSkill(request.SkillId, skill);
        _logger.LogInformation("Skill bound: Agent {AgentId} <- {SkillId}", agentId, request.SkillId);

        return Ok(new
        {
            Message = $"Skill '{request.SkillId}' bound to agent '{agentId}'",
            AvailableSkills = agent.GetAvailableSkills()
        });
    }
}

/// <summary>
/// Request model for creating an agent
/// </summary>
public class CreateAgentRequest
{
    public required string AgentId { get; set; }
    public string? AgentType { get; set; }
    public string? SystemPrompt { get; set; }
    public string? Model { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public int? MaxTokens { get; set; }
    public Dictionary<string, object>? CustomConfig { get; set; }
}

/// <summary>
/// Request model for running an agent
/// </summary>
public class RunAgentRequest
{
    public required string Prompt { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Request model for binding a skill to an agent
/// </summary>
public class BindSkillRequest
{
    public required string SkillId { get; set; }
}
