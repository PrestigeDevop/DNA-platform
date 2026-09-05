# DNA Platform - Bioinformatic Workflow Engine

**A low-code/no-code platform for computational biology workflows using .NET agents framework with drag-and-drop workflow designer.**

![DNA Platform Demo](https://img.shields.io/badge/Status-Alpha-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![.NET](https://img.shields.io/badge/.NET-8.0+-blue)

## 🎯 Overview

DNA Platform enables bioinformaticians to:
- **Build complex workflows** without coding using a visual drag-and-drop designer
- **Leverage AI agents** for intelligent analysis and decision-making
- **Compose reusable skills** for bioinformatic algorithms
- **Execute in parallel** for distributed processing
- **Interoperate** with Python, R, Node.js through polyglot runtime

**Built on**: .NET 8.0+ | **Philosophy**: FFI, IPC, and transpilation for cloud-native scalability

## ✨ Key Features

### 🔧 Agent-Driven Workflow Engine
- **Topological execution** for correct dependency ordering
- **Parallel node execution** for performance
- **Smart retry policies** with exponential backoff
- **Full async/await** support for non-blocking operations

### 🎨 Extensible Node Types
- **Input/Output nodes** for data sources and sinks
- **Agent nodes** with AI reasoning capabilities
- **Processing skills** for deterministic operations
- **Conditional branching** for complex logic
- **Loop constructs** for iteration
- **Subworkflow references** for composition

### 🤖 AI Agent Framework
- **Microsoft Semantic Kernel** integration (planned)
- **Local LLM inference** support (Ollama, LM Studio)
- **Tool calling** for skill invocation
- **Reasoning traces** for interpretability

### 🧩 Extensible Skill System
- **Plugin architecture** for custom bioinformatic algorithms
- **Built-in skills**: Data transform, Validation, Merge results
- **Dynamic skill discovery** and registration
- **Input/output schema** validation

### 🌐 Polyglot Runtime
- **.NET polyglot kernel** for Jupyter notebooks
- **Python interop** (pythonnet/PyO3)
- **Node.js bridge** for JavaScript tools
- **Rust FFI** for performance-critical code

### 💻 DevUI - Web-Based Designer
- **Drag-and-drop workflow builder** (coming soon)
- **REST API** for workflow management
- **Real-time execution monitoring**

## 🚀 Quick Start

### Installation

```bash
# Clone repository
git clone https://github.com/PrestigeDevop/DNA-platform.git
cd DNA-platform/prestigedevop-bioinformatic-workflow-platform

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run demo
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run
```

### Demo Output
The console app demonstrates three workflow patterns:
1. **Sequential**: Simple data input → transform → output
2. **Parallel**: Two agents analyze in parallel, merge results
3. **Dynamic**: Loop over collection with conditional processing

## 📁 Project Structure

```
DNA-platform/
├── src/
│   ├── 03_AgentWorkflowPatterns/          # PRIMARY WORKING AREA ✅
│   │   ├── DNAPlatform.AgentFramework/    # Core engine (stable)
│   │   └── ConsoleApp/                    # Demo application (working)
│   ├── 04_WorkflowEngine/                 # Advanced patterns (TODO)
│   ├── 05_DevUI/                          # ASP.NET Core API (TODO)
│   ├── 06_Skills/                         # Bioinformatic skills (TODO)
│   └── 07_PolyglotRuntime/                # Language interop (TODO)
├── ARCHITECTURE.md                        # Detailed design
├── DEVELOPMENT.md                         # Development guide
└── README.md                              # This file
```

## 📖 Documentation

- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - System design, execution model, service contracts
- **[DEVELOPMENT.md](./DEVELOPMENT.md)** - Getting started, examples, testing, debugging

## 🛣️ Roadmap

### Phase 1: Foundation ✅ 
- [x] Core workflow orchestration engine
- [x] Node execution framework
- [x] Agent manager & skill registry
- [x] Console demo with 3 patterns
- [ ] Unit tests

### Phase 2: DevUI (September)
- [ ] ASP.NET Core REST API
- [ ] Workflow CRUD endpoints
- [ ] Execution monitoring API
- [ ] WebSocket real-time updates

### Phase 3: Web Designer (October)
- [ ] React workflow designer
- [ ] Drag-and-drop node composition
- [ ] Workflow templates library
- [ ] Execution visualization

### Phase 4: AI Integration (November)
- [ ] Microsoft Semantic Kernel
- [ ] Local LLM support (Ollama)
- [ ] Function calling
- [ ] Agent tool binding

### Phase 5: Bioinformatics (December)
- [ ] Sequence alignment skills
- [ ] Molecular docking
- [ ] Statistical analysis
- [ ] Visualization components

## 🧪 Current Status

**v0.1.0-alpha**: Fully functional workflow engine with:
- ✅ Topological DAG execution
- ✅ Parallel node processing
- ✅ Error handling & retry logic
- ✅ Extensible architecture
- ✅ Working console demo

## 💡 Quick Example

```csharp
var workflow = new Workflow 
{
    Id = "gene-analysis",
    Name = "Gene Analysis Pipeline",
    Nodes = new[] { inputNode, analysisNode, outputNode },
    Connections = new[] { conn1, conn2 }
};

var result = await orchestrator.ExecuteWorkflow(workflow);
Console.WriteLine($"Status: {result.Status} - {result.Duration.TotalMilliseconds}ms");
```

See [DEVELOPMENT.md](./DEVELOPMENT.md) for complete examples.

## 🤝 Contributing

Contributions welcome! Please:
1. Fork the repo
2. Create feature branch
3. Add tests
4. Build & test: `dotnet build && dotnet test`
5. Submit PR

## 📞 Resources

- **GitHub Issues**: Bug reports & feature requests
- **Wiki**: [Terminology & concepts](https://github.com/PrestigeDevop/DNA-platform/wiki)
- **References**: 
  - [Open Free Energy](https://www.openfree.energy/)
  - [OpenBioSim](https://www.openbiosim.org/)
  - [NVIDIA BioNeMo](https://github.com/NVIDIA-BioNeMo/bionemo-agent-toolkit)
  - [Kubeflow](https://www.kubeflow.org/) | [MageAI](https://www.mage.ai/)

## 📄 License

MIT License - Open source for research & development

---

**Status**: 🚀 Alpha | **Version**: 0.1.0 | **Updated**: 2026-09-04
