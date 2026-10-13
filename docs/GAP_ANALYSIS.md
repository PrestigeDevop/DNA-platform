# DNA Platform — Gap Analysis

> **What this is:** the gap analysis `refactor.md` §16 (lines 505-524) requires
> *before* any further code changes. It assesses the spec against the code as it
> stands on **2026-10-05**.
>
> **Status:** assessment only. Nothing in this document required a code change.
> **Assesses:** [`refactor.md`](./refactor.md) — the spec, kept verbatim apart from
> its own header note. **Companion decision record:**
> [`AGENT_FRAMEWORK_INTEGRATION.md`](./AGENT_FRAMEWORK_INTEGRATION.md).
>
> **Method:** every row cites a first-party file that was read to produce it. Where
> the spec and the code disagree, the code wins — see [`AGENTS.md`](../AGENTS.md)
> → Gotchas.

---

## 📊 Verdict

**Re-scored 2026-10-05 after the consolidation pass** (`tasks 1–8` of the plan;
`ACP` is the only unstarted item). The original assessment is preserved below the
re-score so the delta stays auditable.

Both halves of `refactor.md` §4 Configure are **done**: the workflow-level Configure
view, and the mise GREEN/YELLOW/RED runtime status. The **Test** half now exists
too — structural validation only, wired through the vite proxy, labelled as such.

Of the three highest-priority items in `refactor.md` §16:

| §16 priority | State | Note |
|---|---|---|
| #1 realtime log streaming | 🔲 Not started | Unchanged. Still the highest open item, and the only one whose premise the spec got wrong — there is no transport to reuse, so it has to be built. Start with SSE |
| #2 ACP + CLI | ⚠️ **Half done** | CLI shipped: `src/07_CLI/DNAPlatform.Cli`, six subcommands, HTTP-only. ACP (`src/08_ACP/`) is not started |
| #3 workflow-level Test | ✅ Done | `POST /api/workflows/validate` proxied, `🩺 Test` on `/workflows`, `WorkflowTestResult.svelte`. Structural only, by design |

Two structural findings from the original analysis were **blocking** and are now
resolved: the two-engine split (rows 5, 27) is gone, and the incomplete solution
files / broken `build.ps1` / drifted version string are fixed. Those were the
prerequisites for the CLI and ACP work.

Rows 5, 11, 14, 15, 20, 22, 24, 27 and 29 changed state. Everything else is
carried forward unchanged.

Nothing in this analysis contradicts
[`AGENT_FRAMEWORK_INTEGRATION.md`](./AGENT_FRAMEWORK_INTEGRATION.md), which reaches
the same ordering from the agent-framework side and treats the mise work as landing
ahead of any agent work.

---

## 🎯 Requirement → state → evidence

Priority column is the ordering from `refactor.md` §16 lines 528-538. "hard
constraint" means the row is a `§17` rule, not a feature.

**Re-scored 2026-10-05.** Rows marked ✅/⚠️/🔲 reflect the consolidation pass; the
changed rows carry a `(re-scored)` note with the new evidence.

| # | Spec requirement | Section | State | Evidence (verified first-party path) | Priority |
|---|---|---|---|---|---|
| 1 | Keep SvelteKit + TypeScript | §1, §17 | ✅ Done | `src/05_DevUI/DNAPlatform.DevUI.Web/vite.config.ts`; routes under that project's `src/routes/` | hard constraint |
| 2 | Keep .NET 8 | §1, §17 | ✅ Done | `src/05_DevUI/DNAPlatform.DevUI.API/DNAPlatform.DevUI.API.csproj` → `<TargetFramework>net8.0</TargetFramework>` | hard constraint |
| 3 | Keep Node LTS | §1, §17 | ✅ Done | `mise.toml` → `[tools] node = "lts"`; `mise.toml` → `[env] DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "1"` | hard constraint |
| 4 | Preserve the existing workflow designer | §1, §5 | ✅ Done | `src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/components/WorkflowDesigner.svelte`, opened by `src/routes/workflows/+page.svelte:117` | — |
| 5 | Preserve the existing execution engine | §1, §15 | ✅ Done *(re-scored)* | One engine. `Program.cs` registers the real `DNAPlatform.AgentFramework.WorkflowOrchestrator` and the real `DNAPlatform.Skills.SkillRegistry`. `Services/InMemoryAgentFramework.cs`, `Services/SkillRegistryAdapter.cs` and `src/06_Skills/AgentFrameworkStubs.cs` are **deleted**. `SkillBackedNodeExecutor.cs` runs a `ProcessingSkill` node through the same registry `POST /api/skills/{id}/execute` uses. Covered by 8 tests in `src/09_Tests/DNAPlatform.Tests`, `dotnet test` green | was: blocks #1/#2 |
| 6 | "Preserve the existing CLI" | §1, §6 | ⚠️ **Spec was wrong; now satisfied differently** | There was no CLI to preserve. One is now written from scratch: `src/07_CLI/DNAPlatform.Cli` (System.CommandLine). See [Where the spec is wrong](#1-there-is-no-cli-to-preserve-16) | #2 |
| 7 | Workflow-level **Configure** view | §4 | ✅ Done | `src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/components/EnvironmentConfigure.svelte` — full-screen overlay (`.configure { position: fixed; inset: 0; z-index: 200 }`), client in `src/lib/services/environment.ts`, opened by the `⚙️ Configure` action at `src/routes/workflows/+page.svelte:116` | #3 |
| 8 | Import / export / check mise config | §4 | ✅ Done | `src/05_DevUI/DNAPlatform.DevUI.API/Controllers/EnvironmentController.cs` — `POST /api/environment/mise/export`, `POST /api/environment/mise/import`, `POST /api/environment/verify`; import writes `<projectRoot>/mise.toml` after a path-containment check (`src/05_DevUI/DNAPlatform.DevUI.API/Services/MiseService.cs:238-286`) | #3 |
| 9 | GREEN / YELLOW / RED status; platform defaults are .NET 8 + Node LTS **only** | §4 | ✅ Done | `src/05_DevUI/DNAPlatform.DevUI.API/Services/MiseService.cs` — `EnvironmentStatusValues` (:18-28), `PlatformDefaults` (:148-152), worst-of roll-up `OverallStatus` (:532-547). Rendered as colour **+ icon + the literal uppercase word** (`EnvironmentConfigure.svelte:43-47`), so status is never colour-only | #4 |
| 10 | "The UI must make it obvious which environment the workflow will execute in" | §4 | ✅ Done | `EnvironmentConfigure.svelte:207-209` renders the workflow name plus "will execute in this environment"; the page passes the selected workflow as a prop (`src/routes/workflows/+page.svelte:152`) | #3 |
| 11 | Workflow-level **Test** | §4 | ✅ Done *(re-scored)* | `POST /api/workflows/validate` is in the `vite.config.ts` proxy list and returns `{ isValid, errors, warnings, checks, summary }`. `WorkflowTestResult.svelte` renders it, labelled **structural only**. Verified against: valid chains, cycles, dangling edges, duplicate ids, missing names, missing skills, empty workflows, and three-node propagation. It deliberately does **not** execute, per `refactor.md` §4 | #3 |
| 12 | Verify the environment before execution | §4 | ⚠️ Partial | `POST /api/environment/verify` exists and re-runs every check (`Controllers/EnvironmentController.cs:83-88`), but nothing **blocks** execution on the result. `src/routes/api/workflows/[id]/execute/+server.ts` calls no environment endpoint before forwarding to `.NET` | #3 |
| 13 | Canvas UX: zoom/pan, typed connections, per-node validation + execution state, minimap | §5 | ⚠️ Partial *(re-scored)* | `WorkflowDesigner.svelte` gives a drag-and-drop canvas, a node palette, IO ports, an inline node editor, **per-node execution-state chips** and **drawn connections** (SVG cubic `<path>` with a `marker-end` arrowhead; a wide transparent hit path carries the click so the thin curve stays visible). Still missing: zoom/pan, typed connections, a minimap. Node `x`/`y` live inside the `nodes` JSON blob — `schema.prisma` has no column for them | #5 |
| 14 | Real node execution state (Idle → Queued → Running → Success / Failed / Skipped) | §5 | ✅ Done *(re-scored)* | `Execution.nodeResults` persists the engine's per-node record; `/api/workflows/[id]/execute` returns it, and the designer derives each chip from it. Rendered colour **+ icon + literal word**, never colour alone. A node the engine never reported stays `Pending` rather than being given an invented state. The vocabulary still comes from `/api/status-values` | #5 |
| 15 | CLI as a first-class interface over the same execution services | §6 | ✅ Done *(re-scored)* | `src/07_CLI/DNAPlatform.Cli` — `run`, `validate`, `status`, `logs`, `cancel`, `env`; `--json` everywhere; `run`/`validate` exit non-zero so scripts can branch. **HTTP only**: no `ProjectReference` to `DNAPlatform.AgentFramework` and it never constructs a `WorkflowOrchestrator`, which is what keeps row 5 true from a second direction. It needs two base URLs because `POST /api/executions/{id}/cancel` is not in the vite proxy list — see `AGENTS.md` → CLI | #2 |
| 16 | ACP (Agent Client Protocol) over stdio | §7, §17 | 🔲 Not started | No ACP code exists. A case-insensitive search for `ModelContextProtocol`/`mcp` across first-party `.cs`/`.ts`/`.svelte` returns 6 hits, all of them prose about `mise mcp` (`Services/MiseService.cs:81,689`; `EnvironmentConfigure.svelte:301,303,320`) — and `mise mcp` is **not** wired into this platform. Note: MAF cannot supply this, see `AGENT_FRAMEWORK_INTEGRATION.md:42` | #2 |
| 17 | Realtime execution/log streaming, no polling | §8, §17 | ⚠️ **Spec is wrong** + 🔲 Not started | See [Where the spec is wrong](#2-there-is-no-existing-realtime-transport-8) | #1 |
| 18 | Log page: live stream; run history survives a refresh | §9 | ⚠️ Partial | `src/routes/logs/+page.svelte` + `src/lib/services/logs.ts` merge frontend console capture with the backend buffer (`src/05_DevUI/DNAPlatform.DevUI.API/Services/LogStore.cs`, `InMemoryLogStore`, capacity 2000 at :47). The buffer is **in-memory only**, so a backend restart erases every past run and a page refresh cannot retrieve one | #1 |
| 19 | Skills UI: "Import MCP Plugins" + "Add New Plugin" placeholders | §10 | 🔲 Not started | `src/routes/skills/+page.svelte` has neither action. Its buttons are refresh (:266), two list controls (:309, :316), and the details-dialog pair (:456, :460) | #6 |
| 20 | Skills architecture stays extensible | §10 | ✅ Done *(re-scored)* | `src/06_Skills/SkillInterfaces.cs` `ISkill` + `SkillRegistry.cs`. The API now binds that registry **directly** — the `Services/SkillRegistryAdapter.cs` bridge is deleted — and `GetInputFields()` still drives the "Details" editor | #6 |
| 21 | Agent configuration: prompt, model, tools, MCP, memory, RICE, vector knowledge, permissions, runtime | §12 | 🔲 Not started | `src/05_DevUI/DNAPlatform.DevUI.Web/prisma/schema.prisma` `Agent` is 7 flat columns (`id`, `name`, `type`, `prompt`, `model`, `createdAt`, `updatedAt`). No `AgentConfiguration` model exists | #7 |
| 22 | Agent page must not become an orchestration platform | §12, §17 | ✅ Respected *(re-scored)* | No agent framework is wired. The deterministic-echo agent implementation that used to sit in `Services/InMemoryAgentFramework.cs` is deleted with the rest of the in-memory double, so the constraint now holds by absence rather than by convention. `AGENT_FRAMEWORK_INTEGRATION.md:69` still states the hard rule that MAF must never own workflow execution | hard constraint |
| 23 | PostgreSQL as primary datastore | §1, §13, §17 | 🔲 Not started | `schema.prisma` uses `provider = "sqlite"`. No `postgres` string occurs in any first-party `.cs`/`.ts`/`.prisma` file, and there is no Dockerfile or compose file anywhere in the repo | #8 |
| 24 | Explicit domain models: `WorkflowVersion`, `WorkflowNode`, `WorkflowEdge`, `WorkflowRun`, `WorkflowEvent`, `Plugin`, `AgentConfiguration`, `RuntimeEnvironment`, `MiseConfiguration`, `ValidationResult`, embeddings | §13, §14 | ⚠️ Partial *(re-scored)* | `schema.prisma` now defines **five** models: `Workflow`, `Agent`, `Execution`, `CustomSnippet` and `WorkflowVersion`. `Workflow` gained `status`/`version`/`versions` and `Execution` gained `version` + `nodeResults`. Still no `WorkflowEvent`, `Plugin`, `AgentConfiguration` or embeddings model. Nodes and edges remain inside `Workflow.nodes`/`Workflow.connections` JSON **strings**, because SQLite has no `Json` column type | #8 |
| 25 | Vectors via pgvector rather than a separate vector database | §13 | 🔲 Not started | Follows row 23 — no PostgreSQL and no vector store of any kind in the repo | #8 |
| 26 | Architecture hook for future Code Blocks (Skill → Code Block → Workflow Node) | §11 | 🔲 Not started (deliberate) | `schema.prisma` has no `CodeBlock` model; `src/06_Skills/` exposes `ISkill` only. `refactor.md` §11 explicitly says not to force this abstraction in yet | #9 |
| 27 | One execution concept, multiple interfaces over it | §15 | ⚠️ Partial *(re-scored)* | Two of the three interfaces the spec names now exist and both go through HTTP to the same orchestrator: the Web UI and the CLI. **ACP does not**, so the platform has two interfaces over one engine rather than one over two. That is the correct direction of travel and it is complete once ACP lands | #2 |
| 28 | No duplicate implementations of the same concept | §15, §17 | ✅ Respected *(re-scored)* | The consolidation pass removed the one real duplicate — the in-memory orchestrator/registry pair. The remaining near-duplicate risk is adopting an agent framework; that was declined. `AGENT_FRAMEWORK_INTEGRATION.md:61` recommends deferring Microsoft Agent Framework and building the `IAgent` seam first, and `IAgent` itself stays untouched | — |
| 29 | Automated tests | (quality, not spec) | ⚠️ Partial *(re-scored)* | `src/09_Tests/DNAPlatform.Tests` exists: 8 orchestrator tests, `dotnet test` 8/8 green. `scripts/test-e2e.ps1` adds a three-stage stack check with a real exit code (A static, B backend, C frontend). The frontend still has no tests — `npm run check` reports `svelte-check found 6 errors and 17 warnings in 7 files`, all pre-existing, and the e2e script fails only on a *new* diagnostic | — |
| 30 | Gap analysis produced before further code changes | §16 steps 1-6 | ✅ Done | This document | — |

---

## ⚠️ Where the spec is wrong about this repo

### 1. There is no CLI to preserve (§1, §6)

`refactor.md` §1 lists "Existing CLI" among the things to keep, and §6 says
"Preserve the existing CLI and inspect how it currently works before modifying
it." There is nothing to inspect.

- Exactly one project in the repository has `<OutputType>Exe</OutputType>`:
  `src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp/DNAPlatform.AgentWorkflowPatterns.ConsoleApp.csproj`.
- Its entry point, `Program.cs:14`, is `static async Task Main(string[] args)` —
  and **`args` is never read anywhere in the file**. It prints a banner, builds a
  `ServiceCollection`, and runs three hard-coded demo workflows
  (`RunSimpleWorkflow`, `RunParallelAgentWorkflow`, `RunDynamicWorkflow`).
- No argument parsing, no command surface, no `System.CommandLine` reference.

So §6's "Use the actual existing command structure where possible rather than
inventing a completely new CLI design" has nothing to build on. The CLI is
priority #2 work written from scratch, and — per §6 and §15 — it must be a **thin
client over the same execution services the UI calls**, not a second engine.

**Update (consolidation pass):** the CLI now exists —
`src/07_CLI/DNAPlatform.Cli`, `System.CommandLine`, subcommands `run`, `validate`,
`status`, `logs`, `cancel`, `env`. It honours the §15 constraint by having no
`ProjectReference` to `DNAPlatform.AgentFramework` and never constructing a
`WorkflowOrchestrator`; everything goes over HTTP. The ConsoleApp is still a demo
and was not converted, which is correct: §15 asks for an interface, not a
replacement of the demo.

### 2. There is no existing realtime transport (§8)

`refactor.md` §8 says "Use the project's existing realtime transport if one already
exists", and also asserts that the Logs page "can hang/block because execution
output is being handled in a buffered or blocking way."

- No `SignalR`, no `WebSocket`, no `EventSource` / `text/event-stream` occurs in any
  first-party `.cs` or `.ts` file.
- `/logs` is implemented by polling. `src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/services/logs.ts:36`
  sets `POLL_INTERVAL_MS = 1500`, and :170 drives it with `setInterval`. It fetches
  `GET /api/logs?sinceId=…&limit=200` and clears with `DELETE /api/logs`.
- That *is* "poll the output from the browser", which §8 explicitly forbids.
- The storage side is in-memory only. `src/05_DevUI/DNAPlatform.DevUI.API/Services/LogStore.cs`
  holds an `InMemoryLogStore` ring buffer (capacity 2000) fed by
  `RequestLoggingMiddleware` and `LogStoreLoggerProvider`. Nothing is persisted, so
  run history does not survive a backend restart.

§8's own fallback guidance therefore applies verbatim: prefer a .NET-native
streaming mechanism such as **SSE or SignalR** rather than adding a message broker.
The blocking-output diagnosis in §8 is accurate about the symptom, but there is no
buffering-behind-a-websocket to unpick — the transport itself has to be built.

---

## 🧩 Repo traps this analysis surfaced

These are not gaps against the spec; they are places where the repo misleads on
first inspection. They are carried as next-up items in [`TODO.md`](./TODO.md) too.

| Trap | Detail |
|---|---|
| ~~Neither solution file is complete~~ **resolved** | `DNAPlatform.slnx` is deleted and `DNAPlatform.sln` is the only solution. It includes `DNAPlatform.Skills`, `DNAPlatform.DevUI.AGUI.Server`, `DNAPlatform.Tests` and `DNAPlatform.Cli`, and builds clean at 0 warnings. Two solution files were two competing sources of truth, not just an incomplete one |
| ~~`build.ps1` is fully broken~~ **resolved** | Rewritten around `$PSScriptRoot`, so the `DNA-platform Cat` path is gone, and it now fails on a non-zero exit. `run-all.bat` was never touched and still works |
| **Three empty placeholder projects look like the engine** | `src/01_Core/DNAPlatform.Core/`, `src/04_WorkflowEngine/DNAPlatform.Workflow/` and `src/06_Skills/DNAPlatform.Skills.Core/` each contain a single empty `Class1.cs`. **The real engine is `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework/`** (`WorkflowOrchestrator`, `AgentManager`, `NodeExecutor`). `04_WorkflowEngine` is a naming trap. None of the placeholders is on the working DevUI path |
| **`POST /api/logs` is unreachable** | `src/05_DevUI/DNAPlatform.DevUI.API/Controllers/LogsController.cs:59` declares `[HttpPost]`, but `src/routes/api/logs/+server.ts` exports only `GET` and `DELETE`, and nothing in `src/lib/services/logs.ts` ever POSTs. Frontend logs are captured client-side instead. Not a live bug — an unused endpoint awaiting a decision |
| ~~`/health` and `/api/info` still report `0.2.0-beta`~~ **resolved** | One `PlatformVersion` constant in `Program.cs` now feeds `/health`, `/api/info`, the startup banner and the Swagger doc. Verified `0.3.0-alpha` in all four |
| **The CLI needs two base URLs** | Most of the platform is SvelteKit-only (`/api/workflows`, `/api/executions`, `/api/snippets`), reachable on `:5173`. But `POST /api/executions/{id}/cancel` lives on `ExecutionsController` and is **not** in the `vite.config.ts` proxy list, so it must go to `:5254`. Pointing everything at `:5254` makes `dnaplatform status` 404 |
| **`--no-launch-profile` silently hides Swagger** | The mandated backend command (`dotnet run --no-launch-profile --urls http://127.0.0.1:5254`) also drops the `ASPNETCORE_ENVIRONMENT=Development` that `launchSettings.json` would set. The API then runs as Production and `/swagger` 404s, while `run-all.bat` — which omits the flag — serves it. Verified both ways |
| **A Vite child outlives its `npm` parent** | Killing the wrapper leaves node holding `[::1]:5173`, invisible on `127.0.0.1`, and the next `prisma generate` then fails with `EPERM` renaming `query_engine-windows.dll.node`. Use `taskkill /T /F /PID` and check both address families |
| **`Execution.duration` is an integer of milliseconds** | `parseDurationMs` turns .NET's `"00:00:00.0002204"` into ~0.0002 and then rounds, so every run under 1 ms persists as `0` and `/executions` shows a blank duration. The engine really took 0.19–0.24 ms. Fixing it means widening the Prisma column |

---

## 🔀 Suggested next increment

Re-scored 2026-10-05. Items 1–3 below are struck where they have landed; the
ordering of what remains is unchanged.

1. ~~**#1** — give the Logs page a real transport~~ **still open, and still first.**
   `/logs` polls `GET /api/logs?sinceId=…` every 1500 ms and the buffer is in-memory
   only, so run history does not survive a refresh. No `SignalR`, `WebSocket` or
   `EventSource` occurs in any first-party file, so this has to be built, not
   wired up. Start with SSE.
2. ~~**#2** — build the ACP adapter and the CLI~~ **CLI done, ACP open.** The
   duplicate-engine question in row 5 that blocked this is settled, so ACP can now
   be a thin stdio adapter mapping `initialize` / `newSession` / `prompt` / `cancel`
   onto HTTP calls to `:5254`. Zero DAG logic in TypeScript — that would be a §15
   violation. stdout must stay protocol-pure; every diagnostic goes to stderr.
3. ~~**#3** — add the workflow-level Test~~ **done.** Reopen only for row 12: making
   the environment check *block* execution, rather than merely report it.
4. **#4** is done, and gets revisited when Test gates execution (row 12).
5. **#5**–**#9** follow as `refactor.md` orders them. **#7** (agent configuration)
   should not start before #1–#6 land, and per `AGENT_FRAMEWORK_INTEGRATION.md` it
   must land provider-agnostic — the seam first, Microsoft Agent Framework (if
   ever) as one implementation behind it.

Deferred with rationale, so these do not reopen: PostgreSQL/pgvector (#8),
ACP v2, expression-selector inputs, sensor blocks and the `Suspended` workflow
state. See [`TODO.md`](./TODO.md) → *Deferred, with rationale*.

---

## 📎 Related documents

| Document | Relationship |
|---|---|
| [`refactor.md`](./refactor.md) | The spec this document assesses. Input artifact, preserved verbatim |
| [`AGENT_FRAMEWORK_INTEGRATION.md`](./AGENT_FRAMEWORK_INTEGRATION.md) | Decision record for rows 21, 22 and 28. Reaches the same §16 ordering independently |
| [`TODO.md`](./TODO.md) | Working task list; carries the traps above as next-up items |
| [`../AGENTS.md`](../AGENTS.md) | Authoritative repo guidance. When this document and `AGENTS.md` disagree, `AGENTS.md` wins |