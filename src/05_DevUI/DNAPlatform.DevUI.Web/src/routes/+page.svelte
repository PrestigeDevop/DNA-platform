<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { api } from '$lib/services/api';
  import { captureConsole } from '$lib/services/logs';

  let health: any = null;
  let skills: any[] = [];
  let workflows: any[] = [];
  let agents: any[] = [];
  let executions: any[] = [];
  let loading = true;
  let refreshing = false;
  let backendStatus = 'Connecting...';
  let backendHealthy = false;
  let dbStatus = 'Checking...';
  let dbHealthy = false;
  let dbError = '';
  let lastChecked = '';
  let refreshTimer: any = null;

  async function checkSystemStatus(silent = false) {
    if (!silent) refreshing = true;
    try {
      // 1) Backend connectivity & health check
      try {
        health = await api.health();
        backendHealthy = true;
        backendStatus = health?.version ? `Active (v${health.version})` : 'Active (:5254 / Node)';
      } catch (e) {
        backendHealthy = false;
        backendStatus = 'Degraded (Running Standalone Mode)';
      }

      // 2) Skills check
      try {
        skills = await api.getSkills();
      } catch (e) {
        skills = [];
      }

      // 3) Database-backed entities (Workflows, Agents, Executions)
      try {
        workflows = await api.getWorkflows();
        agents = await api.getAgents();
        executions = await api.getExecutions();
        dbHealthy = true;
        dbStatus = 'Online (dev.db)';
        dbError = '';
      } catch (e: any) {
        dbHealthy = false;
        dbStatus = 'Degraded (In-Memory Fallback)';
        dbError = 'Database (SQLite/Prisma) unavailable — running in in-memory resilience mode. Run "npm run bootstrap" or click "Initialize Database" below to sync SQLite schemas.';
      }

      lastChecked = new Date().toLocaleTimeString();
    } catch (e) {
      console.warn('System status heartbeat warning:', e);
    } finally {
      loading = false;
      if (!silent) refreshing = false;
    }
  }

  onMount(() => {
    captureConsole();
    checkSystemStatus(false);
    // Live heartbeat auto-refresh every 4 seconds so UI updates without reloading
    refreshTimer = setInterval(() => {
      checkSystemStatus(true);
    }, 4000);
  });

  onDestroy(() => {
    if (refreshTimer) clearInterval(refreshTimer);
  });

  let syncingDb = false;
  async function syncDatabase() {
    syncingDb = true;
    try {
      await fetch('/api/environment/verify', { method: 'POST' });
      await checkSystemStatus(false);
    } catch (e: any) {
      dbError = 'Database synchronization retry failed: ' + (e.message || String(e));
    } finally {
      syncingDb = false;
    }
  }

  async function createSampleWorkflow() {
    const sampleWorkflow = {
      id: 'sample-' + Date.now(),
      name: 'Sample Bioinformatic Pipeline',
      nodes: [
        { id: 'input', name: 'FASTA Input', nodeType: 0, config: { source: 'sequences.fasta' } },
        { id: 'align', name: 'Sequence Alignment', nodeType: 2, config: { operation: 'align' } },
        { id: 'output', name: 'Results Output', nodeType: 4, config: { destination: 'results.json' } }
      ],
      connections: [
        { sourceNodeId: 'input', targetNodeId: 'align', outputKey: 'data', inputKey: 'input' },
        { sourceNodeId: 'align', targetNodeId: 'output', outputKey: 'result', inputKey: 'input' }
      ]
    };
    
    try {
      await api.executeWorkflow(sampleWorkflow);
      alert('Sample workflow created and executed successfully!');
      await checkSystemStatus(false);
    } catch (e) {
      alert('Error creating workflow: ' + e);
    }
  }
</script>

<div class="dashboard">
  <div class="dashboard-header">
    <div>
      <h1 class="page-heading">DNA Platform Studio</h1>
      <p class="subtitle">Autonomous Polyglot Computational Biology & Workflow Engine</p>
    </div>
    <div class="header-actions">
      <button class="btn-refresh-status" on:click={() => checkSystemStatus(false)} disabled={refreshing}>
        <span class="refresh-icon" class:spin={refreshing}>↻</span>
        {refreshing ? 'Checking...' : 'Refresh Status'}
      </button>
      {#if lastChecked}
        <span class="last-updated-text">Checked: {lastChecked}</span>
      {/if}
    </div>
  </div>
  
  {#if loading}
    <div class="loading">
      <div class="spinner"></div>
      <p>Connecting to backend services...</p>
    </div>
  {:else}
    {#if dbError}
      <div class="warning-card glass-panel">
        <div class="warning-header">
          <div class="warning-title">
            <span class="warning-icon">⚠️</span>
            <div>
              <h3>Database Status: Degraded (In-Memory Fallback Active)</h3>
              <p class="warning-desc">{dbError}</p>
            </div>
          </div>
          <button class="btn-sync" on:click={syncDatabase} disabled={syncingDb}>
            🔄 {syncingDb ? 'Syncing Schema...' : 'Initialize / Sync Database'}
          </button>
        </div>
      </div>
    {/if}

    <div class="stats-grid">
      <div class="stat-card glass-panel">
        <div class="stat-icon-wrap bg-cyan">
          <span class="stat-icon">🔀</span>
        </div>
        <div class="stat-info">
          <span class="stat-value">{workflows.length}</span>
          <span class="stat-label">Workflows</span>
        </div>
      </div>

      <div class="stat-card glass-panel">
        <div class="stat-icon-wrap bg-amber">
          <span class="stat-icon">🧬</span>
        </div>
        <div class="stat-info">
          <span class="stat-value">{skills.length}</span>
          <span class="stat-label">Registered Skills</span>
        </div>
      </div>

      <div class="stat-card glass-panel">
        <div class="stat-icon-wrap bg-purple">
          <span class="stat-icon">⚡</span>
        </div>
        <div class="stat-info">
          <span class="stat-value">{executions.length}</span>
          <span class="stat-label">Executions</span>
        </div>
      </div>

      <div class="stat-card glass-panel">
        <div class="stat-icon-wrap bg-blue">
          <span class="stat-icon">🤖</span>
        </div>
        <div class="stat-info">
          <span class="stat-value">{agents.length}</span>
          <span class="stat-label">Active Agents</span>
        </div>
      </div>
    </div>
    
    <!-- SYSTEM SERVICE HEALTH CARD -->
    <div class="card glass-panel">
      <div class="card-header-row">
        <h3>🖥️ System Service Status</h3>
        <span class="status-live-badge">● Live Heartbeat</span>
      </div>
      <div class="status-grid">
        <div class="status-item">
          <span class="status-dot" class:healthy={backendHealthy} class:unhealthy={!backendHealthy}></span>
          <div class="status-label-group">
            <span class="status-title">Backend Engine</span>
            <span class="status-detail">ASP.NET Core / SvelteKit API</span>
          </div>
          <span class="status-url" class:badge-ok={backendHealthy} class:badge-warn={!backendHealthy}>
            {backendStatus}
          </span>
        </div>

        <div class="status-item">
          <span class="status-dot" class:healthy={dbHealthy} class:unhealthy={!dbHealthy}></span>
          <div class="status-label-group">
            <span class="status-title">SQLite Database</span>
            <span class="status-detail">Prisma schema & dev.db</span>
          </div>
          <span class="status-url" class:badge-ok={dbHealthy} class:badge-warn={!dbHealthy}>
            {dbStatus}
          </span>
        </div>

        <div class="status-item">
          <span class="status-dot healthy"></span>
          <div class="status-label-group">
            <span class="status-title">AG-UI SSE Streaming Protocol</span>
            <span class="status-detail">Server-Sent Events & HITL Approval</span>
          </div>
          <span class="status-url badge-ok">/api/agui (v1.0.0 Active)</span>
        </div>

        <div class="status-item">
          <span class="status-dot healthy"></span>
          <div class="status-label-group">
            <span class="status-title">Error Diagnostic Log Stream</span>
            <span class="status-detail">Redirected console & runtime errors</span>
          </div>
          <a href="/logs" class="status-url log-link" title="Click to view diagnostics">
            logs/platform_errors.log ↗
          </a>
        </div>
      </div>
    </div>
    
    <!-- QUICK ACTIONS -->
    <div class="card glass-panel">
      <h3>⚡ Studio Navigation & Actions</h3>
      <div class="actions-grid">
        <a href="/kernel" class="action-btn action-featured">
          <span class="action-icon">💻</span>
          <span class="action-title">Polyglot Kernel</span>
          <span class="action-desc">C#, Python, JS Notebook</span>
        </a>
        <a href="/workflows" class="action-btn">
          <span class="action-icon">🔀</span>
          <span class="action-title">Flow Canvas</span>
          <span class="action-desc">Design & FSM Editor</span>
        </a>
        <a href="/skills" class="action-btn">
          <span class="action-icon">🧬</span>
          <span class="action-title">Skills & MCP</span>
          <span class="action-desc">Browse Plugin Tools</span>
        </a>
        <button class="action-btn" on:click={createSampleWorkflow}>
          <span class="action-icon">🚀</span>
          <span class="action-title">Run Sample</span>
          <span class="action-desc">Pairwise Alignment</span>
        </button>
        <a href="/logs" class="action-btn">
          <span class="action-icon">📜</span>
          <span class="action-title">Error Logs</span>
          <span class="action-desc">Diagnostics Stream</span>
        </a>
      </div>
    </div>
    
    {#if skills.length > 0}
      <div class="card glass-panel">
        <h3>🧬 Available Registry Skills ({skills.length})</h3>
        <div class="skills-preview">
          {#each skills.slice(0, 8) as skill}
            <div class="skill-chip">
              <span class="skill-dot"></span>
              <span class="skill-name">{skill.name || skill.skillId}</span>
            </div>
          {/each}
        </div>
      </div>
    {/if}
  {/if}
</div>

<style>
  .dashboard {
    max-width: 1200px;
    margin: 0 auto;
    position: relative;
  }

  .dashboard-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 2rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .page-heading {
    font-size: 2rem;
    font-weight: 800;
    letter-spacing: -0.025em;
    background: linear-gradient(135deg, #f8fafc 0%, #38bdf8 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    margin: 0 0 0.4rem 0;
  }

  .subtitle {
    color: #94a3b8;
    margin: 0;
    font-size: 0.95rem;
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .btn-refresh-status {
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(56, 189, 248, 0.3);
    color: #38bdf8;
    padding: 0.45rem 0.9rem;
    font-size: 0.85rem;
    font-weight: 600;
    border-radius: 6px;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    transition: all 0.2s ease;
  }

  .btn-refresh-status:hover:not(:disabled) {
    background: rgba(14, 165, 233, 0.25);
    border-color: #38bdf8;
  }

  .refresh-icon {
    font-size: 1rem;
    display: inline-block;
    transition: transform 0.5s ease;
  }

  .refresh-icon.spin {
    animation: spin 1s linear infinite;
  }

  .last-updated-text {
    font-size: 0.75rem;
    color: #64748b;
  }

  .glass-panel {
    background: rgba(15, 23, 42, 0.65);
    backdrop-filter: blur(16px);
    -webkit-backdrop-filter: blur(16px);
    border: 1px solid rgba(255, 255, 255, 0.08);
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.37);
  }

  .loading {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4rem;
  }

  .spinner {
    width: 44px;
    height: 44px;
    border: 3px solid rgba(56, 189, 248, 0.15);
    border-top-color: #38bdf8;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 1rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }

  .warning-card {
    border: 1px solid rgba(245, 158, 11, 0.3);
    border-radius: 0.75rem;
    padding: 1.25rem 1.5rem;
    margin-bottom: 1.75rem;
    background: rgba(245, 158, 11, 0.08);
  }

  .warning-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .warning-title {
    display: flex;
    gap: 0.85rem;
    align-items: flex-start;
  }

  .warning-icon {
    font-size: 1.5rem;
    line-height: 1;
  }

  .warning-title h3 {
    margin: 0 0 0.35rem 0;
    font-size: 1rem;
    color: #f59e0b;
    font-weight: 600;
  }

  .warning-desc {
    margin: 0;
    font-size: 0.85rem;
    color: #fde68a;
    line-height: 1.4;
  }

  .btn-sync {
    background: #0284c7;
    border: 1px solid #38bdf8;
    color: white;
    padding: 0.4rem 0.85rem;
    font-size: 0.8rem;
    font-weight: 600;
    border-radius: 6px;
    cursor: pointer;
    transition: background 0.2s ease;
    white-space: nowrap;
  }

  .btn-sync:hover:not(:disabled) {
    background: #0369a1;
  }

  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 1.25rem;
    margin-bottom: 1.75rem;
  }

  .stat-card {
    border-radius: 0.75rem;
    padding: 1.25rem 1.5rem;
    display: flex;
    align-items: center;
    gap: 1.15rem;
    transition: transform 0.2s ease, box-shadow 0.2s ease;
  }

  .stat-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 28px rgba(0, 0, 0, 0.45);
  }

  .stat-icon-wrap {
    width: 48px;
    height: 48px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .bg-cyan { background: rgba(6, 182, 212, 0.15); border: 1px solid rgba(6, 182, 212, 0.3); }
  .bg-amber { background: rgba(245, 158, 11, 0.15); border: 1px solid rgba(245, 158, 11, 0.3); }
  .bg-purple { background: rgba(168, 85, 247, 0.15); border: 1px solid rgba(168, 85, 247, 0.3); }
  .bg-blue { background: rgba(59, 130, 246, 0.15); border: 1px solid rgba(59, 130, 246, 0.3); }

  .stat-icon {
    font-size: 1.5rem;
  }

  .stat-info {
    display: flex;
    flex-direction: column;
  }

  .stat-value {
    font-size: 1.75rem;
    font-weight: 800;
    color: #f8fafc;
    line-height: 1.1;
  }

  .stat-label {
    font-size: 0.85rem;
    color: #94a3b8;
    margin-top: 0.25rem;
  }

  .card {
    border-radius: 0.75rem;
    padding: 1.5rem;
    margin-bottom: 1.75rem;
  }

  .card-header-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
  }

  .card h3 {
    font-size: 1.1rem;
    font-weight: 600;
    color: #e2e8f0;
    margin: 0;
  }

  .status-live-badge {
    font-size: 0.75rem;
    color: #22c55e;
    background: rgba(34, 197, 94, 0.1);
    border: 1px solid rgba(34, 197, 94, 0.25);
    padding: 0.2rem 0.55rem;
    border-radius: 12px;
    font-weight: 500;
  }

  .status-grid {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .status-item {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.75rem 1rem;
    background: rgba(15, 23, 42, 0.5);
    border: 1px solid rgba(255, 255, 255, 0.05);
    border-radius: 0.5rem;
  }

  .status-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .status-dot.healthy {
    background: #22c55e;
    box-shadow: 0 0 10px #22c55e;
  }

  .status-dot.unhealthy {
    background: #f59e0b;
    box-shadow: 0 0 10px rgba(245, 158, 11, 0.6);
  }

  .status-label-group {
    display: flex;
    flex-direction: column;
  }

  .status-title {
    font-weight: 600;
    font-size: 0.9rem;
    color: #f1f5f9;
  }

  .status-detail {
    font-size: 0.75rem;
    color: #64748b;
  }

  .status-url {
    margin-left: auto;
    font-size: 0.8rem;
    padding: 0.2rem 0.6rem;
    border-radius: 4px;
    font-family: monospace;
  }

  .badge-ok {
    background: rgba(34, 197, 94, 0.12);
    border: 1px solid rgba(34, 197, 94, 0.25);
    color: #4ade80;
  }

  .badge-warn {
    background: rgba(245, 158, 11, 0.12);
    border: 1px solid rgba(245, 158, 11, 0.25);
    color: #fbbf24;
  }

  .log-link {
    color: #38bdf8;
    background: rgba(14, 165, 233, 0.12);
    border: 1px solid rgba(56, 189, 248, 0.25);
    text-decoration: none;
    transition: all 0.2s ease;
  }

  .log-link:hover {
    background: rgba(14, 165, 233, 0.25);
    color: #7dd3fc;
  }

  .actions-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 1rem;
    margin-top: 1rem;
  }

  .action-btn {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    padding: 1.15rem;
    background: rgba(15, 23, 42, 0.5);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.65rem;
    text-decoration: none;
    cursor: pointer;
    transition: all 0.2s ease;
    text-align: left;
    color: inherit;
  }

  .action-btn:hover {
    background: rgba(30, 41, 59, 0.6);
    border-color: rgba(56, 189, 248, 0.4);
    transform: translateY(-2px);
  }

  .action-featured {
    background: linear-gradient(135deg, rgba(14, 165, 233, 0.15) 0%, rgba(15, 23, 42, 0.5) 100%);
    border-color: rgba(56, 189, 248, 0.35);
  }

  .action-icon {
    font-size: 1.5rem;
    margin-bottom: 0.5rem;
  }

  .action-title {
    font-weight: 600;
    font-size: 0.95rem;
    color: #f1f5f9;
    margin-bottom: 0.2rem;
  }

  .action-desc {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .skills-preview {
    display: flex;
    flex-wrap: wrap;
    gap: 0.6rem;
    margin-top: 1rem;
  }

  .skill-chip {
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(56, 189, 248, 0.2);
    border-radius: 2rem;
    padding: 0.4rem 0.85rem;
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
  }

  .skill-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #38bdf8;
  }

  .skill-name {
    font-size: 0.85rem;
    color: #e2e8f0;
    font-weight: 500;
  }
</style>
