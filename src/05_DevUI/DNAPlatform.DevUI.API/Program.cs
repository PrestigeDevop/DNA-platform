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

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

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
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow, Version = "0.2.0-beta" });

// API info endpoint
app.MapGet("/", () => new
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
        Health = "/health"
    }
});

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  DNA Platform - DevUI API v0.2.0-beta");
Console.WriteLine("  Low-Code/No-Code Bioinformatic Workflow Engine");
Console.WriteLine("  Phase 3: Custom Skills System Active");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine($"  Swagger UI:  https://localhost:5001/");
Console.WriteLine($"  Health:      https://localhost:5001/health");
Console.WriteLine($"  API Root:    https://localhost:5001/");
Console.WriteLine();

app.Run();
