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
- **annotate any library** use plugin MCP servers or run locally via predefined serializied  objects [smart_commponets]
- **Interoperate** with Python, R, Node.js, JVM or .net core all through polyglot platform ready to scale  

**Built on**: .NET 8.0+ and Node LTS | uses Mise MCP poyglot resolver for workflow or platform extnesions tools.
**Philosophy**: 
Polyglot backend :dotNET core with CSsnake runtime, sveltekit for Frontend. middlewareAPI proxy with seperation of concerns and DRY princples.
Author workflow with AG-UI Protocol integrate A2A,ACP, FFI, IPC, and FaaS for durpable cloud-native scalability.
Use CLI with python, bash or PowerShell_Core for Cmdlet,Structured .NET objects and extendability e.g Rapid tool development and Automation.   

## ✨ Key Features

`Visual Workflow Canvas: Build and test directed acyclic graph (DAG) pipelines via an intuitive drag-and-drop designer.
Polyglot Execution with CSnakes: Execute compiled C# .NET algorithms and Python bioinformatics packages (BioPython, NumPy) side-by-side with zero-overhead data transfer.
Human-in-the-Loop (HITL) Governance: AI agents stream reasoning traces and pause automatically before sensitive operations (e.g., sequence mutations, system execution) to request operator sign-off.`

### 🔧 Agent-Driven Workflow Engine
`:`
- **Topological execution** for correct dependency ordering
- **Parallel node execution** for performance
- **Smart retry policies** with exponential backoff
- **Full async/await** support for non-blocking operations
- **use agents as MCP service** use or build agents alongside with tools and skills_set .
- **recompose any workflow preset as nested node** use any prebuilt workflow 
- **CLI moudles** Audit CLI tools and extensions  (WIP)
- **Human-in-the-Loop** AI agents stream reasoning traces and pause automatically before sensitive operations.
- **RLHF & MARL** model native paradigm with lightning and meta MARL (WIP)
- **Statemachine workflow** a tab for seperate complex pipelines 

### 🎨 Extensible Node Types
- **strongly typed Input/Output** autoboxed if needed 
- **Agent nodes** with AI reasoning capabilities
- **Processing skills** for deterministic FILE system operations e.g save or load or marshling data or pip objects.
- **Conditional branching** for complex logic
- **Loop constructs** for iteration
- **Subworkflow references** for composition
- **CLI node** for exposing stdio machansim 
- **CRON JOBS** for automated JOBS

### 🤖 AI Agent Framework
- **Provider-agnostic `IAgent`** — Microsoft Agent Framework (hosted),A2A with AG-UI Protocol
- **Local LLM inference** support (onnxtuntime) (ollama wip)
- **Tool calling** for local skill invocation
- **Reasoning traces** for interpretability (ACP protocol stud IO observability layer) 
- **Mutlomodal** support mulitple data formats

### 🧩 Extensible MCP & Skill System
- **Plugin architecture** for customizing complex bioinformatic pipelines locally
- **Built-in skills**: Basic Bioinformatics tool sample, Data transform, Validation, Merge results
- **Dynamic skill discovery** and registration (test)
- **Modular** Modular  BCL&FCL DI pattren scaffold prewritten library into Boilerplate Code (stubs) and S you can also use CLI's future for automated insalls and annotation 
- **Input/output schema** params testing and validation
- **MCP and tools** tests on the fly 
- **Marketplace and ecosystem** local or remote MCP filter agents, mcp tools ,moudles, extensions etc.. 

### 🌐 Polyglot Runtime

`Polyglot Execution with CSnakes: Execute compiled C# .NET algorithms and Python bioinformatics packages (BioPython, NumPy) side-by-side with zero-overhead data transfer.`
- **.NET polyglot kernel** for Jupyter notebook connected to the platform
- **Python interop** (with csnake runtime)
- **Node.js bridge** for JavaScript tooling e.g threejs and D3 for interactive widgets or alternative scripting 
- **mise-based environment** — `mise.toml` pins the toolchain and test for workflow needed env in the ⚙️ Configure view.
- **pack drylab experiments** make an abstract interactive webapps sort of  like hugging face's playground locally for now maybe -----(WIP:Distroless docker image)
Dual Interfaces: Work seamlessly in your browser via the SvelteKit web studio or in the terminal using the native Terminal.Gui console dashboard.


### 💻 DevUI - Web-Based Designer

- **Drag-and-drop workflow builder** WYSIWYG for composing nodes and reusable commponet  on a canvas
- **Workflow persistence** via Prisma/SQLite (SvelteKit server routes)
- **Backend execution** most endpoints proxied to the .NET orchestrator
- **REST API** CRUD for workflow management, agents etc..
- **Stateless by deafult** Native implementation of the AG-UI Protocol (docs.ag-ui.com).
- **Real-time execution monitoring** for stdio/sse transport, tool calling etc..
- **Customizble**

## 🚀 Quick Start (DevUI Web App) 

### Prerequisites
- **.NET 8.0+ SDK** installed (net8.0)
- **Node.js 20+** installed  
- **Python 3.10+** (optional, for CSnakes BioPython/NumPy interop; pure C# fallback included)
- **[mise](https://mise.jdx.dev/)** (optional, recommended for toolchain pinning)

### The 3 Core Scripts (Cross-Platform)

#### 1. One-Command Setup & Bootstrap
Probes your toolchain, configures `.env`, builds the C# `Terminal.Gui` CLI, and syncs the Prisma SQLite database:
```bash
npm run bootstrap
# or: node scripts/1-bootstrap.mjs
```

#### 2. Run the System (DeNovoAutomata)
Boots instantly in under 1 second without reinstalling. Starts the Web Studio, ASP.NET Core API, and AG-UI server with an interactive terminal controller:
```bash
npm start
# or: node scripts/3-denovo-automata.mjs
# or on Windows: run-all.bat
```

This starts:
- **Web Studio (DevUI)**: http://localhost:3000 (or http://localhost:5173)
- **Backend API**: http://localhost:5254 (Swagger: `/swagger`)
- **AG-UI Protocol Server**: http://localhost:5000 (`/api/agui`)

#### 3. Test & Clean
Runs the automated 10-point agentic E2E verification suite. Use `test:clean` to clean temporary files and caches before Git push:
```bash
# Run test suite
npm test
# or: node scripts/2-test-dev.mjs

# Run test suite and clean workspace artifacts
npm run test:clean
# or: node scripts/2-test-dev.mjs --clean
```

#### 4. Interactive Terminal.Gui Console TUI
Launch the native windowed C# terminal interface:
```bash
npm run tui
# or: node scripts/dna.js tui
```

### Optional: Pin Toolchain with mise
```bash
mise install        # installs the dotnet 8 + node lts versions mise.toml pins
mise tasks ls       # confirms build, build:web, dev were picked up
mise run build      # dotnet build DNAPlatform.sln
```

---

## 📚 Documentation

- **[System Architecture (`docs/architecture.md`)](./docs/architecture.md)** — Low-code/no-code bioinformatic platform architecture, folder structure, agentic development guide, troubleshooting, and access points.
- **[Development Guide (`docs/development.md`)](./docs/development.md)** — Merged AGENTS guide, command line usage, completed milestones, and roadmap.

---

## 📄 License

MIT License - Open source for research & development

---

**Status**: 🚀 Alpha | **Version**: 0.3.0-alpha | **Updated**: 2026-10-17
