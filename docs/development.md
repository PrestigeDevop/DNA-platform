# DeNovoAutomata — Development Guide & Activity Log

> **Purpose**: This single document consolidates development history, completed phase milestones, active task backlog (TODO), and diagnostic logging guidelines for DeNovoAutomata.

---

## 1. Development Log & Phase History

### Phase 1: The Single Unified Orchestrator Engine (Completed)
- Established `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework` as the single authoritative workflow engine.
- Implemented `WorkflowOrchestrator`, `AgentManager`, and `NodeExecutor`.
- Verified with 8 automated xUnit tests in `src/09_Tests/DNAPlatform.Tests`.

### Phase 2: Visual Designer & Persistence (Completed)
- Built interactive drag-and-drop workflow canvas in SvelteKit (`src/lib/components/WorkflowDesigner.svelte`).
- Configured SQLite persistence with Prisma schema for `Workflow`, `WorkflowVersion`, `Agent`, `Execution`, and `CustomSnippet`.
- Wired CRUD endpoints in `/api/workflows`, `/api/agents`, `/api/executions`.

### Phase 3: Structural Validation & Environment Verification (Completed)
- Created `POST /api/workflows/validate` for acyclic graph (DAG) cycle detection and port connectivity checks.
- Integrated `mise.toml` environment introspection (`GET /api/environment`) via `MiseService.cs`.

### Phase 3.75: ACP (Agent Client Protocol) Adapter (Completed)
- Implemented standalone Node.js/TypeScript ACP adapter in `src/08_ACP/`.
- Maps stdio JSON-RPC protocol to the HTTP API on port 5254 with pure stdout discipline.

### Phase 4: AG-UI Protocol Server & Human-in-the-Loop (HITL) (Completed)
- Implemented official AG-UI protocol specification ([docs.ag-ui.com](https://docs.ag-ui.com/sdk/dotnet/hosting/overview)).
- Hosted framework-agnostic AG-UI server in .NET 8 on port 5000 and mirror route in SvelteKit at `/api/agui`.
- Full streaming lifecycle: `RUN_STARTED`, `THINKING_DELTA`, `TOOL_CALL_STARTED/FINISHED`, `APPROVAL_REQUESTED` (HITL Interrupt), `STATE_SNAPSHOT`, `STATE_DELTA` (JSON Patch), `TEXT_DELTA`, `RUN_FINISHED`.
- Built interactive `AgUiStudio.svelte` component embedded on `/agents` with live operator authorization cards.

### Phase 5: CSnakes Polyglot Python-C# Interop in Core (Completed)
- Implemented in `src/01_Core/DNAPlatform.Core` (`CSnakes.cs` and `BioinformaticsTypes.cs`).
- Zero-overhead in-process Python script and module execution for BioPython, NumPy, and biological algorithms.
- Strongly-typed `DnaSequenceMetrics` calculations: GC content, GC skew, nucleotide frequencies, and reverse complements.
- Pure C# fallback ensures continuous operation when host systems lack Python binaries.

### Phase 6: Terminal.Gui Native TUI & 3 Focused Cross-Platform Scripts (Completed)
- Added `Terminal.Gui` (gui.cs) to `src/07_CLI/DNAPlatform.Cli` with native windowed console studio (`TerminalGuiDashboard.cs`).
- Replaced fragmented scripts with 3 focused, cross-platform runners:
  - `scripts/1-bootstrap.mjs` (`npm run bootstrap`): First-time install & database setup.
  - `scripts/2-test-dev.mjs` (`npm test`): Comprehensive 10-check E2E test suite with `--clean` pre-push artifact clean.
  - `scripts/3-denovo-automata.mjs` (`npm start`): Animated, colored runner with instant < 1s boot and hotkey controls.

---

## 2. Active Task List (TODO)

### Priority A: Additional Bioinformatic Skills
- [ ] Add Multi-FASTA batch parser skill in `src/06_Skills/Bioinformatics/`.
- [ ] Add IUPAC Degenerate Codon Translation Table skill.
- [ ] Add Local BLAST query runner skill.

### Priority B: Pre-Execution Environment Gating
- [ ] Make `POST /api/environment/verify` block execution on `/api/workflows/[id]/execute` if any required tool runtime returns non-GREEN.

### Priority C: Durable Step Replay
- [ ] Record intermediate node outputs to `Execution.nodeResults` for offline step replay and checkpointing.

---

## 3. Diagnostic & Logging Reference

DeNovoAutomata maintains an in-memory and file-backed diagnostic log ring buffer accessible via:
- **API Endpoint**: `GET /api/logs?sinceId=0&limit=100`
- **Web UI**: `/logs` (with live console capture and filtering)
- **Log Levels**: `Information`, `Warning`, `Error`
- **Categories**: `WorkflowOrchestrator`, `AGUI`, `PolyglotKernel`, `MiseService`, `SkillRegistry`

---

## 4. Verification Gate

Before committing code or submitting pull requests, run:
```bash
# Run full automated test suite (10 checks)
npm test

# Run tests and clean build artifacts before Git push
npm run test:clean
```
