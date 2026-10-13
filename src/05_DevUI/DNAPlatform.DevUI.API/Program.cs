using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using DNAPlatform.AgentFramework;
using DNAPlatform.Skills;
using DNAPlatform.DevUI.API.Services;
using DNAPlatform.DevUI.API.Controllers;
using DNAPlatform.DevUI.API.Services.Mcp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Workflow/node enums must bind from and serialize to strings so the
        // SvelteKit client can send node types like "Input" and read statuses
        // like "Completed" without numeric mapping.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Minimal APIs do NOT inherit the MVC JSON options above — they use their own
// JsonOptions instance. Without this, POST /api/workflows/execute fails to bind
// "nodeType": "Input" and returns a bodyless 400, because minimal APIs swallow
// binding errors and hand the handler null.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Surface minimal-API binding failures as a problem-details 400 instead of a
// bodyless one, so a malformed request is diagnosable rather than looking like a
// rejected empty workflow.
builder.Services.Configure<RouteHandlerOptions>(options =>
{
    options.ThrowOnBadRequest = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DNA Platform - DevUI API",
        Version = "0.3.0-alpha",
        Description = "Low-Code/No-Code Bioinformatic Workflow Engine - REST API with Custom Skills",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "DNA Platform",
            Url = new Uri("https://github.com/PrestigeDevop/DNA-platform")
        }
    });
});

// Add CORS for Svelte frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging - console provider first so CLI output keeps working
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// In-memory log buffer shared by the API and the web log viewer.
var logStore = new InMemoryLogStore(capacity: 2000);
builder.Services.AddSingleton<ILogStore>(logStore);

// Mirror every ILogger record into the buffer so the web Logs page shows the
// exact same lines as the CLI console.
builder.Logging.AddProvider(new LogStoreLoggerProvider(logStore));

// SSE log stream service - broadcasts new log entries to subscribers
builder.Services.AddSingleton<LogStreamService>();

// MCP server discovery - scans for .json manifests and registers tools as skills
builder.Services.Configure<McpDiscoveryOptions>(options =>
{
    var projectRoot = AppContext.BaseDirectory;
    // Navigate up from bin/Debug/net8.0 to project root
    var root = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", "..", ".."));
    options.ManifestDirectory = Path.Combine(root, "mcp-servers");
});
builder.Services.AddHostedService<McpServerDiscovery>();

// The skill system (Phase 3) plus the whole workflow engine. AddAgentFramework is
// the single registration point shared with the AGUI server, the ConsoleApp and
// the tests, so every host binds the same orchestrator, node executor, agent
// manager and skill catalogue (docs/refactor.md section 15). Previously this bound
// a second InMemory* orchestrator whose nodes echoed their input and ignored
// Workflow.Connections entirely.
builder.Services.AddAgentFramework();

// Register in-memory workflow store
builder.Services.AddSingleton<IWorkflowStore, InMemoryWorkflowStore>();

// Environment/mise introspection for the workflow Configure view (docs/refactor.md
// section 4). Shells out to the mise CLI and degrades to a yellow status when
// mise is missing or unresponsive, so /api/environment never fails.
// NOTE: EnvironmentController owns /api/environment - do NOT add a MapGet for it.
builder.Services.AddSingleton<IMiseService, MiseService>();

var app = builder.Build();

// Single source of truth for the version reported by /health, /api/info and the
// startup banner. Keep README.md and docs/TODO.md in step with this value.
const string PlatformVersion = "0.3.0-alpha";

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DNA Platform - DevUI API v0.3.0-alpha");
        // Mounted under /swagger so the root "/" info endpoint stays reachable.
        c.RoutePrefix = "swagger";
    });
}

// NOTE: UseHttpsRedirection is intentionally NOT enabled in Development.
// The Vite dev proxy targets plain http://127.0.0.1:5254 and an HTTPS redirect
// produces noisy "Failed to determine the https port for redirect" warnings.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// Log every /api + /health request to the console AND the in-memory buffer.
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow, Version = PlatformVersion });

// Node types / status values - consumed by the workflow designer (api.ts)
app.MapGet("/api/node-types", () => new
{
    NodeTypes = new[]
    {
        "Input", "Value", "Output", "Agent", "ProcessingSkill", "Log", "Conditional", "Loop", "SubWorkflow"
    }
});

app.MapGet("/api/status-values", () => new
{
    WorkflowStatus = new[] { "Pending", "Running", "Paused", "Completed", "Failed", "Cancelled" },
    NodeStatus = new[] { "Pending", "Waiting", "Running", "Completed", "Failed", "Skipped" }
});

// Ad-hoc workflow execution - runs a workflow definition supplied in the request
// body without persisting it first. Used by the designer (api.ts -> executeWorkflow)
// and by the SvelteKit /api/workflows/{id}/execute route, which loads a stored
// workflow from Prisma and forwards its definition here.
app.MapPost("/api/workflows/execute", async (Workflow workflow, IWorkflowOrchestrator orchestrator) =>
{
    if (workflow is null || workflow.Nodes is null || !workflow.Nodes.Any())
    {
        return Results.BadRequest(new { Error = "Workflow must contain at least one node" });
    }

    var result = await orchestrator.ExecuteWorkflow(workflow);
    return Results.Ok(result);
});

// API info endpoint (api.ts -> getInfo)
app.MapGet("/api/info", () => new
{
    Name = "DNA Platform - DevUI API",
    Version = PlatformVersion,
    Description = "Low-Code/No-Code Bioinformatic Workflow Engine with Custom Skills",
    Swagger = "/swagger",
    Endpoints = new
    {
        Workflows = "/api/workflows",
        Executions = "/api/executions",
        Agents = "/api/agents",
        Skills = "/api/skills",
        Snippets = "/api/snippets",
        Logs = "/api/logs",
        LogsStream = "/api/logs/stream",
        NodeTypes = "/api/node-types",
        StatusValues = "/api/status-values",
        Health = "/health"
    }
});

// Snippet execution endpoint (hello-world .NET handler + future polyglot runtime placeholder)
app.MapPost("/api/snippets/{id}/execute", (string id, [FromBody] Dictionary<string, object>? inputs, ILogger<Program> logger) =>
{
    // NOTE: real snippet persistence/metadata lookup will happen via the Prisma-backed
    // /api/snippets CRUD route on the frontend side. Here we implement the concrete
    // hello-world .NET handler and leave an obvious extension point for other runtimes.
    var action = new Dictionary<string, object>();
    try
    {
        // If callers pass an explicit action payload, prefer it; otherwise treat this
        // as the hello-world handler.
        if (inputs != null && inputs.TryGetValue("action", out var a) && a != null)
            action = a as Dictionary<string, object> ?? new Dictionary<string, object>();
    }
    catch
    {
        action = new Dictionary<string, object>();
    }

    var kind = action.TryGetValue("kind", out var k) && k != null
        ? k.ToString()
        : (action.TryGetValue("kind", out var _2) ? "dotnet-hello-world" : "dotnet-hello-world");

    if (string.Equals(kind, "dotnet-hello-world", StringComparison.OrdinalIgnoreCase))
    {
        // hello-world: take first input string, console write it, return JSON.
        string inputText = "World";
        foreach (var (key, value) in inputs ?? new Dictionary<string, object>())
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                inputText = s;
                break;
            }
            if (value != null)
            {
                inputText = value.ToString() ?? "World";
                break;
            }
        }

        logger.LogInformation("Snippet '{Id}' hello-world executing with input: {Input}", id, inputText);
        Console.WriteLine($"[Snippet {id}] Hello {inputText} from .NET (polyglot kernel placeholder)");

        return Results.Ok(new
        {
            Success = true,
            SnippetId = id,
            Data = new Dictionary<string, object>
            {
                { "result", $"Hello {inputText} from .NET (polyglot kernel placeholder)" },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            },
            ExecutedAt = DateTime.UtcNow
        });
    }

    return Results.BadRequest(new
    {
        Success = false,
        Error = $"Runtime/action not implemented yet: {kind}. Only 'dotnet-hello-world' is supported for now."
    });
});

// -------- Log endpoints --------
// NOTE: /api/logs (GET / POST / DELETE) is served EXCLUSIVELY by LogsController.
// Do NOT add minimal-API mappings for the same route here - two handlers for one
// route cause an AmbiguousMatchException at request time.
//
//   GET    /api/logs?sinceId=0&limit=500  -> incremental poll (LogsController)
//   POST   /api/logs                      -> frontend log forwarding (LogsController)
//   DELETE /api/logs                      -> clear buffer (LogsController)
//
// NEW: SSE stream endpoint for real-time log updates
app.MapGet("/api/logs/stream", async (HttpContext context, LogStreamService logStream, [FromQuery] long sinceId = 0) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    var reader = logStream.Subscribe(sinceId);
    var cancellationToken = context.RequestAborted;

    // Heartbeat: send a 15s SSE comment line to prevent proxies from buffering.
    // SSE comment lines start with ":" and are ignored by the browser.
    _ = Task.Run(async () =>
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken);
                await context.Response.WriteAsync(": keepalive\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
    }, cancellationToken);

    try
    {
        await foreach (var entry in reader.ReadAllAsync(cancellationToken))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(entry);
            await context.Response.WriteAsync($"event: log\n");
            await context.Response.WriteAsync($"data: {json}\n\n");
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        // Client disconnected
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error in log stream");
    }
});

// Root - same payload as /api/info for convenience.
app.MapGet("/", () => Results.Redirect("/swagger"));

// -------- startup banner + skill registry dump --------
var skillRegistry = app.Services.GetRequiredService<DNAPlatform.Skills.ISkillRegistry>();
var registeredSkills = (await skillRegistry.ListSkills()).ToList();
var backendUrls = (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5254")
    .Split(';', StringSplitOptions.RemoveEmptyEntries);

Console.WriteLine("-----------------------------------------------------------");
Console.WriteLine($"  DNA Platform - DevUI API v{PlatformVersion}");
Console.WriteLine("  Low-Code/No-Code Bioinformatic Workflow Engine");
Console.WriteLine("  Phase 3: Custom Skills System Active");
Console.WriteLine("-----------------------------------------------------------");
Console.WriteLine();
foreach (var url in backendUrls)
{
    Console.WriteLine($"  Listening on:  {url}");
}
Console.WriteLine($"  Swagger UI:    {(backendUrls.FirstOrDefault() ?? "http://localhost:5254").TrimEnd('/')}/swagger");
Console.WriteLine($"  Health:        {(backendUrls.FirstOrDefault() ?? "http://localhost:5254").TrimEnd('/')}/health");
Console.WriteLine($"  Logs (web):    {(backendUrls.FirstOrDefault() ?? "http://localhost:5254").TrimEnd('/')}/api/logs");
Console.WriteLine($"  Logs (SSE):    {(backendUrls.FirstOrDefault() ?? "http://localhost:5254").TrimEnd('/')}/api/logs/stream");
Console.WriteLine();
Console.WriteLine($"  Registered skills ({registeredSkills.Count}):");
foreach (var skill in registeredSkills.OrderBy(s => s.Category).ThenBy(s => s.SkillId))
{
    Console.WriteLine($"    • {skill.SkillId,-22} {skill.Name}  [{skill.Category}]");
}
Console.WriteLine();
Console.WriteLine("  Request logging is ACTIVE - every /api call is printed below.");
Console.WriteLine("-----------------------------------------------------------");
Console.WriteLine();

app.Run();
