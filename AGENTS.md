# AGENTS.md — Agent & Developer Quick Reference

> **Notice**: The comprehensive engineering guide, toolchain specs, extension recipes, and gap analysis have been consolidated into **[`docs/development.md`](./docs/development.md)** and **[`docs/architecture.md`](./docs/architecture.md)**.

## Core Rules for Agents
This is a polyglot project, use minimal modular approach to align with the developer's intent, 
Plan ,you can search solutions, Use Github for rapid development, ask if ambiguit but only if needed, and offer your solution summarized. 

1. **The Single Execution Engine**:
   - `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework` is the **single** workflow and agent engine across the repository.
   - Never instantiate or register a second orchestrator double. The DevUI API, AG-UI server, and CLI all bind to this one instance.

2. **CSnakes Polyglot in Core**:
   - `src/01_Core/DNAPlatform.Core` contains the `CSnakes` Python-C# zero-overhead interop bridge and `BioinformaticsTypes.cs`.
   - Use CSnakes for in-process high-speed execution of Python modules (BioPython, NumPy) and DNA metrics calculations.

3. **AG-UI Protocol & Human-in-the-Loop (HITL)**:
   - Server-Sent Events (SSE) streaming server is hosted at `src/05_DevUI/DNAPlatform.DevUI.AGUI` on port 5000 and mirrored at `/api/agui`.
   - Emits `THINKING_DELTA`, pauses on `APPROVAL_REQUESTED` interrupts, resumes with `approvals: [...]`, and applies state changes via `STATE_DELTA` (JSON Patch).

4. **Routing Ownership**:
   - SvelteKit routes (`/api/workflows`, `/api/agents`, `/api/executions`) take precedence over the Vite proxy and handle Prisma/SQLite CRUD.
   - Backend compute routes (`/api/skills`, `/api/node-types`, `/api/environment`) are proxied to ASP.NET Core on port 5254.

5. **The 3 Cross-Platform Scripts**:
   - Setup: `npm run bootstrap` (or `node scripts/1-bootstrap.mjs`)
   - Test & Clean: `npm test` / `npm run test:clean` (or `node scripts/2-test-dev.mjs`)
   - Run: `npm start` (or `node scripts/3-denovo-automata.mjs`)
   - Terminal.Gui TUI: `npm run tui` (or `dnaplatform tui`)

Refer to **`docs/architecture.md`** for full system topology, access points, and troubleshooting.
Refer to **`docs/development.md`** for complete command line usage, response envelopes, and development logs.
