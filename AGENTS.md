# AGENTS.md

Repository-specific guidance for agents working on the DNA Platform.

## Layout

- `src/01_Core` — core domain types.
- `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework` — full workflow/agent engine
  (`WorkflowOrchestrator`, `AgentManager`, `NodeExecutor`).
- `src/05_DevUI/DNAPlatform.DevUI.API` — ASP.NET Core backend (port 5254).
- `src/05_DevUI/DNAPlatform.DevUI.Web` — SvelteKit frontend (port 5173).
- `src/06_Skills` — skill registry and the lightweight API contracts
  (`AgentFrameworkStubs.cs`).

## Toolchain

- .NET 8 SDK. On Debian, ICU is often missing; export
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` before running `dotnet`.
- Node 20+/npm.

## Build & run

```bash
# Backend
dotnet build DNAPlatform.sln
cd src/05_DevUI/DNAPlatform.DevUI.API
dotnet run --no-launch-profile --urls http://127.0.0.1:5254

# Frontend
cd src/05_DevUI/DNAPlatform.DevUI.Web
npm install
npx prisma db push          # requires DATABASE_URL (see .env)
npm run dev                 # port 5173
```

`launchSettings.json` pins the API to ports 5000/5001; always pass
`--no-launch-profile --urls http://127.0.0.1:5254` so the Vite proxy target
matches.

## Architecture notes

- The DevUI API consumes the **stub** contracts in
  `src/06_Skills/AgentFrameworkStubs.cs`, not the full `DNAPlatform.AgentFramework`
  project. In-memory implementations live in
  `src/05_DevUI/DNAPlatform.DevUI.API/Services/InMemoryAgentFramework.cs`, and
  `SkillRegistryAdapter.cs` bridges the stub `ISkillRegistry` to the real
  `DNAPlatform.Skills` registry.
- `ISkillRegistry` is ambiguous between `DNAPlatform.AgentFramework` and
  `DNAPlatform.Skills`; fully qualify it.
- Persistence: SvelteKit server routes use Prisma/SQLite
  (`prisma/schema.prisma`). Workflow `nodes`/`connections` are JSON strings.
- Routing: `/api/workflows` CRUD, `/api/agents` CRUD and `/api/executions` are
  served by SvelteKit (Prisma). `/api/skills*`, `/api/node-types`,
  `/api/status-values`, `/health` and `/api/workflows/{id}/execute` proxy to
  .NET.
- Ad-hoc execution endpoint: `POST /api/workflows/execute` on the backend runs a
  workflow definition supplied in the body (used by the designer).
- Workflow node types are strings (`"Input"`, `"Agent"`, ...) end to end; the
  stub `WorkflowNode.NodeType` is a `string`.

## Frontend

- Workflow designer: `src/lib/components/WorkflowDesigner.svelte`, opened from
  `/workflows` via the "Edit" action. Nodes are positioned with `x`/`y` stored
  alongside the definition.
- Logs console: `/logs` page backed by `src/lib/services/logs.ts` (merges
  frontend console capture with the backend buffer polled via `/api/logs`).

## Gotchas

- When `dotnet run --no-build` fails with "address already in use", an older
  build is still bound to the port. Kill the specific PID
  (`ss -ltnp | grep 5254`) before restarting.
