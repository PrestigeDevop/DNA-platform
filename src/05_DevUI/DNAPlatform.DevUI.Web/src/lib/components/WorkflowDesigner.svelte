<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { api } from '$lib/services/api';

  export let workflow: any;

  const dispatch = createEventDispatcher();

  const NODE_W = 176;
  const NODE_H = 68;

  const TYPE_META: Record<string, { icon: string; color: string }> = {
    Input: { icon: '📥', color: '#0ea5e9' },
    Output: { icon: '📤', color: '#10b981' },
    Agent: { icon: '🤖', color: '#a855f7' },
    ProcessingSkill: { icon: '🧩', color: '#f59e0b' },
    Conditional: { icon: '🔀', color: '#ec4899' },
    Loop: { icon: '🔁', color: '#6366f1' },
    SubWorkflow: { icon: '🗂️', color: '#64748b' }
  };

  let nodes: any[] = [];
  let connections: any[] = [];
  let selectedId: string | null = null;
  let pendingFrom: string | null = null;
  let nodeTypes: string[] = Object.keys(TYPE_META);
  let canvasEl: HTMLDivElement;
  let dragState: { id: string; offsetX: number; offsetY: number } | null = null;
  let configText = '';
  let saving = false;
  let executing = false;
  let message = '';
  let error = '';

  onMount(async () => {
    nodes = (workflow.nodes || []).map(normalizeNode);
    connections = (workflow.connections || []).map((c: any) => ({
      sourceNodeId: c.sourceNodeId,
      targetNodeId: c.targetNodeId,
      outputKey: c.outputKey || '',
      inputKey: c.inputKey || ''
    }));

    try {
      const res = await api.getNodeTypes();
      if (res?.nodeTypes?.length) nodeTypes = res.nodeTypes;
    } catch {
      // Backend unavailable - fall back to the built-in palette.
    }
  });

  function normalizeNode(n: any) {
    return {
      id: n.id || crypto.randomUUID(),
      name: n.name || n.type || 'Node',
      nodeType: n.nodeType || n.type || 'ProcessingSkill',
      config: n.config || {},
      x: typeof n.x === 'number' ? n.x : 40 + nodes.length * 40,
      y: typeof n.y === 'number' ? n.y : 40 + nodes.length * 40
    };
  }

  function meta(type: string) {
    return TYPE_META[type] || { icon: '⚙️', color: '#64748b' };
  }

  function addNode(type: string, x: number, y: number) {
    const node = {
      id: crypto.randomUUID(),
      name: type,
      nodeType: type,
      config: {},
      x: Math.max(0, x),
      y: Math.max(0, y)
    };
    nodes = [...nodes, node];
    select(node.id);
  }

  function select(id: string) {
    selectedId = id;
    const n = nodes.find((x) => x.id === id);
    configText = n ? JSON.stringify(n.config ?? {}, null, 2) : '';
  }

  function onPaletteDragStart(e: DragEvent, type: string) {
    e.dataTransfer?.setData('application/x-node-type', type);
    if (e.dataTransfer) e.dataTransfer.effectAllowed = 'copy';
  }

  function onCanvasDrop(e: DragEvent) {
    e.preventDefault();
    const type = e.dataTransfer?.getData('application/x-node-type');
    if (!type || !canvasEl) return;
    const rect = canvasEl.getBoundingClientRect();
    addNode(type, e.clientX - rect.left - NODE_W / 2, e.clientY - rect.top - NODE_H / 2);
  }

  function startMove(e: MouseEvent, node: any) {
    e.stopPropagation();
    if (!canvasEl) return;
    select(node.id);
    const rect = canvasEl.getBoundingClientRect();
    dragState = {
      id: node.id,
      offsetX: e.clientX - rect.left - node.x,
      offsetY: e.clientY - rect.top - node.y
    };
    window.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', endMove);
  }

  function onMove(e: MouseEvent) {
    if (!dragState || !canvasEl) return;
    const rect = canvasEl.getBoundingClientRect();
    const { id, offsetX, offsetY } = dragState;
    nodes = nodes.map((n) =>
      n.id === id
        ? {
            ...n,
            x: Math.max(0, e.clientX - rect.left - offsetX),
            y: Math.max(0, e.clientY - rect.top - offsetY)
          }
        : n
    );
  }

  function endMove() {
    dragState = null;
    window.removeEventListener('mousemove', onMove);
    window.removeEventListener('mouseup', endMove);
  }

  function onOutputClick(e: MouseEvent, node: any) {
    e.stopPropagation();
    pendingFrom = node.id;
    message = 'Select an input port on another node to connect.';
    error = '';
  }

  function onInputClick(e: MouseEvent, node: any) {
    e.stopPropagation();
    if (!pendingFrom) return;
    if (pendingFrom === node.id) {
      pendingFrom = null;
      message = '';
      return;
    }
    const exists = connections.some(
      (c) => c.sourceNodeId === pendingFrom && c.targetNodeId === node.id
    );
    if (!exists) {
      connections = [
        ...connections,
        { sourceNodeId: pendingFrom, targetNodeId: node.id, outputKey: '', inputKey: '' }
      ];
    }
    pendingFrom = null;
    message = '';
  }

  function removeConnection(id: string) {
    connections = connections.filter((c) => `${c.sourceNodeId}>${c.targetNodeId}` !== id);
  }

  function deleteSelected() {
    if (!selectedId) return;
    nodes = nodes.filter((n) => n.id !== selectedId);
    connections = connections.filter(
      (c) => c.sourceNodeId !== selectedId && c.targetNodeId !== selectedId
    );
    selectedId = null;
    configText = '';
  }

  function applyConfig() {
    if (!selectedId) return;
    try {
      const parsed = configText.trim() ? JSON.parse(configText) : {};
      nodes = nodes.map((n) => (n.id === selectedId ? { ...n, config: parsed } : n));
      error = '';
    } catch {
      error = 'Config must be valid JSON.';
    }
  }

  function updateSelected(patch: Record<string, any>) {
    if (!selectedId) return;
    nodes = nodes.map((n) => (n.id === selectedId ? { ...n, ...patch } : n));
  }

  async function persist() {
    await api.updateWorkflow(workflow.id, {
      name: workflow.name,
      description: workflow.description,
      nodes,
      connections
    });
  }

  async function save() {
    saving = true;
    error = '';
    message = '';
    try {
      await persist();
      message = 'Workflow saved.';
      dispatch('saved');
    } catch (e: any) {
      error = e?.message || 'Failed to save workflow.';
    } finally {
      saving = false;
    }
  }

  async function run() {
    executing = true;
    error = '';
    message = '';
    try {
      await persist();
      const result = await api.executeWorkflowById(workflow.id);
      message = `Executed: ${result?.status || 'Completed'}`;
      dispatch('saved');
    } catch (e: any) {
      error = e?.message || 'Failed to execute workflow.';
    } finally {
      executing = false;
    }
  }

  $: edges = connections
    .map((c) => {
      const s = nodes.find((n) => n.id === c.sourceNodeId);
      const t = nodes.find((n) => n.id === c.targetNodeId);
      if (!s || !t) return null;
      const x1 = s.x + NODE_W;
      const y1 = s.y + NODE_H / 2;
      const x2 = t.x;
      const y2 = t.y + NODE_H / 2;
      const dx = Math.max(40, Math.abs(x2 - x1) * 0.5);
      return {
        id: `${c.sourceNodeId}>${c.targetNodeId}`,
        d: `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`
      };
    })
    .filter(Boolean) as { id: string; d: string }[];

  $: selected = nodes.find((n) => n.id === selectedId) || null;
</script>

<div class="designer">
  <header class="designer-header">
    <div class="title">
      <h2>🧬 {workflow.name}</h2>
      <span class="subtitle">{nodes.length} nodes · {connections.length} connections</span>
    </div>
    <div class="header-actions">
      {#if message}<span class="msg ok">{message}</span>{/if}
      {#if error}<span class="msg err">{error}</span>{/if}
      <button class="btn" on:click={save} disabled={saving}>{saving ? 'Saving…' : '💾 Save'}</button>
      <button class="btn primary" on:click={run} disabled={executing}>{executing ? 'Running…' : '▶ Execute'}</button>
      <button class="btn ghost" on:click={() => dispatch('close')}>✕ Close</button>
    </div>
  </header>

  <div class="designer-body">
    <aside class="palette">
      <h3>Nodes</h3>
      {#each nodeTypes as type}
        <div
          class="palette-item"
          draggable="true"
          role="button"
          tabindex="0"
          on:dragstart={(e) => onPaletteDragStart(e, type)}
          on:click={() => addNode(type, 60, 60)}>
          <span class="icon">{meta(type).icon}</span>
          <span>{type}</span>
        </div>
      {/each}
      <p class="hint">Drag onto the canvas or click to add.</p>
    </aside>

    <div
      class="canvas"
      bind:this={canvasEl}
      on:dragover|preventDefault
      on:drop={onCanvasDrop}
      on:click={() => (pendingFrom = null)}
      role="application"
      aria-label="Workflow canvas">
      <svg class="edges">
        {#each edges as edge (edge.id)}
          <path class="edge" d={edge.d} on:click={() => removeConnection(edge.id)} role="button" tabindex="0" on:keydown={(e) => e.key === 'Enter' && removeConnection(edge.id)} />
        {/each}
      </svg>

      {#each nodes as node (node.id)}
        <div
          class="node"
          class:selected={selectedId === node.id}
          class:pending={pendingFrom === node.id}
          style="left:{node.x}px; top:{node.y}px; width:{NODE_W}px; height:{NODE_H}px; border-color:{meta(node.nodeType).color}"
          on:mousedown={(e) => startMove(e, node)}
          role="button"
          tabindex="0"
          on:keydown={(e) => e.key === 'Enter' && select(node.id)}>
          <span class="port in" on:click={(e) => onInputClick(e, node)} on:mousedown|stopPropagation role="button" tabindex="0" aria-label="Input port"></span>
          <span class="node-icon">{meta(node.nodeType).icon}</span>
          <div class="node-text">
            <span class="node-name">{node.name}</span>
            <span class="node-type">{node.nodeType}</span>
          </div>
          <span class="port out" on:click={(e) => onOutputClick(e, node)} on:mousedown|stopPropagation role="button" tabindex="0" aria-label="Output port"></span>
        </div>
      {/each}

      {#if nodes.length === 0}
        <div class="canvas-empty">Drag a node from the palette to begin.</div>
      {/if}
    </div>

    <aside class="inspector">
      <h3>Properties</h3>
      {#if selected}
        <label>Name
          <input value={selected.name} on:input={(e) => updateSelected({ name: e.currentTarget.value })} />
        </label>
        <label>Type
          <select value={selected.nodeType} on:change={(e) => updateSelected({ nodeType: e.currentTarget.value })}>
            {#each nodeTypes as type}<option value={type}>{type}</option>{/each}
          </select>
        </label>
        <label>Config (JSON)
          <textarea bind:value={configText} on:blur={applyConfig} spellcheck="false"></textarea>
        </label>
        <button class="btn danger" on:click={deleteSelected}>🗑 Delete node</button>
      {:else}
        <p class="hint">Select a node to edit its properties.</p>
      {/if}
    </aside>
  </div>
</div>

<style>
  .designer { position: fixed; inset: 0; background: var(--bg-dark, #0f172a); display: flex; flex-direction: column; z-index: 200; }
  .designer-header { display: flex; justify-content: space-between; align-items: center; padding: 0.75rem 1.25rem; border-bottom: 1px solid var(--border, #1e293b); gap: 1rem; flex-wrap: wrap; }
  .designer-header h2 { margin: 0; font-size: 1.1rem; color: #f1f5f9; }
  .subtitle { color: #64748b; font-size: 0.8rem; }
  .header-actions { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
  .msg { font-size: 0.8rem; }
  .msg.ok { color: #10b981; }
  .msg.err { color: #ef4444; }
  .btn { background: var(--bg-card, #1e293b); color: #e2e8f0; border: 1px solid var(--border, #334155); padding: 0.4rem 0.8rem; border-radius: 0.375rem; cursor: pointer; font-size: 0.85rem; }
  .btn:hover { opacity: 0.9; }
  .btn.primary { background: #0ea5e9; border-color: #0ea5e9; color: white; }
  .btn.danger { background: rgba(239, 68, 68, 0.15); border-color: rgba(239, 68, 68, 0.4); color: #ef4444; width: 100%; }
  .btn.ghost { background: transparent; }
  .btn:disabled { opacity: 0.5; cursor: default; }
  .designer-body { flex: 1; display: grid; grid-template-columns: 180px 1fr 240px; min-height: 0; }
  .palette, .inspector { padding: 0.75rem; border-right: 1px solid var(--border, #1e293b); overflow-y: auto; }
  .inspector { border-right: none; border-left: 1px solid var(--border, #1e293b); }
  .palette h3, .inspector h3 { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; color: #64748b; margin: 0 0 0.75rem 0; }
  .palette-item { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem; margin-bottom: 0.4rem; background: var(--bg-card, #1e293b); border: 1px solid var(--border, #334155); border-radius: 0.375rem; cursor: grab; font-size: 0.85rem; color: #e2e8f0; }
  .palette-item:hover { border-color: #0ea5e9; }
  .icon { font-size: 1rem; }
  .hint { color: #64748b; font-size: 0.75rem; margin-top: 0.75rem; }
  .canvas { position: relative; overflow: auto; background-color: #0b1220; background-image: radial-gradient(rgba(148, 163, 184, 0.15) 1px, transparent 1px); background-size: 20px 20px; min-height: 0; }
  .edges { position: absolute; inset: 0; width: 100%; height: 100%; pointer-events: none; overflow: visible; }
  .edge { fill: none; stroke: #38bdf8; stroke-width: 2; pointer-events: stroke; cursor: pointer; }
  .edge:hover { stroke: #ef4444; stroke-dasharray: 5 4; }
  .node { position: absolute; display: flex; align-items: center; gap: 0.5rem; background: var(--bg-card, #1e293b); border: 2px solid #64748b; border-radius: 0.5rem; padding: 0 0.6rem; cursor: move; box-sizing: border-box; user-select: none; }
  .node.selected { box-shadow: 0 0 0 2px #0ea5e9; }
  .node.pending { box-shadow: 0 0 0 2px #f59e0b; }
  .node-icon { font-size: 1.1rem; }
  .node-text { display: flex; flex-direction: column; overflow: hidden; }
  .node-name { color: #e2e8f0; font-size: 0.82rem; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .node-type { color: #64748b; font-size: 0.7rem; }
  .port { position: absolute; width: 12px; height: 12px; border-radius: 50%; background: #0ea5e9; border: 2px solid #0b1220; cursor: crosshair; }
  .port.in { left: -7px; top: calc(50% - 6px); }
  .port.out { right: -7px; top: calc(50% - 6px); background: #10b981; }
  .canvas-empty { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; color: #475569; font-size: 0.9rem; }
  .inspector label { display: block; color: #94a3b8; font-size: 0.78rem; margin-bottom: 0.75rem; }
  .inspector input, .inspector select, .inspector textarea { width: 100%; margin-top: 0.3rem; background: #0b1220; border: 1px solid var(--border, #334155); border-radius: 0.375rem; padding: 0.4rem; color: #e2e8f0; font-size: 0.82rem; box-sizing: border-box; }
  .inspector textarea { min-height: 120px; font-family: ui-monospace, monospace; resize: vertical; }
</style>
