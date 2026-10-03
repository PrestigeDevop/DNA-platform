# DNA Platform — Development Log

> Append-only log. Newest first. One entry per working session.

## 2026-09-06 (later) — Backend crash after wwwroot removal + startup hardening

**Problem reported:** `[vite] http proxy error: /health — AggregateError [ECONNREFUSED]` even though everything had been working.

**Root causes found (two):**
1. **Backend was simply not running** — both dev servers live in console windows; closing those windows (or ending the session that spawned them) kills the services. This is expected behavior, but...
2. **Deleting the legacy `wwwroot` folder made the backend crash on startup** with `System.IO.DirectoryNotFoundException: ...\wwwroot\` from ASP.NET Core's `StaticWebAssetsLoader` (the build manifest still referenced the folder). Even after `run-all.bat`, the backend died before binding port 5254 → permanent proxy errors.

**Fixes:**
- Recreated `src/05_DevUI/DNAPlatform.DevUI.API/DNAPlatform.DevUI.API/wwwroot/README.txt` — the folder must exist in Development mode.
- `run-all.bat` hardened: kills any stale process holding port 5254 before building (prevents the locked-DLL build failure), then **polls `/health` for up to 30 s** before starting the frontend, with a clear warning if the backend never comes up.
- Vite proxy targets and `BACKEND_URL` switched to `http://127.0.0.1:5254` (deterministic IPv4, avoids IPv6/`localhost` ambiguity).

**Verified:** backend healthy, `/health` + `/api/skills` via the 5173 proxy, and a full workflow create → execute (`Completed`) → delete round-trip.

**Note for future sessions:** dev servers must be running for the UI to work. Use `run-all.bat` and keep both console windows open.

## 2026-09-06 — Fix DevUI connectivity + Prisma persistence (Option C)

**Problem reported:** run-all.bat showed "⚠️ Connection Error — Failed to connect to backend on port 5254" even though the backend was running; Agents/Workflows/Executions pages showed no data.

**Root causes found:**
1. The frontend health check called `/health` (relative to port 5173) but Vite did not proxy it to .NET on 5254 → false "Connection Error".
2. `/api/skills` returned a raw array; the UI expected `{ skills: [...] }`.
3. There was **no persistence layer at all** — agents lived in an in-memory .NET dictionary; workflows had no storage; no list endpoint for executions.

**Changes:**
- `vite.config.ts` — proxy `/health`, `/api/skills`, `/api/node-types`, `/api/status-values`, `/api/info` to 5254; SvelteKit owns `/api/workflows*`, `/api/agents*`, `/api/executions`.
- **Prisma + SQLite** (Option C — Prisma inside SvelteKit server endpoints):
  - `prisma/schema.prisma` — `Workflow`, `Agent`, `Execution` models (JSON kept in `String` columns — SQLite has no `Json` type; parsed in handlers)
  - `src/lib/server/db.ts` — singleton PrismaClient
  - `src/routes/api/workflows/+server.ts` (GET list/POST), `[id]/+server.ts` (GET/PUT/DELETE), `[id]/execute/+server.ts` (execute via .NET, record result)
  - `src/routes/api/agents/+server.ts`, `agents/[id]/+server.ts`
  - `src/routes/api/executions/+server.ts`
- `.NET` backend (`Program.cs`): wrapped responses, `GET /api/executions`, `DELETE /api/agents/{id}`, `JsonStringEnumConverter` (enum-by-string binding), `/` redirects to `/swagger`.
- `AgentFramework`: `Name`/`AgentType` surfaced on agents; `ListAgents` returns full summaries.
- Frontend: `src/lib/services/api.ts` hardened (`res.ok` checks, clear errors); agents page wired to `api` service; fixed duplicate `</script>` tag that broke `agents/+page.svelte`.
- `run-all.bat`: auto-creates SQLite DB on first run (`npx prisma db push` when `dev.db` missing).

**Verified live (all passing):**
- `GET /health` through 5173 proxy → `{"status":"healthy",...}`
- Agent create → list → delete (SQLite)
- Workflow create → list → execute (`Completed`, 2 nodes ran) → recorded in executions with parsed duration (224 ms)
- `dotnet build`: 0 errors; `vite build`: success

**Housekeeping:** all docs consolidated under `docs/`; ~25 stray files from previous AI sessions deleted (probe files, mangled-name files, duplicate .bat launchers, logs).

## Earlier sessions
See `docs/PROJECT_SUMMARY.md`, `docs/README2.md`, `docs/DEVELOPMENT.md`, `docs/QUICKREF.md` for historical context from prior sessions.
