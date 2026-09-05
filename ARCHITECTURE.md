# DNA Platform - Agent Workflow Engine Architecture

## Overview

**DNA Platform** is a low-code/no-code bioinformatic workflow engine built on .NET with a focus on:
- **Agent-driven execution** using Microsoft agents framework
- **Drag-and-drop workflow designer** (DevUI)
- **Polyglot runtime** for interoperability with Python, Node.js, and other languages
- **Extensible skills system** for composable bioinformatic tools
- **Parallel execution** support for distributed processing

## Architecture Layers

### 1. **Core Foundation** (`src/01_Core/DNAPlatform.Core`)
- Base models and utilities
- Common interfaces
- Error handling
- Logging configuration

### 2. **Agent Workflow Framework** (`src/03_AgentWorkflowPatterns`)
**Namespace: `DNAPlatform.AgentFramework`**

Core components:
- **Models.cs**: Workflow, Node, Connection, Execution data structures
- **Interfaces.cs**: Service contracts for orchestration, agents, skills
- **WorkflowOrchestrator.cs**: Main execution engine
  - Topological graph execution
  - Parallel node execution
  - State management
  - Error handling & retry logic

- **NodeExecutor.cs**: Individual node execution
- **AgentManager.cs**: AI agent lifecycle management
- **SkillRegistry.cs**: Plugin/skill registration and lookup

#### Key Classes:
- `Workflow`: Defines workflow graph structure
- `WorkflowNode`: Individual processing node
  - Types: Input, Output, Agent, ProcessingSkill, Conditional, Loop, SubWorkflow
- `NodeConnection`: Edges between nodes
- `WorkflowExecution`: Runtime execution instance
- `IAgent`: AI agent interface with tool binding
- `ISkill`: Reusable plugin interface

### 3. **Workflow Engine** (`src/04_WorkflowEngine/DNAPlatform.Workflow`)
Advanced execution patterns:
- Conditional branching
- Loop handling
- Subworkflow composition
- Workflow versioning
- Execution history

### 4. **DevUI API** (`src/05_DevUI/DNAPlatform.DevUI.API`)
REST API for web-based workflow designer:
- Workflow CRUD operations
- Execution monitoring
- Real-time websocket updates
- Workflow templates
- Audit logging

### 5. **Skills Core** (`src/06_Skills/DNAPlatform.Skills.Core`)
Extensible plugin system:
- Bioinformatic algorithms
- Data transformation utilities
- Validation skills
- Machine learning integration hooks

### 6. **Polyglot Runtime** (`src/07_PolyglotRuntime/DNAPlatform.PolyglotRuntime`)
Multi-language support:
- .NET polyglot kernel for Jupyter
- Python interop (PyO3/pythonnet)
- Node.js bridge
- Rust FFI for performance-critical code

## Execution Model

### Workflow Execution Flow
```
1. Parse Workflow Graph
   ↓
2. Validate DAG (Directed Acyclic Graph)
   ↓
3. Execute Topologically
   - Identify executable nodes (no unmet dependencies)
   - Execute in parallel when possible
   - Collect outputs
   ↓
4. Feed Outputs to Dependents
   ↓
5. Report Completion/Errors
```

### Node Types

| Type | Purpose | Example |
|------|---------|---------|
| **Input** | Data source node | Read CSV file, API call |
| **ProcessingSkill** | Deterministic operation | Data transform, validation |
| **Agent** | AI reasoning & decisions | Gene analysis, pattern detection |
| **Conditional** | Branch on condition | If/then/else logic |
| **Loop** | Iterate over collection | Process multiple sequences |
| **SubWorkflow** | Nested workflow | Composition/reuse |
| **Output** | Data sink | Write results, visualize |

### Agent Framework Integration

Agents are AI-powered nodes that can:
- Make reasoning decisions
- Call tools (bound skills)
- Process natural language prompts
- Learn from context

**Example Agent Prompt:**
```
"Analyze this DNA sequence for putative drug binding sites. 
Return JSON with: sites[], confidence[], binding_energy[]"
```

## Data Flow

Nodes pass data via **NodeConnections**:
```csharp
var connection = new NodeConnection 
{
    SourceNodeId = "node-1",
    TargetNodeId = "node-2",
    OutputKey = "results",      // Output field from source
    InputKey = "input_data",    // Input field for target
    Condition = "results.score > 0.8"  // Optional conditional
};
```

## Service Registration

The DI container automatically wires:
```csharp
services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
services.AddSingleton<INodeExecutor, NodeExecutor>();
services.AddSingleton<IAgentManager, AgentManager>();
services.AddSingleton<ISkillRegistry, SkillRegistry>();
```

## Error Handling & Resilience

### Retry Policy
```csharp
var retryPolicy = new RetryPolicy 
{
    MaxRetries = 3,
    InitialDelayMs = 1000,
    BackoffMultiplier = 2.0,
    MaxDelayMs = 60000
};
```

### Execution Status Tracking
- Each node execution is independently tracked
- Failures don't cascade (configurable)
- Full error context preserved for debugging

## Next Steps (Roadmap)

### Phase 1: Foundation ✅
- [x] Core workflow engine
- [x] Agent framework setup
- [x] Basic node types
- [ ] Unit tests

### Phase 2: DevUI
- [ ] Web API endpoints
- [ ] React/Vue workflow designer
- [ ] Real-time execution monitoring
- [ ] Workflow templates library

### Phase 3: Advanced Features
- [ ] Microsoft Semantic Kernel integration
- [ ] Local LLM inference (Ollama, LM Studio)
- [ ] Polyglot runtime bridges
- [ ] Distributed execution (Dapr, Kubernetes)

### Phase 4: Bioinformatics
- [ ] Sequence analysis skills
- [ ] Molecular docking integrations
- [ ] Statistical analysis nodes
- [ ] Visualization components

## Running the Demo

```bash
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run
```

**Output:**
- Simple sequential workflow execution
- Parallel agent workflow demonstration
- Dynamic loop workflow example

## File Structure
```
DNA-platform/
├── src/
│   ├── 01_Core/
│   │   └── DNAPlatform.Core/
│   ├── 03_AgentWorkflowPatterns/
│   │   ├── DNAPlatform.AgentFramework/
│   │   │   ├── Models.cs               (Data structures)
│   │   │   ├── Interfaces.cs           (Service contracts)
│   │   │   ├── WorkflowOrchestrator.cs (Execution engine)
│   │   │   ├── NodeExecutor.cs         (Node execution)
│   │   │   └── AgentManager.cs         (Agent & skills)
│   │   └── DNAPlatform.AgentWorkflowPatterns.ConsoleApp/
│   │       └── Program.cs              (Demo application)
│   ├── 04_WorkflowEngine/
│   ├── 05_DevUI/
│   ├── 06_Skills/
│   └── 07_PolyglotRuntime/
├── test/
│   └── DNAPlatform.Tests/
├── DNAPlatform.sln
└── README.md
```

## Key Design Decisions

1. **Topological Execution**: Ensures correct dependency order without explicit scheduling
2. **Async/Await**: Full async support for I/O and long-running operations
3. **Dependency Injection**: Loose coupling and easy testability
4. **Retry Policies**: Resilience to transient failures
5. **Extensible Interfaces**: Easy to add new node types and skills

## References

- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- [Microsoft AutoGen](https://github.com/microsoft/autogen)
- [Kubeflow](https://www.kubeflow.org/)
- [MageAI](https://www.mage.ai/)
- [DAPR](https://dapr.io/)
