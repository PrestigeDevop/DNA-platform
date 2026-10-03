using DNAPlatform.AgentFramework;
using RealSkill = DNAPlatform.Skills.ISkill;
using RealSkillRegistry = DNAPlatform.Skills.ISkillRegistry;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// Bridges the DevUI API's lightweight <see cref="DNAPlatform.AgentFramework.ISkillRegistry"/>
/// contract to the real <see cref="DNAPlatform.Skills.ISkillRegistry"/> so the
/// AgentsController can bind real skills to in-memory agents.
/// </summary>
public class SkillRegistryAdapter : DNAPlatform.AgentFramework.ISkillRegistry
{
    private readonly RealSkillRegistry _inner;

    public SkillRegistryAdapter(RealSkillRegistry inner)
    {
        _inner = inner;
    }

    public async Task<DNAPlatform.AgentFramework.ISkill?> GetSkill(string skillId)
    {
        var skill = await _inner.GetSkill(skillId);
        return skill == null ? null : new SkillAdapter(skill);
    }

    public async Task<IEnumerable<DNAPlatform.AgentFramework.ISkill>> ListSkills()
    {
        var skills = await _inner.ListSkills();
        return skills.Select(m => (DNAPlatform.AgentFramework.ISkill)new MetadataSkill(m));
    }

    public Task RegisterSkill(string skillId, DNAPlatform.AgentFramework.ISkill skill)
        => Task.CompletedTask;
}

/// <summary>
/// Adapts a real skill to the API's lightweight skill contract.
/// </summary>
internal class SkillAdapter : DNAPlatform.AgentFramework.ISkill
{
    private readonly RealSkill _inner;

    public SkillAdapter(RealSkill inner)
    {
        _inner = inner;
    }

    public string SkillId => _inner.SkillId;
    public string Name => _inner.Name;
    public string Description => _inner.Description;

    public Dictionary<string, string>? GetInputSchema() => _inner.GetInputSchema();
    public Dictionary<string, string>? GetOutputSchema() => _inner.GetOutputSchema();

    public async Task<DNAPlatform.AgentFramework.SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        var result = await _inner.Execute(inputs);
        return new DNAPlatform.AgentFramework.SkillOutput
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Data = result.Data
        };
    }
}

/// <summary>
/// Metadata-only skill used when only the registry listing is available.
/// </summary>
internal class MetadataSkill : DNAPlatform.AgentFramework.ISkill
{
    public MetadataSkill(DNAPlatform.Skills.SkillMetadata metadata)
    {
        SkillId = metadata.SkillId;
        Name = metadata.Name;
        Description = metadata.Description;
    }

    public string SkillId { get; }
    public string Name { get; }
    public string Description { get; }

    public Dictionary<string, string>? GetInputSchema() => null;
    public Dictionary<string, string>? GetOutputSchema() => null;

    public Task<DNAPlatform.AgentFramework.SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        => throw new NotSupportedException("This skill entry is metadata-only.");
}
