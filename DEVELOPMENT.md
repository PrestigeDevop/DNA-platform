# Development Guide - DNA Platform

## Prerequisites

- **.NET 8.0+** (tested with .NET 10)
- **Git** for version control
- **Visual Studio Code** or **Visual Studio 2022+** (recommended)
- **Node.js 18+** (for DevUI later)

## Project Setup

### 1. Clone & Initial Build

```bash
git clone <repo-url>
cd DNA-platform
cd prestigedevop-bioinformatic-workflow-platform

# Restore all dependencies
dotnet restore

# Build all projects
dotnet build

# Run the console demo
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run
```

## Creating Your First Workflow

### Step 1: Define Nodes

```csharp
var inputNode = new WorkflowNode 
{
    Id = "read-data",
    Name = "Read Input File",
    NodeType = NodeType.Input,
    Config = new Dictionary<string, object> 
    { 
        { "source", "sequences.fasta" }
    }
};

var analysisNode = new WorkflowNode 
{
    Id = "analyze",
    Name = "Gene Analysis Agent",
    NodeType = NodeType.Agent,
    Config = new Dictionary<string, object> 
    {
        { "agentType", "GenomeAnalysisAgent" },
        { "prompt", "Find coding regions in this sequence" }
    }
};
```

### Step 2: Connect Nodes

```csharp
var connection = new NodeConnection 
{
    SourceNodeId = "read-data",
    TargetNodeId = "analyze",
    OutputKey = "sequence_data",
    InputKey = "input"
};
```

### Step 3: Create Workflow

```csharp
var workflow = new Workflow 
{
    Id = "gene-analysis-workflow",
    Name = "Gene Analysis Pipeline",
    Nodes = new[] { inputNode, analysisNode },
    Connections = new[] { connection }
};
```

### Step 4: Execute

```csharp
var result = await orchestrator.ExecuteWorkflow(workflow);
Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
```

## Writing Custom Skills

### Step 1: Implement ISkill Interface

```csharp
public class SequenceAlignmentSkill : ISkill
{
    public string SkillId => "sequence-alignment";
    public string Name => "Sequence Alignment";
    public string Description => "Aligns multiple DNA sequences using dynamic programming";

    public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        if (inputs == null)
            return new SkillOutput { Success = false, ErrorMessage = "No inputs provided" };

        try 
        {
            var sequences = (List<string>)inputs["sequences"];
            // Implement alignment logic here
            
            return new SkillOutput
            {
                Success = true,
                Data = new Dictionary<string, object>
                {
                    { "alignedSequences", alignmentResult },
                    { "scoreMatrix", scoreMatrix }
                }
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
        return new Dictionary<string, string>
        {
            { "sequences", "array[string]" },
            { "algorithm", "string" }
        };
    }

    public Dictionary<string, string>? GetOutputSchema()
    {
        return new Dictionary<string, string>
        {
            { "alignedSequences", "array[string]" },
            { "scoreMatrix", "array[array[float]]" }
        };
    }
}
```

### Step 2: Register Skill

```csharp
var skillRegistry = serviceProvider.GetRequiredService<ISkillRegistry>();
await skillRegistry.RegisterSkill("seq-align", new SequenceAlignmentSkill());
```

### Step 3: Use in Workflow

```csharp
var node = new WorkflowNode 
{
    Id = "align",
    Name = "Align Sequences",
    NodeType = NodeType.ProcessingSkill,
    Config = new Dictionary<string, object> 
    {
        { "skillId", "sequence-alignment" },
        { "algorithm", "needleman-wunsch" }
    }
};
```

## Creating Custom Agents

### Step 1: Extend AgentConfig

```csharp
public class BioinformaticsAgentConfig : AgentConfig
{
    public string? SpecializationArea { get; set; } // e.g., "genomics", "proteomics"
    public string? ReferenceDatabase { get; set; }
    public double ConfidenceThreshold { get; set; } = 0.85;
}
```

### Step 2: Implement Custom Agent

```csharp
public class BioinformaticsAgent : IAgent
{
    private readonly string _agentId;
    private readonly BioinformaticsAgentConfig _config;
    private readonly Dictionary<string, ISkill> _skills;

    public string AgentId => _agentId;

    public BioinformaticsAgent(string agentId, BioinformaticsAgentConfig config)
    {
        _agentId = agentId;
        _config = config;
        _skills = new Dictionary<string, ISkill>();
    }

    public async Task<AgentResponse> Run(string prompt, Dictionary<string, object>? context = null)
    {
        // Call your LLM API or local model here
        var systemPrompt = $"You are a {_config.SpecializationArea} specialist. " +
                          $"Use {_config.ReferenceDatabase} as your reference database. " +
                          $"Confidence threshold: {_config.ConfidenceThreshold}";
        
        // Implementation here
        await Task.Delay(100);
        
        return new AgentResponse
        {
            Response = "Analysis complete",
            TokensUsed = 250
        };
    }

    public IEnumerable<string> GetAvailableSkills() => _skills.Keys;

    public Task BindSkill(string skillId, ISkill skill)
    {
        _skills[skillId] = skill;
        return Task.CompletedTask;
    }
}
```

## Testing Your Workflows

### Unit Test Example

```csharp
[TestFixture]
public class WorkflowExecutionTests
{
    private IWorkflowOrchestrator _orchestrator;
    private INodeExecutor _nodeExecutor;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<INodeExecutor, NodeExecutor>();
        services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
        
        var sp = services.BuildServiceProvider();
        _nodeExecutor = sp.GetRequiredService<INodeExecutor>();
        _orchestrator = sp.GetRequiredService<IWorkflowOrchestrator>();
    }

    [Test]
    public async Task ExecuteWorkflow_WithValidGraph_ReturnsCompleted()
    {
        // Arrange
        var workflow = CreateTestWorkflow();

        // Act
        var result = await _orchestrator.ExecuteWorkflow(workflow);

        // Assert
        Assert.That(result.Status, Is.EqualTo(WorkflowStatus.Completed));
        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }
}
```

## Debugging Tips

### 1. Enable Verbose Logging

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug); // Change to Debug
});
```

### 2. Inspect Execution Details

```csharp
var result = await orchestrator.ExecuteWorkflow(workflow);

foreach (var (nodeId, nodeExecution) in result.FullExecution.NodeExecutions)
{
    Console.WriteLine($"Node: {nodeId}");
    Console.WriteLine($"  Status: {nodeExecution.Status}");
    Console.WriteLine($"  Duration: {nodeExecution.Duration?.TotalMilliseconds}ms");
    Console.WriteLine($"  Outputs: {string.Join(", ", nodeExecution.Outputs?.Keys ?? new string[0])}");
}
```

### 3. Use Breakpoints in VS Code

Add `.vscode/launch.json`:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp/bin/Debug/net8.0/DNAPlatform.AgentWorkflowPatterns.ConsoleApp.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp",
            "preLaunchTask": "build"
        }
    ]
}
```

## Next Development Tasks

### Phase 1: Complete Core Framework
- [ ] Add circular dependency detection
- [ ] Implement conditional branching logic
- [ ] Add loop handling
- [ ] Write comprehensive unit tests

### Phase 2: Microsoft Agents Integration
- [ ] Integrate Microsoft Semantic Kernel
- [ ] Setup tool calling framework
- [ ] Implement ReAct pattern
- [ ] Add function calling support

### Phase 3: DevUI Backend
- [ ] Create ASP.NET Core API
- [ ] Implement workflow CRUD endpoints
- [ ] Add execution monitoring API
- [ ] Setup WebSocket for real-time updates

### Phase 4: Frontend
- [ ] React workflow designer
- [ ] Node palette UI
- [ ] Connection editor
- [ ] Execution viewer

## Common Issues & Solutions

### Issue: Circular Dependencies in Workflow
**Solution**: The orchestrator includes DAG validation:
```csharp
if (!IsDAG(workflow))
    throw new InvalidOperationException("Workflow contains cycles");
```

### Issue: Node Execution Timeout
**Solution**: Set timeout in retry policy:
```csharp
node.TimeoutMs = 30000; // 30 seconds
```

### Issue: Skill Not Found
**Solution**: Check skill registration:
```csharp
var skill = await skillRegistry.GetSkill("my-skill");
if (skill == null)
    throw new KeyNotFoundException("Skill not registered");
```

## Performance Considerations

1. **Parallel Execution**: Multiple independent nodes execute concurrently
2. **Async Operations**: All I/O is non-blocking
3. **Memory**: Consider workflow size and node state
4. **Timeouts**: Set reasonable limits for long-running operations

## Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Implement changes
3. Add tests
4. Run full build: `dotnet build`
5. Submit PR

## Resources

- [Architecture Documentation](./ARCHITECTURE.md)
- [.NET Async/Await Patterns](https://docs.microsoft.com/dotnet/csharp/async)
- [Dependency Injection](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Unit Testing](https://docs.microsoft.com/dotnet/core/testing/)
