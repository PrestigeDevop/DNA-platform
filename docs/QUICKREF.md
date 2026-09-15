# Quick Reference - DNA Platform

## ⚡ Current Polyglot Reality (2026-09-06)

**The system is now polyglot: SvelteKit frontend + .NET backend.** 

### System Architecture
```
┌──────────────────────────────┐   HTTP Proxy    ┌─────────────────────────────┐
│  DevUI (SvelteKit, port 5173) │ ◄────────────► │  Backend API (.NET Core,     │
│                               │   server.ts     │  port 5254)                 │
│  • CRUD operations            │────────────────│  • Workflow execution        │
│  • UI / Validation            │                │  • Skill registry            │
└──────────────────────────────┘                 └─────────────────────────────┘
         │                                             │
         ▼                                             ▼
    Prisma + SQLite (dev)                        Local in-memory (prod demo)
    Workflow | Execution tables                  NodeExecutor, AgentManager
```

### Key Differences from "Legacy" Docs
| Legacy Assumption | Current Reality |
|-------------------|-----------------|
| Monolithic app with all routes | Split: DevUI + Backend API via HTTP proxy |
| All compute in one service | SvelteKit (CRUD) ↔ .NET (compute) |
| Direct DB access from frontend | Frontend uses SvelteKit; DB is backend only |
| Console demo = complete system | Console = compute-only; use DevUI for full stack |

### Getting Started Today
```bash
# Build everything
dotnet build

# Run console demo (compute-only, no persistence)
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run

# For full experience: start DevUI separately (see DEVELOPMENT.md)
cd src/05_DevUI
npm install
npm run dev
```

### API Endpoints (Backend only - port 5254)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/swagger` | Swagger/OpenAPI UI |
| POST | `/api/workflows/execute` | Execute workflow |
| GET | `/api/workflows/{id}` | Get workflow details |
| (CRUD via DevUI) | - | SvelteKit handles create/read/update/delete |

### Important Constraints
- ✅ Workflows stored as **JSON strings** in SQLite (not native graph DB)  
- ✅ **HTTP proxy calls** for inter-service communication (no sockets/gRPC)  
- ✅ Console demo works standalone; DevUI provides full experience  
- ❌ Don't use legacy route assumptions (e.g., direct browser → `/api/...`)  

---

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

### Custom Skill Snippet (hello-world .NET example)

For the polyglot kernel / custom snippet flow, you can create a snippet that the backend executes as a .NET endpoint and that becomes a designer node with editable IO.

**Example snippet payload (JSON):**
```json
{
  "name": "Hello World .NET",
  "runtime": "PolyglotKernel",
  "description": "Echoes a string to the backend console and returns a JSON result",
  "inputs": [
    { "name": "name", "label": "Name", "type": "string", "required": true, "defaultValue": "World" }
  ],
  "outputs": [
    { "name": "result", "label": "Result", "type": "string" }
  ],
  "action": { "kind": "dotnet-hello-world" }
}
```

**Backend execution behavior (hello-world):**
- Input: `name` (string)
- Console: `Console.WriteLine($"Hello {name} from .NET (polyglot kernel placeholder)")`
- Output: `{ "result": "Hello {name} from .NET (polyglot kernel placeholder)", "timestamp": "..." }`

**Create snippet (SvelteKit server route):**
```bash
curl -X POST http://localhost:5173/api/snippets \
  -H "Content-Type: application/json" \
  -d '{ "name": "Hello World .NET", "runtime": "PolyglotKernel", "inputs": [{"name":"name","label":"Name","type":"string","required":true}], "outputs": [{"name":"result","label":"Result","type":"string"}], "action": {"kind":"dotnet-hello-world"} }'
```

**Execute snippet (hello-world):**
```bash
curl -X POST http://localhost:5254/api/snippets/{snippetId}/execute \
  -H "Content-Type: application/json" \
  -d '{ "name": "DNA Platform" }'
```

### Drag a Custom Snippet Node into the Designer

Once a custom snippet is created, it appears in the palette on the `/workflows` designer page. Dragging it onto the canvas creates a node whose IO ports come from the snippet’s `inputs` and `outputs`.

**Example:**
- Snippet “Hello World .NET” has input `name` (string) and output `result` (string)
- Drag it onto the canvas
- Click the node → inspect/edit panel shows `name` (editable) and `result` (output)
- When the workflow runs, the node executes the backend snippet endpoint with the node’s inputs

> **Note:** The snippet metadata (name, inputs, outputs, runtime, action) is static at registration time. The node data you edit in the canvas is the per-instance data used when the workflow executes the snippet.

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
