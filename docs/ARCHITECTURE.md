# DNA Platform — Architecture (Current State)

> Updated: 2026-09-06. This document reflects the system **as built today**, not the original plan. Historical docs live in [`archive/`](./archive/).
## 🎯 Development Narrative & Evolution

### How We Got Here: The "Refactor → Document" Approach

This system didn't emerge from a perfect initial plan. It evolved through an iterative loop of **build, discover, document, refine**:

**Initial Vision (Phase 1)**: We started by building a core workflow engine in .NET with:
- Topological DAG execution for bioinformatic pipelines
- In-memory agent management with plugin-based skills  
- Console demo to validate patterns before investing in UI

**Reality Check (The Split)**: As we expanded, constraints became apparent:
- SvelteKit needed for fast, reactive frontend with Prisma/SQLite persistence
- .NET remained the compute engine for heavy-lifting (orchestration, skills)
- Communication settled on HTTP proxy calls (`localhost:5173` → `localhost:5254`)

**Data Reality**: We initially assumed a graph database would be needed, but realized:
- SQLite + JSON blobs for nodes/connections work fine for this scale
- Complex queries can reconstruct graphs from flat structures  
- No need to overengineer prematurely

**Current Reality**: What you see is a pragmatic compromise:
- Polyglot stack (TypeScript + C#) communicating via HTTP
- Workflows as JSON strings in SQLite (not a native graph DB)
- SvelteKit owns data CRUD; .NET owns compute execution

**Key Insight**: The system demonstrates that "good enough" often beats "perfect" architecture. We built the engine first, then layered persistence and UI around it. This means:
- Early prototypes in console form to validate core patterns
- Later addition of Prisma/SQLite for auditability  
- HTTP proxy calls emerged naturally (no gRPC or sockets initially)

**For New Agents**: Don't reverse-engineer from code alone. Read this narrative first to understand:
- Why certain constraints exist (e.g., JSON blobs vs graph DB)
- That "legacy" assumptions are deprecated; the current state is truth
- The iterative "document what exists" philosophy over idealized design

---

## Overview

A two-service system for building and running bioinformatic workflows:

```
┌────────────────────────────────────┐         ┌────────────────────────────────────┐
│  DevUI Web App (port 5173)         │         │  Backend API (port 5254)           │
│  SvelteKit + TypeScript            │◄───────►│  ASP.NET Core 8 Minimal APIs       │
│                                    │   HTTP  │  • Workflow execution engine       │
│  • Pages: dashboard, workflows,    │─────────│    (topological DAG, parallel)     │
│    agents, skills, executions      │   proxy │  • Skill registry (built-ins)      │
│  • PERSISTENCE: Prisma + SQLite    │         │  • Agent manager (in-memory)       │
│    (src/05_DevUI/.../prisma/)      │         │  • Swagger UI (/swagger)           │
└────────────────────────────────────┘         └────────────────────────────────────┘
```

**Ownership rule:** SvelteKit owns **data** (CRUD via Prisma). The .NET API owns **compute** (workflow execution, skills). In dev, Vite proxies compute routes to 5254; SvelteKit routes win over the proxy, so CRUD never leaves the app.


## Execution Flow (stored workflow)

```
Browser: POST /api/workflows/{id}/execute
   → SvelteKit +server.ts: load Workflow row from SQLite (nodes/connections are JSON strings)
   → SvelteKit: POST http://127.0.0.1:5254/api/workflows/execute   (server-to-server)
       → WorkflowOrchestrator: topological DAG execution, parallel nodes, retry policy
       → returns WorkflowExecutionResult (status, outputs, per-node detail)
   → SvelteKit: INSERT Execution row (status, startTime, endTime, durationMs, outputs, errors)
   → Browser: result JSON
```

## Data Model (Prisma / SQLite)

### Prisma Schema (`src/05_DevUI/prisma/schema.prisma`)

| Model | Fields | Purpose |
|---|---|---|
| `Workflow` | `id`, `name`, `createdAt`, `nodes`, `connections`, `version` | DAG definition; nodes/connections are JSON blobs (serialized in TypeScript) |
| `Execution` | `id`, `workflowId`, `status`, `startTime`, `endTime`, `durationMs`, `outputs`, `errors`, `createdAt` | Immutable audit trail of each run; links to workflow via FK |

*(See [`src/05_DevUI/prisma/schema.prisma`](./src/05_DevUI/prisma/schema.prisma) for full schema with indexes.)*

---

## Workflow Serialization (in-memory / DB)

Workflows are **graph** objects in memory but stored as **JSON strings** in SQLite:

- `Workflow.nodes` → JSON array of node objects (id, type, inputs, outputs, parameters, etc.)
- `Workflow.connections` → JSON array of edge objects (sourceId, targetId, sourceOutput, targetInput)

### TypeScript ↔ Database Mapping

| TypeScript | Database | Notes |
|---|---|---|
| `Workflow` | `Workflow` table | `nodes`/`connections` are blobs in SQLite; Prisma serializes/deserializes automatically (`.jsonb` not needed here) |

*(In .NET, we use POCOs that mirror the DB columns.)*

## Skills

Built-in skill registry provides common operations (file I/O, HTTP clients, etc.). See [`src/04_API/.../skills.ts`](./src/04_API/...) for the implementation. Custom skills can extend this base.