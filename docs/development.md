# DeNovoAutomata — Development Guide, AGENTS & Platform Documentation

> **Consolidated Platform Reference**: This document merges historical development logs, the AGENTS engineering guide, command-line usage specifications, gap analysis status, and roadmap.

---

## 📖 Documentation & Platform Guide

### 1. The Single Unified Engine
`DNAPlatform.AgentFramework` (`src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework`) is the **single** workflow and agent engine across the entire repository. The ASP.NET Core API, SvelteKit DevUI, the AG-UI server, and the CLI all bind to this one instance. There is no second orchestrator anywhere in the repository.

### 2. Command Line Usage

The platform provides dual CLI/TUI interfaces: the native C# executable `dnaplatform` (`src/07_CLI/DNAPlatform.Cli`) powered by **Terminal.Gui** and the cross-platform Node runner `scripts/dna.js` (aliased via `npm run cli`).

#### Subcommands

| Command | Usage | Description |
|---|---|---|
| `tui` | `dnaplatform tui`<br>`npm run tui` | Launches the interactive **Terminal.Gui** windowed console studio with menus, health monitor, and keybindings. |
| `run` | `dnaplatform run <workflow-id>` | Executes a stored workflow through the API and prints per-node execution chips. |
| `validate` | `dnaplatform validate <workflow-id>` | Runs structural DAG validation (cycle detection, port links) without executing compute. |
| `status` | `dnaplatform status [--all]` | Queries recent execution status and timing metrics. |
| `logs` | `dnaplatform logs [--limit 50]` | Streams real-time diagnostic log records from the backend ring buffer. |
| `cancel` | `dnaplatform cancel <exec-id>` | Dispatches a cancellation request to abort a running workflow. |
| `env` | `dnaplatform env` | Probes host toolchain and runtime status reported by the API. |
| `smoketest` | `npm test`<br>`dnaplatform smoketest` | Runs the 10-point automated end-to-end verification suite. |
| `bootstrap` | `npm run bootstrap`<br>`dnaplatform bootstrap` | Probes toolchain, restores dependencies, builds Terminal.Gui app, and synchronizes SQLite database. |
| `clean` | `npm run test:clean`<br>`dnaplatform clean` | Cleans temporary build artifacts, SQLite journals, and cache files before Git push. |

### 3. Response Envelope Conventions
- **SvelteKit Routes**: Use lowercase singular/plural keys (`{ workflows }`, `{ workflow }`, `{ agents }`, `{ agent }`, `{ executions }`). JSON columns are pre-parsed into objects before returning to the frontend.
- **ASP.NET Core Controllers**: Use PascalCase with counts (`{ Count, Skills }`, `{ Count, Workflows }`, `{ Success, SkillId, Data, ExecutedAt, DurationMs }`).
- **Error Payloads**: `{ error }` on SvelteKit and `{ Message }` or `{ Success, Error }` on .NET.

### 4. Persistence & Database Constraints
- Persistence uses **Prisma with SQLite** (`src/05_DevUI/DNAPlatform.DevUI.Web/prisma/schema.prisma`).
- **No Native JSON Type**: SQLite stores complex objects (`Workflow.nodes`, `Workflow.connections`, `Execution.nodeResults`) as `String` columns. Always serialize/deserialize at the route boundary.
- **Database Path**: `DATABASE_URL` must use the `file:` scheme. Normalized automatically by `src/lib/server/db.ts`.

### 5. Extension Recipes
- **New Node Type**: Add string to the `NodeTypes` array in `Program.cs`, then register in `WorkflowDesigner.svelte` palette and inspector.
- **New Skill**: Add class under `src/06_Skills/BuiltIn/` or `src/06_Skills/Bioinformatics/`; self-registers through `DNAPlatform.Skills.SkillRegistry`.
- **New Proxied Route**: Add path to `vite.config.ts` `server.proxy`, otherwise SvelteKit will capture and 404 before reaching .NET.

---

## 🧪 Current Status & Gap Analysis

All baseline platform capabilities, AG-UI protocol streaming, CSnakes polyglot interop, Terminal.Gui TUI, and cross-platform scripting are 100% operational.

### Achieved ✅

- ✅ **ACP adapter** (`src/08_ACP/`) — Thin Node/TypeScript adapter over the HTTP API using `@agentclientprotocol/sdk`. Maps `initialize`, `session/new`, `session/prompt`, and `session/cancel` to `:5254`. Pure stdout discipline for JSON-RPC frames with stderr diagnostic logging.
- ✅ **CSnakes Polyglot in Core** (`src/01_Core/DNAPlatform.Core/`) — Zero-overhead Python/BioPython and C# interop. High-speed biological sequence metrics (`DnaSequenceMetrics`), GC content, GC skew, and reverse complements.
- ✅ **AG-UI Protocol & Human-in-the-Loop** (`src/05_DevUI/DNAPlatform.DevUI.AGUI/`) — Server-Sent Events streaming server supporting `RUN_STARTED`, `THINKING_DELTA`, `APPROVAL_REQUESTED` interrupts, `STATE_SNAPSHOT`, `STATE_DELTA` (JSON Patch), and `RUN_FINISHED`. Includes interactive operator authorization cards in the DevUI.
- ✅ **Native C# Terminal.Gui TUI** (`src/07_CLI/DNAPlatform.Cli/`) — Windowed console interface using Miguel de Icaza's `Terminal.Gui` (`gui.cs`) with MenuBar, StatusBar, ColorScheme, and ListView. Automatically compiled on bootstrap.
- ✅ **The 3 Focused Cross-Platform Scripts** (`scripts/`):
  - `1-bootstrap.mjs` (`npm run bootstrap`): Toolchain check, dependencies install, Terminal.Gui build, and database initialization.
  - `2-test-dev.mjs` (`npm test`): 10-check E2E suite with `--clean` pre-push artifact cleaner.
  - `3-denovo-automata.mjs` (`npm start`): Animated, instant runner with keybindings (`[R]`estart, `[T]`est, `[C]`lean, `[L]`ogs, `[O]`pen, `[Q]`uit).
- ✅ **Visual DAG Workflow Designer** — SvelteKit WYSIWYG canvas with cycle detection, parallel execution chips, and node connection editing.
- ✅ **Skill Catalog & Clock Provider** — Needleman-Wunsch sequence alignment, IUPAC genetic sequence validation, and microsecond-precision C# clock providers.

---

## 🛣️ Roadmap

*(Future enhancements will be scheduled strictly based on explicit user requests and requirements.)*

- **Prospective Capability 1: Multi-FASTA Batch Pipeline Processing**: Adding streaming batch sequence loaders for multi-megabase genomic FASTA records.
- **Prospective Capability 2: Codon Optimization & Translation Tables**: Adding native NCBI translation tables (Bacterial, Mitochondrial, Standard Nuclear).
- **Prospective Capability 3: Distributed FSM State Machine Nodes**: Enabling pause-and-resume workflow checkpointing across container restarts.

---

## 🔬 Verification Gate

Run the automated test suite before any Git commit or push:
```bash
# Run full automated test suite (10 checks)
npm test

# Run tests and clean workspace artifacts before Git push
npm run test:clean
```
