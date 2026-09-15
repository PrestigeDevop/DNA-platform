using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DNAPlatform.Skills;
using DNAPlatform.DevUI.API.Services;
using DNAPlatform.DevUI.API.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DNA Platform - DevUI API",
        Version = "v2.0.0-beta",
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

// Register skill system (Phase 3 - Custom Skills)
builder.Services.AddSingleton<ISkillRegistry, SkillRegistry>();

// Register in-memory workflow store
builder.Services.AddSingleton<IWorkflowStore, InMemoryWorkflowStore>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DNA Platform - DevUI API v2.0");
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
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow, Version = "0.2.0-beta" });

// Node types / status values - consumed by the workflow designer (api.ts)
app.MapGet("/api/node-types", () => new
{
    NodeTypes = new[]
    {
        "Input", "Output", "Agent", "ProcessingSkill", "Conditional", "Loop", "SubWorkflow"
    }
});

app.MapGet("/api/status-values", () => new
{
    WorkflowStatus = new[] { "Pending", "Running", "Paused", "Completed", "Failed", "Cancelled" },
    NodeStatus = new[] { "Pending", "Waiting", "Running", "Completed", "Failed", "Skipped" }
});

// API info endpoint (api.ts -> getInfo)
app.MapGet("/api/info", () => new
{
    Name = "DNA Platform - DevUI API",
    Version = "0.2.0-beta",
    Description = "Low-Code/No-Code Bioinformatic Workflow Engine with Custom Skills",
    Swagger = "/swagger",
    Endpoints = new
    {
        Workflows = "/api/workflows",
        Executions = "/api/executions",
        Agents = "/api/agents",
        Skills = "/api/skills",
        Logs = "/api/logs",
        NodeTypes = "/api/node-types",
        StatusValues = "/api/status-values",
        Health = "/health"
    }
});

// -------- Log endpoints --------
// NOTE: /api/logs (GET / POST / DELETE) is served EXCLUSIVELY by LogsController.
// Do NOT add minimal-API mappings for the same route here - two handlers for one
// route cause an AmbiguousMatchException at request time.
//
//   GET    /api/logs?sinceId=0&limit=500  -> incremental poll (LogsController)
//   POST   /api/logs                      -> frontend log forwarding (LogsController)
//   DELETE /api/logs                      -> clear buffer (LogsController)

// Root - same payload as /api/info for convenience.
app.MapGet("/", () => Results.Redirect("/swagger"));

// -------- startup banner + skill registry dump --------
var skillRegistry = app.Services.GetRequiredService<ISkillRegistry>();
var registeredSkills = (await skillRegistry.ListSkills()).ToList();
var backendUrls = (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5254")
    .Split(';', StringSplitOptions.RemoveEmptyEntries);

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  DNA Platform - DevUI API v0.2.0-beta");
Console.WriteLine("  Low-Code/No-Code Bioinformatic Workflow Engine");
Console.WriteLine("  Phase 3: Custom Skills System Active");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();
foreach (var url in backendUrls)
{
    Console.WriteLine($"  Listening on:  {url}");
}
Console.WriteLine($"  Swagger UI:    {(backendUrls.FirstOrDefault() ?? "http://localhost:5254").TrimEnd('/')}/swagger");
Console.WriteLine($"  Health:        {(backendUrls.FirstOrDefault() ?? "http://localhost:5254").TrimEnd('/')}/health");
Console.WriteLine($"  Logs (web):    {(backendUrls.FirstOrDefault() ?? "http://localhost:5254").TrimEnd('/')}/api/logs");
Console.WriteLine();
Console.WriteLine($"  Registered skills ({registeredSkills.Count}):");
foreach (var skill in registeredSkills.OrderBy(s => s.Category).ThenBy(s => s.SkillId))
{
    Console.WriteLine($"    • {skill.SkillId,-22} {skill.Name}  [{skill.Category}]");
}
Console.WriteLine();
Console.WriteLine("  Request logging is ACTIVE - every /api call is printed below.");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

app.Run();
