<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import {
    logRecords,
    logStreaming,
    logStreamPaused,
    logStreamError,
    consoleCaptureEnabled,
    captureConsole,
    startLogStream,
    stopLogStream,
    togglePauseStream,
    clearLogs
  } from '$lib/services/logs';
  import ExecutionJournal from '$lib/components/ExecutionJournal.svelte';

  let filterSource: 'all' | 'frontend' | 'backend' = 'all';
  let filterLevel = 'all';
  let searchQuery = '';
  let autoScroll = true;
  let consoleEl: HTMLDivElement;
  let scrollScheduled = false;

  const levels = ['all', 'Debug', 'Information', 'Warning', 'Error'];

  onMount(() => {
    captureConsole();
    startLogStream();
  });

  onDestroy(() => {
    stopLogStream();
  });

  $: records = $logRecords;

  $: filtered = records.filter((r) => {
    if (filterSource !== 'all' && r.source !== filterSource) return false;
    if (filterLevel !== 'all' && r.level !== filterLevel) return false;
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      return (
        r.message.toLowerCase().includes(q) ||
        r.category.toLowerCase().includes(q) ||
        r.level.toLowerCase().includes(q)
      );
    }
    return true;
  });

  // Limit rendering to 150 items to keep DOM lightweight and completely responsive
  $: visible = filtered.slice(-150);

  $: if (autoScroll && consoleEl && visible.length && !scrollScheduled && !$logStreamPaused) {
    scrollScheduled = true;
    requestAnimationFrame(() => {
      if (consoleEl) {
        consoleEl.scrollTop = consoleEl.scrollHeight;
      }
      scrollScheduled = false;
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
      <h1>Logs & System Console</h1>
      <p class="page-description">
        Real-time non-blocking log stream from backend execution, ONNX runtime, and MCP servers.
      </p>
    </div>
    <div class="header-actions">
      <button
        class="btn pause-btn"
        class:paused={$logStreamPaused}
        on:click={togglePauseStream}
        title={$logStreamPaused ? 'Resume live stream' : 'Pause live stream'}
      >
        {$logStreamPaused ? '▶ Resume Stream' : '⏸ Pause Stream'}
      </button>

      <span class="status" class:live={$logStreaming && !$logStreamPaused}>
        {#if $logStreamPaused}
          ⏸ paused
        {:else if $logStreaming}
          ● live stream
        {:else}
          ○ offline/polling
        {/if}
      </span>

      <button class="btn" on:click={() => clearLogs()}>🗑 Clear Buffer</button>
    </div>
  </div>

  {#if $logStreamError}
    <div class="error-card">Notice: {$logStreamError}</div>
  {/if}

  <div class="filters">
    <label>
      Source
      <select bind:value={filterSource}>
        <option value="all">All Sources</option>
        <option value="frontend">Frontend</option>
        <option value="backend">Backend</option>
      </select>
    </label>

    <label>
      Level
      <select bind:value={filterLevel}>
        {#each levels as lvl}
          <option value={lvl}>{lvl === 'all' ? 'All Levels' : lvl}</option>
        {/each}
      </select>
    </label>

    <label class="search-label">
      Search
      <input
        type="text"
        bind:value={searchQuery}
        placeholder="Filter by keyword..."
        class="search-input"
      />
    </label>

    <label class="checkbox">
      <input type="checkbox" bind:checked={autoScroll} /> Auto-scroll
    </label>

    <label class="checkbox" title="Capture browser console messages (console.log/error)">
      <input type="checkbox" bind:checked={$consoleCaptureEnabled} /> Browser Console
    </label>

    <span class="count">Showing {visible.length} of {records.length} records</span>
  </div>

  <div class="console" bind:this={consoleEl}>
    {#if visible.length === 0}
      <div class="empty">
        No matching log records. Actions across workflows, agents, ONNX runtime, and skills will appear here live.
      </div>
    {:else}
      {#each visible as record (record.id)}
        <div class="line {levelClass(record.level)}">
          <span class="ts">{time(record.timestamp)}</span>
          <span class="lvl">{record.level}</span>
          <span class="src {record.source}">{record.source}</span>
          <span class="cat" title={record.category}>{record.category}</span>
          <span class="msg">{record.message}</span>
        </div>
      {/each}
    {/if}
  </div>

  <ExecutionJournal />
</div>

<style>
  .logs-page {
    max-width: 1200px;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .console {
    height: 380px;
    overflow-y: auto;
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 0.5rem;
    padding: 0.6rem;
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 0.8rem;
    line-height: 1.5;
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
  }

  .page-header h1 {
    font-size: 1.75rem;
    font-weight: 700;
    color: #f1f5f9;
    margin: 0;
  }

  .page-description {
    color: #94a3b8;
    margin: 0.25rem 0 0.5rem 0;
    font-size: 0.9rem;
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .status {
    color: #64748b;
    font-size: 0.85rem;
    font-weight: 500;
  }

  .status.live {
    color: #10b981;
  }

  .btn {
    background: rgba(255, 255, 255, 0.05);
    color: #e2e8f0;
    border: 1px solid rgba(255, 255, 255, 0.15);
    padding: 0.4rem 0.8rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.82rem;
    transition: background 0.15s;
  }

  .btn:hover {
    background: rgba(255, 255, 255, 0.1);
  }

  .pause-btn.paused {
    background: rgba(245, 158, 11, 0.2);
    border-color: #f59e0b;
    color: #fbbf24;
  }

  .error-card {
    background: rgba(239, 68, 68, 0.1);
    border: 1px solid rgba(239, 68, 68, 0.3);
    border-radius: 0.5rem;
    padding: 0.5rem 0.8rem;
    color: #f87171;
    font-size: 0.82rem;
  }

  .filters {
    display: flex;
    align-items: center;
    gap: 1rem;
    flex-wrap: wrap;
    background: rgba(255, 255, 255, 0.02);
    padding: 0.5rem 0.75rem;
    border-radius: 0.375rem;
    border: 1px solid rgba(255, 255, 255, 0.05);
  }

  .filters label {
    color: #94a3b8;
    font-size: 0.8rem;
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .filters select,
  .search-input {
    background: #0f172a;
    color: #e2e8f0;
    border: 1px solid rgba(255, 255, 255, 0.15);
    border-radius: 0.375rem;
    padding: 0.25rem 0.5rem;
    font-size: 0.8rem;
  }

  .search-input {
    width: 140px;
  }

  .checkbox {
    cursor: pointer;
    user-select: none;
  }

  .count {
    margin-left: auto;
    color: #64748b;
    font-size: 0.78rem;
  }

  .empty {
    color: #64748b;
    padding: 2rem;
    text-align: center;
    font-style: italic;
  }

  .line {
    display: grid;
    grid-template-columns: 80px 85px 75px 120px 1fr;
    gap: 0.5rem;
    padding: 0.15rem 0.4rem;
    border-radius: 0.25rem;
  }

  .line:hover {
    background: rgba(255, 255, 255, 0.04);
  }

  .ts {
    color: #64748b;
    font-size: 0.75rem;
  }

  .lvl {
    font-weight: 600;
    font-size: 0.75rem;
  }

  .line.error .lvl,
  .line.error .msg {
    color: #ef4444;
  }

  .line.warn .lvl {
    color: #f59e0b;
  }

  .line.debug .lvl {
    color: #64748b;
  }

  .line.info .lvl {
    color: #38bdf8;
  }

  .src {
    text-transform: uppercase;
    font-size: 0.7rem;
    font-weight: 600;
  }

  .src.frontend {
    color: #c084fc;
  }

  .src.backend {
    color: #34d399;
  }

  .cat {
    color: #94a3b8;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 0.75rem;
  }

  .msg {
    color: #e2e8f0;
    white-space: pre-wrap;
    word-break: break-word;
  }
</style>
