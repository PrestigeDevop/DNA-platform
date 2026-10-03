using Microsoft.Extensions.Logging;
using DNAPlatform.AgentFramework;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5000);
    });

// Add services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add DNA Platform services
builder.Services.AddLogging(b => b.AddConsole());
builder.Services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
builder.Services.AddSingleton<INodeExecutor, NodeExecutor>();
builder.Services.AddSingleton<IAgentManager, AgentManager>();
builder.Services.AddSingleton<ISkillRegistry, SkillRegistry>();

var app = builder.Build();

app.UseCors();

// Root endpoint
app.MapGet("/", () => new
{
    message = "DNA Platform AGUI Server",
    version = "0.1.0",
    endpoints = new[]
    {
        "/health",
        "/workflows",
        "/workflows/{id}",
        "/workflows/{id}/execute",
        "/agents",
        "/skills"
    }
});

// Health check
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Get all workflows
app.MapGet("/workflows", () =>
{
    return new
    {
        workflows = new[]
        {
            new
            {
                id = "simple-workflow",
                name = "Simple Sequential Workflow",
                description = "Demonstrates basic node-to-node execution",
                nodeCount = 3
            },
            new
            {
                id = "parallel-workflow",
                name = "Parallel Agent Workflow",
                description = "Demonstrates parallel execution with agents",
                nodeCount = 3
            },
            new
            {
                id = "loop-workflow",
                name = "Dynamic Loop Workflow",
                description = "Demonstrates workflow with conditional loops",
                nodeCount = 3
            }
        }
    };
});

// Get workflow details
app.MapGet("/workflows/{id}", (string id) =>
{
    return id switch
    {
        "simple-workflow" => Results.Ok(new
        {
            id = "simple-workflow",
            name = "Simple Sequential Workflow",
            nodes = new[]
            {
                new { id = "node-1", name = "Data Input", type = "Input" },
                new { id = "node-2", name = "Transform Data", type = "ProcessingSkill" },
                new { id = "node-3", name = "Output Results", type = "Output" }
            },
            connections = new[]
            {
                new { source = "node-1", target = "node-2" },
                new { source = "node-2", target = "node-3" }
            }
        }),
        _ => Results.NotFound(new { error = "Workflow not found" })
    };
});

// Execute workflow
app.MapPost("/workflows/{id}/execute", async (string id, IWorkflowOrchestrator orchestrator) =>
{
    try
    {
        // Build and execute workflow
        var workflow = new Workflow
        {
            Id = id,
            Name = $"Workflow {id}",
            Nodes = new[]
            {
                new WorkflowNode { Id = "node-1", Name = "Step 1", NodeType = NodeType.Input },
                new WorkflowNode { Id = "node-2", Name = "Step 2", NodeType = NodeType.ProcessingSkill },
                new WorkflowNode { Id = "node-3", Name = "Step 3", NodeType = NodeType.Output }
            },
            Connections = new[]
            {
                new NodeConnection { SourceNodeId = "node-1", TargetNodeId = "node-2" },
                new NodeConnection { SourceNodeId = "node-2", TargetNodeId = "node-3" }
            }
        };

        var result = await orchestrator.ExecuteWorkflow(workflow);

        return Results.Ok(new
        {
            executionId = result.ExecutionId,
            status = result.Status.ToString(),
            duration = result.Duration.TotalMilliseconds,
            outputs = result.Outputs
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// List agents
app.MapGet("/agents", async (IAgentManager manager) =>
{
    var agents = await manager.ListAgents();
    return new
    {
        agents = agents.Select(id => new { id, status = "ready" }).ToList()
    };
});

// List skills
app.MapGet("/skills", async (ISkillRegistry registry) =>
{
    var skills = await registry.ListSkills();
    return new
    {
        skills = skills.Select(s => new
        {
            id = s.SkillId,
            name = s.Name,
            description = s.Description,
            category = s.Category ?? "general"
        }).ToList()
    };
});

app.Run();
