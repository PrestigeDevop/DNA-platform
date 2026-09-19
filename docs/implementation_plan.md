# Implementation Plan

## Overview

Fix the DNA Platform DevUI so the frontend (SvelteKit on port 5173) reliably connects to the backend (ASP.NET Core on port 5254), and so agents, workflows, and executions actually retrieve and persist via the APIs. The broader goal is to add Prisma (SQLite) persistence on the SvelteKit server side (Option C) while keeping the existing .NET backend for workflow execution.

### Scope
1. **Backend (.NET) fixes** — response format mismatches, missing endpoints, workflow store, agent summaries.
2. **Frontend (SvelteKit) fixes** — API client cleanup, Prisma + SQLite setup, server-side CRUD routes.
3. **Routing** — Vite proxy: `/api/workflows/*` CRUD + `/api/agents/*` CRUD handled by SvelteKit server endpoints (Prisma); `/api/workflows/execute*` + `/api/skills*` + `/health` proxied to .NET backend.
4. **Doc updates** — `todo.txt` with full issue/task log.

---

## Types

### C# (AgentFramework) — `Interfaces.cs`
- **`AgentSummary`** (new record/class): `{ string Id; string Name; string Type; string? Prompt; }`
- **`IAgent`** — add `string Name { get; }`
- **`AgentConfig`** — add `public string Name { get; set; } = "";`
- **`DefaultAgent`** — implement `Name` from `config.Name`.

### TypeScript / Prisma — `prisma/schema.prisma`
```prisma
datasource db {
  provider = "sqlite"
  url      = env("DATABASE_URL")
}

generator client {
  provider = "prisma-client-js"
}

model WorkflowModel {
  id          String   @id
  name        String
  description String?
  nodes       String   // JSON string: WorkflowNode[]
  connections String   // JSON string: NodeConnection[]
  createdAt   DateTime @default(now())
  updatedAt   DateTime @updatedAt
}
```

```prisma
model AgentModel {
  id       String   @id
  name     String
  type     String
  prompt   String?
  createdAt DateTime @default(now())
}

model ExecutionModel {
  id           String   @id
  workflowId   String
  workflowName String?
  status       String
  startTime    DateTime
  durationMs   Int?
  outputs      String?  // JSON string
  error        String?  // JSON string
  createdAt    DateTime @default(now())
}
```
### Shared API response shapes (frontend expects)
- `GET /api/skills` → `{ skills: [...] }`
- `GET /api/agents` → `{ agents: [{ id, name, type, prompt }] }`
- `GET /api/workflows` → `{ workflows: [...] }`
- `GET /api/executions` → `{ executions: [...] }`

---

## Files

### New files
| Path | Purpose |
|------|---------|
| `src/05_DevUI/DNAPlatform.DevUI.Web/prisma/schema.prisma` | Prisma schema (SQLite) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/prisma/seed.ts` | Seed built-in skills as data (optional) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/server/db.ts` | Prisma client singleton |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/workflows/+server.ts` | GET list, POST create (Prisma) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/workflows/[id]/+server.ts` | DELETE, PUT (Prisma) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/workflows/[id]/execute/+server.ts` | POST execute via .NET backend |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/workflows/execute/+server.ts` | POST execute ad-hoc workflow via .NET |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/agents/+server.ts` | GET list, POST create (Prisma) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/agents/[id]/+server.ts` | DELETE (Prisma) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/skills/+server.ts` | GET list (delegate to .NET) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/skills/[id]/+server.ts` | GET details, POST execute (delegate to .NET) |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/executions/+server.ts` | GET list (Prisma), also records executions |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/health/+server.ts` | GET health (delegate to .NET) |
| `.env` | `DATABASE_URL="file:./dev.db"` (in DevUI.Web) |
| `todo.txt` | Issue/task log with checkpoints |

### Modified files
| Path | Change |
|------|--------|
| `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework/DNAPlatform.AgentFramework/Interfaces.cs` | Add `Name` to `IAgent`, `AgentConfig`; add `AgentSummary`; change `IAgentManager.ListAgents()` return type to `Task<IEnumerable<AgentSummary>>` |
| `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework/DNAPlatform.AgentFramework/AgentManager.cs` | Implement `ListAgents()` mapping to `AgentSummary`; expose `Name` on `DefaultAgent` |
| `src/05_DevUI/DNAPlatform.DevUI.API/DNAPlatform.DevUI.API/Program.cs` | Fix `/api/skills` to return `{ skills }`; add in-memory workflow store + CRUD; fix `/api/agents` to return full objects; add `GET /api/executions` (recent list); add CORS refinements |
| `src/05_DevUI/DNAPlatform.DevUI.Web/package.json` | Add `@prisma/client`, `prisma`, `better-sqlite3`, scripts: `prisma generate`, `prisma migrate`, `prisma seed` |
| `src/05_DevUI/DNAPlatform.DevUI.Web/vite.config.ts` | Keep `/api` proxy to 5254 OR configure selective proxy; add env-based override |
| `src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/services/api.ts` | Clean formatting; add `getWorkflows`, `createWorkflow`, `deleteWorkflow`, `getAgents`, `getExecutions`, error handling |
---

## Functions

### Backend `Program.cs`
| Function | Signature | Purpose |
|----------|-----------|---------|
| `GET /api/skills` (modify) | `async (ISkillRegistry reg) => ...` | Return `Results.Ok(new { skills = ... })` |
| `GET /api/agents` (modify) | `async (IAgentManager mgr) => ...` | Return `{ agents: [...] }` |
| `GET /api/executions` (new) | `(IWorkflowOrchestrator orch) => ...` | Return recent executions |
| mwf store (new inline) | `ConcurrentDictionary<string, Workflow>` | In-memory workflow CRUD in Program.cs |
| `GET /api/workflows` (new) | `() => ...` | List stored workflows |
| `POST /api/workflows` (new) | `(Workflow wf) => ...` | Store workflow, return 201 |
| `DELETE /api/workflows/{id}` (new) | `(string id) => ...` | Remove stored workflow |
| `POST /api/workflows/{id}/execute` (new) | `(string id, orch) => ...` | Load then execute stored workflow |
| `DELETE /api/agents/{id}` (new) | `(string id, mgr) => ...` | Delete agent |

### AgentFramework
| Function | File | Change |
|----------|------|--------|
| `IAgentManager.ListAgents()` | `Interfaces.cs` | Return type `Task<IEnumerable<AgentSummary>>` |
| `AgentManager.ListAgents()` | `AgentManager.cs` | Map stored agents to `AgentSummary` |
| `DefaultAgent.Name` | `AgentManager.cs` | Getter returning `_config.Name` |
| `AgentConfig.Name` | `Interfaces.cs` | New property |

---

## Classes
### Modified
| Class | File | Change |
|-------|------|--------|
| `DefaultAgent` | `AgentManager.cs` | Add `Name` property |
| `AgentConfig` | `Interfaces.cs` | Add `Name` |
| `SkillRegistry` | `AgentManager.cs` | No change needed |

### New (frontend server-side)
| Class | File | Purpose |
|-------|------|---------|
| `PrismaClient` (singleton) | `src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/server/db.ts` | Shared Prisma instance |

---

## Dependencies

### Add (run in `src/05_DevUI/DNAPlatform.DevUI.Web`)
```bash
npm install -D prisma
npm install @prisma/client better-sqlite3
npm install -D @types/better-sqlite3
```

### package.json scripts
```json
{
  "prisma:generate": "prisma generate",
  "prisma:migrate": "prisma migrate dev",
  "prisma:seed": "prisma db seed"
}
```

### Prisma config
- `prisma generate` runs postinstall.
- `.env` → `DATABASE_URL="file:./dev.db"`
---

## Testing

1. **Backend builds & runs**: `dotnet build DNAPlatform.slnx` → 0 errors; run DevUI.API on 5254.
2. **API smoke tests**: `GET /health`, `GET /api/skills` → `{skills}`, `POST /api/workflows` → 201, `GET /api/workflows`, `GET /api/agents` → `{agents}`, `GET /api/executions`.
3. **Frontend builds**: `cd src/05_DevUI/DNAPlatform.DevUI.Web; npm install; npx prisma generate; npm run dev`.
4. **End-to-end**: Dashboard healthy + skills; Workflows page lists created; Agents page lists; Skills page lists.

---

## Implementation Order

1. **Backend AgentFramework**: Update `Interfaces.cs` and `AgentManager.cs`.
2. **Backend API**: Update `Program.cs` — response format fixes, workflow CRUD, executions, agent DELETE.
3. **Build backend**: `dotnet build DNAPlatform.slnx`; verify no errors.
4. **Frontend deps**: `npm install -D prisma`, `npm install @prisma/client better-sqlite3`; create `.env`.
5. **Prisma schema + generate**: Create `prisma/schema.prisma`; run migrate + generate.
6. **Server routes**: Create `src/lib/server/db.ts` and all `src/routes/api/*` endpoints.
7. **Update `api.ts`**: Add workflow/agent/execution methods, clean error handling.
8. **Vite proxy**: Ensure execute/skills/health proxy to 5254; SvelteKit server handlers take precedence for CRUD.
9. **Doc**: Write `todo.txt` with quick description, TODO list, checkpoint log.
10. **Validate**: Run backend + frontend, exercise all pages/APIs.