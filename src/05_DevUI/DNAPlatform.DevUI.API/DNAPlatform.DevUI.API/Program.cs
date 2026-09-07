using System.Text.Json;
using System.Collections.Concurrent;
using DNAPlatform.AgentFramework;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Accept enum values as strings in request binding (e.g. "nodeType": "ProcessingSkill")
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
builder.Services.AddSingleton<INodeExecutor, NodeExecutor>();
builder.Services.AddSingleton<IAgentManager, AgentManager>();
builder.Services.AddSingleton<ISkillRegistry, SkillRegistry>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevUI", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DevUI");

// In-memory workflow store (SvelteKit/Prisma handles persistent storage)
var _workflows = new ConcurrentDictionary<string, Workflow>();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow, platform = "DNA Platform DevUI" }));

app.MapGet("/api/skills", async (ISkillRegistry skillRegistry) =>
{
    var skills = await skillRegistry.ListSkills();
    return Results.Ok(new { skills = skills });
});

app.MapGet("/api/skills/{skillId}", async (string skillId, ISkillRegistry skillRegistry) =>
{
    var skill = await skillRegistry.GetSkill(skillId);
    if (skill == null)
        return Results.NotFound(new { error = $"Skill '{skillId}' not found" });
    return Results.Ok(new
    {
        skillId = skill.SkillId,
        name = skill.Name,
        description = skill.Description,
        inputSchema = skill.GetInputSchema(),
        outputSchema = skill.GetOutputSchema()
    });
});

app.MapPost("/api/skills/{skillId}/execute", async (string skillId, ISkillRegistry skillRegistry, JsonElement? inputs) =>
{
    var skill = await skillRegistry.GetSkill(skillId);
    if (skill == null)
        return Results.NotFound(new { error = $"Skill '{skillId}' not found" });
    Dictionary<string, object>? inputDict = null;
    if (inputs.HasValue)
        inputDict = JsonSerializer.Deserialize<Dictionary<string, object>>(inputs.Value.GetRawText());
    var result = await skill.Execute(inputDict);
    return Results.Ok(result);
});

app.MapGet("/api/node-types", (INodeExecutor nodeExecutor) =>
{
    var nodeTypes = nodeExecutor.GetSupportedNodeTypes();
    return Results.Ok(nodeTypes.Select(nt => new
    {
        name = nt.ToString(),
        value = (int)nt,
        description = nt switch
        {
            NodeType.Input => "Data input node (sources)",
            NodeType.Agent => "Agent-based processing with AI reasoning",
            NodeType.ProcessingSkill => "Deterministic skill-based processing",
            NodeType.Conditional => "Conditional branching logic",
            NodeType.Output => "Data output node (sinks)",
            NodeType.Loop => "Loop/iteration control",
            NodeType.SubWorkflow => "Subworkflow reference",
            _ => "Unknown"
        }
    }));
});

app.MapGet("/api/status-values", () =>
{
    return Results.Ok(new
    {
        workflowStatus = Enum.GetNames(typeof(WorkflowStatus)),
        nodeStatus = Enum.GetNames(typeof(NodeStatus))
    });
});

app.MapPost("/api/workflows/execute", async (IWorkflowOrchestrator orchestrator, Workflow workflow) =>
{
    if (workflow.Nodes == null || !workflow.Nodes.Any())
        return Results.BadRequest(new { error = "Workflow must contain at least one node" });
    var result = await orchestrator.ExecuteWorkflow(workflow);
    return Results.Ok(result);
});

app.MapPost("/api/workflows/execute-with-context", async (IWorkflowOrchestrator orchestrator, WorkflowExecutionRequest request) =>
{
    if (request.Workflow == null || request.Workflow.Nodes == null || !request.Workflow.Nodes.Any())
        return Results.BadRequest(new { error = "Workflow must contain at least one node" });
    var result = await orchestrator.ExecuteWorkflow(request.Workflow, request.Context ?? new Dictionary<string, object>());
    return Results.Ok(result);
});

app.MapGet("/api/executions/{executionId}", async (string executionId, IWorkflowOrchestrator orchestrator) =>
{
    try
    {
        var execution = await orchestrator.GetExecution(executionId);
        return Results.Ok(execution);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Execution '{executionId}' not found" });
    }
});

app.MapGet("/api/executions", (IWorkflowOrchestrator orchestrator) =>
{
    var executions = orchestrator.ListExecutions().ToList();
    return Results.Ok(new { executions = executions });
});

app.MapGet("/api/agents", async (IAgentManager agentManager) =>
{
    var agents = await agentManager.ListAgents();
    return Results.Ok(new { agents = agents });
});

app.MapPost("/api/agents", async (IAgentManager agentManager, AgentConfig config) =>
{
    var agent = await agentManager.CreateAgent(Guid.NewGuid().ToString(), config);
            return Results.Created($"/api/agents/{agent.AgentId}", new { agentId = agent.AgentId, name = config.Name, type = config.AgentType, skills = agent.GetAvailableSkills() });
});

app.MapDelete("/api/agents/{agentId}", async (string agentId, IAgentManager agentManager) =>
{
    var agent = await agentManager.GetAgent(agentId);
    if (agent == null)
        return Results.NotFound(new { error = $"Agent '{agentId}' not found" });
    await agentManager.DeleteAgent(agentId);
    return Results.Ok(new { success = true, message = $"Agent '{agentId}' deleted" });
});

app.MapGet("/api/info", () =>
{
    return Results.Ok(new
    {
        platform = "DNA Platform",
        version = "0.2.0-alpha",
        description = "Low-Code/No-Code Bioinformatic Workflow Engine",
        features = new[] { "Topological DAG execution", "Parallel node processing", "Agent-driven workflows", "Extensible skill system", "REST API", "DevUI web designer" },
        endpoints = new[] { "GET /", "GET /health", "GET /api/info", "GET /api/skills", "GET /api/skills/{id}", "POST /api/skills/{id}/execute", "GET /api/node-types", "GET /api/status-values", "POST /api/workflows/execute", "POST /api/workflows/execute-with-context", "GET /api/executions", "GET /api/executions/{id}", "GET /api/agents", "POST /api/agents", "DELETE /api/agents/{id}" }
    });
});

app.Run();

public class WorkflowExecutionRequest
{
    public required Workflow Workflow { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}
