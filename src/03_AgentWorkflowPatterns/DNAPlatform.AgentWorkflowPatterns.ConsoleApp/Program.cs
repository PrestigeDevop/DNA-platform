using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DNAPlatform.AgentFramework;
using System;

namespace DNAPlatform.AgentWorkflowPatterns.ConsoleApp;

/// <summary>
/// Agent Workflow Patterns - Console application demonstrating
/// the DNA Platform's agent-driven workflow execution model
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  DNA Platform - Agent Workflow Patterns");
        Console.WriteLine("  Low-Code/No-Code Bioinformatic Workflow Engine");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        using (serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("Starting DNA Platform Agent Workflow System");
                
                // Initialize core components
                var workflowOrchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();
                
                // Example 1: Simple sequential workflow
                await RunSimpleWorkflow(workflowOrchestrator, logger);
                
                // Example 2: Parallel workflow with agents
                await RunParallelAgentWorkflow(workflowOrchestrator, logger);
                
                // Example 3: Dynamic workflow generation
                await RunDynamicWorkflow(workflowOrchestrator, logger);

                logger.LogInformation("All examples completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Application error");
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Core services
        services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
        services.AddSingleton<INodeExecutor, NodeExecutor>();
        services.AddSingleton<IAgentManager, AgentManager>();
        services.AddSingleton<ISkillRegistry, SkillRegistry>();
    }

    private static async Task RunSimpleWorkflow(IWorkflowOrchestrator orchestrator, ILogger<Program> logger)
    {
        Console.WriteLine("\n📋 Example 1: Simple Sequential Workflow");
        Console.WriteLine("─────────────────────────────────────────");
        
        var workflow = new Workflow 
        { 
            Id = "simple-workflow",
            Name = "Simple Sequential Workflow",
            Description = "Demonstrates basic node-to-node execution"
        };

        var node1 = new WorkflowNode 
        { 
            Id = "node-1",
            Name = "Data Input",
            NodeType = NodeType.Input,
            Config = new Dictionary<string, object> { { "source", "sample.csv" } }
        };

        var node2 = new WorkflowNode 
        { 
            Id = "node-2",
            Name = "Transform Data",
            NodeType = NodeType.ProcessingSkill,
            Config = new Dictionary<string, object> { { "operation", "normalize" } }
        };

        var node3 = new WorkflowNode 
        { 
            Id = "node-3",
            Name = "Output Results",
            NodeType = NodeType.Output,
            Config = new Dictionary<string, object> { { "destination", "results.csv" } }
        };

        workflow.Nodes = new[] { node1, node2, node3 };
        workflow.Connections = new[]
        {
            new NodeConnection { SourceNodeId = "node-1", TargetNodeId = "node-2", OutputKey = "data" },
            new NodeConnection { SourceNodeId = "node-2", TargetNodeId = "node-3", OutputKey = "transformed" }
        };

        logger.LogInformation("Executing workflow: {WorkflowName}", workflow.Name);
        var result = await orchestrator.ExecuteWorkflow(workflow);
        
        Console.WriteLine($"✓ Workflow completed with status: {result.Status}");
        Console.WriteLine($"  Duration: {result.Duration.TotalMilliseconds:F0}ms");
    }

    private static async Task RunParallelAgentWorkflow(IWorkflowOrchestrator orchestrator, ILogger<Program> logger)
    {
        Console.WriteLine("\n🤖 Example 2: Parallel Agent Workflow");
        Console.WriteLine("─────────────────────────────────────");
        
        var workflow = new Workflow 
        { 
            Id = "parallel-agent-workflow",
            Name = "Parallel Agent Processing",
            Description = "Demonstrates parallel execution with agents making decisions"
        };

        // Analysis node 1
        var analyzeNode1 = new WorkflowNode 
        { 
            Id = "analyze-1",
            Name = "AI Analysis Branch A",
            NodeType = NodeType.Agent,
            Config = new Dictionary<string, object> 
            { 
                { "agentType", "AnalysisAgent" },
                { "prompt", "Analyze genomic sequence patterns" },
                { "parallelizable", true }
            }
        };

        // Analysis node 2
        var analyzeNode2 = new WorkflowNode 
        { 
            Id = "analyze-2",
            Name = "AI Analysis Branch B",
            NodeType = NodeType.Agent,
            Config = new Dictionary<string, object> 
            { 
                { "agentType", "AnalysisAgent" },
                { "prompt", "Perform statistical validation" },
                { "parallelizable", true }
            }
        };

        // Merge results
        var mergeNode = new WorkflowNode 
        { 
            Id = "merge",
            Name = "Merge Results",
            NodeType = NodeType.ProcessingSkill,
            Config = new Dictionary<string, object> { { "mergeStrategy", "consensus" } }
        };

        workflow.Nodes = new[] { analyzeNode1, analyzeNode2, mergeNode };
        workflow.Connections = new[]
        {
            new NodeConnection { SourceNodeId = "analyze-1", TargetNodeId = "merge", OutputKey = "analysis1" },
            new NodeConnection { SourceNodeId = "analyze-2", TargetNodeId = "merge", OutputKey = "analysis2" }
        };

        logger.LogInformation("Executing parallel workflow: {WorkflowName}", workflow.Name);
        var result = await orchestrator.ExecuteWorkflow(workflow);
        
        Console.WriteLine($"✓ Parallel workflow completed with status: {result.Status}");
        Console.WriteLine($"  Parallel nodes executed: 2");
        Console.WriteLine($"  Total duration: {result.Duration.TotalMilliseconds:F0}ms");
    }

    private static async Task RunDynamicWorkflow(IWorkflowOrchestrator orchestrator, ILogger<Program> logger)
    {
        Console.WriteLine("\n🔄 Example 3: Dynamic Workflow Generation");
        Console.WriteLine("──────────────────────────────────────────");
        
        var workflow = new Workflow 
        { 
            Id = "dynamic-workflow",
            Name = "Dynamic Loop Workflow",
            Description = "Demonstrates workflow with conditional loops"
        };

        var initNode = new WorkflowNode 
        { 
            Id = "init",
            Name = "Initialize Iterator",
            NodeType = NodeType.ProcessingSkill
        };

        var loopNode = new WorkflowNode 
        { 
            Id = "loop",
            Name = "Process Item",
            NodeType = NodeType.Agent,
            Config = new Dictionary<string, object> 
            { 
                { "isLoopBody", true },
                { "iterationKey", "items" }
            }
        };

        var aggregateNode = new WorkflowNode 
        { 
            Id = "aggregate",
            Name = "Aggregate Results",
            NodeType = NodeType.ProcessingSkill
        };

        workflow.Nodes = new[] { initNode, loopNode, aggregateNode };
        workflow.Connections = new[]
        {
            new NodeConnection { SourceNodeId = "init", TargetNodeId = "loop" },
            new NodeConnection { SourceNodeId = "loop", TargetNodeId = "aggregate" }
        };

        logger.LogInformation("Executing dynamic workflow: {WorkflowName}", workflow.Name);
        var result = await orchestrator.ExecuteWorkflow(workflow);
        
        Console.WriteLine($"✓ Dynamic workflow completed with status: {result.Status}");
        Console.WriteLine($"  Iterations completed: 1");
    }
}
