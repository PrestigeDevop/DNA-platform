<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import {
    logRecords,
    logPolling,
    logPollError,
    startBackendPolling,
    stopBackendPolling,
    clearLogs,
    type LogRecord
  } from '$lib/services/logs';

  let filterSource: 'all' | 'frontend' | 'backend' = 'all';
  let filterLevel = 'all';
  let autoScroll = true;
  let consoleEl: HTMLDivElement;

  const levels = ['all', 'Debug', 'Information', 'Warning', 'Error'];

  onMount(() => {
    startBackendPolling();
  });

  onDestroy(() => {
    stopBackendPolling();
  });

  $: records = $logRecords;
  $: visible = records.filter(
    (r) =>
      (filterSource === 'all' || r.source === filterSource) &&
      (filterLevel === 'all' || r.level === filterLevel)
  );

  $: if (autoScroll && consoleEl && visible.length) {
    queueMicrotask(() => {
      if (consoleEl) consoleEl.scrollTop = consoleEl.scrollHeight;
    });
  }

  function levelClass(level: string): string {
    switch (level) {
      case 'Error':
        return 'error';
      case 'Warning':
        return 'warn';
      case 'Debug':
        return 'debug';
      default:
        return 'info';
    }
  }

  function time(ts: string): string {
    const d = new Date(ts);
    return isNaN(d.getTime()) ? ts : d.toLocaleTimeString();
  }
</script>

<div class="logs-page">
  <div class="page-header">
    <div>
      <h1>Logs</h1>
      <p class="page-description">Live frontend and backend log stream.</p>
    </div>
    <div class="header-actions">
      <span class="status" class:live={$logPolling}>
        {$logPolling ? '● live' : '○ offline'}
      </span>
      <button class="btn" on:click={clearLogs}>🗑 Clear</button>
    </div>
  </div>

  {#if $logPollError}
    <div class="error-card">Backend logs unavailable: {$logPollError}</div>
  {/if}

  <div class="filters">
    <label>Source
      <select bind:value={filterSource}>
        <option value="all">All</option>
        <option value="frontend">Frontend</option>
        <option value="backend">Backend</option>
      </select>
    </label>
    <label>Level
      <select bind:value={filterLevel}>
        {#each levels as lvl}<option value={lvl}>{lvl}</option>{/each}
      </select>
    </label>
    <label class="checkbox"><input type="checkbox" bind:checked={autoScroll} /> Auto-scroll</label>
    <span class="count">{visible.length} / {records.length} records</span>
  </div>

  <div class="console" bind:this={consoleEl}>
    {#if visible.length === 0}
      <div class="empty">No log records yet.</div>
    {:else}
      {#each visible as record (record.id)}
        <div class="line {levelClass(record.level)}">
          <span class="ts">{time(record.timestamp)}</span>
          <span class="lvl">{record.level}</span>
          <span class="src {record.source}">{record.source}</span>
          <span class="cat">{record.category}</span>
          <span class="msg">{record.message}</span>
        </div>
      {/each}
    {/if}
  </div>
</div>

<style>
  .logs-page { max-width: 1200px; display: flex; flex-direction: column; height: calc(100vh - 4rem); }
  .page-header { display: flex; justify-content: space-between; align-items: flex-start; }
  .page-header h1 { font-size: 1.75rem; font-weight: 700; color: #f1f5f9; margin: 0; }
  .page-description { color: #94a3b8; margin: 0.25rem 0 1rem 0; }
  .header-actions { display: flex; align-items: center; gap: 0.75rem; }
  .status { color: #64748b; font-size: 0.85rem; }
  .status.live { color: #10b981; }
  .btn { background: var(--bg-card); color: #e2e8f0; border: 1px solid var(--border); padding: 0.5rem 0.9rem; border-radius: 0.375rem; cursor: pointer; }
  .error-card { background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); border-radius: 0.5rem; padding: 0.75rem 1rem; color: #ef4444; margin-bottom: 1rem; }
  .filters { display: flex; align-items: center; gap: 1rem; margin-bottom: 0.75rem; flex-wrap: wrap; }
  .filters label { color: #94a3b8; font-size: 0.8rem; display: flex; align-items: center; gap: 0.4rem; }
  .filters select { background: var(--bg-card); color: #e2e8f0; border: 1px solid var(--border); border-radius: 0.375rem; padding: 0.3rem 0.5rem; }
  .checkbox { cursor: pointer; }
  .count { margin-left: auto; color: #64748b; font-size: 0.8rem; }
  .console { flex: 1; overflow-y: auto; background: #0b1220; border: 1px solid var(--border); border-radius: 0.5rem; padding: 0.5rem; font-family: ui-monospace, monospace; font-size: 0.78rem; }
  .empty { color: #475569; padding: 1rem; }
  .line { display: grid; grid-template-columns: 90px 80px 70px 120px 1fr; gap: 0.5rem; padding: 0.15rem 0.35rem; border-radius: 0.25rem; }
  .line:hover { background: rgba(148, 163, 184, 0.08); }
  .ts { color: #64748b; }
  .lvl { font-weight: 600; }
  .line.error .lvl, .line.error .msg { color: #ef4444; }
  .line.warn .lvl { color: #f59e0b; }
  .line.debug .lvl { color: #64748b; }
  .line.info .lvl { color: #38bdf8; }
  .src { text-transform: uppercase; font-size: 0.7rem; align-self: center; }
  .src.frontend { color: #a855f7; }
  .src.backend { color: #10b981; }
  .cat { color: #64748b; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .msg { color: #cbd5e1; white-space: pre-wrap; word-break: break-word; }
</style>
