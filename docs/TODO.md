# DNA Platform — TODO

> Last updated: 2026-09-15 | Version: v0.2.0-beta

## ✅ Completed

### Foundation & DevUI API (Phase 1–2)
- [x] Core workflow orchestration engine (topological DAG, parallel nodes, retry)
- [x] Agent manager & skill registry
- [x] Console demo with 3 patterns
- [x] ASP.NET Core REST API + Swagger
- [x] Prisma + SQLite persistence (Workflow, Agent, Execution)
- [x] Workflow CRUD (`/api/workflows`, `/api/workflows/{id}`)
- [x] Agent CRUD (`/api/agents`, `/api/agents/{id}`)
- [x] Execution list + workflow execute → record result in SQLite
- [x] Response format alignment (wrapped `{skills:[...]}`, `{agents:[...]}`, `{executions:[...]}`)
- [x] Enum string binding for workflow JSON input
- [x] Request logging middleware (console + in-memory buffer for web Logs page)
- [x] Web Logs page (`/logs`) with backend + captured frontend logs

### Web Designer + Custom Skills System (Phase 3)
- [x] Custom skills framework (`DNAPlatform.Skills`)
- [x] Built-in skills: MsgBoxAlertSkill, DataTransformSkill, ValidationSkill, MergeResultsSkill
- [x] Bioinformatics skills: FastaLoaderSkill, SequenceAlignerSkill
- [x] GUI-triggered skill execution (`POST /api/skills/{id}/execute`)
- [x] “Details” parameter editor driven by `ISkill.GetInputFields()`
- [x] Custom snippet model (`CustomSnippet` via Prisma)
- [x] Snippet CRUD (`/api/snippets`, `/api/snippets/{id}`)
- [x] Snippet execute endpoint (`/api/snippets/{id}/execute`) with hello-world .NET handler
- [x] “Create Custom Snippet” dialog on `/skills` (runtime selector + IO editors + test + save)
- [x] Drag-and-drop designer integrated into `/workflows`
- [x] Designer IO ports rendered from snippet/node metadata (inputs on left, outputs on right)
- [x] Click node → inline inspect/edit panel (metadata static, node data editable)
- [x] Connected services status panel (backend + polyglot kernel + placeholder external MCP/local executor)

## 🔲 Next Up

- [ ] Full connection drawing (click source port → click target port → render SVG edge)
- [ ] Workflow execution in the designer (run nodes in connected order, show progress per node)
- [ ] Export/import workflow JSON with runtime environment metadata
- [ ] Agent-backed nodes in workflows (link to Prisma `Agent` records)
- [ ] Real custom code execution path for polyglot kernel snippets (extend hello-world placeholder)
- [ ] External MCP / local executor integration (wiring, not UI)
- [ ] Prisma Studio exposure for DB inspection

## 🗺️ Backlog (future)

- [ ] Microsoft Semantic Kernel integration for agent reasoning
- [ ] Local LLM support (Ollama, LM Studio)
- [ ] Polyglot runtime (Python/R/Node.js interop)
- [ ] `07_PolyglotRuntime/` language bridges

## 📝 Checkpoints

### 2026-09-15 — Docs refresh + Phase 2/3 marked complete
- README.md, TODO.md, DEVELOPMENT.md, QUICKREF.md, PROJECT_SUMMARY.md, README2.md, PHASE3_PROGRESS.md, src/06_Skills/README.md updated
- Phase 2 and Phase 3 marked complete in roadmap/docs
- New active work: custom snippet dialog, snippet execute endpoint, designer inside `/workflows`, connected services panel

### 2026-09-06 — Persistence + integration fix session
- Prisma 5 + SQLite wired into SvelteKit (`schema.prisma`, `src/lib/server/db.ts`, server routes)
- Note: SQLite connector does not support `Json` columns — JSON stored as `String` and parsed in route handlers
- Verified live: create/list/delete agents & workflows, execute workflow (status `Completed`), executions recorded
