# Quick Reference - DNA Platform

## Building & Running

```bash
# Build all projects
dotnet build

# Run console demo
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run

# Run tests
dotnet test

# Build specific project
dotnet build src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework
```

## Core Classes

### Workflow Definition
```csharp
var workflow = new Workflow 
{
    Id = "my-workflow",
    Name = "My Workflow",
    Nodes = new[] { node1, node2 },
    Connections = new[] { connection }
};
```

### Creating Nodes
```csharp
// Input node
var inputNode = new WorkflowNode 
{
    Id = "input",
    Name = "Input Data",
    NodeType = NodeType.Input,
    Config = new Dictionary<string, object> { { "source", "data.csv" } }
};

// Agent node
var agentNode = new WorkflowNode 
{
    Id = "agent",
    Name = "AI Analysis",
    NodeType = NodeType.Agent,
    Config = new Dictionary<string, object> 
    { 
        { "agentType", "AnalysisAgent" },
        { "prompt", "Analyze this data" }
    }
};

// Skill node
var skillNode = new WorkflowNode 
{
    Id = "skill",
    Name = "Transform",
    NodeType = NodeType.ProcessingSkill,
    Config = new Dictionary<string, object> { { "operation", "normalize" } }
};

// Output node
var outputNode = new WorkflowNode 
{
    Id = "output",
    Name = "Save Results",
    NodeType = NodeType.Output,
    Config = new Dictionary<string, object> { { "destination", "results.csv" } }
};
```

### Connecting Nodes
```csharp
var connection = new NodeConnection 
{
    SourceNodeId = "input",
    TargetNodeId = "agent",
    OutputKey = "data",        // Data field from source node
    InputKey = "input_data"    // Data field for target node
};
```

### Executing Workflows
```csharp
var orchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();

// Simple execution
var result = await orchestrator.ExecuteWorkflow(workflow);

// Execution with context
var context = new Dictionary<string, object> { { "userId", "user123" } };
var result = await orchestrator.ExecuteWorkflow(workflow, context);

// Check results
if (result.Status == WorkflowStatus.Completed)
{
    Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
    foreach (var output in result.Outputs)
    {
        Console.WriteLine($"{output.Key}: {output.Value}");
    }
}
else if (result.Status == WorkflowStatus.Failed)
{
    foreach (var error in result.Errors ?? new List<ExecutionError>())
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
```

## Service Registration

```csharp
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add core services
services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
services.AddSingleton<INodeExecutor, NodeExecutor>();
services.AddSingleton<IAgentManager, AgentManager>();
services.AddSingleton<ISkillRegistry, SkillRegistry>();

var sp = services.BuildServiceProvider();
```

## Writing Custom Skills

```csharp
public class MySkill : ISkill
{
    public string SkillId => "my-skill";
    public string Name => "My Custom Skill";
    public string Description => "Does something useful";

    public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        try 
        {
            // Do work here
            
            return new SkillOutput
            {
                Success = true,
                Data = new Dictionary<string, object> { { "result", "value" } }
            };
        }
        catch (Exception ex)
        {
            return new SkillOutput
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Dictionary<string, string>? GetInputSchema()
    {
        return new Dictionary<string, string> { { "input", "string" } };
    }

    public Dictionary<string, string>? GetOutputSchema()
    {
        return new Dictionary<string, string> { { "result", "string" } };
    }
}

// Register skill
var registry = sp.GetRequiredService<ISkillRegistry>();
await registry.RegisterSkill("my-skill", new MySkill());
```

## Node Types Reference

| Type | Description | Config Keys |
|------|-------------|------------|
| `Input` | Data source | `source` (filepath) |
| `Output` | Data sink | `destination` (filepath) |
| `Agent` | AI reasoning | `agentType`, `prompt`, `model` |
| `ProcessingSkill` | Deterministic operation | `skillId`, `operation` |
| `Conditional` | Branching | `condition` (expression) |
| `Loop` | Iteration | `iterationKey` (field name) |
| `SubWorkflow` | Nested workflow | `workflowId` (ref) |

## Execution Status Values

- `Pending` - Not started
- `Running` - Currently executing
- `Paused` - Paused by user
- `Completed` - Finished successfully
- `Failed` - Error occurred
- `Cancelled` - Cancelled by user

## Node Status Values

- `Pending` - Awaiting execution
- `Running` - Currently executing
- `Completed` - Finished successfully
- `Failed` - Execution error
- `Skipped` - Skipped (condition not met)
- `Waiting` - Waiting on dependencies

## Common Patterns

### Sequential Processing
```
Node1 (Input) → Node2 (Process) → Node3 (Output)
```

### Parallel Processing
```
         ├→ Node2a (Agent)
Node1 →  ├→ Node2b (Agent)  → Node3 (Merge)
         └→ Node2c (Agent)
```

### Conditional Branching
```
Node1 → Node2 (Conditional) ├→ Node3 (if true)
                            └→ Node4 (if false)
```

### Loop Processing
```
Node1 (Init) → Node2 (Loop Body) ─→ Node3 (Aggregate)
              └─ repeat n times ─┘
```

## Debugging Tips

### Enable Debug Logging
```csharp
builder.SetMinimumLevel(LogLevel.Debug);
```

### Inspect Execution Details
```csharp
var full = result.FullExecution;
foreach (var (nodeId, exec) in full.NodeExecutions)
{
    Console.WriteLine($"{nodeId}: {exec.Status} ({exec.Duration?.TotalMilliseconds}ms)");
    if (exec.Error != null)
        Console.WriteLine($"  Error: {exec.Error.Message}");
}
```

### Check Node Outputs
```csharp
if (result.Outputs != null)
{
    foreach (var kvp in result.Outputs)
    {
        Console.WriteLine($"{kvp.Key} = {kvp.Value}");
    }
}
```

## File Locations

- **Core Framework**: `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework/`
- **Console Demo**: `src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp/`
- **Tests**: `test/DNAPlatform.Tests/`
- **Documentation**: Root directory (ARCHITECTURE.md, DEVELOPMENT.md)

## Key Interfaces

- `IWorkflowOrchestrator` - Main orchestration service
- `INodeExecutor` - Individual node execution
- `IAgentManager` - Agent lifecycle management
- `ISkillRegistry` - Skill registration & lookup
- `IAgent` - AI agent contract
- `ISkill` - Reusable skill contract

## Environment Setup

```bash
# Prerequisites
dotnet --version              # Should be 8.0+
git --version                 # Should be 2.x+

# First time setup
git clone <repo>
cd DNA-platform/prestigedevop-bioinformatic-workflow-platform
dotnet restore
dotnet build
```

## Useful Commands

```bash
# Run demo
dotnet run --project src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp

# Build specific project
dotnet build src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Clean all build artifacts
dotnet clean

# List all projects
dotnet sln list
```

## Next Steps

1. Read [ARCHITECTURE.md](./ARCHITECTURE.md) for system design
2. Read [DEVELOPMENT.md](./DEVELOPMENT.md) for examples
3. Run the console demo to see it in action
4. Create your first workflow
5. Write a custom skill
6. Check out the roadmap for upcoming features
