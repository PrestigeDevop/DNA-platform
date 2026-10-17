

---

## 9. AG-UI Protocol Hosting & Human-in-the-Loop (HITL) Architecture

### 9.1 Background & References
- **AG-UI Protocol Overview**: [https://docs.ag-ui.com/sdk/dotnet/hosting/overview](https://docs.ag-ui.com/sdk/dotnet/hosting/overview)
- **Microsoft Learn AG-UI Integration**: [https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/ui/ag-ui/?pivots=programming-language-csharp](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/ui/ag-ui/?pivots=programming-language-csharp)
- **JavaScript/SvelteKit Core SDK**: [https://docs.ag-ui.com/sdk/js/core/overview](https://docs.ag-ui.com/sdk/js/core/overview)

AG-UI (Agent User Interface Protocol) is an open, lightweight, and event-based protocol designed to standardize communication between AI agents and interactive frontends. It formalizes streaming text, agent reasoning/thinking traces, tool invocations, state synchronization, and **Human-in-the-Loop (HITL)** approval workflows.

### 9.2 Key Architectural Tenets
1. **Framework-Agnostic Hosting (.NET 8.0 / 9.0 / 10.0)**:
   - The AG-UI server adapter does not depend strictly on ASP.NET Core or any specific web framework. It can be hosted inside any HTTP web host (ASP.NET Core Kestrel Minimal APIs, custom HTTP listeners, or SvelteKit/Node.js endpoints).
   - In DNA Platform, the .NET AG-UI Server runs in `src/05_DevUI/DNAPlatform.DevUI.AGUI/Server` on port 5000, targeting `net8.0`.
2. **SvelteKit / Client Interoperability**:
   - The DevUI frontend (`src/05_DevUI/DNAPlatform.DevUI.Web`) interacts directly with AG-UI streams via standard Server-Sent Events (SSE).
   - `AgUiStudio.svelte` provides an interactive inspector for streaming thinking deltas, tool calls, shared state, and operator approval cards.

### 9.3 Human-in-the-Loop (HITL) Workflow Lifecycle

When an agent proposes an action flagged as sensitive (e.g. sequence mutation, high-compute pipeline execution, filesystem modification):

```
Client (SvelteKit)              AG-UI Server (.NET / Web)           Skill Registry
       |                                   |                              |
       |--- POST /agui/run (Prompt) ------>|                              |
       |<-- event: RUN_STARTED ------------|                              |
       |<-- event: STATE_SNAPSHOT ---------|                              |
       |<-- event: THINKING_DELTA ---------|                              |
       |<-- event: TOOL_CALL (Safe Time) --|----------------------------->| (Executes Clock)
       |                                   |                              |
       | [Sensitive Operation Identified:  |                              |
       |  sequence-mutation]               |                              |
       |<-- event: APPROVAL_REQUESTED -----|                              |
       |<-- event: RUN_FINISHED (PAUSED) --|                              |
       |                                   |                              |
[Operator reviews UI Prompt:               |                              |
 "Approve Point Mutation Glu6Val?"]        |                              |
       |                                   |                              |
       |--- POST /agui/run --------------->|                              |
       |    { approvals: [{ approved: true }]}                            |
       |<-- event: RUN_STARTED ------------|                              |
       |<-- event: THINKING_DELTA ---------|                              |
       |<-- event: TOOL_CALL_STARTED ------|----------------------------->| (Applies Mutation)
       |<-- event: TOOL_CALL_FINISHED -----|<-----------------------------| (Success)
       |<-- event: STATE_DELTA (Patch) ----|                              |
       |<-- event: TEXT_DELTA (Tokens) ----|                              |
       |<-- event: RUN_FINISHED (OK) ------|                              |
```

### 9.4 Event Type Matrix

| Event Type | Direction | Payload Structure | Purpose |
|---|---|---|---|
| `RUN_STARTED` | Server -> Client | `{ runId, threadId, timestamp, agent }` | Initiates the streaming turn |
| `THINKING_DELTA` | Server -> Client | `{ runId, thought }` | Streams internal reasoning / chain-of-thought |
| `TEXT_DELTA` | Server -> Client | `{ runId, delta }` | Streams generated assistant tokens |
| `TOOL_CALL_STARTED` | Server -> Client | `{ runId, toolCallId, toolName, arguments }` | Signals tool execution initiation |
| `TOOL_CALL_FINISHED` | Server -> Client | `{ runId, toolCallId, result }` | Returns tool execution output |
| `APPROVAL_REQUESTED` | Server -> Client | `{ runId, threadId, toolCallId, toolName, reason, arguments, stateSnapshot }` | **Human-in-the-Loop Interrupt** requesting operator sign-off |
| `STATE_SNAPSHOT` | Server -> Client | `{ runId, state }` | Provides full application state for initial sync / recovery |
| `STATE_DELTA` | Server -> Client | `{ runId, patch: [{ op, path, value }] }` | RFC 6902 JSON Patch incremental state updates |
| `RUN_FINISHED` | Server -> Client | `{ runId, threadId, status, timestamp, tokensGenerated }` | Concludes run turn (`COMPLETED`, `WAITING_FOR_APPROVAL`, `REJECTED`) |
| `ERROR` | Server -> Client | `{ runId, threadId, error }` | Surfaces runtime exceptions |

### 9.5 Current Implementation Status in DNA Platform
- **.NET Server (`DNAPlatform.DevUI.AGUI.Server`)**: Implements `GET /agui` (discovery), `POST /agui/run` (full SSE stream with HITL interrupts and approval resumption), bound to `DNAPlatform.AgentFramework`.
- **SvelteKit Server (`src/routes/api/agui/+server.ts`)**: Parity endpoint running in Node.js / SvelteKit for local browser and preview environments without requiring separate Kestrel ports.
- **Frontend Studio (`src/lib/components/AgUiStudio.svelte`)**: Fully reactive UI embedded in `/agents` with real-time SSE streaming, thinking traces, interactive authorization modals, and live JSON Patch state inspection.
