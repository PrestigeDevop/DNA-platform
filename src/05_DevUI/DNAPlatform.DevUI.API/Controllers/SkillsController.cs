using Microsoft.AspNetCore.Mvc;
using DNAPlatform.Skills;

namespace DNAPlatform.DevUI.API.Controllers;

/// <summary>
/// API controller for skill registry management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SkillsController : ControllerBase
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(
        ISkillRegistry skillRegistry,
        ILogger<SkillsController> logger)
    {
        _skillRegistry = skillRegistry;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/skills - List all registered skills
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var skills = await _skillRegistry.ListSkills();
        return Ok(new { Count = skills.Count(), Skills = skills });
    }

    /// <summary>
    /// GET /api/skills/{skillId} - Get skill details
    /// </summary>
    [HttpGet("{skillId}")]
    public async Task<IActionResult> GetById(string skillId)
    {
        var skill = await _skillRegistry.GetSkill(skillId);
        if (skill == null)
        {
            return NotFound(new { Message = $"Skill '{skillId}' not found" });
        }

        return Ok(skill.GetMetadata());
    }

    /// <summary>
    /// GET /api/skills/category/{category} - List skills by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var skills = await _skillRegistry.ListSkillsByCategory(category);
        return Ok(new { Count = skills.Count(), Category = category, Skills = skills });
    }

    /// <summary>
    /// POST /api/skills/{skillId}/execute - Execute a skill directly
    /// This is the main endpoint for GUI-triggered skill execution
    /// </summary>
    [HttpPost("{skillId}/execute")]
    public async Task<IActionResult> ExecuteSkill(string skillId, [FromBody] Dictionary<string, object>? inputs = null)
    {
        var skill = await _skillRegistry.GetSkill(skillId);
        if (skill == null)
        {
            return NotFound(new { Message = $"Skill '{skillId}' not found" });
        }

        try
        {
            _logger.LogInformation("Executing skill: {SkillId} with inputs: {@Inputs}", skillId, inputs);
            var result = await _skillRegistry.ExecuteSkill(skillId, inputs);

            if (!result.Success)
            {
                return BadRequest(new { Success = false, Error = result.ErrorMessage });
            }

            return Ok(new
            {
                Success = true,
                SkillId = skillId,
                Data = result.Data,
                ExecutedAt = result.ExecutedAt,
                DurationMs = result.DurationMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skill execution failed: {SkillId}", skillId);
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}
