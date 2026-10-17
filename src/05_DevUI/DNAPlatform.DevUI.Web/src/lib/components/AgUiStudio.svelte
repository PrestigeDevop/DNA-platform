<script lang="ts">
  import { onMount } from 'svelte';

  export let selectedAgentName: string = 'Microsoft Copilot Bioinformatician';
  export let selectedAgentModel: string = 'Microsoft-Copilot-Local-ONNX';

  interface AgUiEvent {
    type: string;
    timestamp: string;
    payload: any;
  }

  interface PendingApproval {
    runId: string;
    threadId: string;
    toolCallId: string;
    toolName: string;
    reason: string;
    arguments: any;
    stateSnapshot?: any;
  }

  let prompt = 'Evaluate HBB gene sequence ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC and apply point mutation at Codon 6 using C# clock provider';
  let endpoint = '/api/agui';
  let requireApproval = true;
  let isRunning = false;
  let runStatus = 'idle'; // idle, running, waiting_for_approval, completed, rejected, error

  let events: AgUiEvent[] = [];
  let thinkingLog = '';
  let responseText = '';
  let toolCalls: Array<{ id: string; name: string; args: any; result: any; status: string }> = [];
  let pendingApproval: PendingApproval | null = null;
  let sharedState: Record<string, any> = {};
  let currentThreadId = '';
  let currentRunId = '';
  let showRawLogs = false;
  let protocolInfo: any = null;

  onMount(async () => {
    try {
      const res = await fetch('/api/agui');
      if (res.ok) {
        protocolInfo = await res.json();
      }
    } catch {
      // ignore
    }
  });

  async function startRun(isResuming = false, approved = true, rejectionReason = '') {
    if (!isResuming) {
      events = [];
      thinkingLog = '';
      responseText = '';
      toolCalls = [];
      pendingApproval = null;
      sharedState = {};
      currentThreadId = `thread_${Date.now()}`;
      currentRunId = `run_${Date.now()}`;
    }

    isRunning = true;
    runStatus = 'running';

    const requestPayload: any = {
      threadId: currentThreadId,
      runId: currentRunId,
      prompt,
      requireApproval,
      agentId: selectedAgentName.toLowerCase().replace(/\s+/g, '-'),
      state: sharedState
    };

    if (isResuming && pendingApproval) {
      requestPayload.approvals = [
        {
          toolCallId: pendingApproval.toolCallId,
          toolName: pendingApproval.toolName,
          approved,
          reason: rejectionReason || (approved ? 'Approved by operator in DevUI' : 'Rejected by operator in DevUI')
        }
      ];
      pendingApproval = null;
    }

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'text/event-stream'
        },
        body: JSON.stringify(requestPayload)
      });

      if (!response.ok) {
        throw new Error(`Server returned status ${response.status} (${response.statusText})`);
      }

      if (!response.body) {
        throw new Error('Response body is null, cannot stream SSE.');
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { value, done } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n\n');
        buffer = lines.pop() || '';

        for (const block of lines) {
          if (!block.trim()) continue;
          parseSseBlock(block);
        }
      }

      if (buffer.trim()) {
        parseSseBlock(buffer);
      }
    } catch (err: any) {
      runStatus = 'error';
      events = [
        ...events,
        {
          type: 'ERROR',
          timestamp: new Date().toLocaleTimeString(),
          payload: { message: err.message || String(err) }
        }
      ];
    } finally {
      isRunning = false;
      if (runStatus === 'running') {
        runStatus = 'completed';
      }
    }
  }

  function parseSseBlock(block: string) {
    const lines = block.split('\n');
    let eventType = 'MESSAGE';
    let dataStr = '';

    for (const line of lines) {
      if (line.startsWith('event:')) {
        eventType = line.replace('event:', '').trim();
      } else if (line.startsWith('data:')) {
        dataStr = line.replace('data:', '').trim();
      }
    }

    if (!dataStr) return;

    try {
      const data = JSON.parse(dataStr);
      events = [
        ...events,
        {
          type: eventType,
          timestamp: new Date().toLocaleTimeString(),
          payload: data
        }
      ];

      switch (eventType) {
        case 'RUN_STARTED':
          currentThreadId = data.threadId || currentThreadId;
          currentRunId = data.runId || currentRunId;
          runStatus = 'running';
          break;

        case 'THINKING_DELTA':
          thinkingLog += (data.thought || '') + '';
          break;

        case 'TEXT_DELTA':
          responseText += (data.delta || '');
          break;

        case 'TOOL_CALL_STARTED':
          toolCalls = [
            ...toolCalls,
            {
              id: data.toolCallId,
              name: data.toolName,
              args: data.arguments,
              result: null,
              status: 'executing'
            }
          ];
          break;

        case 'TOOL_CALL_FINISHED':
          toolCalls = toolCalls.map((tc) =>
            tc.id === data.toolCallId
              ? { ...tc, result: data.result, status: 'completed' }
              : tc
          );
          break;

        case 'APPROVAL_REQUESTED':
          pendingApproval = {
            runId: data.runId,
            threadId: data.threadId,
            toolCallId: data.toolCallId,
            toolName: data.toolName,
            reason: data.reason,
            arguments: data.arguments,
            stateSnapshot: data.stateSnapshot
          };
          runStatus = 'waiting_for_approval';
          break;

        case 'STATE_SNAPSHOT':
          if (data.state) {
            sharedState = { ...sharedState, ...data.state };
          }
          break;

        case 'STATE_DELTA':
          if (Array.isArray(data.patch)) {
            // Apply lightweight JSON patch
            for (const p of data.patch) {
              const key = p.path.replace(/^\//, '');
              sharedState[key] = p.value;
            }
            sharedState = { ...sharedState };
          }
          break;

        case 'RUN_FINISHED':
          if (data.status === 'WAITING_FOR_APPROVAL') {
            runStatus = 'waiting_for_approval';
          } else if (data.status === 'REJECTED') {
            runStatus = 'rejected';
          } else {
            runStatus = 'completed';
          }
          break;

        case 'ERROR':
          runStatus = 'error';
          break;
      }
    } catch (e) {
      console.warn('Failed to parse AG-UI event data:', dataStr, e);
    }
  }

  function handleApprove() {
    startRun(true, true);
  }

  function handleReject() {
    startRun(true, false, 'Declined by user in UI prompt');
  }
</script>

<div class="agui-studio">
  <!-- Header & Specs Banner -->
  <div class="studio-header">
    <div class="header-left">
      <div class="title-row">
        <h3>⚡ AG-UI Protocol & Human-in-the-Loop (HITL) Studio</h3>
        <span class="badge protocol-badge">AG-UI v1.0.0</span>
        <span class="badge framework-badge">net8.0 · Framework-Agnostic</span>
      </div>
      <p class="subtitle">
        Standardized agent-to-UI streaming protocol over Server-Sent Events (SSE). Experience real-time thinking traces,
        tool dispatch, synchronized shared state, and Human-in-the-Loop approval workflows.
      </p>
    </div>
    <div class="header-links">
      <a
        href="https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/ui/ag-ui/?pivots=programming-language-csharp"
        target="_blank"
        rel="noreferrer"
        class="doc-link"
      >
        📖 MS Learn Docs
      </a>
      <a
        href="https://docs.ag-ui.com/sdk/dotnet/hosting/overview"
        target="_blank"
        rel="noreferrer"
        class="doc-link"
      >
        🌐 AG-UI .NET Docs
      </a>
      <a
        href="https://docs.ag-ui.com/sdk/js/core/overview"
        target="_blank"
        rel="noreferrer"
        class="doc-link"
      >
        📦 JS Core Docs
      </a>
    </div>
  </div>

  <!-- Run Configuration Bar -->
  <div class="config-bar">
    <div class="form-group flex-2">
      <label for="agui-prompt">User Prompt / Task Instructions</label>
      <input
        id="agui-prompt"
        type="text"
        bind:value={prompt}
        placeholder="Enter agent task..."
        disabled={isRunning}
      />
    </div>

    <div class="form-group flex-1">
      <label for="agui-endpoint">Server Endpoint</label>
      <select id="agui-endpoint" bind:value={endpoint} disabled={isRunning}>
        <option value="/api/agui">DevUI SvelteKit SSE (/api/agui)</option>
        <option value="http://localhost:5000/agui/run">.NET AG-UI Host (:5000/agui/run)</option>
      </select>
    </div>

    <div class="toggle-group">
      <label class="toggle-label">
        <input type="checkbox" bind:checked={requireApproval} disabled={isRunning} />
        <span>Require Human-in-the-Loop (HITL)</span>
      </label>
    </div>

    <div class="action-col">
      <button
        class="btn-run"
        on:click={() => startRun(false)}
        disabled={isRunning || !prompt.trim()}
      >
        {#if isRunning}
          <span class="spinner"></span> Running...
        {:else}
          ▶ Run AG-UI Agent
        {/if}
      </button>
    </div>
  </div>

  <!-- Status Ribbon -->
  <div class="status-ribbon">
    <div class="status-item">
      <span class="status-dot {runStatus}"></span>
      <span class="status-label">Status: <strong>{runStatus.replace(/_/g, ' ').toUpperCase()}</strong></span>
    </div>
    {#if currentThreadId}
      <div class="status-item">
        <span class="text-muted">Thread:</span> <code>{currentThreadId}</code>
      </div>
    {/if}
    {#if currentRunId}
      <div class="status-item">
        <span class="text-muted">Run:</span> <code>{currentRunId}</code>
      </div>
    {/if}
    <div class="status-item actions-right">
      <button class="btn-text" on:click={() => (showRawLogs = !showRawLogs)}>
        {showRawLogs ? 'Hide Raw SSE Events' : 'Show Raw SSE Events (' + events.length + ')'}
      </button>
    </div>
  </div>

  <!-- HUMAN-IN-THE-LOOP APPROVAL MODAL / INTERRUPT BANNER -->
  {#if pendingApproval}
    <div class="hitl-banner">
      <div class="hitl-header">
        <div class="hitl-icon">🛡️</div>
        <div>
          <h4>Human-in-the-Loop (HITL) Authorization Required</h4>
          <p class="hitl-desc">
            The agent paused execution to request operator confirmation before applying sensitive changes.
          </p>
        </div>
      </div>

      <div class="hitl-body">
        <div class="hitl-detail">
          <strong>Tool:</strong> <code>{pendingApproval.toolName}</code>
        </div>
        <div class="hitl-detail">
          <strong>Reason:</strong> <span>{pendingApproval.reason}</span>
        </div>
        <div class="hitl-detail">
          <strong>Arguments:</strong>
          <pre class="hitl-args">{JSON.stringify(pendingApproval.arguments, null, 2)}</pre>
        </div>
      </div>

      <div class="hitl-actions">
        <button class="btn-approve" on:click={handleApprove}>
          ✅ Authorize & Resume Execution
        </button>
        <button class="btn-reject" on:click={handleReject}>
          ❌ Reject Action
        </button>
      </div>
    </div>
  {/if}

  <!-- Main Output Grid -->
  <div class="grid-layout">
    <!-- Left Column: Reasoning & Response -->
    <div class="col-main">
      <!-- 1. Thinking / Reasoning Stream -->
      {#if thinkingLog}
        <div class="stream-card thinking-card">
          <div class="card-header">
            <span>🧠 Agent Thinking / Reasoning Stream (THINKING_DELTA)</span>
            <span class="tag">Cognitive Trace</span>
          </div>
          <div class="card-content thinking-text">
            {thinkingLog}
          </div>
        </div>
      {/if}

      <!-- 2. Tool Calls Stream -->
      {#if toolCalls.length > 0}
        <div class="stream-card tools-card">
          <div class="card-header">
            <span>🛠️ Dispatched Tools (TOOL_CALL_STARTED / FINISHED)</span>
            <span class="tag">{toolCalls.length} tool(s)</span>
          </div>
          <div class="card-content tool-list">
            {#each toolCalls as tc}
              <div class="tool-item">
                <div class="tool-header">
                  <span class="tool-name">🔧 {tc.name}</span>
                  <span class="badge tool-status {tc.status}">{tc.status}</span>
                </div>
                {#if tc.args}
                  <div class="tool-sub">
                    <span class="text-muted">Arguments:</span>
                    <code>{JSON.stringify(tc.args)}</code>
                  </div>
                {/if}
                {#if tc.result}
                  <div class="tool-sub">
                    <span class="text-muted">Output:</span>
                    <code>{JSON.stringify(tc.result)}</code>
                  </div>
                {/if}
              </div>
            {/each}
          </div>
        </div>
      {/if}

      <!-- 3. Assistant Output Stream -->
      <div class="stream-card output-card">
        <div class="card-header">
          <span>💬 Assistant Response (TEXT_DELTA)</span>
          {#if isRunning}
            <span class="tag live-tag">Streaming live</span>
          {/if}
        </div>
        <div class="card-content output-text">
          {#if responseText}
            {responseText}
          {:else if isRunning}
            <p class="placeholder-text"><span class="pulse">●</span> Agent is evaluating prompt and generating response...</p>
          {:else}
            <p class="placeholder-text">Click "Run AG-UI Agent" above to initiate a real-time Server-Sent Events (SSE) session.</p>
          {/if}
        </div>
      </div>
    </div>

    <!-- Right Column: Synchronized State & Info -->
    <div class="col-side">
      <!-- Shared State Card -->
      <div class="stream-card state-card">
        <div class="card-header">
          <span>📦 Shared Application State</span>
          <span class="tag">STATE_SNAPSHOT / DELTA</span>
        </div>
        <div class="card-content">
          {#if Object.keys(sharedState).length > 0}
            <pre class="state-json">{JSON.stringify(sharedState, null, 2)}</pre>
          {:else}
            <p class="text-muted small">No state synced yet. State updates will appear dynamically.</p>
          {/if}
        </div>
      </div>

      <!-- Protocol Features Checklist -->
      <div class="stream-card checklist-card">
        <div class="card-header">
          <span>📋 AG-UI Hosting Features</span>
        </div>
        <div class="card-content">
          <ul class="feature-checklist">
            <li><span class="check">✔</span> Server-Sent Events (SSE) Streaming</li>
            <li><span class="check">✔</span> Human-in-the-Loop (HITL) Interruption</li>
            <li><span class="check">✔</span> Operator Approval Resume Loop</li>
            <li><span class="check">✔</span> Real-Time Thinking / Reasoning Traces</li>
            <li><span class="check">✔</span> C# Clock & Bioinformatics Tool Dispatch</li>
            <li><span class="check">✔</span> Bi-directional JSON Patch State Sync</li>
            <li><span class="check">✔</span> Framework-Agnostic Hosting (.NET 8)</li>
          </ul>
        </div>
      </div>
    </div>
  </div>

  <!-- Raw SSE Event Log Drawer -->
  {#if showRawLogs}
    <div class="raw-logs-drawer">
      <div class="drawer-header">
        <h4>📡 Raw AG-UI Event Frames (SSE Wire Format)</h4>
        <button class="btn-text" on:click={() => (showRawLogs = false)}>Close</button>
      </div>
      <div class="drawer-content">
        {#each events as ev}
          <div class="raw-event-line">
            <span class="ev-time">[{ev.timestamp}]</span>
            <span class="ev-type badge">{ev.type}</span>
            <span class="ev-data">{JSON.stringify(ev.payload)}</span>
          </div>
        {/each}
        {#if events.length === 0}
          <p class="text-muted small">No events recorded yet.</p>
        {/if}
      </div>
    </div>
  {/if}
</div>

<style>
  .agui-studio {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 0.75rem;
    padding: 1.5rem;
    color: #e2e8f0;
    margin-top: 1.5rem;
  }

  .studio-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    padding-bottom: 1.25rem;
    border-bottom: 1px solid #1e293b;
    margin-bottom: 1.25rem;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .title-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
  }

  .title-row h3 {
    margin: 0;
    font-size: 1.25rem;
    color: #f8fafc;
  }

  .subtitle {
    margin: 0.4rem 0 0 0;
    color: #94a3b8;
    font-size: 0.875rem;
    max-width: 800px;
  }

  .header-links {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
  }

  .doc-link {
    font-size: 0.75rem;
    padding: 0.35rem 0.65rem;
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 0.375rem;
    color: #38bdf8;
    text-decoration: none;
    transition: all 0.2s;
  }

  .doc-link:hover {
    background: #0284c7;
    color: #ffffff;
  }

  .badge {
    font-size: 0.7rem;
    padding: 0.2rem 0.5rem;
    border-radius: 0.375rem;
    font-weight: 600;
  }

  .protocol-badge {
    background: #0369a1;
    color: #e0f2fe;
  }

  .framework-badge {
    background: #4338ca;
    color: #e0e7ff;
  }

  .config-bar {
    display: flex;
    gap: 1rem;
    align-items: flex-end;
    background: #1e293b;
    padding: 1rem;
    border-radius: 0.5rem;
    margin-bottom: 1rem;
    flex-wrap: wrap;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .flex-1 { flex: 1; min-width: 200px; }
  .flex-2 { flex: 2; min-width: 320px; }

  .form-group label {
    font-size: 0.75rem;
    font-weight: 500;
    color: #94a3b8;
  }

  .form-group input,
  .form-group select {
    background: #0f172a;
    border: 1px solid #334155;
    color: #f8fafc;
    padding: 0.5rem 0.75rem;
    border-radius: 0.375rem;
    font-size: 0.875rem;
  }

  .toggle-group {
    display: flex;
    align-items: center;
    padding-bottom: 0.5rem;
  }

  .toggle-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.8rem;
    color: #f59e0b;
    cursor: pointer;
  }

  .btn-run {
    background: #0284c7;
    color: white;
    border: none;
    padding: 0.55rem 1.25rem;
    border-radius: 0.375rem;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.2s;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .btn-run:hover:not(:disabled) {
    background: #0369a1;
  }

  .btn-run:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .status-ribbon {
    display: flex;
    align-items: center;
    gap: 1.5rem;
    padding: 0.5rem 0.75rem;
    background: #111827;
    border-radius: 0.375rem;
    font-size: 0.8rem;
    margin-bottom: 1rem;
  }

  .status-item {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .actions-right {
    margin-left: auto;
  }

  .status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
  }

  .status-dot.idle { background: #64748b; }
  .status-dot.running { background: #38bdf8; box-shadow: 0 0 8px #38bdf8; animation: pulse 1.5s infinite; }
  .status-dot.waiting_for_approval { background: #f59e0b; box-shadow: 0 0 8px #f59e0b; }
  .status-dot.completed { background: #22c55e; }
  .status-dot.rejected { background: #ef4444; }
  .status-dot.error { background: #dc2626; }

  /* HITL Banner */
  .hitl-banner {
    background: linear-gradient(135deg, #451a03 0%, #1c1917 100%);
    border: 2px solid #f59e0b;
    border-radius: 0.5rem;
    padding: 1.25rem;
    margin-bottom: 1.25rem;
    box-shadow: 0 0 20px rgba(245, 158, 11, 0.2);
  }

  .hitl-header {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    margin-bottom: 0.75rem;
  }

  .hitl-icon {
    font-size: 1.75rem;
  }

  .hitl-header h4 {
    margin: 0;
    color: #fef3c7;
    font-size: 1.1rem;
  }

  .hitl-desc {
    margin: 0.25rem 0 0 0;
    color: #fde68a;
    font-size: 0.85rem;
  }

  .hitl-body {
    background: rgba(0, 0, 0, 0.4);
    padding: 0.75rem;
    border-radius: 0.375rem;
    margin-bottom: 1rem;
  }

  .hitl-detail {
    font-size: 0.85rem;
    margin-bottom: 0.4rem;
  }

  .hitl-args {
    background: #0c0a09;
    padding: 0.5rem;
    border-radius: 0.25rem;
    color: #38bdf8;
    font-size: 0.8rem;
    margin: 0.25rem 0 0 0;
  }

  .hitl-actions {
    display: flex;
    gap: 0.75rem;
  }

  .btn-approve {
    background: #16a34a;
    color: white;
    border: none;
    padding: 0.5rem 1.25rem;
    border-radius: 0.375rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-approve:hover {
    background: #15803d;
  }

  .btn-reject {
    background: #dc2626;
    color: white;
    border: none;
    padding: 0.5rem 1.25rem;
    border-radius: 0.375rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-reject:hover {
    background: #b91c1c;
  }

  /* Grid */
  .grid-layout {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 1rem;
  }

  @media (max-width: 900px) {
    .grid-layout {
      grid-template-columns: 1fr;
    }
  }

  .col-main, .col-side {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .stream-card {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 0.5rem;
    overflow: hidden;
  }

  .card-header {
    background: #0f172a;
    padding: 0.6rem 0.85rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.8rem;
    font-weight: 600;
    color: #cbd5e1;
    border-bottom: 1px solid #334155;
  }

  .tag {
    font-size: 0.7rem;
    background: #334155;
    padding: 0.15rem 0.4rem;
    border-radius: 0.25rem;
    color: #94a3b8;
  }

  .live-tag {
    background: #0369a1;
    color: #e0f2fe;
    animation: pulse 1.5s infinite;
  }

  .card-content {
    padding: 0.85rem;
  }

  .thinking-card {
    border-left: 3px solid #8b5cf6;
  }

  .thinking-text {
    font-family: ui-monospace, monospace;
    font-size: 0.8rem;
    color: #c4b5fd;
    white-space: pre-wrap;
    line-height: 1.4;
    max-height: 160px;
    overflow-y: auto;
  }

  .tool-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .tool-item {
    background: #0f172a;
    padding: 0.5rem 0.75rem;
    border-radius: 0.375rem;
    font-size: 0.8rem;
  }

  .tool-header {
    display: flex;
    justify-content: space-between;
    margin-bottom: 0.25rem;
  }

  .tool-name {
    font-weight: 600;
    color: #38bdf8;
  }

  .tool-status.completed {
    background: #14532d;
    color: #86efac;
  }

  .tool-status.executing {
    background: #1e3a8a;
    color: #93c5fd;
  }

  .tool-sub {
    font-size: 0.75rem;
    margin-top: 0.2rem;
  }

  .tool-sub code {
    color: #a5f3fc;
  }

  .output-card {
    border-left: 3px solid #0284c7;
    min-height: 180px;
  }

  .output-text {
    font-size: 0.95rem;
    line-height: 1.6;
    color: #f8fafc;
    white-space: pre-wrap;
  }

  .placeholder-text {
    color: #64748b;
    font-style: italic;
  }

  .state-json {
    background: #0f172a;
    padding: 0.6rem;
    border-radius: 0.375rem;
    color: #38bdf8;
    font-size: 0.75rem;
    max-height: 240px;
    overflow-y: auto;
    margin: 0;
  }

  .feature-checklist {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    font-size: 0.8rem;
  }

  .feature-checklist li {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .check {
    color: #22c55e;
    font-weight: bold;
  }

  .raw-logs-drawer {
    margin-top: 1rem;
    background: #090d16;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    padding: 1rem;
    max-height: 300px;
    overflow-y: auto;
  }

  .drawer-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;
  }

  .drawer-header h4 {
    margin: 0;
    font-size: 0.85rem;
    color: #94a3b8;
  }

  .drawer-content {
    font-family: ui-monospace, monospace;
    font-size: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .raw-event-line {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .ev-time {
    color: #64748b;
  }

  .ev-type {
    background: #1e293b;
    color: #38bdf8;
  }

  .ev-data {
    color: #cbd5e1;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .btn-text {
    background: none;
    border: none;
    color: #38bdf8;
    cursor: pointer;
    font-size: 0.8rem;
  }

  .btn-text:hover {
    text-decoration: underline;
  }

  .text-muted { color: #64748b; }
  .small { font-size: 0.75rem; }

  @keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.4; }
  }
</style>
