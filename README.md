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

## 🚀 Quick Start (DevUI Web App)

### Prerequisites
- **.NET 8.0+ SDK** installed
- **Node.js 20+** installed  
- **Git** (optional, for version control)

### One-Command Setup
```bash
run-all.bat
```

This starts:
- **Backend API**: http://localhost:5254 (Swagger: `/swagger`)
- **Frontend DevUI**: http://localhost:5173

### Detailed Manual Setup

#### 1. Clone & Restore
```bash
git clone https://github.com/PrestigeDevop/DNA-platform.git
cd DNA-platform

# Restore .NET dependencies
dotnet restore

# Build solution
dotnet build
```

#### 2. Install Frontend Dependencies
```bash
cd src\05_DevUI\DNAPlatform.DevUI.Web
npm install
cd ..\..\..\..
```

#### 3. Run the System
From main directory:
```bash
run-all.bat
```

Or run separately:

**Backend only:**
```bash
cd src\05_DevUI\DNAPlatform.DevUI.API
dotnet run --urls http://localhost:5254
```

**Frontend only (Svelte):**
```bash
cd src\05_DevUI\DNAPlatform.DevUI.Web
npm install
npm run dev
```

### Access Points

| Service | URL | Notes |
|---------|-----|-------|
| Frontend (SvelteKit + Prisma) | http://localhost:5173 | Main UI; persists workflows/agents/executions in SQLite |
| Backend API (.NET 8) | http://localhost:5254 | Workflow execution engine + skills |
| Swagger docs | http://localhost:5254/swagger | REST API reference |
| Health check | http://localhost:5254/health | Also proxied at http://localhost:5173/health |

### Troubleshooting

| Problem | Solution |
|---------|----------|
| Port 5254 already in use | Change port in `run-all.bat` or kill the process using it |
| Port 5173 already in use | Change port in `vite.config.ts` or kill the process using it |
| `npm install` fails | Clear cache: `npm cache clean --force` then retry |
| `dotnet run` fails | Run `dotnet restore` and `dotnet build` first |
| Frontend can't connect to backend | Ensure backend is running on port 5254 (check proxy in vite.config.ts) |

### First Time Setup (Combined Steps)
```bash
# Clone repository (if not done)
git clone https://github.com/PrestigeDevop/DNA-platform.git
cd DNA-platform

# Restore .NET dependencies
dotnet restore

# Build the solution
dotnet build

# Install frontend dependencies
cd src\05_DevUI\DNAPlatform.DevUI.Web
npm install

# Run everything
cd ..\..\..\..
run-all.bat
```

### Next Steps
1. Open http://localhost:5173 in your browser
2. Navigate to **Workflows** to create your first workflow
3. Check **Skills** to see available bioinformatic skills
4. Monitor **Executions** to track workflow progress
5. Manage **Agents** to configure AI agents

### Console demo (agent framework patterns)

```bash
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
├── run-all.bat                              # Single entry point: starts API + UI
├── docs/                                    # All documentation
│   ├── ARCHITECTURE.md                      # Detailed design
│   ├── DEVELOPMENT.md                       # Development guide
│   ├── QUICKSTART.md                        # Step-by-step setup
│   ├── TODO.md                              # Current task list
│   └── DEVELOPMENT_LOG.md                   # Progress log
├── src/
│   ├── 03_AgentWorkflowPatterns/            # Core agent framework (stable)
│   │   └── DNAPlatform.AgentFramework/      # Engine: orchestrator, agents, skills
│   └── 05_DevUI/                            # Web UI + API (active)
│       ├── DNAPlatform.DevUI.API/           # .NET 8 REST API (port 5254)
│       └── DNAPlatform.DevUI.Web/           # SvelteKit UI + Prisma/SQLite (port 5173)
├── DNAPlatform.sln                          # .NET solution
└── README.md                                # This file
```

## 📖 Documentation

All docs live in **[docs/](./docs/)**:
- **[docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)** - System design, execution model, service contracts
- **[docs/DEVELOPMENT.md](./docs/DEVELOPMENT.md)** - Getting started, examples, testing, debugging
- **[docs/QUICKSTART.md](./docs/QUICKSTART.md)** - Step-by-step setup guide
- **[docs/TODO.md](./docs/TODO.md)** - Current task list
- **[docs/DEVELOPMENT_LOG.md](./docs/DEVELOPMENT_LOG.md)** - Progress log

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
  - [Kubeflow](https://www.kubeflow.org/) 

## 📄 License

MIT License - Open source for research & development

---

**Status**: 🚀 Alpha | **Version**: 0.1.0 | **Updated**: 2026-09-04
