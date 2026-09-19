# DNA Platform - Bioinformatic Workflow Engine

**A low-code/no-code platform for computational biology workflows using .NET agents framework with drag-and-drop workflow designer.**

![DNA Platform Demo](https://img.shields.io/badge/Status-Alpha-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![.NET](https://img.shields.io/badge/.NET-8.0+-blue)

<img width="1237" height="594" alt="image" src="https://github.com/user-attachments/assets/1d262d63-315d-43a4-a159-458e37a7b9e0" />

## 🎯 Overview

DNA Platform enables bioinformaticians to:
- **Build complex workflows**  DSL using a visual WYSIWYG tooling drag-and-drop reusable commponet 
- **Centralized polyglot runtime** one monolithic platform, automated dependency resolver 
- **Compose reusable nodes** plug and play, validate scheme before running the workflow  
- **annotate any library ** use plugin MCP servers or run locally via predefined serialization objects [logic_component]
- **Interoperate** with Python, R, Node.js, JVM or .net core all through polyglot platform ready to scale  

**Built on**: .NET 8.0+ and Node LTS | **Philosophy**: integrate A2A , FFI, IPC, and FaaS for cloud-native scalability

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
- **Microsoft Semantic Kernel with MARL** integration (planned)
- **Local LLM inference** support (Ollama, LM Studio)
- **Tool calling** for skill invocation
- **Reasoning traces** for interpretability (todo: unfied IO logs observability layer) 

### 🧩 Extensible Skill System
- **Plugin architecture** for custom bioinformatic algorithms
- **Built-in skills**: Data transform, Validation, Merge results
- **Dynamic skill discovery** and registration (WIP)
- - **Modular ** Modular  BCL and FCL scaffold any prewritten class into Boilerplate Code (stubs) 
- **Input/output schema** validation

### 🌐 Polyglot Runtime
- **.NET polyglot kernel** for Jupyter notebooks
- **Python interop** (pythonnet/PyO3)
- **Node.js bridge** for JavaScript tools
- **one Mise file** package and env reslover (the core idea)
- **pack drylab experiments** make abstract interactive webapps sort of  like hugging face's playground locally for now maybe "Distroless docker image"


### 💻 DevUI - Web-Based Designer
- **Drag-and-drop workflow builder** (coming soon)
- **REST API** for workflow management
- **Real-time execution monitoring**
- **Customizble**

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

### Phase 1: Foundation ✅ COMPLETE
- [x] Core workflow orchestration engine
- [x] Node execution framework with topological DAG
- [x] Agent manager & skill registry
- [x] Console demo with 3 patterns
- [x] ASP.NET Core REST API + Swagger

### Phase 2: DevUI API ✅ COMPLETE
- [x] SvelteKit frontend (port 5173) + Prisma/SQLite persistence
- [x] ASP.NET Core REST API (port 5254)
- [x] Workflow CRUD + agent CRUD + execution list
- [x] Workflow execute → record result in SQLite
- [x] Request logging (console + web Logs page)

### Phase 3: Web Designer + Custom Skills ✅ COMPLETE
- [x] Custom skills framework (`DNAPlatform.Skills`)
- [x] Built-in + bioinformatics skills
- [x] GUI-triggered skill execution (`/api/skills/{id}/execute`)
- [x] Skill “Details” parameter editor (typed input fields)
- [x] Custom snippet model (`CustomSnippet`) + CRUD (`/api/snippets`)
- [x] Snippet execute endpoint with hello-world .NET handler
- [x] “Create Custom Snippet” dialog on `/skills` (runtime selector, IO editors, test, save to palette)
- [x] Drag-and-drop designer integrated into `/workflows`
- [x] Designer IO ports from snippet/node metadata
- [x] Click node → inline inspect/edit panel
- [x] Connected services status panel (backend, polyglot kernel, placeholder external MCP/local executor)

### Phase 4: AI Integration 📋 Planned (November)
- [ ] Microsoft Semantic Kernel integration for agent reasoning
- [ ] Local LLM support (Ollama, LM Studio) with model switching
- [ ] Function calling / tool use patterns
- [ ] Agent reasoning traces display
- [ ] Prompt templating and versioning
- [ ] Token usage tracking per execution

### Phase 5: Bioinformatics 📋 Planned (December)
- [ ] Sequence alignment skills (BLAST, Needleman-Wunsch, Smith-Waterman)
- [ ] FASTA/FASTQ file loading and parsing
- [ ] Molecular docking simulations
- [ ] Statistical analysis nodes
- [ ] Visualization components (D3.js, Plotly integration)
- [ ] Database integration (UniProt, NCBI E-utilities)

### Phase 6: Production 📋 Planned (Q1 2027)
- [ ] PostgreSQL persistence (replace SQLite with production DB)
- [ ] Redis caching for workflow states and agent contexts
- [ ] Docker containerization for all services
- [ ] Kubernetes deployment manifests
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] User authentication (JWT/OAuth integration)
- [ ] Multi-user collaboration features
- [ ] Comprehensive audit logging
- [ ] Performance monitoring & alerting

## 🧪 Current Status

**v0.2.0-beta**: Core engine + DevUI API complete, web designer + custom skills complete

### Achieved ✅
- ✅ Topological DAG execution with parallel processing
- ✅ Parallel node processing via Task.WhenAll
- ✅ Error handling & retry logic with exponential backoff
- ✅ Extensible architecture (ISkill, IAgent interfaces)
- ✅ Working console demo with 3 workflow patterns
- ✅ **REST API fully functional** (workflows, agents, executions, skills, logs)
- ✅ **Prisma + SQLite persistence** for audit trail
- ✅ **SvelteKit reactive frontend** (CRUD via server routes)
- ✅ **Custom skills system** (backend + GUI execution)
- ✅ **Custom snippet model** + hello-world .NET snippet endpoint
- ✅ **Drag-and-drop designer** integrated into `/workflows`
- ✅ **Connected services panel** (backend + polyglot kernel + placeholder external MCP/local executor)

### In Development 🚧
- 🚧 Full connection drawing in the designer (click source → target port → SVG edge)
- 🚧 Workflow execution inside the canvas (run nodes in connected order, progress per node)
- 🚧 Export/import workflow JSON with runtime environment metadata
- 🚧 Agent-backed nodes in workflows
- 🚧 Real custom code execution path for polyglot kernel snippets (hello-world placeholder is live)

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
