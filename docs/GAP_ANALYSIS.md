

---

## ⚡ AG-UI Protocol & Human-in-the-Loop (HITL) Status
- **Specification Status**: Active and documented. Implements [https://docs.ag-ui.com/sdk/dotnet/hosting/overview](https://docs.ag-ui.com/sdk/dotnet/hosting/overview) and [https://docs.ag-ui.com/sdk/js/core/overview](https://docs.ag-ui.com/sdk/js/core/overview).
- **.NET Server (`DNAPlatform.DevUI.AGUI.Server`)**: Hosted on port 5000, targeting `net8.0`. Exposes `GET /agui` (discovery) and `POST /agui/run` (SSE streaming). Binds directly to `DNAPlatform.AgentFramework`. Supports `RunAgentInput`, `THINKING_DELTA`, `TOOL_CALL_STARTED/FINISHED`, `APPROVAL_REQUESTED` (Human-in-the-Loop Interrupt), `STATE_SNAPSHOT`, and `STATE_DELTA`.
- **SvelteKit Server & UI (`src/routes/api/agui/+server.ts` & `AgUiStudio.svelte`)**: Interactive frontend studio with real-time SSE streaming, cognitive thinking traces, interactive authorization modals for sensitive tools (`sequence-mutation`, `csharp-script`), and live JSON Patch state inspection.
