# AGENTS.md

Repository-specific guidance for agents working on the DNA Platform.

## Layout

- `src/01_Core/DNAPlatform.Core` — **CSnakes Polyglot Python-C# Bridge**
  (`CSnakes.cs`, `BioinformaticsTypes.cs`). High-performance zero-copy execution of
  Python modules, scripts, and bioinformatics algorithms directly from C#.
- `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework` — the **single**
  workflow/agent engine (`WorkflowOrchestrator`, `AgentManager`, `NodeExecutor`).
  The DevUI API, the ConsoleApp demo and the AGUI server all bind this one
  instance; there is no second orchestrator anywhere in the repo.
- `src/05_DevUI/DNAPlatform.DevUI.API` — ASP.NET Core 8 backend (port 5254).
  `Program.cs` holds DI registration + Minimal APIs, `Controllers/` holds the
  attribute-routed endpoints, `Services/` holds `MiseService`, `LogStore` and
  `SkillBackedNodeExecutor` (the bridge from a workflow node to the real skill
  registry).
- `src/05_DevUI/DNAPlatform.DevUI.AGUI` — **AG-UI Server** (port 5000), implementing
  the official AG-UI protocol specification with Human-in-the-Loop (HITL) approval
  interrupts, reasoning traces (`THINKING_DELTA`), and bi-directional state synchronization.
- `src/05_DevUI/DNAPlatform.DevUI.Web` — SvelteKit frontend (port 5173 / port 3000).
  Prisma schema is at `src/05_DevUI/DNAPlatform.DevUI.Web/prisma/schema.prisma`.
- `src/06_Skills` — skill registry (`DNAPlatform.Skills` + `DNAPlatform.Skills.Core`).
  The API consumes the real registry directly; the old
  `AgentFrameworkStubs.cs` shim is **deleted**.
- `src/07_CLI/DNAPlatform.Cli` — `dnaplatform`, an HTTP-only CLI client. See
  the CLI section below.
- `src/08_ACP` — **ACP adapter** (Node/TypeScript, standalone). Maps ACP methods
  to HTTP calls to the .NET backend at :5254. Own `package.json`; not a dependency
  of DevUI.Web.
- `src/09_Tests/DNAPlatform.Tests` — xUnit tests for the orchestrator
  (8 tests). `dotnet test` is the only automated gate; the frontend has none.
- `scripts/` — `dna.js` / `dna` (unified cross-platform CLI & TUI runner with
  `smoketest`, `bootstrap`, `clean`, `tui`, and `env`). Root `.bat` files delegate
  seamlessly to this unified runner across Windows, macOS, and Linux.
- `docs/architecture.md` — Complete platform architecture, folder structure, agentic guide, gotchas, and final intent.
- `docs/development.md` — Consolidated development logs, task lists (TODO), and milestone roadmap.

## Toolchain

- .NET 8 SDK (target framework is `net8.0`, not .NET 10 � some docs are stale).
- Node 20+ / npm.
- On Debian, ICU is often missing; export
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` before running `dotnet`.

## Build & run

```bash
# One-time setup: toolchain probe, dependencies, and Prisma database initialization
npm run bootstrap
# or: node scripts/1-bootstrap.mjs

# Launch development environment (instant boot)
npm start
# or: node scripts/3-denovo-automata.mjs

# Verification & test suite (10 automated checks with --clean pre-push option)
npm test
# or: node scripts/2-test-dev.mjs
```

`run-all.bat` starts both. Ports: 5173 SvelteKit, 5254 .NET, 11434 Ollama
(planned, not yet wired).

## Routing ownership (the rule that matters most)

SvelteKit routes win over the Vite proxy, so CRUD never leaves the app. Only
compute and static metadata are proxied. Verify against
`DNAPlatform.DevUI.Web/vite.config.ts` before changing any route.

| Route | Served by | Notes |
|---|---|---|
| `/api/workflows` (GET/POST) | SvelteKit | Prisma CRUD |
| `/api/workflows/[id]` | SvelteKit | Prisma |
| `/api/workflows/[id]/execute` | **SvelteKit** | Loads the Prisma row, calls .NET server-to-server, then INSERTs `Execution` |
| `/api/workflows/execute` | **.NET** (proxied) | Ad-hoc; the definition comes from the request body. Used by the designer |
| `/api/agents`, `/api/agents/[id]` | SvelteKit | Prisma |
| `/api/executions` | SvelteKit | Prisma, list only |
| `/api/snippets`, `/api/snippets/[id]` | SvelteKit | Prisma |
| `/api/snippets/[id]/execute` | SvelteKit ? .NET | hello-world .NET handler only |
| `/api/logs` | SvelteKit ? .NET | SvelteKit exports GET/DELETE only and proxies to `LogsController` |
| `/api/skills*` | .NET (proxied) | `DNAPlatform.Skills` registry |
| `/api/node-types`, `/api/status-values`, `/api/info`, `/health` | .NET (proxied) | Static metadata, `Program.cs:113-161` |
| `/api/environment*` | .NET (proxied) | `EnvironmentController`; mise introspection. No SvelteKit route |

Two easy-to-confuse points:

- `/api/workflows/execute` (proxied to .NET) and `/api/workflows/[id]/execute`
  (handled by SvelteKit) are **different endpoints**. Only the first appears in
  `vite.config.ts`.
- The `WorkflowsController` / `AgentsController` list endpoints are **dead in
  dev** � SvelteKit shadows those paths, so browser traffic never reaches them.
  Changes there are invisible from the UI and only matter when calling `:5254`
  directly.

## Response envelope conventions

Two conventions coexist. Match the layer you are editing.

- SvelteKit routes ? lowercase singular/plural key, no `Count`:
  `{ workflows }`, `{ workflow }`, `{ agents }`, `{ agent }`, `{ executions }`.
  These also pre-parse the JSON blob columns into real arrays.
- .NET controllers ? PascalCase with `Count`: `{ Count, Skills }`,
  `{ Count, Workflows }`, `{ Count, Category, Skills }`, and for skill execution
  `{ Success, SkillId, Data, ExecutedAt, DurationMs }`.
  Exceptions: `GET /api/agents` returns `{ Count, AgentIds }`, and
  `GET /api/skills/{id}` returns an unwrapped `GetMetadata()` object.

`src/lib/services/api.ts` reads the lowercase keys, so the frontend is pinned to
the SvelteKit convention. Error bodies are `{ error }` on SvelteKit and
`{ Message }` or `{ Success, Error }` on .NET.

## Architecture notes

- **One engine.** `Program.cs` registers the real
  `DNAPlatform.AgentFramework.WorkflowOrchestrator` and the real
  `DNAPlatform.Skills.SkillRegistry`. There is no in-memory double left; the files
  that used to provide one are deleted. `SkillBackedNodeExecutor` runs a
  `ProcessingSkill` node through the same registry that
  `POST /api/skills/{id}/execute` uses, so a node run and a direct skill run take
  the same code path.
- `ISkillRegistry` is ambiguous between `DNAPlatform.AgentFramework` and
  `DNAPlatform.Skills`; fully qualify it.
- Workflow node types are strings (`"Input"`, `"Agent"`, ...) end to end.
  `Program.cs` registers a `JsonStringEnumConverter` so enums bind from and
  serialize to strings.
- `PlatformVersion` in `Program.cs` is the single source of truth for the version
  reported by `/health`, `/api/info`, the startup banner and the Swagger doc.
  Keep README.md and docs/development.md in step with it.

## CLI

`src/07_CLI/DNAPlatform.Cli` (`dnaplatform`). Subcommands: `run`, `validate`,
`status`, `logs`, `cancel`, `env`.

- **HTTP only.** No `ProjectReference` to `DNAPlatform.AgentFramework` and it
  never constructs a `WorkflowOrchestrator`. It asks the platform to run things.
  Adding a project reference would reintroduce the second-engine split that
  `docs/architecture.md` �15 forbids.
- **It needs two base URLs.** `--url` (default `DNA_PLATFORM_URL`, then
  `http://127.0.0.1:5173`) is the DevUI origin and serves `/api/workflows`,
  `/api/executions`, `/api/snippets` and `/api/logs`. `--api-url` (default
  `DNA_PLATFORM_API_URL`, then `http://127.0.0.1:5254`) is the .NET origin and is
  used for `cancel`, because `POST /api/executions/{id}/cancel` lives on
  `ExecutionsController` and is **not** in the `vite.config.ts` proxy list.
  Pointing everything at `:5254` makes `status` 404.
- `run` and `validate` exit non-zero when the workflow did not complete or did not
  validate, so a script can branch. `env` works with either server up and says
  which origin it read.
- Status is never colour-alone: every state prints a word plus a short marker.

## ACP adapter

`src/08_ACP` � standalone Node/TypeScript project using `@agentclientprotocol/sdk`
(Apache-2.0, Zed Industries). Own `package.json`; **not** added to DevUI.Web deps.

**Transport rules (non-negotiable):**
- stdin = client?agent JSON-RPC frames (newline-delimited)
- stdout = agent?client JSON-RPC frames � **stdout must stay PROTOCOL-PURE**
- ALL diagnostics (console.log, errors, warnings) go to stderr
- A stray `console.log` on stdout corrupts the stream

**Method mapping:**
- `initialize` ? no HTTP call; responds with agent capabilities (`promptCapabilities.text=true`, no session lifecycle methods)
- `session/new` ? `GET /api/info` health check; returns `{ sessionId }` on success, JSON-RPC error `-32603` if backend down
- `session/prompt` ? `POST /api/workflows/execute` with workflow JSON from the prompt's text content block; returns `{ stopReason: "end_turn" }` on success, `{ stopReason: "refusal" }` on failure
- `session/cancel` ? notification only (no response); logs and returns � ad-hoc execute has no persisted execution ID to cancel

**Run:**
```bash
cd src/08_ACP
npm install
npm run build
node dist/index.js   # or: npx tsx src/index.ts
```

**Smoke test:**
```bash
cd src/08_ACP
npm run smoke        # pipes a real session, asserts valid JSON-RPC on stdout
```

**Environment variable:**
- `DNA_PLATFORM_API_URL` � default `http://127.0.0.1:5254`

## Persistence constraints

- SvelteKit server routes use Prisma/SQLite. **SQLite has no `Json` column
  type**, so `Workflow.nodes`, `Workflow.connections`, `Execution.outputs`,
  `Execution.errors`, `Execution.nodeResults` and every `CustomSnippet` field are
  `String` and must be `JSON.parse`d in the route handler.
- Models: `Workflow`, `Agent`, `Execution`, `CustomSnippet`, `WorkflowVersion`.
  `Execution.workflowId` is a nullable FK with `onDelete: SetNull`.
- Node `x`/`y` positions live inside the `nodes` JSON blob � there is no
  separate column.
- `Execution.duration` is an integer column of milliseconds, and
  `parseDurationMs` rounds to it. Any run under 1 ms stores `0`. See
  docs/development.md ? *Next Up*.

## Serialization gotchas

- .NET serializes `TimeSpan` as a string such as `"00:00:00.2246541"`.
  `api/workflows/[id]/execute/+server.ts` carries a hand-rolled `parseDurationMs`
  that tries `duration` ? `durationMs` ? `fullExecution.duration` in that order.
  Renaming the .NET field silently breaks duration reporting.
- Status casing is compared defensively (`'Completed' || 'completed'`). Keep new
  code casing-tolerant or normalize at the boundary.

## Frontend

- Workflow designer: `src/lib/components/WorkflowDesigner.svelte`, opened from
  `/workflows` via the "Edit" action. It accepts an optional `execution` prop
  (`{ workflowId, nodeResults: [] }`) and derives each node's chip from it; a node
  the engine never reported stays `Pending` rather than being given a state.
  Render per-node status with colour **+ icon + literal word**, never colour
  alone. Guard with `(nodes || [])` � `Workflow.Nodes` is expected to become
  non-nullable, and a null today would throw inside the reactive block.
- Connection editing: click a source port to arm (`pendingFrom`), click a target
  port to commit, click the canvas to cancel. Edges are SVG `<path>`s between node
  ports with `marker-end: url(#arrow)`; a wide transparent hit path carries the
  click so the thin curve stays visible.
- Test step: `WorkflowTestResult.svelte` renders
  `POST /api/workflows/validate`. It is labelled **structural only** and must not
  grow a speculative validation engine (`docs/architecture.md` �4).
- Logs console: `/logs`, backed by `src/lib/services/logs.ts` (merges frontend
  console capture with the backend buffer).
- Pages: `/`, `/workflows`, `/skills`, `/agents`, `/executions`, `/logs`, `/about`.

## Extension recipes

- **New node type** ? add the string to the `NodeTypes` array in `Program.cs`,
  then add it to the designer palette in `WorkflowDesigner.svelte`.
- **New skill** ? add a class under `src/06_Skills/BuiltIn/` or
  `src/06_Skills/Bioinformatics/`; it self-registers through the registry.
- **New proxied route** ? add the path to `vite.config.ts` `server.proxy`, or
  SvelteKit 404s it before it ever reaches .NET. If the CLI needs the same route,
  remember it defaults to :5173 and only has `--api-url` for the .NET-only ones.
- **New persisted field** ? add it to `schema.prisma`, run `npx prisma db push`,
  and `JSON.stringify` / `JSON.parse` at the route boundary.

## Verification

`dotnet test` covers the orchestrator. For the stack:

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-e2e.ps1
```

Three stages, real exit code. A: `dotnet build` + `npm run check` against a
recorded baseline. B: backend on :5254, static metadata routes, a skill
execution, and `dnaplatform env`. C: `npm run build`, dev server on :5173, the
Prisma route, the proxied routes, and `dnaplatform validate` / `status`. Both
servers are stopped afterwards; a leaked :5254 breaks the next build and a leaked
:5173 breaks the next `prisma generate`.

`npm run check` is **red on arrival** � 6 errors and 17 warnings in 7 files. The
e2e script fails only on a *new* diagnostic, so treat a rise in those numbers as
a real regression.

The manual pass that is still not automated: create a workflow on `/workflows`,
Configure ? Test ? Execute, confirm a `Completed` row on `/executions` with a
per-node chip in the designer, and check `/logs` for the mirrored request lines.
That sequence exercises the Prisma write, the .NET execution call, and duration
parsing together.

## Gotchas

- `launchSettings.json` pins `https://localhost:5001;http://localhost:5000`.
  Always pass `--no-launch-profile --urls http://127.0.0.1:5254` so the Vite
  proxy target matches. `Program.cs` also skips `UseHttpsRedirection` in
  Development for the same reason.
- **`--no-launch-profile` also drops `ASPNETCORE_ENVIRONMENT=Development`**, which
  `launchSettings.json` would otherwise supply. So the mandated command starts the
  API in Production and `/swagger` returns 404. Verified: with
  `$env:ASPNETCORE_ENVIRONMENT=Development` set, `/swagger/v1/swagger.json` and
  `/swagger/index.html` both answer 200. `run-all.bat` does *not* pass the flag,
  which is why Swagger works there.
- If `dotnet run` fails with "address already in use", an older build still holds
  the port. On Windows: `netstat -ano | findstr :5254`, then
  `taskkill /PID <pid> /F`. On Linux: `ss -ltnp | grep 5254`.
- A Vite child can outlive its `npm` parent and keep `[::1]:5173` held. That is
  invisible on `127.0.0.1` and then breaks `prisma generate` with `EPERM` on
  `query_engine-windows.dll.node`. Check both address families, and use
  `taskkill /T /F /PID` on the wrapper.
- Do **not** add a Minimal API mapping for `/api/logs`. `LogsController` already
  owns that route, and two handlers for one route throw
  `AmbiguousMatchException` at request time � see the warning in `Program.cs`.
- Several files under `docs/` are stale relative to the code. When they disagree
  with the source, trust the source: `Program.cs`, `vite.config.ts`,
  `schema.prisma`, and the `+server.ts` route handlers.

## Mise

- `mise.toml` at the repo root pins the two platform-provided defaults and nothing else:
  `dotnet = "8"` and `node = "lts"` (refactor.md �4, lines 154-159). It also sets
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` and defines the `build`, `build:web`, and
  `dev` tasks. Task names containing `:` must be quoted (`[tasks."build:web"]`).
- `MiseService` (`Services/MiseService.cs`) is the only thing that talks to mise. It
  shells out to the CLI with a hard 5 s timeout and degrades to `MiseInstalled = false`
  + `"yellow"` when mise is absent, off PATH, or hangs. It never throws for environment
  reasons. `EnvironmentController` owns `/api/environment`; do not add Minimal API
  mappings for those routes.
- Commands depended on, and their exit-code contract:
  - `mise --version` � exit 0 means available; anything else is treated as "not installed".
  - `mise --cd <root> ls --json` � exit 0 with JSON keyed by tool name. Each entry may
    carry `version`, `install_path`, `installed`, `active`, `source`. An entry is only
    considered usable when `active && installed`; missing fields degrade, never throw.
  - `mise --cd <root> tasks ls --json` � exit 0 with an array of `{ name, description }`.
  - Any nonzero exit code, unparsable stdout, or timeout yields an empty result rather
    than an error, which downgrades the affected runtimes to `yellow`/`red`.
- Status semantics: GREEN = requested tool+version resolves and is installed/active;
  YELLOW = missing but a platform default (.NET 8, Node LTS) can serve it; RED =
  unavailable/incompatible/unresolvable. Only .NET 8 and Node LTS count as fallbacks.

### mise MCP server (experimental, NOT wired into the platform)

`mise mcp` is an **EXPERIMENTAL** Model Context Protocol server over stdio. It requires
`MISE_EXPERIMENTAL=1` and refuses to start without it. `IMiseService` shells out to the
mise CLI directly � nothing in this repo launches or speaks to `mise mcp`.

Client configuration shape:

```json
{
  "mcpServers": {
    "mise": {
      "command": "mise",
      "args": ["--cd", "<project>", "mcp"],
      "env": { "MISE_EXPERIMENTAL": "1" }
    }
  }
}
```

Resources: `mise://tools` (add `?include_inactive=true` for other installed versions),
`mise://tasks`, `mise://env`, `mise://config`.

Tools:
- `list_commands` � every mise command with its declared effect.
- `run_task` � works. Runs a project task with the user's full account permissions.
- `install_tool` � advertised but currently returns "not yet implemented". Install with
  `mise install` outside MCP instead.

**SECURITY.** `mise://env` returns real resolved values including secrets, and
`run_task` executes project tasks with the user's full permissions (it sets `MISE_YES=1`
and cannot prompt). MCP access to mise must therefore be **opt-in** and scoped to trusted
projects only. Never enable it for untrusted checkouts, and never auto-grant
`run_task` to an assistant.
