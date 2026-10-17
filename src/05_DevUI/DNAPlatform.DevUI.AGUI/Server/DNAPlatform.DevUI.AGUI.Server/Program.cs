using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DNAPlatform.AgentFramework;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add DNA Platform services.
// AddAgentFramework is the single registration point shared with DevUI API and tests.
builder.Services.AddLogging(b => b.AddConsole());
builder.Services.AddAgentFramework();

var app = builder.Build();
app.UseCors();

// Root endpoint - Platform & Protocol Info
app.MapGet("/", () => new
{
    message = "DNA Platform AG-UI Hosting Server",
    version = "1.0.0",
    protocol = "AG-UI (Agent User Interface Protocol)",
    protocolSpecs = "https://docs.ag-ui.com/sdk/dotnet/hosting/overview",
    features = new[]
    {
        "Human-in-the-Loop (HITL) Tool Approvals",
        "Agent Thinking / Reasoning Streams (THINKING_DELTA)",
        "Bi-directional State Synchronization (STATE_SNAPSHOT & STATE_DELTA)",
        "Server-Sent Events (SSE) Streaming",
        "Framework-Agnostic Hosting (net8.0 compatible)",
        "DNA Platform AgentFramework & SkillRegistry Integration"
    },
    endpoints = new[]
    {
        "/health",
        "/agui",
        "/agui/run",
        "/workflows",
        "/workflows/{id}",
        "/workflows/{id}/execute",
        "/agents",
        "/skills"
    }
});

// Health check
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// AG-UI Protocol Discovery endpoint (GET /agui)
app.MapGet("/agui", () => Results.Ok(new
{
    protocol = "AG-UI",
    version = "1.0.0",
    server = "DNA Platform .NET AG-UI Server (net8.0)",
    referenceDocs = "https://docs.ag-ui.com/sdk/dotnet/hosting/overview",
    frameworkAgnostic = true,
    capabilities = new
    {
        sseStreaming = true,
        humanInTheLoop = true,
        thinkingTraces = true,
        toolApprovals = true,
        stateSynchronization = true,
        clientToolExecution = true
    },
    supportedEvents = new[]
    {
        "RUN_STARTED",
        "THINKING_DELTA",
        "TEXT_DELTA",
        "APPROVAL_REQUESTED",
        "TOOL_CALL_STARTED",
        "TOOL_CALL_FINISHED",
        "STATE_SNAPSHOT",
        "STATE_DELTA",
        "RUN_FINISHED",
        "ERROR"
    },
    sensitiveToolsRequiringApproval = new[]
    {
        "sequence-mutation",
        "pipeline-execution",
        "filesystem-write",
        "external-api-call"
    }
}));

// Workflows metadata
app.MapGet("/workflows", () => new
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
});

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

// =========================================================================
// AG-UI Protocol Integration Endpoint with Human-in-the-Loop (HITL)
// Specifications: https://docs.ag-ui.com/sdk/dotnet/hosting/overview
// Microsoft Learn: https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/ui/ag-ui/?pivots=programming-language-csharp
// =========================================================================
app.MapPost("/agui/run", async (HttpContext context, ISkillRegistry registry) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");

    // Read and parse RunAgentInput from request body
    using var reader = new StreamReader(context.Request.Body);
    var bodyText = await reader.ReadToEndAsync();
    
    var runInput = ParseRunAgentInput(bodyText);
    var runId = string.IsNullOrEmpty(runInput.RunId) ? $"run_{Guid.NewGuid():N}" : runInput.RunId;
    var threadId = string.IsNullOrEmpty(runInput.ThreadId) ? $"thread_{Guid.NewGuid():N}" : runInput.ThreadId;

    async Task SendEventAsync(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n");
        await context.Response.Body.FlushAsync();
    }

    try
    {
        // 1. RUN_STARTED
        await SendEventAsync("RUN_STARTED", new
        {
            runId,
            threadId,
            timestamp = DateTime.UtcNow,
            agent = runInput.AgentId ?? "dna-copilot-bioinformatician"
        });

        // 2. Initial STATE_SNAPSHOT
        var sharedState = runInput.State ?? new Dictionary<string, object>();
        sharedState["status"] = "in_progress";
        sharedState["startedAt"] = DateTime.UtcNow.ToString("o");
        await SendEventAsync("STATE_SNAPSHOT", new
        {
            runId,
            state = sharedState
        });

        // 3. THINKING_DELTA (Agent Reasoning / Inner monologue)
        await SendEventAsync("THINKING_DELTA", new
        {
            runId,
            thought = "Analyzing input prompt and inspecting available DNA platform skills...\n"
        });

        await SendEventAsync("THINKING_DELTA", new
        {
            runId,
            thought = "Identified requested operations: Retrieve system time via C# clock, and inspect sequence alignment.\n"
        });

        // Check if there are approvals provided from a previous interrupt
        var existingApprovals = runInput.Approvals ?? new List<AgUiApprovalResponse>();
        var sensitiveToolName = "sequence-mutation";
        var isSensitiveActionRequested = runInput.RequireApproval || (runInput.Prompt?.Contains("mutate", StringComparison.OrdinalIgnoreCase) ?? false);

        // 4. Autonomous Safe Tool Execution: C# Current Time Skill
        var timeToolCallId = $"call_{Guid.NewGuid():N}";
        await SendEventAsync("TOOL_CALL_STARTED", new
        {
            runId,
            toolCallId = timeToolCallId,
            toolName = "current-time",
            arguments = new { timezone = "UTC", precision = "high" }
        });

        var timeSkillResult = await registry.ExecuteSkill("current-time");
        await SendEventAsync("TOOL_CALL_FINISHED", new
        {
            runId,
            toolCallId = timeToolCallId,
            result = timeSkillResult.Data ?? new { timestamp = DateTime.UtcNow.ToString("o") }
        });

        // 5. HUMAN-IN-THE-LOOP (HITL) INTERRUPT / APPROVAL FLOW
        if (isSensitiveActionRequested)
        {
            var approvalForAction = existingApprovals.FirstOrDefault(a => a.ToolName == sensitiveToolName || a.ToolCallId.Contains("mutation"));
            
            if (approvalForAction == null)
            {
                // No approval yet: AG-UI INTERRUPT / APPROVAL_REQUESTED
                var approvalCallId = $"call_mutation_{Guid.NewGuid():N}";
                await SendEventAsync("THINKING_DELTA", new
                {
                    runId,
                    thought = "Sensitive operation detected ('sequence-mutation'). Pausing execution for Human-in-the-Loop approval.\n"
                });

                await SendEventAsync("APPROVAL_REQUESTED", new
                {
                    runId,
                    threadId,
                    toolCallId = approvalCallId,
                    toolName = sensitiveToolName,
                    reason = "Modifying genome sequence base pairs requires operator approval to prevent unintended reading frame shifts.",
                    arguments = new
                    {
                        targetSequence = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC",
                        mutationType = "PointMutation_Glu6Val",
                        position = 6
                    },
                    stateSnapshot = sharedState
                });

                // Update state snapshot to waiting
                sharedState["status"] = "waiting_for_approval";
                await SendEventAsync("STATE_SNAPSHOT", new { runId, state = sharedState });

                await SendEventAsync("RUN_FINISHED", new
                {
                    runId,
                    threadId,
                    status = "WAITING_FOR_APPROVAL",
                    timestamp = DateTime.UtcNow,
                    pendingApprovalId = approvalCallId
                });

                return; // Interrupt complete, awaiting user response
            }
            else if (!approvalForAction.Approved)
            {
                // Operator rejected
                await SendEventAsync("THINKING_DELTA", new
                {
                    runId,
                    thought = $"Operator rejected the approval for {sensitiveToolName}. Reason: {approvalForAction.Reason ?? "Cancelled by user"}.\n"
                });

                await SendEventAsync("TEXT_DELTA", new
                {
                    runId,
                    delta = "Action rejected by operator. Execution halted safely without modifying sequence data.\n"
                });

                sharedState["status"] = "rejected_by_operator";
                await SendEventAsync("STATE_SNAPSHOT", new { runId, state = sharedState });

                await SendEventAsync("RUN_FINISHED", new
                {
                    runId,
                    threadId,
                    status = "REJECTED",
                    timestamp = DateTime.UtcNow
                });
                return;
            }
            else
            {
                // Operator APPROVED! Resume execution
                await SendEventAsync("THINKING_DELTA", new
                {
                    runId,
                    thought = "Human approval received. Proceeding with sequence mutation step...\n"
                });

                await SendEventAsync("TOOL_CALL_STARTED", new
                {
                    runId,
                    toolCallId = approvalForAction.ToolCallId,
                    toolName = sensitiveToolName,
                    arguments = new { status = "approved_by_operator" }
                });

                await SendEventAsync("TOOL_CALL_FINISHED", new
                {
                    runId,
                    toolCallId = approvalForAction.ToolCallId,
                    result = new
                    {
                        status = "Executed",
                        resultSequence = "ATGGTGCACCTGACTCCTGTGGAGAAGTCTGCC",
                        mutationApplied = "A->T at codon 6 (Sickle Cell variant HbS)",
                        approvedAt = DateTime.UtcNow
                    }
                });
            }
        }

        // 6. STATE_DELTA: Synchronize incremental state changes via JSON Patch
        await SendEventAsync("STATE_DELTA", new
        {
            runId,
            patch = new[]
            {
                new { op = "replace", path = "/status", value = "completed" },
                new { op = "add", path = "/completedAt", value = DateTime.UtcNow.ToString("o") }
            }
        });

        // 7. TEXT_DELTA: Stream assistant response text
        var responseText = "Analysis complete. DNA Platform AG-UI server processed the agent task with real-time state synchronization, tool invocation, and human-in-the-loop verification.";
        foreach (var word in responseText.Split(' '))
        {
            await SendEventAsync("TEXT_DELTA", new { runId, delta = word + " " });
            await Task.Delay(20);
        }

        // 8. RUN_FINISHED
        await SendEventAsync("RUN_FINISHED", new
        {
            runId,
            threadId,
            status = "COMPLETED",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        await SendEventAsync("ERROR", new
        {
            runId,
            threadId,
            error = ex.Message
        });
    }
});

// Helper model parsing for RunAgentInput
static AgUiRunInput ParseRunAgentInput(string bodyText)
{
    if (string.IsNullOrWhiteSpace(bodyText))
        return new AgUiRunInput();

    try
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<AgUiRunInput>(bodyText, options) ?? new AgUiRunInput();
    }
    catch
    {
        return new AgUiRunInput();
    }
}

app.Run();

// Protocol Type Definitions
public class AgUiRunInput
{
    public string? ThreadId { get; set; }
    public string? RunId { get; set; }
    public string? AgentId { get; set; }
    public string? Prompt { get; set; }
    public bool RequireApproval { get; set; }
    public Dictionary<string, object>? State { get; set; }
    public List<AgUiApprovalResponse>? Approvals { get; set; }
}

public class AgUiApprovalResponse
{
    public string ToolCallId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string? Reason { get; set; }
}
