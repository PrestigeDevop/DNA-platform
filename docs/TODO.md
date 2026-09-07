# DNA Platform — TODO

> Last updated: 2026-09-06

## ✅ Completed

- [x] **Fix backend crash on startup after legacy UI removal** — `StaticWebAssetsLoader` threw `DirectoryNotFoundException` because `wwwroot/` was deleted; recreated the folder with a placeholder and documented why it must exist.
- [x] **Harden `run-all.bat`** — kills stale port-5254 holders (prevents locked-DLL build failures), waits for a healthy `/health` response (up to 30 s) before launching the frontend, prints a warning if the backend fails to start.
- [x] **Deterministic proxy targets** — Vite proxy + `BACKEND_URL` now use `http://127.0.0.1:5254` instead of `localhost` (avoids IPv6/IPv4 resolution ambiguity).
- [x] **Fix "Connection Error" on DevUI startup** — root cause: `/health` was not proxied by Vite to the .NET backend, so the UI health check failed even with the backend running. Added `/health`, `/api/skills`, `/api/node-types`, `/api/status-values`, `/api/info` to the Vite proxy.
- [x] **Agents now persist and list correctly** — moved agent storage from the .NET in-memory store to SvelteKit server endpoints backed by **Prisma + SQLite** (`Agent` model). `GET/POST /api/agents`, `DELETE /api/agents/{id}`.
- [x] **Workflows now persist and list correctly** — new `Workflow` model + `GET/POST /api/workflows`, `GET/PUT/DELETE /api/workflows/{id}` (SvelteKit + Prisma).
- [x] **Workflow execution end-to-end** — `POST /api/workflows/{id}/execute` loads the workflow from SQLite, forwards it to the .NET orchestrator (port 5254), then records the result as an `Execution` row in SQLite.
- [x] **Executions list endpoint** — `GET /api/executions` returns all recorded executions.
- [x] **Response format alignment** — backend returns wrapped shapes (`{skills:[...]}`, `{agents:[...]}`, `{executions:[...]}`) matching the frontend `api.ts` service.
- [x] **Enum string binding** — .NET now accepts `"nodeType": "ProcessingSkill"` style enums (`JsonStringEnumConverter`), required by the workflow designer.
- [x] **Docs consolidation** — all documentation moved to `docs/`; root `README.md` updated; redundant root scripts/docs (`run.bat`, `start.bat`, `start-all.bat`, `README2.md`, `PROJECT_SUMMARY.md`, etc.) removed.

## 🔲 Next Up

- [ ] **Workflow designer canvas** — drag-and-drop node editor (frontend currently list/create/delete only)
- [ ] **Agent-backed nodes** — wire `Agent` nodes in workflows to the Prisma `Agent` records
- [ ] **Real skill execution** — connect `ProcessingSkill` nodes to bioinformatic skills beyond the 3 built-ins
- [ ] **Live execution monitoring** — poll execution status while running (WebSockets or SSE)
- [ ] **Prisma Studio** — expose `npm run prisma:studio` for DB inspection
- [ ] **Export/Import workflows** — JSON download/upload of workflow definitions

## 🗺️ Backlog (from roadmap)

- [ ] Microsoft Semantic Kernel integration for agent reasoning
- [ ] Local LLM support (Ollama, LM Studio)
- [ ] Polyglot runtime (Python/R/Node.js interop)
- [ ] `06_Skills/` bioinformatic skill library
- [ ] `07_PolyglotRuntime/` language bridges

## 📝 Checkpoints

### 2026-09-06 — Persistence + integration fix session
- Prisma 5 + SQLite wired into SvelteKit (`schema.prisma`, `src/lib/server/db.ts`, 6 `+server.ts` routes)
- Note: SQLite connector does not support `Json` columns — JSON is stored as `String` and parsed in route handlers
- Verified live: create/list/delete agents & workflows, execute workflow (status `Completed`, duration parsed from .NET `TimeSpan` string), executions recorded
