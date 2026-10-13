<script lang="ts">
  import { onMount } from 'svelte';
  
  let executions: any[] = [];
  let loading = true;
  let error = '';
  
  onMount(async () => {
    await loadExecutions();
  });
  
  async function loadExecutions() {
    try {
      loading = true;
      const res = await fetch('/api/executions');
      const data = await res.json();
      executions = data.executions || [];
      loading = false;
    } catch (e) {
      error = 'Failed to load executions';
      loading = false;
    }
  }
  
  function getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'completed': return 'status-completed';
      case 'running': return 'status-running';
      case 'failed': return 'status-failed';
      case 'pending': return 'status-pending';
      default: return 'status-default';
    }
  }
  
  function formatDuration(ms: number): string {
    if (!ms) return '-';
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  }
</script>

<div class="executions-page">
  <div class="page-header">
    <h1>Executions</h1>
    <button class="btn-secondary" on:click={loadExecutions}>↻ Refresh</button>
  </div>
  <p class="page-description">
    Monitor workflow and skill executions. Track progress, view results, and diagnose issues.
  </p>
  
  {#if loading}
    <div class="loading">Loading executions...</div>
  {:else if error}
    <div class="error-card">{error}</div>
  {:else if executions.length === 0}
    <div class="empty-state">
      <span class="empty-icon">⚡</span>
      <h3>No Executions Yet</h3>
      <p>Execute a workflow or skill to see results here.</p>
    </div>
  {:else}
    <div class="executions-list">
      {#each executions as execution}
        <div class="execution-card">
          <div class="execution-header">
            <div class="execution-info">
              <h3>{execution.workflowName || execution.id}</h3>
              <span class="execution-id">ID: {execution.id}</span>
            </div>
            <span class="status-badge {getStatusClass(execution.status)}">{execution.status}</span>
          </div>
          <div class="execution-meta">
            <span>Started: {new Date(execution.startTime).toLocaleString()}</span>
            <span>Duration: {formatDuration(execution.duration)}</span>
          </div>
          {#if execution.error}
            <div class="execution-error">
              <strong>Error:</strong> {execution.error.message || execution.error}
            </div>
          {/if}
          {#if execution.outputs && Object.keys(execution.outputs).length > 0}
            <div class="execution-outputs">
              <strong>Outputs:</strong>
              <pre>{JSON.stringify(execution.outputs, null, 2)}</pre>
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .executions-page { max-width: 1200px; }
  .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; }
  .page-header h1 { font-size: 1.75rem; font-weight: 700; color: #f1f5f9; margin: 0; }
  .page-description { color: #94a3b8; margin: 0 0 2rem 0; }
  .btn-secondary { background: var(--bg-card); color: #e2e8f0; border: 1px solid var(--border); padding: 0.5rem 1rem; border-radius: 0.375rem; cursor: pointer; }
  .loading { text-align: center; padding: 3rem; color: #94a3b8; }
  .error-card { background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); border-radius: 0.5rem; padding: 1rem; color: #ef4444; }
  .empty-state { text-align: center; padding: 4rem; background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; }
  .empty-icon { font-size: 3rem; }
  .empty-state h3 { color: #e2e8f0; margin: 1rem 0 0.5rem 0; }
  .empty-state p { color: #94a3b8; margin: 0; }
  .executions-list { display: flex; flex-direction: column; gap: 1rem; }
  .execution-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.25rem; }
  .execution-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem; }
  .execution-info h3 { font-size: 1rem; font-weight: 600; color: #e2e8f0; margin: 0 0 0.25rem 0; }
  .execution-id { font-size: 0.75rem; color: #64748b; }
  .status-badge { padding: 0.25rem 0.75rem; border-radius: 1rem; font-size: 0.75rem; font-weight: 500; text-transform: uppercase; }
  .status-completed { background: rgba(34, 197, 94, 0.15); color: #22c55e; }
  .status-running { background: rgba(14, 165, 233, 0.15); color: var(--primary); }
  .status-failed { background: rgba(239, 68, 68, 0.15); color: #ef4444; }
  .status-pending { background: rgba(234, 179, 8, 0.15); color: #eab308; }
  .status-default { background: rgba(100, 116, 139, 0.15); color: #64748b; }
  .execution-meta { display: flex; gap: 1.5rem; font-size: 0.8rem; color: #64748b; margin-bottom: 0.75rem; }
  .execution-error { background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); border-radius: 0.25rem; padding: 0.75rem; color: #fca5a5; font-size: 0.85rem; }
  .execution-outputs { margin-top: 0.75rem; }
  .execution-outputs strong { color: #94a3b8; font-size: 0.8rem; }
  .execution-outputs pre { background: var(--bg-dark); border: 1px solid var(--border); border-radius: 0.25rem; padding: 0.75rem; font-size: 0.8rem; color: #94a3b8; overflow-x: auto; margin: 0.5rem 0 0 0; }
</style>