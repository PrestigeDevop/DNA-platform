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
- **Provider-agnostic `IAgent` seam** — Microsoft Agent Framework assessed and
  **deferred**; it will be added later as one provider among several, never as the
  execution engine. See [docs/AGENT_FRAMEWORK_INTEGRATION.md](./docs/AGENT_FRAMEWORK_INTEGRATION.md)
- **Local LLM inference** support (Ollama, LM Studio) — planned, not yet wired
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
- **mise-based environment** — `mise.toml` pins the toolchain and the ⚙️ Configure view
  reports GREEN/YELLOW/RED runtime status with export/import/verify (the core idea,
  now implemented)
- **pack drylab experiments** make abstract interactive webapps sort of  like hugging face's playground locally for now maybe "Distroless docker image"


### 💻 DevUI - Web-Based Designer
- **Workflow-level lifecycle: Configure → Test → Run.** Configure ships today (⚙️ on
  every workflow card: mise/runtime status, export, import, verify). **Test is done** —
  `🩺 Test` renders `WorkflowTestResult.svelte` with structural validation only
- **Drag-and-drop workflow builder** for composing nodes on a canvas
- **Workflow persistence** via Prisma/SQLite (SvelteKit server routes)
- **Backend execution** proxied to the .NET orchestrator
- **REST API** for workflow management
- **Real-time execution monitoring**
- **Customizble**

## 🚀 Quick Start (DevUI Web App)

### Prerequisites
- **.NET 8.0+ SDK** installed
- **Node.js 20+** installed  
- **[mise](https://mise.jdx.dev/)** — optional, but recommended for reproducible tool
  pinning and for the ⚙️ Configure view. The app runs without it.
- **Git** (optional, for version control)

### One-Command Setup
```bash
# 1. Once, on a fresh clone: install, build, generate the Prisma client, push the schema
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\bootstrap.ps1

# 2. Every session
run-all.bat
```

This starts:
- **Backend API**: http://localhost:5254 (Swagger: `/swagger`)
- **Frontend DevUI**: http://localhost:5173

`bootstrap.ps1` is idempotent, so re-running it is safe. It probes the toolchain,
runs `dotnet restore` / `dotnet build`, `npm install`, `prisma generate` and
`prisma db push`, and prints both URLs when it finishes.

### Verifying the stack

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-e2e.ps1
```

Three stages with a real exit code: a static build + `npm run check` against a
recorded baseline, a backend stage that starts and stops the API on :5254, and a
frontend stage that builds and serves the app. It stops everything it starts.

`verify-stack.bat` is kept and still works — it is the quick interactive check.
`test-e2e.ps1` is the one to gate a build on, because it exits non-zero.

### Optional: pin the toolchain with mise

```bash
mise install        # installs the dotnet 8 + node lts versions mise.toml pins
mise tasks ls       # confirms build, build:web, dev were picked up
mise run build      # dotnet build DNAPlatform.sln
```

`mise.toml` pins `.NET 8` and `Node.js LTS` — the two platform-provided defaults —
and sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`. With mise absent, the platform
still runs and `GET /api/environment` reports `miseInstalled: false` with a
`"yellow"` overall status rather than failing. `bootstrap.ps1` probes mise with
`Get-Command` and never requires it.

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
| Swagger docs | http://localhost:5254/swagger | REST API reference. Needs the Development environment — see Troubleshooting |
| Health check | http://localhost:5254/health | Also proxied at http://localhost:5173/health |
| Environment status | http://localhost:5254/api/environment | mise/runtime status; proxied at http://localhost:5173/api/environment |

### Command line

`dnaplatform` is a thin HTTP client for the same API. It never runs a workflow
itself — it asks the platform to, so there is only ever one execution engine.

```bash
dnaplatform env                       # mise + runtime status; works with either server up
dnaplatform validate <workflow-id>    # structural checks only; exit 0 = valid
dnaplatform run <workflow-id>         # execute and print per-node results
dnaplatform status [--all]            # latest run per workflow
dnaplatform logs [-n 50] [--since ID] # tail the platform log buffer
dnaplatform cancel <execution-id>     # request cancellation
```

Add `--json` to any of them for machine-readable output. `run` and `validate`
exit non-zero on failure, so they work as CI gates.

The default targets are `--url http://127.0.0.1:5173` (the DevUI origin, which
serves the workflow and execution routes) and `--api-url http://127.0.0.1:5254`
(the .NET origin, used only for `cancel`). Override either with the flag or with
`DNA_PLATFORM_URL` / `DNA_PLATFORM_API_URL`.

### Troubleshooting

| Problem | Solution |
|---------|----------|
| Port 5254 already in use | Change port in `run-all.bat` or kill the process using it. `run-all.bat:8` already pre-kills stale listeners; `test-e2e.ps1` does too |
| Port 5173 already in use | A Vite child can outlive its `npm` parent and keep `[::1]:5173` held, which is invisible on `127.0.0.1`. Check both address families and use `taskkill /T /F /PID <wrapper>` |
| `prisma generate` fails with `EPERM` | A dev server still holds `query_engine-windows.dll.node`. Stop it first |
| `swagger` returns 404 | `dotnet run --no-launch-profile` also drops `ASPNETCORE_ENVIRONMENT=Development`, and Swagger is only mapped in Development. Set it explicitly: `$env:ASPNETCORE_ENVIRONMENT=Development`. `run-all.bat` does not pass the flag, so it works there |
| `npm install` fails | Clear cache: `npm cache clean --force` then retry |
| `dotnet run` fails | Run `dotnet restore` and `dotnet build` first |
| `npm run check` reports 6 errors | Expected — that is the recorded baseline. `test-e2e.ps1` fails only on a *new* diagnostic |
| Frontend can't connect to backend | Ensure backend is running on port 5254 (check proxy in vite.config.ts) |
| `dnaplatform status` 404s | You pointed `--url` at `:5254`. The execution list is SvelteKit's; keep the default `:5173` |
| Configure view shows YELLOW | mise is not installed, not on `PATH`, or `mise install` has not run. Yellow is a valid state — the platform-provided default (.NET 8 / Node LTS) is standing in. Check `mise --version` (exit 0 = available) |

### First Time Setup (Combined Steps)
```bash
git clone https://github.com/PrestigeDevop/DNA-platform.git
cd DNA-platform

powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\bootstrap.ps1
run-all.bat
```

### Next Steps
1. Open http://localhost:5173 in your browser
2. Navigate to **Workflows** to create your first workflow
3. On any workflow card, click **⚙️ Configure** to see the execution environment:
   mise availability, the resolved version of each runtime (.NET 8, Node.js LTS), and
   GREEN/YELLOW/RED dependency status. You can also export the project `mise.toml`,
   import a replacement (it overwrites — export first), and re-verify
4. Check **Skills** to see available bioinformatic skills
5. Monitor **Executions** to track workflow progress
6. Manage **Agents** to configure AI agents

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
├── mise.toml                                # Toolchain pins (.NET 8, Node LTS) + build/dev tasks
├── run-all.bat                              # Single entry point: starts API + UI
├── verify-stack.bat                         # Quick interactive stack check
├── AGENTS.md                                # Agent-facing repo guidance (read this first)
├── scripts/                                 # Setup and verification
│   ├── bootstrap.ps1                        # Idempotent one-time setup
│   └── test-e2e.ps1                         # Three-stage verification, real exit code
├── docs/                                    # All documentation
│   ├── GAP_ANALYSIS.md                      # Spec requirement vs. current state
│   ├── AGENT_FRAMEWORK_INTEGRATION.md       # Microsoft Agent Framework decision record
│   ├── refactor.md                          # The user-supplied spec (input artifact)
│   └── TODO.md                              # Current task list
├── src/
│   ├── 01_Core/
│   │   └── DNAPlatform.Core/                # ⚠ Placeholder only, not wired up
│   ├── 03_AgentWorkflowPatterns/            # Core agent framework (stable)
│   │   ├── DNAPlatform.AgentFramework/      # THE engine: orchestrator, agents, nodes
│   │   └── DNAPlatform.AgentWorkflowPatterns.ConsoleApp/   # 3-pattern demo
│   ├── 04_WorkflowEngine/
│   │   └── DNAPlatform.Workflow/            # ⚠ Placeholder only — NOT the engine
│   ├── 05_DevUI/                            # Web UI + API (active)
│   │   ├── DNAPlatform.DevUI.API/           # .NET 8 REST API (port 5254)
│   │   ├── DNAPlatform.DevUI.Web/           # SvelteKit UI + Prisma/SQLite (port 5173)
│   │   └── DNAPlatform.DevUI.AGUI/          # Port 5000 server (built, not the UI path)
│   ├── 06_Skills/                           # DNAPlatform.Skills — the real registry
│   │   └── DNAPlatform.Skills.Core/         # ⚠ Placeholder — NOT the registry
│   ├── 07_CLI/
│   │   └── DNAPlatform.Cli/                 # dnaplatform — HTTP-only CLI client
│   ├── 08_ACP/                              # ACP adapter — thin Node/TypeScript adapter over the HTTP API
│   └── 09_Tests/
│       └── DNAPlatform.Tests/               # Orchestrator tests (dotnet test)
├── DNAPlatform.sln                          # The one .NET solution
└── README.md                                # This file
```

> ✅ **One solution, one engine.** `DNAPlatform.slnx` is deleted and
> `DNAPlatform.sln` is the only solution; it builds `DNAPlatform.Skills`, the
> AGUI server, the tests and the CLI, clean at 0 warnings. The DevUI API binds the
> real `DNAPlatform.AgentFramework` orchestrator and the real skill registry — the
> in-memory double is gone, so there is exactly one execution engine in the repo.
>
> ⚠️ **Two `Class1.cs` placeholders** remain in `DNAPlatform.Core` and
> `DNAPlatform.Workflow` (`src/04_WorkflowEngine/`), plus `DNAPlatform.Skills.Core`.
> None is on a live code path. `04_WorkflowEngine` is a naming trap: the real
> engine is `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework`.

## 📖 Documentation

All docs live in **[docs/](./docs/)**:
- **[AGENTS.md](./AGENTS.md)** - Repo-specific guidance for coding agents (routing ownership, envelope conventions, gotchas)
- **[docs/GAP_ANALYSIS.md](./docs/GAP_ANALYSIS.md)** - Spec requirement vs. current state, with evidence and §16 priorities
- **[docs/AGENT_FRAMEWORK_INTEGRATION.md](./docs/AGENT_FRAMEWORK_INTEGRATION.md)** - Microsoft Agent Framework decision record (defer adoption, build the seam first)
- **[docs/refactor.md](./docs/refactor.md)** - The user-supplied spec. An input artifact, not a description of current reality; assessed by GAP_ANALYSIS.md
- **[docs/TODO.md](./docs/TODO.md)** - Current task list

> Those four files are the whole documentation surface. `docs/refactor.md` is an
> input artifact, not current state; when any document disagrees with the source,
> trust `Program.cs`, `vite.config.ts`, `schema.prisma`, and the `+server.ts`
> route handlers.

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

### Phase 3.5: Environment & Toolchain ✅ COMPLETE (2026-10-05)
- [x] `mise.toml` pinning .NET 8 + Node LTS, with `build` / `build:web` / `dev` tasks
- [x] `MiseService` + `EnvironmentController` (4 endpoints under `/api/environment*`)
- [x] GREEN/YELLOW/RED runtime status with platform-default fallbacks
- [x] Workflow-level Configure view (`⚙️ Configure` on `/workflows`) — export / import / verify
- [x] Workflow-level Test (`🩺 Test` on `/workflows`) — structural validation only, no execution

### Phase 3.75: Consolidation ✅ COMPLETE (2026-10-05)
- [x] One execution engine: the DevUI API binds the real `WorkflowOrchestrator` and the
      real `SkillRegistry`; the in-memory double is deleted
- [x] Live per-node execution state — a status chip per designer node (colour + icon + word)
- [x] Connection drawing: source port → target port, SVG edge with an arrowhead
- [x] `dnaplatform` CLI (`run` / `validate` / `status` / `logs` / `cancel` / `env`)
- [x] MCP plugin UI on `/skills` — `McpPluginShell.svelte` shell (buttons not wired yet); the .NET API serves `GET /api/mcp/servers` and `POST /api/mcp/servers/import` via `McpServersController` + `McpServerDiscovery`
- [x] `scripts/bootstrap.ps1` + `scripts/test-e2e.ps1`, both with a confirmed green run
- [x] One solution file; version reconciled to `0.3.0-alpha` across `/health`, `/api/info`,
      the startup banner and Swagger
- [x] ACP adapter (`src/08_ACP/`) — done: a thin Node/TypeScript adapter over the HTTP API

### Phase 4: AI Integration 📋 Planned (November)
- [ ] `IAgent` provider seam (provider-agnostic, no new package)
- [ ] Agent configuration model
- [ ] Microsoft Agent Framework as an optional provider — **deferred** behind the seam; see [docs/AGENT_FRAMEWORK_INTEGRATION.md](./docs/AGENT_FRAMEWORK_INTEGRATION.md)
- [ ] Local LLM support (Ollama, LM Studio) with model switching
- [ ] Function calling / tool use patterns
- [ ] Agent reasoning traces display
- [ ] Prompt templating and versioning
- [ ] Token usage tracking per execution

### Phase 5: local Bioinformatics MCPS 📋 Planned 
- [] Sequence alignment skills (BLAST, Needleman-Wunsch, Smith-Waterman)
- [ ] FASTA/FASTQ file loading and parsing
- [ ] Molecular docking simulations
- [ ] Statistical analysis nodes
- [ ] Visualization components (D3.js, Plotly integration)
- [ ] Database integration (UniProt, NCBI E-utilities)

### Phase 6: Production 📋 Planned 
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

**v0.3.0-alpha**: Core engine + DevUI API complete, web designer + custom skills
complete, the mise-based Configure view, the consolidation pass (one engine, CLI,
scripts, workflow Test, live node state, MCP plugin UI). 2026-10-12.

The version is now real rather than documentary: a single `PlatformVersion`
constant in `Program.cs` feeds `/health`, `/api/info`, the startup banner and the
Swagger document. Verified: `/health` → `"version":"0.3.0-alpha"`, and
`swagger.json` → `info.version` `0.3.0-alpha`.

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
- ✅ **mise integration** — `mise.toml`, `MiseService`, `EnvironmentController`, GREEN/YELLOW/RED status
- ✅ **Workflow-level Configure view** (`⚙️` on `/workflows`) with export / import / verify
- ✅ **Workflow-level Test** (`🩺` on `/workflows`) — structural validation only
- ✅ **Live per-node execution state** — a status chip per node (colour + icon + word)
- ✅ **Connection drawing** in the designer (source port → target port → SVG edge with arrowhead)
- ✅ **One execution engine** — the DevUI API uses the real orchestrator and skill registry
- ✅ **`dnaplatform` CLI** — HTTP-only, six subcommands, `--json` everywhere, scriptable exit codes
- ✅ **`bootstrap.ps1` + `test-e2e.ps1`** — idempotent setup and three-stage verification with a real exit code
- ✅ **ACP adapter** (`src/08_ACP/`) — thin Node/TypeScript adapter over the HTTP API
- ✅ **Orchestrator tests** — `dotnet test`, 8 passing

### In Development 🚧
- 🚧 Export/import workflow JSON with runtime environment metadata
- 🚧 Agent-backed nodes in workflows
- 🚧 Real custom code execution path for polyglot kernel snippets (hello-world placeholder is live)
- 🚧 Real-time log streaming — `/logs` still polls every 1500 ms and the buffer is in-memory only
- 🚧 PostgreSQL persistence — deferred; SQLite is adequate today and the JSON-blob columns would need migrating

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

For a runnable version of all three patterns (sequential, parallel, dynamic), see
`src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp`.

## 🤝 Contributing

Contributions welcome! Please:
1. Fork the repo
2. Create feature branch
3. Add tests — note there is currently **no test project** and coverage is 0%, so this
   means adding the first one
4. Build & verify: `dotnet build` (or `mise run build`), then verify manually per
   [AGENTS.md](./AGENTS.md) → Verification. `npm run check` in the Web project has 6
   pre-existing errors — do not add to them
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

**Status**: 🚀 Alpha | **Version**: 0.3.0-alpha | **Updated**: 2026-10-05
