<script lang="ts">
  import { createEventDispatcher, onMount, onDestroy } from 'svelte';
  import { api } from '$lib/services/api';

  export let workflow: any;
  export let execution: any = null;

  const dispatch = createEventDispatcher();

  const NODE_W = 176;
  const NODE_H = 68;
  const PORT_SIZE = 10;
  const CANVAS_PADDING = 200;

  type NodeMeta = { icon: string; color: string; label: string };
  const TYPE_META: Record<string, NodeMeta> = {
    Input: { icon: '??', color: '#0ea5e9', label: 'Input' },
    Value: { icon: '??', color: '#6366f1', label: 'Value' },
    Output: { icon: '??', color: '#10b981', label: 'Output' },
    Agent: { icon: '??', color: '#a855f7', label: 'Agent' },
    ProcessingSkill: { icon: '??', color: '#f59e0b', label: 'Processing Skill' },
    Log: { icon: '??', color: '#ef4444', label: 'Log' },
    Conditional: { icon: '??', color: '#ec4899', label: 'Conditional' },
    Loop: { icon: '??', color: '#6366f1', label: 'Loop' },
    SubWorkflow: { icon: '??', color: '#64748b', label: 'Sub-Workflow' }
  };

  let nodes: any[] = [];
  let connections: any[] = [];
  let selectedId: string | null = null;
  let selectedEdgeId: string | null = null;
  let pendingFrom: string | null = null;
  let nodeTypes: string[] = Object.keys(TYPE_META);
  let fileInput: HTMLInputElement;
  let canvasEl: HTMLDivElement;
  let dragState: { id: string; offsetX: number; offsetY: number } | null = null;
  let canvasScroll = { left: 0, top: 0 };
  let saving = false;
  let executing = false;
  let message = '';
  let error = '';

  function parseJsonField(field: any): any {
    if (field == null) return field;
    if (typeof field === 'string') {
      try { return JSON.parse(field); } catch { return field; }
    }
    return field;
  }

  function loadCustomNodeTypes() {
    try {
      const raw = localStorage.getItem('customNodeTypes');
      if (!raw) return;
      const parsed = JSON.parse(raw);
      if (!parsed || !parsed.nodeTypes || typeof parsed.nodeTypes !== 'object') return;
      for (const [name, meta] of Object.entries(parsed.nodeTypes as Record<string, any>)) {
        if (typeof name !== 'string' || !meta) continue;
        if (!TYPE_META[name]) {
          TYPE_META[name] = {
            icon: String(meta.icon || '??'),
            color: String(meta.color || '#64748b'),
            label: String(meta.label || name)
          };
        } else {
          if (meta.icon != null) TYPE_META[name].icon = String(meta.icon);
          if (meta.color != null) TYPE_META[name].color = String(meta.color);
          if (meta.label != null) TYPE_META[name].label = String(meta.label);
        }
      }
      nodeTypes = Object.keys(TYPE_META);
    } catch {
      // ignore
    }
  }

  function exportPalette() {
    const payload = { version: 1, nodeTypes: TYPE_META };
    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'palette.json';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  function importPalette() {
    const input = fileInput;
    if (!input || !input.files || input.files.length === 0) return;
    const file = input.files[0];
    const reader = new FileReader();
    reader.onload = () => {
      try {
        const parsed = JSON.parse(String(reader.result));
        if (!parsed || !parsed.nodeTypes || typeof parsed.nodeTypes !== 'object') {
          error = 'Invalid palette file: expected { version, nodeTypes }.';
          message = '';
          return;
        }
        for (const [name, meta] of Object.entries(parsed.nodeTypes as Record<string, any>)) {
          if (typeof name !== 'string' || !meta) continue;
          TYPE_META[name] = {
            icon: String(meta.icon || '??'),
            color: String(meta.color || '#64748b'),
            label: String(meta.label || name)
          };
        }
        nodeTypes = Object.keys(TYPE_META);
        try {
          localStorage.setItem('customNodeTypes', JSON.stringify({ version: 1, nodeTypes: TYPE_META }));
        } catch { /* ignore */ }
        error = '';
        message = 'Palette imported.';
      } catch (e: any) {
        error = 'Failed to parse palette file: ' + (e?.message || 'unknown error');
        message = '';
      }
    };
    reader.readAsText(file);
    input.value = '';
  }

  onMount(() => {
    loadCustomNodeTypes();
    const wf = workflow || {};
    nodes = parseJsonField(wf.nodes) || [];
    connections = parseJsonField(wf.connections) || [];
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
    window.addEventListener('keydown', onKeyDown);
  });

  onDestroy(() => {
    window.removeEventListener('mousemove', onMouseMove);
    window.removeEventListener('mouseup', onMouseUp);
    window.removeEventListener('keydown', onKeyDown);
  });

  function getMeta(type: string): NodeMeta {
    return TYPE_META[type] || { icon: '??', color: '#64748b', label: type };
  }

  function addNode(type: string) {
    const id = 'n' + Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
    const x = CANVAS_PADDING + Math.floor(Math.random() * 200);
    const y = CANVAS_PADDING + Math.floor(Math.random() * 150);
    const meta = getMeta(type);
    nodes = [...nodes, {
      id,
      type,
      name: meta.label,
      x,
      y,
      config: {}
    }];
    selectedId = id;
    selectedEdgeId = null;
  }

  function onPaletteDragStart(e: DragEvent, type: string) {
    e.dataTransfer?.setData('text/plain', type);
  }

  function onCanvasDrop(e: DragEvent) {
    e.preventDefault();
    const type = e.dataTransfer?.getData('text/plain');
    if (!type || !TYPE_META[type]) return;
    const rect = canvasEl.getBoundingClientRect();
    const x = e.clientX - rect.left + canvasEl.scrollLeft;
    const y = e.clientY - rect.top + canvasEl.scrollTop;
    const id = 'n' + Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
    const meta = getMeta(type);
    nodes = [...nodes, { id, type, name: meta.label, x, y, config: {} }];
    selectedId = id;
    selectedEdgeId = null;
    pendingFrom = null;
  }

  function onCanvasDragOver(e: DragEvent) {
    e.preventDefault();
  }

  function onCanvasScroll(e: any) {
    canvasScroll = { left: canvasEl.scrollLeft, top: canvasEl.scrollTop };
  }

  function onNodeMouseDown(e: MouseEvent, node: any) {
    if ((e.target as HTMLElement).closest('.port')) return;
    if ((e.target as HTMLElement).closest('.node-header')) {
      e.preventDefault();
      const rect = canvasEl.getBoundingClientRect();
      dragState = {
        id: node.id,
        offsetX: (e.clientX - rect.left + canvasEl.scrollLeft) - node.x,
        offsetY: (e.clientY - rect.top + canvasEl.scrollTop) - node.y
      };
      selectedId = node.id;
      selectedEdgeId = null;
      pendingFrom = null;
    } else {
      selectedId = node.id;
      selectedEdgeId = null;
      pendingFrom = null;
    }
  }

  function onMouseMove(e: MouseEvent) {
    if (!dragState || !canvasEl) return;
    const ds = dragState;
    const rect = canvasEl.getBoundingClientRect();
    const x = (e.clientX - rect.left + canvasEl.scrollLeft) - ds.offsetX;
    const y = (e.clientY - rect.top + canvasEl.scrollTop) - ds.offsetY;
    nodes = nodes.map(n => n.id === ds.id ? { ...n, x, y } : n);
  }

  function onMouseUp() {
    dragState = null;
  }

  function onPortClick(nodeId: string, kind: 'in' | 'out') {
    if (kind === 'out') {
      pendingFrom = nodeId + ':out';
      return;
    }
    if (kind === 'in' && pendingFrom) {
      const from = pendingFrom.split(':')[0];
      const to = nodeId;
      if (from !== to) {
        const id = 'c' + Date.now().toString(36) + Math.random().toString(36).slice(2, 4);
        connections = [...connections, { id, from, to }];
      }
      pendingFrom = null;
    }
  }

  function onCanvasClick(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (target.closest('.node') || target.closest('.connection-hit') || target.closest('.port')) return;
    selectedId = null;
    selectedEdgeId = null;
    pendingFrom = null;
  }

  function onEdgeClick(e: MouseEvent, edgeId: string) {
    e.stopPropagation();
    selectedEdgeId = edgeId;
    selectedId = null;
    pendingFrom = null;
  }

  function onEdgeRightClick(e: MouseEvent, edgeId: string) {
    e.preventDefault();
    connections = connections.filter(c => c.id !== edgeId);
    if (selectedEdgeId === edgeId) selectedEdgeId = null;
  }

  function onNodeRightClick(e: MouseEvent, nodeId: string) {
    e.preventDefault();
    nodes = nodes.filter(n => n.id !== nodeId);
    connections = connections.filter(c => c.from !== nodeId && c.to !== nodeId);
    if (selectedId === nodeId) selectedId = null;
  }

  function onKeyDown(e: KeyboardEvent) {
    if (e.key === 'Delete' || e.key === 'Backspace') {
      if (selectedEdgeId) {
        connections = connections.filter(c => c.id !== selectedEdgeId);
        selectedEdgeId = null;
      } else if (selectedId) {
        nodes = nodes.filter(n => n.id !== selectedId);
        connections = connections.filter(c => c.from !== selectedId && c.to !== selectedId);
        selectedId = null;
      }
    }
    if (e.key === 'Escape') {
      pendingFrom = null;
      selectedEdgeId = null;
    }
  }

  function getPortPos(nodeId: string, kind: 'in' | 'out') {
    const node = nodes.find(n => n.id === nodeId);
    if (!node) return { x: 0, y: 0 };
    if (kind === 'in') return { x: node.x, y: node.y + NODE_H / 2 };
    return { x: node.x + NODE_W, y: node.y + NODE_H / 2 };
  }

  function edgePath(edge: any) {
    const from = getPortPos(edge.from, 'out');
    const to = getPortPos(edge.to, 'in');
    const dx = to.x - from.x;
    const midX = from.x + dx * 0.5;
    return `M ${from.x} ${from.y} C ${midX} ${from.y}, ${midX} ${to.y}, ${to.x} ${to.y}`;
  }

  function nodeStatus(nodeId: string) {
    const res = execution?.nodeResults?.find((r: any) => r.nodeId === nodeId);
    if (!res || !res.status) return null;
    const s = String(res.status).toLowerCase();
    if (s === 'completed') return { text: 'Completed', icon: '?', color: '#10b981' };
    if (s === 'running') return { text: 'Running', icon: '?', color: '#f59e0b' };
    if (s === 'failed') return { text: 'Failed', icon: '?', color: '#ef4444' };
    if (s === 'skipped') return { text: 'Skipped', icon: '??', color: '#94a3b8' };
    return { text: res.status, icon: '•', color: '#94a3b8' };
  }

  function selectedNode() {
    return nodes.find(n => n.id === selectedId) || null;
  }
  $: currentNode = selectedNode();

  function selectedEdge() {
    return connections.find(c => c.id === selectedEdgeId) || null;
  }

  async function onSave() {
    if (!workflow?.id) return;
    saving = true;
    error = '';
    message = '';
    try {
      const wf = {
        name: workflow.name,
        description: workflow.description,
        nodes: JSON.stringify(nodes),
        connections: JSON.stringify(connections),
        status: workflow.status
      };
      await api.updateWorkflow(workflow.id, wf);
      dispatch('saved', {});
      message = 'Workflow saved.';
    } catch (e: any) {
      error = e?.message || String(e);
    } finally {
      saving = false;
    }
  }

  function parseDurationMs(val: any): number {
    if (val == null) return 0;
    if (typeof val === 'number') return Math.round(val);
    if (typeof val === 'string') {
      const s = val.trim();
      const m = s.match(/(\d+)\.(\d+)/);
      if (m) {
        const sec = parseInt(m[1] || '0');
        const frac = (m[2] || '').padEnd(3, '0').slice(0, 3);
        return sec * 1000 + parseInt(frac || '0');
      }
      const ms = parseInt(s);
      if (!isNaN(ms)) return ms;
    }
    if (typeof val === 'object') {
      if (typeof val.TotalMilliseconds === 'number') return Math.round(val.TotalMilliseconds);
      if (typeof val.totalMilliseconds === 'number') return Math.round(val.totalMilliseconds);
    }
    return 0;
  }

  async function onRun() {
    executing = true;
    error = '';
    message = '';
    try {
      const payload = {
        name: workflow?.name || 'workflow',
        nodes: JSON.parse(JSON.stringify(nodes)),
        connections: JSON.parse(JSON.stringify(connections))
      };
      const result = await api.executeWorkflow(payload);
      const full = result?.FullExecution || result?.fullExecution || result;
      const nodeExecs = full?.NodeExecutions || full?.nodeExecutions || [];
      const nodeResults = Array.isArray(nodeExecs) ? nodeExecs.map((ne: any) => ({
        nodeId: ne.NodeId || ne.nodeId,
        status: ne.Status || ne.status,
        startedAt: ne.StartedAt || ne.startedAt,
        completedAt: ne.CompletedAt || ne.completedAt,
        inputs: ne.Inputs || ne.inputs,
        outputs: ne.Outputs || ne.outputs,
        error: ne.Error || ne.error || ne.Exception || ne.exception
      })) : [];
      const durationMs = parseDurationMs(full?.Duration || full?.duration || full?.DurationMs || full?.durationMs);
      dispatch('saved', { nodeResults });
      const failed = nodeResults.filter((r: any) => String(r.status).toLowerCase() === 'failed').length;
      const status = full?.Status || full?.status || result?.status || 'Completed';
      message = `Run complete. Status: ${status}, Duration: ${durationMs}ms${failed ? `, Failed: ${failed}` : ''}`;
    } catch (e: any) {
      error = e?.message || String(e);
    } finally {
      executing = false;
    }
  }

  function onClose() {
    dispatch('close');
  }
</script>
<div class="designer" role="application" aria-label="Workflow Designer">
  <div class="designer-header">
    <div class="header-left">
      <strong>{workflow?.name || 'Untitled Workflow'}</strong>
    </div>
    <div class="header-right">
      <span class="palette-tools">
        <button type="button" on:click={exportPalette} title="Export palette">?? Export Palette</button>
        <button type="button" on:click={() => fileInput?.click()} title="Import palette">?? Import Palette</button>
        <input bind:this={fileInput} type="file" accept="application/json" style="display:none" on:change={importPalette} />
      </span>
      <button type="button" on:click={onClose}>? Close</button>
      <button type="button" on:click={onSave} disabled={saving}>?? {saving ? 'Saving...' : 'Save'}</button>
      <button type="button" class="run" on:click={onRun} disabled={executing}>? {executing ? 'Running...' : 'Run'}</button>
    </div>
  </div>
  {#if message}
    <div class="banner info">{message}</div>
  {/if}
  {#if error}
    <div class="banner error">{error}</div>
  {/if}
  <div class="designer-body">
    <aside class="palette">
      <div class="palette-title">Node Palette</div>
      {#each nodeTypes as t}
        <div class="palette-card" draggable="true" on:dragstart={(e) => onPaletteDragStart(e, t)} on:click={() => addNode(t)}>
          <span class="palette-icon" style="color:{getMeta(t).color}">{getMeta(t).icon}</span>
          <span class="palette-label">{getMeta(t).label}</span>
        </div>
      {/each}
    </aside>
    <main class="canvas-wrap">
      <div bind:this={canvasEl} class="canvas" on:drop={onCanvasDrop} on:dragover={onCanvasDragOver} on:click={onCanvasClick} on:scroll={onCanvasScroll}>
        <svg class="edges" width="100%" height="100%">
          <defs>
            <marker id="arrow" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
              <polygon points="0 0, 10 3.5, 0 7" fill="#94a3b8" />
            </marker>
          </defs>
          {#each connections as edge}
            {@const p = edgePath(edge)}
            {@const selected = selectedEdgeId === edge.id}
            <path d={p} class="edge-line" class:selected fill="none" marker-end="url(#arrow)" />
            <path d={p} class="connection-hit" on:click={(e) => onEdgeClick(e, edge.id)} on:contextmenu={(e) => onEdgeRightClick(e, edge.id)} />
          {/each}
        </svg>
        {#each (nodes || []) as node}
          {@const meta = getMeta(node.type)}
          {@const st = nodeStatus(node.id)}
          <div class="node" class:selected={selectedId === node.id} style="left:{node.x}px; top:{node.y}px; width:{NODE_W}px; height:{NODE_H}px; border-color:{meta.color}" on:mousedown={(e) => onNodeMouseDown(e, node)} on:contextmenu={(e) => onNodeRightClick(e, node.id)}>
            <div class="node-header" style="background:linear-gradient(90deg, {meta.color}33, transparent)">
              <span class="node-icon">{meta.icon}</span>
              <span class="node-name" title={node.name}>{node.name || meta.label}</span>
              <span class="node-type">{meta.label}</span>
            </div>
            {#if st}
              <div class="status-chip" style="color:{st.color}; border-color:{st.color}66; background:{st.color}22">
                <span class="status-icon">{st.icon}</span>
                <span class="status-text">{st.text}</span>
              </div>
            {/if}
            <div class="port in" title="Input" style="left:-{PORT_SIZE/2}px; top:{NODE_H/2 - PORT_SIZE/2}px" on:click|stopPropagation={() => onPortClick(node.id, 'in')}></div>
            <div class="port out" title="Output" style="left:{NODE_W - PORT_SIZE/2}px; top:{NODE_H/2 - PORT_SIZE/2}px" on:click|stopPropagation={() => onPortClick(node.id, 'out')}></div>
          </div>
        {/each}
        {#if pendingFrom}
          <div class="pending">Click target input port to connect</div>
        {/if}
      </div>
    </main>
    <aside class="inspector">
      <div class="inspector-title">Inspector</div>
      {#if currentNode}
        {@const meta = getMeta(currentNode.type)}
        <div class="section">
          <div class="field">
            <label>Name</label>
            <input type="text" bind:value={currentNode.name} />
          </div>
          <div class="field">
            <label>Type</label>
            <div class="badge-type" style="border-color:{meta.color}; color:{meta.color}; background:{meta.color}22">{meta.icon} {meta.label}</div>
          </div>
        </div>
        <div class="section">
          <div class="section-title">Config</div>
          {#if currentNode.type === 'Value'}
            <div class="field">
              <label>Value</label>
              <input type="text" bind:value={currentNode.config.value} placeholder="e.g. 42 or hello" />
            </div>
          {:else if currentNode.type === 'Log'}
            <div class="field">
              <label>Message</label>
              <input type="text" bind:value={currentNode.config.message} placeholder="Log message" />
            </div>
            <div class="field">
              <label>Level</label>
              <select bind:value={currentNode.config.level}>
                <option value="Info">Info</option>
                <option value="Warn">Warn</option>
                <option value="Error">Error</option>
                <option value="Debug">Debug</option>
              </select>
            </div>
          {:else if currentNode.type === 'ProcessingSkill'}
            <div class="field">
              <label>Skill ID</label>
              <input type="text" bind:value={currentNode.config.skillId} placeholder="e.g. dna.sequence.translate" />
            </div>
          {:else if currentNode.type === 'Agent'}
            <div class="field">
              <label>Agent ID</label>
              <input type="text" bind:value={currentNode.config.agentId} placeholder="Agent identifier" />
            </div>
          {:else if currentNode.type === 'Input'}
            <div class="field">
              <label>Key</label>
              <input type="text" bind:value={currentNode.config.key} placeholder="input key" />
            </div>
            <div class="field">
              <label>Default Value</label>
              <input type="text" bind:value={currentNode.config.defaultValue} placeholder="optional default" />
            </div>
          {:else if currentNode.type === 'Output'}
            <div class="field">
              <label>Output Key</label>
              <input type="text" bind:value={currentNode.config.outputKey} placeholder="output key" />
            </div>
          {:else}
            <div class="hint">No editable config for this node type.</div>
          {/if}
        </div>
        <div class="section">
          <button type="button" class="danger" on:click={() => { nodes = nodes.filter(n=>n.id!==selectedId); connections = connections.filter(c=>c.from!==selectedId&&c.to!==selectedId); selectedId=null; }}>Delete Node</button>
        </div>
      {:else if selectedEdge()}
        {@const edge = selectedEdge()}
        <div class="section">
          <div class="field"><label>From</label><div class="readonly">{edge.from}</div></div>
          <div class="field"><label>To</label><div class="readonly">{edge.to}</div></div>
        </div>
        <div class="section">
          <button type="button" class="danger" on:click={() => { connections = connections.filter(c=>c.id!==selectedEdgeId); selectedEdgeId=null; }}>Delete Edge</button>
        </div>
      {:else}
        <div class="hint">Select a node or edge to edit.</div>
      {/if}
    </aside>
  </div>
</div>
<style>
  :root {
    --primary: #0ea5e9;
    --bg-dark: #0f172a;
    --bg-panel: #0b1225;
    --bg-card: #0f1b36;
    --border: #1e293b;
  }
  .designer { display: flex; flex-direction: column; height: 100vh; background: var(--bg-dark); color: #e2e8f0; position: fixed; inset: 0; z-index: 200; }
  .designer-header { display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 1rem; background: var(--bg-panel); border-bottom: 1px solid var(--border); }
  .header-left strong { font-size: 1.1rem; }
  .header-right { display: flex; gap: 0.5rem; align-items: center; }
  .palette-tools { display: flex; gap: 0.25rem; padding-right: 0.5rem; border-right: 1px solid var(--border); }
  .header-right button { background: rgba(14,165,233,0.1); color: var(--primary); border: 1px solid rgba(14,165,233,0.3); padding: 0.375rem 0.75rem; border-radius: 0.25rem; cursor: pointer; font-size: 0.85rem; }
  .header-right button.run { background: rgba(16,185,129,0.15); color: #10b981; border-color: rgba(16,185,129,0.4); }
  .header-right button:disabled { opacity: 0.55; cursor: not-allowed; }
  .banner { padding: 0.4rem 1rem; font-size: 0.85rem; border-bottom: 1px solid var(--border); }
  .banner.info { background: rgba(14,165,233,0.15); color: #38bdf8; }
  .banner.error { background: rgba(239,68,68,0.15); color: #f87171; }
  .designer-body { display: flex; flex: 1; overflow: hidden; }
  .palette { width: 200px; background: var(--bg-panel); border-right: 1px solid var(--border); padding: 0.5rem; overflow: auto; }
  .palette-title { font-size: 0.8rem; text-transform: uppercase; color: #94a3b8; margin-bottom: 0.5rem; padding: 0 0.25rem; }
  .palette-card { display: flex; align-items: center; gap: 0.5rem; padding: 0.4rem 0.5rem; margin-bottom: 0.25rem; background: rgba(255,255,255,0.03); border: 1px solid transparent; border-radius: 0.25rem; cursor: grab; }
  .palette-card:hover { background: rgba(255,255,255,0.08); border-color: var(--border); }
  .palette-icon { font-size: 1.1rem; }
  .palette-label { font-size: 0.85rem; }
  .canvas-wrap { flex: 1; position: relative; overflow: hidden; background: radial-gradient(circle at 1px 1px, rgba(255,255,255,0.05) 1px, transparent 0); background-size: 20px 20px; }
  .canvas { position: absolute; inset: 0; overflow: auto; }
  .edges { position: absolute; inset: 0; pointer-events: none; }
  .edge-line { stroke: #94a3b8; stroke-width: 2; pointer-events: none; }
  .edge-line.selected { stroke: #38bdf8; stroke-width: 3; }
  .connection-hit { stroke: transparent; stroke-width: 14; pointer-events: auto; cursor: pointer; }
  .node { position: absolute; background: var(--bg-card); border: 2px solid #64748b; border-radius: 0.375rem; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.3); cursor: default; user-select: none; }
  .node.selected { outline: 2px solid #38bdf8; }
  .node-header { display: flex; align-items: center; gap: 0.4rem; padding: 0.25rem 0.4rem; border-bottom: 1px solid rgba(255,255,255,0.1); border-radius: 0.375rem 0.375rem 0 0; }
  .node-icon { font-size: 0.95rem; }
  .node-name { flex: 1; font-size: 0.75rem; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .node-type { font-size: 0.65rem; color: #94a3b8; white-space: nowrap; }
  .status-chip { position: absolute; bottom: 4px; left: 4px; right: 4px; display: flex; align-items: center; justify-content: center; gap: 0.25rem; padding: 0.1rem; border: 1px solid; border-radius: 0.25rem; font-size: 0.65rem; }
  .port { position: absolute; width: 10px; height: 10px; background: #94a3b8; border: 2px solid #0f172a; border-radius: 50%; cursor: crosshair; }
  .port.in { background: #38bdf8; }
  .port.out { background: #10b981; }
  .pending { position: absolute; top: 1rem; left: 50%; transform: translateX(-50%); background: rgba(14,165,233,0.9); color: white; padding: 0.25rem 0.75rem; border-radius: 0.25rem; font-size: 0.85rem; }
  .inspector { width: 260px; background: var(--bg-panel); border-left: 1px solid var(--border); padding: 0.75rem; overflow: auto; }
  .inspector-title { font-size: 0.8rem; text-transform: uppercase; color: #94a3b8; margin-bottom: 0.75rem; }
  .section { margin-bottom: 1rem; padding-bottom: 0.75rem; border-bottom: 1px solid var(--border); }
  .section-title { font-size: 0.75rem; color: #94a3b8; margin-bottom: 0.5rem; }
  .field { margin-bottom: 0.5rem; }
  .field label { display: block; font-size: 0.7rem; color: #94a3b8; margin-bottom: 0.25rem; }
  .field input, .field select { width: 100%; background: var(--bg-dark); border: 1px solid var(--border); border-radius: 0.25rem; padding: 0.4rem 0.5rem; color: #e2e8f0; font-size: 0.85rem; box-sizing: border-box; }
  .badge-type { display: inline-flex; align-items: center; gap: 0.25rem; padding: 0.2rem 0.4rem; border: 1px solid; border-radius: 0.25rem; font-size: 0.75rem; }
  .readonly { padding: 0.4rem 0.5rem; background: rgba(255,255,255,0.05); border-radius: 0.25rem; font-size: 0.75rem; word-break: break-all; }
  .hint { color: #94a3b8; font-size: 0.85rem; }
  button.danger { background: rgba(239,68,68,0.15); color: #f87171; border: 1px solid rgba(239,68,68,0.4); padding: 0.375rem 0.75rem; border-radius: 0.25rem; cursor: pointer; width: 100%; }
</style>
