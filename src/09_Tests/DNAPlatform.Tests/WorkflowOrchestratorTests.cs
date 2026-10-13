using DNAPlatform.AgentFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DNAPlatform.Tests;

/// <summary>
/// Characterisation tests for the orchestrator the DevUI API actually binds.
///
/// Context: <c>refactor.md</c> §15 forbids duplicate execution engines. Before
/// the reconciliation this API bound <c>InMemoryWorkflowOrchestrator</c>, which
/// echoed a fixed string per node and never looked at <c>Workflow.Connections</c>
/// — so a node's output was never carried into its downstream node. These tests
/// exercise the production DI graph (see
/// <c>DNAPlatform.DevUI.API.Services.AgentFrameworkRegistration</c>) rather than a
/// hand-built container, so they fail if anyone rebinds the engine back to a
/// non-propagating implementation.
/// </summary>
public class WorkflowOrchestratorTests
{
    private static IWorkflowOrchestrator ResolveApiOrchestrator()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        AgentFrameworkRegistration.AddAgentFramework(services);

        return services.BuildServiceProvider().GetRequiredService<IWorkflowOrchestrator>();
    }

    private static Workflow TwoNodeChain(Dictionary<string, object>? sinkConfig = null)
    {
        var isSkillNode = sinkConfig is not null && sinkConfig.ContainsKey("skillId");

        return new Workflow
        {
            Id = "wf-chain",
            Name = "Two node chain",
            Nodes = new List<WorkflowNode>
            {
                new() { Id = "n1", Name = "Source", NodeType = NodeType.Input },
                new()
                {
                    Id = "n2",
                    Name = "Sink",
                    NodeType = isSkillNode ? NodeType.ProcessingSkill : NodeType.Output,
                    Config = sinkConfig
                }
            },
            Connections = new List<NodeConnection>
            {
                new() { SourceNodeId = "n1", TargetNodeId = "n2", OutputKey = "output", InputKey = "inherited" }
            }
        };
    }

    [Fact]
    public async Task ExecutesEveryNodeInAConnectedDag()
    {
        var orchestrator = ResolveApiOrchestrator();

        var result = await orchestrator.ExecuteWorkflow(TwoNodeChain());

        Assert.Equal("Completed", result.Status.ToString());
        Assert.Equal(2, result.FullExecution!.NodeExecutions!.Count);
    }

    [Fact]
    public async Task PropagatesUpstreamOutputIntoTheDownstreamNode()
    {
        var orchestrator = ResolveApiOrchestrator();

        var result = await orchestrator.ExecuteWorkflow(TwoNodeChain());

        // The connection maps n1's "output" onto n2's "inherited" input, so the
        // downstream node must be able to see it in what it received.
        var sink = result.FullExecution!.NodeExecutions!["n2"];
        Assert.NotNull(sink.Inputs);
        Assert.True(
            sink.Inputs!.ContainsKey("inherited"),
            "Downstream node received no input from its upstream node: the engine is not propagating data along connections.");
    }

    [Fact]
    public async Task ReportsPerNodeStatusAndTiming()
    {
        var orchestrator = ResolveApiOrchestrator();

        var result = await orchestrator.ExecuteWorkflow(TwoNodeChain());

        foreach (var pair in result.FullExecution!.NodeExecutions!)
        {
            Assert.Equal(NodeStatus.Completed, pair.Value.Status);
            Assert.NotNull(pair.Value.StartedAt);
            Assert.NotNull(pair.Value.CompletedAt);
        }
    }

    [Fact]
    public async Task FailsRatherThanHangsOnACycle()
    {
        var orchestrator = ResolveApiOrchestrator();

        var cyclic = new Workflow
        {
            Id = "wf-cycle",
            Name = "Cyclic",
            Nodes = new List<WorkflowNode>
            {
                new() { Id = "a", Name = "A", NodeType = NodeType.ProcessingSkill },
                new() { Id = "b", Name = "B", NodeType = NodeType.ProcessingSkill }
            },
            Connections = new List<NodeConnection>
            {
                new() { SourceNodeId = "a", TargetNodeId = "b" },
                new() { SourceNodeId = "b", TargetNodeId = "a" }
            }
        };

        var result = await orchestrator.ExecuteWorkflow(cyclic);

        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.NotEmpty(result.Errors!);
    }

    [Fact]
    public async Task ProcessingSkillNodeRunsTheNamedSkillAndFailsLoudlyOnAnUnknownOne()
    {
        var orchestrator = ResolveApiOrchestrator();

        // "data-transform" requires a `data` input, which the node's own config
        // supplies; `inherited` arrives over the edge from the upstream node.
        var withKnownSkill = TwoNodeChain(new Dictionary<string, object>
        {
            ["skillId"] = "data-transform",
            ["data"] = new List<string> { " Alpha ", "Beta", "Alpha" }
        });
        var known = await orchestrator.ExecuteWorkflow(withKnownSkill);
        Assert.Equal(WorkflowStatus.Completed, known.Status);

        var sinkExecution = known.FullExecution!.NodeExecutions!["n2"];
        Assert.Contains("inherited", sinkExecution.Inputs!.Keys);
        Assert.Contains("skillId", sinkExecution.Outputs!.Keys);
        // normalize+dedupe-free run still proves the skill really executed
        Assert.Equal(3, Convert.ToInt32(sinkExecution.Outputs["count"]));

        // An unknown skill id must surface as a Failed workflow with an error, not
        // as a node that quietly "succeeds" with a fabricated output.
        var withUnknownSkill = TwoNodeChain(new Dictionary<string, object> { ["skillId"] = "no-such-skill" });
        var unknown = await orchestrator.ExecuteWorkflow(withUnknownSkill);
        Assert.Equal(WorkflowStatus.Failed, unknown.Status);
        Assert.NotEmpty(unknown.Errors!);
    }

    [Fact]
    public async Task TracksEveryRunSoStatusCanBeQueriedLater()
    {
        var orchestrator = ResolveApiOrchestrator();

        var result = await orchestrator.ExecuteWorkflow(TwoNodeChain());

        Assert.Contains(orchestrator.ListExecutions(), e => e.ExecutionId == result.ExecutionId);
        var fetched = await orchestrator.GetExecution(result.ExecutionId);
        Assert.Equal(result.ExecutionId, fetched.ExecutionId);
    }
}

/// <summary>
/// The API must resolve exactly one agent-manager and one orchestrator. A second
/// binding would reintroduce the duplicate-engine split §15 forbids.
/// </summary>
public class EngineRegistrationTests
{
    [Fact]
    public void ApiGraphResolvesTheSharedAgentManager()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AgentFrameworkRegistration.AddAgentFramework(services);

        using var provider = services.BuildServiceProvider();

        var manager = provider.GetRequiredService<IAgentManager>();
        Assert.IsType<AgentManager>(manager);
    }

    [Fact]
    public void NodeExecutorResolvesToTheSharedEngineImplementation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AgentFrameworkRegistration.AddAgentFramework(services);

        using var provider = services.BuildServiceProvider();

        var executor = provider.GetRequiredService<INodeExecutor>();
        Assert.IsType<SkillBackedNodeExecutor>(executor);    }
}
