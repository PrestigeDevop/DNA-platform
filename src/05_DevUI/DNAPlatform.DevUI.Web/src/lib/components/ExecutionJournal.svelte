<script lang="ts">
  /**
   * Execution journal (Elsa-style, docs/refactor.md section 10 / reference UX).
   *
   * Renders what the engine actually reported for one run: per-node status,
   * timestamps and the real IO values. The data comes from
   * `Execution.nodeResults`, written by the execute route from
   * `WorkflowExecution.NodeExecutions` — nothing here is reconstructed client
   * side. It is persisted, so the journal survives a refresh.
   */
  import { onMount } from 'svelte';
  import { api } from '$lib/services/api';

  interface NodeResult {
    nodeId: string;
    status: string;
    startedAt: string | null;
    completedAt: string | null;
    inputs: Record<string, unknown> | null;
    outputs: Record<string, unknown> | null;
    error: { message?: string; errorCode?: string } | null;
  }

  interface Execution {
    id: string;
    workflowId: string | null;
    workflowName: string | null;
    status: string;
    startTime: string;
    endTime: string | null;
    duration: number;
    outputs: Record<string, unknown> | null;
    errors: unknown[] | null;
    nodeResults: NodeResult[];
    version: number | null;
  }

  const STATUS_META: Record<string, { icon: string; color: string }> = {
    Pending: { icon: '◷', color: '#94a3b8' },
    Waiting: { icon: '⏳', color: '#a78bfa' },
    Running: { icon: '▶', color: '#38bdf8' },
    Completed: { icon: '✓', color: '#4ade80' },
    Failed: { icon: '✕', color: '#f87171' },
    Skipped: { icon: '⊘', color: '#64748b' }
  };

  let executions: Execution[] = [];
  let loading = true;
  let error = '';
  let selectedId: string | null = null;
  let expanded = new Set<string>();

  async function load() {
    loading = true;
    error = '';
    try {
      executions = (await api.getExecutions()) as Execution[];
    } catch (e: any) {
      error = e?.message ?? 'Failed to load executions.';
      executions = [];
    } finally {
      loading = false;
    }
  }

  function statusMeta(status?: string) {
    return STATUS_META[status ?? 'Pending'] ?? STATUS_META.Pending;
  }

  function toggleNode(runId: string, nodeId: string) {
    const key = `${runId}:${nodeId}`;
    const next = new Set(expanded);
    next.has(key) ? next.delete(key) : next.add(key);
    expanded = next;
  }

  function durationMs(r: NodeResult): string {
    if (!r.startedAt || !r.completedAt) return '—';
    const ms = new Date(r.completedAt).getTime() - new Date(r.startedAt).getTime();
    return isNaN(ms) ? '—' : `${ms} ms`;
  }

  function clock(ts: string | null): string {
    if (!ts) return '—';
    const d = new Date(ts);
    return isNaN(d.getTime()) ? ts : d.toLocaleTimeString();
  }

  /** The engine's ExecutionError serializes with a PascalCase `Message`. */
  function errorText(err: unknown): string {
    if (typeof err === 'string') return err;
    if (err && typeof err === 'object' && 'Message' in err) {
      return String((err as { Message: unknown }).Message);
    }
    return JSON.stringify(err);
  }

  function preview(value: unknown): string {
    if (value === null || value === undefined) return '—';
    const text = typeof value === 'string' ? value : JSON.stringify(value);
    return text.length > 90 ? `${text.slice(0, 90)}…` : text;
  }

  // Ordered by startTime so the journal reads chronologically; the API already
  // returns createdAt desc, but a run's own startTime is the honest key here.
  $: runsWithJournal = executions
    .filter((e) => (e.nodeResults?.length ?? 0) > 0)
    .sort((a, b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime());
  $: selected = executions.find((e) => e.id === selectedId) ?? null;

  onMount(load);
</script>

<div class="journal">
  <header>
    <h2>🧾 Execution journal</h2>
    <button class="btn" on:click={load} disabled={loading}>🔄 Refresh</button>
  </header>

  {#if error}
    <p class="error">{error}</p>
  {:else if loading}
    <p class="hint">Loading executions…</p>
  {:else if runsWithJournal.length === 0}
    <p class="hint">
      No runs with per-node state yet. Execute a workflow — the journal records what
      each node actually did, so it survives a refresh.
    </p>
  {:else}
    <ul class="runs">
      {#each runsWithJournal as run (run.id)}
        {@const st = statusMeta(run.status)}
        <li>
          <button
            class="run-head"
            class:open={selectedId === run.id}
            on:click={() => (selectedId = selectedId === run.id ? null : run.id)}
          >
            <span class="status" style="color:{st.color}">
              <span aria-hidden="true">{st.icon}</span> {run.status}
            </span>
            <span class="run-name">{run.workflowName ?? run.workflowId ?? run.id}</span>
            <span class="run-meta">
              {clock(run.startTime)} · {run.duration ? `${Math.round(run.duration)} ms` : '—'}
              {#if run.version} · v{run.version}{/if}
            </span>
          </button>

          {#if selectedId === run.id}
            <ol class="nodes">
              {#each run.nodeResults as node (node.nodeId)}
                {@const nst = statusMeta(node.status)}
                <li class="node">
                  <button
                    class="node-head"
                    on:click={() => toggleNode(run.id, node.nodeId)}
                    aria-expanded={expanded.has(`${run.id}:${node.nodeId}`)}
                  >
                    <span class="node-status" style="color:{nst.color}">
                      <span aria-hidden="true">{nst.icon}</span> {node.status}
                    </span>
                    <span class="node-id">{node.nodeId}</span>
                    <span class="node-time">{durationMs(node)}</span>
                    <span class="chevron" aria-hidden="true">
                      {expanded.has(`${run.id}:${node.nodeId}`) ? '▾' : '▸'}
                    </span>
                  </button>

                  {#if expanded.has(`${run.id}:${node.nodeId}`)}
                    <div class="node-detail">
                      <dl>
                        <dt>Started</dt><dd>{clock(node.startedAt)}</dd>
                        <dt>Finished</dt><dd>{clock(node.completedAt)}</dd>
                      </dl>

                      {#if node.error}
                        <p class="node-error">
                          {node.error.errorCode ? `${node.error.errorCode}: ` : ''}
                          {node.error.message ?? 'Unknown error'}
                        </p>
                      {/if}

                      <div class="io">
                        <div>
                          <h4>Inputs received</h4>
                          {#if node.inputs && Object.keys(node.inputs).length > 0}
                            <ul class="kv">
                              {#each Object.entries(node.inputs) as [key, value]}
                                <li><code>{key}</code><span>{preview(value)}</span></li>
                              {/each}
                            </ul>
                          {:else}
                            <p class="empty-io">No inputs.</p>
                          {/if}
                        </div>
                        <div>
                          <h4>Outputs produced</h4>
                          {#if node.outputs && Object.keys(node.outputs).length > 0}
                            <ul class="kv">
                              {#each Object.entries(node.outputs) as [key, value]}
                                <li><code>{key}</code><span>{preview(value)}</span></li>
                              {/each}
                            </ul>
                          {:else}
                            <p class="empty-io">No outputs.</p>
                          {/if}
                        </div>
                      </div>
                    </div>
                  {/if}
                </li>
              {/each}
            </ol>

            {#if run.errors && run.errors.length > 0}
              <div class="run-errors">
                <h4>Workflow errors</h4>
                <ul>
                  {#each run.errors as err}
                    <li>{errorText(err)}</li>
                  {/each}
                </ul>
              </div>
            {/if}
          {/if}
        </li>
      {/each}
    </ul>
  {/if}
</div>

<style>
  .journal { margin-top: 1.5rem; border-top: 1px solid var(--border); padding-top: 1rem; }
  header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem; }
  h2 { font-size: 1rem; color: #e2e8f0; margin: 0; }
  h4 { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.04em; color: #64748b; margin: 0 0 0.3rem 0; }
  .btn {
    background: var(--bg-card); color: #e2e8f0; border: 1px solid var(--border);
    padding: 0.35rem 0.7rem; border-radius: 0.375rem; cursor: pointer; font-size: 0.8rem;
  }
  .btn:disabled { opacity: 0.55; cursor: default; }
  .hint { color: #64748b; font-size: 0.85rem; margin: 0; }
  .error { color: #f87171; font-size: 0.85rem; margin: 0; }

  ul.runs, ol.nodes { list-style: none; margin: 0; padding: 0; }
  ul.runs > li { border: 1px solid var(--border); border-radius: 0.375rem; margin-bottom: 0.5rem; overflow: hidden; }

  .run-head, .node-head {
    width: 100%; display: flex; align-items: center; gap: 0.75rem;
    background: var(--bg-card); border: none; color: #cbd5e1;
    padding: 0.5rem 0.7rem; cursor: pointer; font-size: 0.82rem; text-align: left;
  }
  .run-head.open, .node-head:hover { background: rgba(14, 165, 233, 0.08); }
  .status, .node-status { font-weight: 600; display: inline-flex; align-items: center; gap: 0.3rem; min-width: 110px; }
  .run-name { color: #e2e8f0; }
  .run-meta { margin-left: auto; color: #64748b; font-size: 0.75rem; }

  ol.nodes { border-top: 1px solid var(--border); }
  .node { border-bottom: 1px solid rgba(148, 163, 184, 0.08); }
  .node-id { color: #e2e8f0; font-family: ui-monospace, monospace; }
  .node-time { margin-left: auto; color: #64748b; font-size: 0.75rem; }
  .chevron { color: #64748b; }

  .node-detail { padding: 0.5rem 0.7rem 0.75rem 0.7rem; background: rgba(0, 0, 0, 0.2); }
  .node-detail dl { display: grid; grid-template-columns: 90px 1fr; gap: 0.15rem 0.5rem; margin: 0 0 0.5rem 0; font-size: 0.75rem; }
  .node-detail dt { color: #64748b; }
  .node-detail dd { margin: 0; color: #cbd5e1; }
  .node-error { color: #f87171; font-size: 0.78rem; margin: 0 0 0.5rem 0; }

  .io { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
  @media (max-width: 720px) { .io { grid-template-columns: 1fr; } }
  ul.kv { list-style: none; margin: 0; padding: 0; }
  ul.kv li { display: flex; gap: 0.4rem; font-size: 0.75rem; padding: 0.1rem 0; }
  ul.kv code { color: #38bdf8; flex-shrink: 0; }
  ul.kv span { color: #94a3b8; word-break: break-word; }
  .empty-io { color: #475569; font-size: 0.75rem; margin: 0; }

  .run-errors { padding: 0.5rem 0.7rem 0.75rem; background: rgba(239, 68, 68, 0.06); }
  .run-errors ul { margin: 0; padding-left: 1rem; }
  .run-errors li { color: #f87171; font-size: 0.78rem; }
</style>