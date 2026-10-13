# DNA Platform -- Task List

> Priority ordering from refactor.md section 16. State reflects GAP_ANALYSIS.md
> (2026-10-05) and README.md "In Development" (2026-10-12). When they disagree,
> README is more recent.

--

## Priority 1 -- Real-time log streaming  [IN PROGRESS]

Highest open item. No SSE/SignalR/WebSocket exists; /logs polls every 1.5 s
against an in-memory ring buffer that dies on backend restart.

- [ ] Add SSE endpoint in .NET: GET /api/logs/stream (keepalive/heartbeat)
- [x] Add /api/logs/stream to vite.config.ts proxy list
- [ ] Bound the unbounded channel in LogStreamService (real backpressure)
- [x] Fix src/lib/services/logs.ts:
  - [x] Track EventSource in module variable
  - [x] Use addEventListener('log') for SSE entries
  - [x] Push SSE entries to shared logRecords store
  - [x] Wire up polling fallback when SSE unavailable
- [x] Fix src/routes/logs/+page.svelte:
  - [x] Use $logRecords store
  - [x] Call captureConsole() / startLogStream() on mount
- [ ] Persist logs to DB (currently in-memory only) -- required for history survival

--

## Priority 2 -- ACP adapter  [DONE]

Thin Node/TypeScript adapter in src/08_ACP/ mapping initialize / session/new /
session/prompt / session/cancel to HTTP calls against :5254. Zero DAG logic in
TypeScript. stdout protocol-pure; diagnostics to stderr. Smoke test passes.

- [x] src/08_ACP/ -- full implementation, npm run build, npm run smoke

> GAP_ANALYSIS.md row 16 still shows "not started"; README section Phase 3.75
> confirms done. Treating as done.

--

## Priority 3 -- Workflow-level Test  [DONE -- structural only]

POST /api/workflows/validate proxied, "Test" on /workflows,
WorkflowTestResult.svelte renders { isValid, errors, warnings, checks, summary }.
Labelled "structural only" by design.

- [x] Structural validation endpoint + UI
- [ ] OPEN (from GAP row 12): Make environment check BLOCK execution
  - [ ] src/routes/api/workflows/[id]/execute/+server.ts must call
        POST /api/environment/verify before forwarding to .NET
  - [ ] Execution should fail/abort when verification returns non-GREEN

--

## Priority 4 -- Environment verification before execution  [PARTIAL]

POST /api/environment/verify exists and re-runs every check, but nothing blocks
execution on the result.

- [ ] Gate workflow execution on environment verification result (see Priority 3
      open item)
- [ ] Surface verification status in the Execute flow (toast / blocking modal)

--

## Priority 5 -- Workflow Designer UX (Pipeline + Elsa refs)  [NOT STARTED]

Canvas has drag-and-drop, node palette, IO ports, inline editor, drawn SVG
connections, per-node execution chips. Missing:

- [ ] Zoom / pan (infinite canvas)
- [ ] Typed connections (visual type compatibility on ports)
- [ ] Minimap / overview controls
- [ ] Connection validation feedback during wiring
- [ ] Per-node validation state (distinct from execution state)

--

## Priority 6 -- Skills UI placeholders  [NOT STARTED]

src/routes/skills/+page.svelte has neither action.

- [ ] Add "Import MCP Plugins" button + shell interaction
- [ ] Add "Add New Plugin" button + shell interaction
- [ ] Create appropriate data/API shape (minimal -- no marketplace/install logic yet)

--

## Priority 7 -- Agent configuration model  [NOT STARTED]

Prisma Agent model is 7 flat columns. No AgentConfiguration model exists.
Must land provider-agnostic -- seam first, Microsoft Agent Framework (if ever)
as one implementation behind it (see AGENT_FRAMEWORK_INTEGRATION.md).

- [ ] Design AgentConfiguration Prisma model (prompt, model, tools, MCP, memory,
      RICE, vector knowledge, permissions, runtime)
- [ ] Add migration / prisma db push
- [ ] Build Agent page configuration UI (system prompt, model selector, tool list,
      MCP integrations, memory/context, RICE, vector knowledge, permissions,
      runtime/environment)
- [ ] Implement provider-agnostic IAgent seam (no new package)

--

## Priority 8 -- PostgreSQL / pgvector migration  [NOT STARTED]

schema.prisma uses provider = "sqlite". No PostgreSQL, Docker, or compose file in
repo. JSON-blob columns (nodes, connections, outputs, errors, nodeResults,
CustomSnippet fields) are String due to SQLite lack of Json type.

- [ ] Add PostgreSQL provider + connection string via env
- [ ] Migrate schema: replace JSON String columns with proper Json / relational
      models (WorkflowNode, WorkflowEdge, WorkflowEvent, Plugin,
      AgentConfiguration, MiseConfiguration, ValidationResult, Embedding)
- [ ] Add pgvector extension + embedding columns
- [ ] Update bootstrap / CI to provision Postgres (Docker Compose for dev)
- [ ] Verify all Prisma routes work with PostgreSQL types

--

## Priority 9 -- Architecture for future Code Blocks  [NOT STARTED -- deliberate]

refactor.md section 11 explicitly says not to force this abstraction yet.
schema.prisma has no CodeBlock model; src/06_Skills/ exposes ISkill only.

- [ ] Design CodeBlock Prisma model (language, source, entrypoint, IO schema, version)
- [ ] Define Skill -> Code Block -> Workflow Node mapping contract
- [ ] Keep as architectural placeholder -- no implementation until repo has
      appropriate abstraction

--

## Additional items from README "In Development" (2026-10-12)

- [ ] Export/import workflow JSON with runtime environment metadata
- [ ] Agent-backed nodes in workflows
- [ ] Real custom code execution path for polyglot kernel snippets
      (hello-world placeholder is live)

--

## Deferred (with rationale -- do not reopen)

- PostgreSQL/pgvector as primary datastore (Priority 8 -- tracked above, but
  deferred until SQLite becomes a blocker)
- ACP v2 / expression-selector inputs / sensor blocks / Suspended workflow state
- Microsoft Agent Framework adoption (deferred behind IAgent seam --
  see AGENT_FRAMEWORK_INTEGRATION.md)
- Redis / Kafka / separate vector DB (refactor.md section 17: do not introduce
  infrastructure just because a reference project uses it)
- Full plugin marketplace / installation system (Priority 6 is shell only)
- Full Code Block system (Priority 9 is architecture prep only)
