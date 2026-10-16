using Microsoft.Extensions.DependencyInjection;
using RealSkillRegistry = DNAPlatform.Skills.ISkillRegistry;

namespace DNAPlatform.AgentFramework;

/// <summary>
/// Adapts the real <see cref="RealSkillRegistry"/> in DNAPlatform.Skills � which
/// self-registers the built-in and bioinformatics skills � to the engine's own
/// <see cref="ISkillRegistry"/> contract.
///
/// <para>
/// The two interfaces differ: the engine's has no <c>ExecuteSkill</c>, returns
/// bare <see cref="ISkill"/> objects and carries its own <see cref="SkillMetadata"/>
/// shape. So this is an adapter, not a reimplementation. There is still exactly one
/// catalogue; the hard-coded <c>SkillRegistry</c> that used to live in
/// <c>AgentManager.cs</c> has been deleted.
/// </para>
/// </summary>
public class SkillRegistryAdapter : ISkillRegistry
{
    private readonly RealSkillRegistry _inner;

    public SkillRegistryAdapter(RealSkillRegistry inner)
    {
        _inner = inner;
    }

    public async Task<ISkill?> GetSkill(string skillId)
    {
        var skill = await _inner.GetSkill(skillId);
        return skill == null ? null : new SkillAdapter(skill);
    }

    public async Task<IEnumerable<SkillMetadata>> ListSkills()
    {
        var skills = await _inner.ListSkills();
        return skills.Select(ToEngineMetadata).ToList();
    }

    public async Task<IEnumerable<SkillMetadata>> ListSkillsByCategory(string category)
    {
        var skills = await _inner.ListSkillsByCategory(category);
        return skills.Select(ToEngineMetadata).ToList();
    }

    /// <summary>
    /// No-op: skills are discovered by self-registration into
    /// <see cref="RealSkillRegistry"/> at construction, not pushed in at runtime.
    /// </summary>
    public Task RegisterSkill(string skillId, ISkill skill) => Task.CompletedTask;

    private static SkillMetadata ToEngineMetadata(DNAPlatform.Skills.SkillMetadata source) => new()
    {
        SkillId = source.SkillId,
        Name = source.Name,
        Description = source.Description,
        Category = source.Category,
        Version = source.Version
    };
}

/// <summary>
/// Adapts a real skill to the engine's skill contract.
/// </summary>
internal class SkillAdapter : ISkill
{
    private readonly DNAPlatform.Skills.ISkill _inner;

    public SkillAdapter(DNAPlatform.Skills.ISkill inner)
    {
        _inner = inner;
    }

    public string SkillId => _inner.SkillId;
    public string Name => _inner.Name;
    public string Description => _inner.Description;

    public Dictionary<string, string>? GetInputSchema() => _inner.GetInputSchema();
    public Dictionary<string, string>? GetOutputSchema() => _inner.GetOutputSchema();

    public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        var result = await _inner.Execute(inputs);
        return new SkillOutput
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Data = result.Data
        };
    }
}

/// <summary>
/// One place that registers the engine, so the DevUI API, the AGUI server, the
/// ConsoleApp demo and the tests cannot drift apart (docs/refactor.md �15).
///
/// <para>
/// <b>Why this exists.</b> The DevUI API used to bind a second, in-memory
/// orchestrator alongside this one. That orchestrator echoed a fixed string per
/// node and ignored <c>Workflow.Connections</c> entirely, so nothing ever flowed
/// along an edge and every node "succeeded" regardless of what it was given. Now
/// the engine selection lives here and a test asserts what hosts actually bind.
/// </para>
///
/// <para>
/// Callers must have already called <c>AddLogging()</c>; the engine takes
/// <c>ILogger<T></c>.
/// </para>
/// </summary>
public static class AgentFrameworkRegistration
{
    /// <summary>
    /// Registers the real skill catalogue and the single implementation of each
    /// engine service. <paramref name="nodeExecutor"/> defaults to
    /// <see cref="SkillBackedNodeExecutor"/>, which routes ProcessingSkill nodes
    /// to registered skills and passes every other node type through.
    /// </summary>
    public static IServiceCollection AddAgentFramework(
        this IServiceCollection services,
        Func<IServiceProvider, INodeExecutor>? nodeExecutor = null)
    {
        services.AddSingleton<RealSkillRegistry, DNAPlatform.Skills.SkillRegistry>();
        services.AddSingleton<ISkillRegistry, SkillRegistryAdapter>();

        if (nodeExecutor is null)
        {
            services.AddSingleton<INodeExecutor, SkillBackedNodeExecutor>();
        }
        else
        {
            services.AddSingleton(nodeExecutor);
        }

        services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
        services.AddSingleton<IAgentManager, AgentManager>();

        return services;
    }
}
