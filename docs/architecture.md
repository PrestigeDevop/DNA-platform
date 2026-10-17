# DeNovoAutomata — Architecture Guide

> **Platform Classification**: Low-code / no-code bioinformatic platform for computational biology workflows, autonomous AI agents, and polyglot algorithm execution.

---

## 1. Executive Overview

**DeNovoAutomata** is a low-code/no-code bioinformatic platform that bridges compiled high-performance .NET execution with the extensive biological computing ecosystem of Python. It enables bioinformaticians, clinical researchers, and software engineers to:
- Visually compose Directed Acyclic Graph (DAG) computational biology pipelines on a drag-and-drop canvas (inspired by n8n).
- Execute zero-copy polyglot bioinformatics routines via the **CSnakes** Python-C# bridge.
- Deploy autonomous AI agents governed by the **AG-UI Protocol** with real-time reasoning traces and Human-in-the-Loop (HITL) approval interrupts.
- Manage runs across multiple interfaces: a reactive web studio (SvelteKit 2), a native console TUI (`Terminal.Gui`), and cross-platform command-line tools.

---

## 2. System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       DeNovoAutomata Interface Layer                        │
│  SvelteKit 2 Web Studio (Port 3000 / 5173)   │  Terminal.Gui C# TUI (CLI)   │
│  - Drag-and-Drop WYSIWYG Workflow Canvas     │  - Native Windows/Linux TUI  │
│  - AG-UI Human-in-the-Loop Approval Modal    │  - Real-time Health Monitor  │
│  - Polyglot Interactive Code Kernel (Python) │  - Interactive Server Menu   │
└───────────────────────┬───────────────────────────────┬─────────────────────┘
                        │ HTTP / SSE                    │ HTTP
                        ▼                               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Service & Protocol Gateway Layer                       │
│                                                                             │
│   ASP.NET Core 8 API (:5254)             AG-UI Hosting Server (:5000)       │
│   - Minimal APIs & Attribute Routes      - AG-UI Protocol v1.0.0 (net8.0)  │
│   - Runtime Environment Introspection    - SSE Streaming (Thinking Traces) │
│   - LogStore & Diagnostic Ring Buffer    - Human-in-the-Loop Interrupts    │
└───────────────────────┬───────────────────────────────┬─────────────────────┘
                        │                               │
                        └───────────────┬───────────────┘
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Single Unified Execution Engine                        │
│               src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework       │
│                                                                             │
│   WorkflowOrchestrator   │   AgentManager   │   NodeExecutor                │
│   - Topological DAG Sort │   - Local ONNX   │   - Input / Output Nodes      │
│   - Cycle Detection      │   - Copilot Flow │   - Loop & Conditional Nodes  │
│   - Async Parallel Step  │   - Permissions  │   - ProcessingSkill Nodes     │
└───────────────────────┬───────────────────────────────┬─────────────────────┘
                        │                               │
                        ▼                               ▼
┌───────────────────────────────────────┐ ┌───────────────────────────────────┐
│       Core Polyglot Bridge            │ │       Skill & MCP Registry        │
│   src/01_Core/DNAPlatform.Core        │ │   src/06_Skills                   │
│                                       │ │                                   │
│   - CSnakes Python-C# Interop Bridge  │ │   - Needleman-Wunsch Aligner      │
│   - Zero-Copy Biological Marshaling   │ │   - C# High-Precision Clock       │
│   - DNA/RNA/Protein Sequence Metrics  │ │   - IUPAC Genetic Validator       │
│   - GC Skew & Reading Frame Analysis  │ │   - Custom Script Dynamic Loader  │
└───────────────────────────────────────┘ └───────────────────────────────────┘
```

---

## 3. Repository & Folder Structure

```
.
├── src/
│   ├── 01_Core/
│   │   └── DNAPlatform.Core/              # CSnakes Polyglot Python-C# bridge & biological sequence types
│   ├── 03_AgentWorkflowPatterns/
│   │   └── DNAPlatform.AgentFramework/    # The SINGLE workflow & agent engine (Orchestrator, NodeExecutor)
│   ├── 05_DevUI/
│   │   ├── DNAPlatform.DevUI.API/         # ASP.NET Core 8 Web API (Port 5254)
│   │   ├── DNAPlatform.DevUI.AGUI/        # AG-UI protocol hosting server (Port 5000)
│   │   └── DNAPlatform.DevUI.Web/         # SvelteKit 2 frontend & Prisma SQLite persistence (Port 3000)
│   ├── 06_Skills/
│   │   ├── DNAPlatform.Skills/            # Skill catalog (Needleman-Wunsch, C# Clock, IUPAC Validator)
│   │   └── DNAPlatform.Skills.Core/       # Core skill contracts & base interfaces
│   ├── 07_CLI/
│   │   └── DNAPlatform.Cli/               # Native C# Terminal.Gui TUI & HTTP CLI client
│   ├── 08_ACP/                            # Standalone Agent Client Protocol (Zed JSON-RPC) adapter
│   └── 09_Tests/
│       └── DNAPlatform.Tests/             # xUnit test suite for the workflow orchestrator
├── scripts/
│   ├── 1-bootstrap.mjs                    # Script 1: First-time toolchain install & Prisma database sync
│   ├── 2-test-dev.mjs                     # Script 2: Agentic E2E test suite with pre-push artifact clean
│   └── 3-denovo-automata.mjs              # Script 3: DeNovoAutomata lightweight animated runner with TUI
├── docs/
│   ├── architecture.md                    # System architecture, folder structure, agentic guide, gotchas
│   └── development.md                     # Consolidated dev logs, task lists, and milestone roadmap
├── metadata.json                          # Applet metadata and capabilities
├── package.json                           # Root workspace configuration and npm commands
└── README.md                              # User-facing platform introduction
```

---

## 4. Overview for Agentic Development

DeNovoAutomata supports agentic AI development across workflows and standalone agents:

### 4.1 AG-UI Protocol & Human-in-the-Loop (HITL)
The platform natively hosts the **AG-UI Protocol** (`https://docs.ag-ui.com/sdk/dotnet/hosting/overview`):
- **Cognitive Traces (`THINKING_DELTA`)**: The agent emits real-time inner monologue reasoning before invoking tools or synthesizing outputs.
- **Human-in-the-Loop Interrupts (`APPROVAL_REQUESTED`)**: If an agent attempts to execute a sensitive tool (such as mutating DNA sequences, running external scripts, or writing files), execution is suspended. The client UI renders an interactive authorization card.
- **Approval Resumption**: When authorized by the user, the client dispatches a resumption request (`POST /api/agui` with `approvals: [{ toolCallId, approved: true }]`). Execution resumes, updates shared state via RFC 6902 JSON Patch (`STATE_DELTA`), streams response tokens (`TEXT_DELTA`), and closes with `RUN_FINISHED`.

### 4.2 Agent Personas & Skill Binding
- Agents configured in `/agents` are backed by local ONNX models (e.g. `Microsoft-Copilot-Local-ONNX`, `Phi-3.5-mini-ONNX`).
- Agents can be assigned arbitrary skills from `src/06_Skills/` and external MCP servers (Model Context Protocol).
- Workflow nodes of type `Agent` pass upstream output into dynamic Prompt Flow templates and pipe agent reasoning to downstream nodes.

---

## 5. Developmental Gaps

*(This section is currently empty as all baseline requirements, AG-UI protocol streaming, CSnakes core bridge, Terminal.Gui TUI, and the 3 cross-platform scripts are fully implemented and verified.)*

> **Architectural Decisions & Limitations Guideline**:
> - Future platform limitations or prospective architecture trade-offs (e.g., PostgreSQL/pgvector migration vs SQLite, remote agent transport models) will be logged in this section.
> - **Rule**: The agent will consult the user when addressing architectural ambiguities or trade-offs before altering core execution primitives.

---

## 6. Gotchas & Operational Guidelines

1. **Routing Ownership in DevUI**:
   - SvelteKit server routes shadow the Vite proxy. CRUD routes (`/api/workflows`, `/api/agents`, `/api/executions`) are handled directly by SvelteKit's Prisma/SQLite layer.
   - Only backend compute routes (`/api/skills`, `/api/node-types`, `/api/environment`) are proxied to the .NET backend.
   - SvelteKit routes return lowercase JSON keys (`{ workflows, workflow }`), while .NET endpoints return PascalCase keys (`{ Count, Skills }`).

2. **Database Persistence with SQLite**:
   - SQLite has no native JSON column type. Complex objects (`Workflow.nodes`, `Workflow.connections`, `Execution.nodeResults`) are serialized as JSON strings in the database and parsed by route handlers.
   - `DATABASE_URL` must always start with `file:`. The database abstraction layer in `src/lib/server/db.ts` automatically normalizes paths to prevent query engine initialization failures.

3. **Linux Globalization (ICU)**:
   - On Linux systems where system ICU libraries are absent, .NET requires `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`. This is configured in `mise.toml` and environment definitions.

4. **Port Allocation**:
   - `3000`: SvelteKit Web UI Dev Server.
   - `5254`: ASP.NET Core 8 Web API.
   - `5000`: AG-UI SSE Protocol Server.

### Access Points

| Component / Interface | URL / Binding | Protocol | Key Endpoints |
|---|---|---|---|
| **SvelteKit Web Studio** | `http://localhost:3000` (or `:5173`) | HTTP / HTML5 | `/workflows`, `/agents`, `/skills`, `/executions`, `/logs` |
| **ASP.NET Core 8 Backend** | `http://localhost:5254` | HTTP / REST | `/swagger`, `/health`, `/api/info`, `/api/environment`, `/api/skills` |
| **AG-UI Protocol Server** | `http://localhost:5000` | HTTP / SSE | `/api/agui` (Server-Sent Events streaming with HITL interrupts) |
| **Terminal.Gui C# TUI** | Console (`npm run tui` or `dnaplatform tui`) | stdio / Terminal | Windowed TUI dashboard with menu, health probe, and status bar |
| **ACP Adapter** | `src/08_ACP` (`node dist/index.js`) | stdio / JSON-RPC | `initialize`, `session/new`, `session/prompt`, `session/cancel` |
| **Polyglot CSnakes Kernel** | `http://localhost:3000/api/polyglot/execute` | HTTP POST | Python 3, C# Interactive, JavaScript, and Bash cell execution |

### Troubleshooting

1. **Port Conflicts (`address already in use`)**:
   - Backend (:5254) or AG-UI (:5000) or Web (:3000) is held by an orphaned background process.
   - **Fix on Linux/macOS**: `lsof -i :5254` then `kill -9 <PID>`.
   - **Fix on Windows**: `netstat -ano | findstr :5254` then `taskkill /PID <PID> /F`.
   - Or run the pre-clean command: `npm run test:clean` / `node scripts/2-test-dev.mjs --clean`.

2. **SQLite `file:` Protocol Validation (`Error validating datasource db`)**:
   - Prisma's SQLite connector strictly requires the `file:` scheme.
   - `src/lib/server/db.ts` automatically normalizes `process.env.DATABASE_URL` to `file:<absolute_path>/dev.db`.
   - If resetting manually: run `npm run bootstrap` to regenerate and push the schema.

3. **Linux Globalization (ICU missing)**:
   - Exception: `Process terminated. Couldn't find a valid ICU package on that assembly`.
   - **Fix**: Export `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` before launching dotnet or use `mise run dev`.

4. **SvelteKit Route Shadowing & Vite Proxy**:
   - SvelteKit server routes (`src/routes/api/...`) take precedence over Vite's `server.proxy`.
   - If a route returns unexpected data or 404, verify whether it is handled by SvelteKit's Prisma handlers or proxied to .NET in `src/05_DevUI/DNAPlatform.DevUI.Web/vite.config.ts`.

---

## 7. Final Intent Summarized

DeNovoAutomata's final objective is to be an **open, visual, n8n-inspired computational biology workbench**:
- **Zero Heavy Infrastructure**: Runs locally on Linux, macOS, and Windows with zero configuration overhead.
- **The 3 Focused Scripts**: Initial setup (`npm run bootstrap`), development testing/cleaning (`npm test`), and immediate startup (`npm start`).
- **Autonomous Governance**: AI agents act as active computational participants while giving human operators complete visibility through real-time thinking traces and Human-in-the-Loop authorization.
