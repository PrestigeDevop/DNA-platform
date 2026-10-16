<script lang="ts">
  import { createEventDispatcher, onMount, onDestroy } from 'svelte';
  import { api } from '$lib/services/api';

  export let workflow: any;
  export let execution: any = null;

  const dispatch = createEventDispatcher();

  const NODE_W = 220;
  const NODE_H = 92;
  const PORT_SIZE = 12;
  const CANVAS_PADDING = 100;

  type NodeMeta = { icon: string; color: string; label: string; desc: string };

  const TYPE_META: Record<string, NodeMeta> = {
    Input: { icon: '📥', color: '#0ea5e9', label: 'Input', desc: 'Pass strongly typed entity string' },
    Agent: { icon: '🤖', color: '#a855f7', label: 'Copilot Agent', desc: 'Microsoft Copilot ONNX Prompt Flow' },
    Output: { icon: '🖥️', color: '#10b981', label: 'Output', desc: 'Prints to backend console' },
    ProcessingSkill: { icon: '🧬', color: '#f59e0b', label: 'Bio Skill', desc: 'Execute registry skill or C# clock' },
    Loop: { icon: '🔁', color: '#8b5cf6', label: 'Loop', desc: 'Repeat iteration / accumulator' },
    Conditional: { icon: '🔀', color: '#ec4899', label: 'Conditional', desc: 'Branching logic' },
    SubWorkflow: { icon: '📦', color: '#64748b', label: 'Sub-Workflow', desc: 'Nested pipeline run' },
    CronJob: { icon: '⏰', color: '#eab308', label: 'Cron Job', desc: 'Scheduled interval trigger' },
    StateMachine: { icon: '🎮', color: '#38bdf8', label: 'State Machine', desc: 'Unity-style layered FSM' },
    SmartComponent: { icon: '🧩', color: '#06b6d4', label: 'Smart Component', desc: 'Authorable custom ECS unit' },
    Value: { icon: '🔢', color: '#6366f1', label: 'Value', desc: 'Constant value' },
    Log: { icon: '📜', color: '#ef4444', label: 'Log', desc: 'Write to log stream' }
  };

  const AVAILABLE_SKILLS = [
    { id: 'csharp-current-time', name: '⏱️ C# System.DateTime Clock Provider' },
    { id: 'sequence-aligner', name: '🔬 Sequence Aligner (Needleman-Wunsch)' },
    { id: 'fasta-loader', name: '📁 FASTA Loader & Parser' },
    { id: 'data-transform', name: '🔄 Data Transform & Normalize' },
    { id: 'validation', name: '✅ Sequence & Format Validator' },
    { id: 'merge-results', name: '🔀 Merge Results' },
    { id: 'msgbox-alert', name: '💬 Message Box Alert' }
  ];

  const AVAILABLE_MCP_TOOLS = [
    { id: 'csharp-runtime/get_current_time', label: '⏱️ C# DateTime.UtcNow Provider' },
    { id: 'sequence-aligner', label: '🔬 Needleman-Wunsch Aligner' },
    { id: 'dna-bioinformatics/reverse_complement', label: '🧬 DNA Reverse Complement' },
    { id: 'dna-bioinformatics/gc_skew_analysis', label: '📊 GC Skew & Percentage' },
    { id: 'dna-bioinformatics/dna_to_protein', label: '🧪 Codon Translation to Protein' },
    { id: 'fasta-loader', label: '📁 FASTA Loader & GC Calculator' },
    { id: 'data-transform', label: '🔄 Data Transform & Clean' },
    { id: 'filesystem-workspace/read_file', label: '📂 Workspace File Reader' },
    { id: 'system-runtime/system_status', label: '⚡ Runtime Diagnostics' }
  ];

  const ENTITY_TYPES = ['String', 'DNA_Sequence', 'Int32', 'Float', 'Boolean', 'Vector3', 'JSON'];

  let activeView: 'canvas' | 'statemachine' | 'smart_component' = 'canvas';

  let nodes: any[] = [];
  let connections: any[] = [];
  let selectedId: string | null = null;
  let selectedEdgeId: string | null = null;
  let pendingFrom: string | null = null;
  let nodeTypes: string[] = Object.keys(TYPE_META);

  let canvasEl: HTMLDivElement;
  let dragState: { id: string; offsetX: number; offsetY: number } | null = null;
  let saving = false;
  let executing = false;
  let message = '';
  let error = '';

  // Unity State Machine Designer State
  let fsmLayers = ['Base Layer', 'QC Layer', 'Inference Layer'];
  let activeFsmLayer = 'Base Layer';
  let fsmStates = [
    { id: 's_entry', name: 'Entry', type: 'entry', x: 60, y: 80 },
    { id: 's_idle', name: 'Idle / Awaiting Sequence', type: 'state', x: 220, y: 80 },
    { id: 's_process', name: 'Copilot ONNX Reasoning', type: 'state', x: 440, y: 80 },
    { id: 's_validate', name: 'Genomic Verification', type: 'state', x: 660, y: 80 },
    { id: 's_exit', name: 'Completed / Exit', type: 'exit', x: 860, y: 80 }
  ];
  let activeFsmState = 's_idle';
  let fsmTransitions = [
    { from: 's_entry', to: 's_idle', trigger: 'OnStart', condition: 'Always' },
    { from: 's_idle', to: 's_process', trigger: 'OnSequenceLoaded', condition: 'length > 10' },
    { from: 's_process', to: 's_validate', trigger: 'OnInferenceDone', condition: 'confidence >= 0.8' },
    { from: 's_validate', to: 's_exit', trigger: 'OnVerified', condition: 'isValid == true' }
  ];

  // Smart Component Authoring in Tab
  let compName = 'DNA Splice Junction Smart Component';
  let compCategory = 'Smart Component';
  let compTransformMode = 'dna-enhance';
  let compInputType = 'DNA_Sequence';
  let compOutputType = 'JSON';

  function safeParseJson(field: any): any[] {
    if (!field) return [];
    if (Array.isArray(field)) return field;
    if (typeof field === 'string') {
      try {
        let p = JSON.parse(field);
        if (typeof p === 'string') {
          try { p = JSON.parse(p); } catch {}
        }
        return Array.isArray(p) ? p : [];
      } catch {
        return [];
      }
    }
    return [];
  }

  onMount(() => {
    const wf = workflow || {};
    const parsedNodes = safeParseJson(wf.nodes);
    const parsedConns = safeParseJson(wf.connections);

    if (parsedNodes.length === 0) {
      loadPipelinePreset('copilot');
    } else {
      nodes = parsedNodes.map((n: any) => ({
        ...n,
        config: n.config || {}
      }));
      connections = parsedConns.map((c: any) => {
        const from = c.from || c.sourceNodeId;
        const to = c.to || c.targetNodeId;
        return {
          id: c.id || 'c_' + Math.random().toString(36).slice(2, 6),
          from,
          to,
          sourceNodeId: from,
          targetNodeId: to
        };
      });
    }

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
    return TYPE_META[type] || { icon: '🧩', color: '#64748b', label: type, desc: '' };
  }

  function loadPipelinePreset(kind: 'copilot' | 'aligner' | 'loop') {
    if (kind === 'copilot') {
      const n1 = 'node_input_1';
      const n2 = 'node_agent_copilot';
      const n3 = 'node_output_console';

      nodes = [
        {
          id: n1,
          type: 'Input',
          name: 'DNA Query Input',
          x: 60,
          y: 120,
          config: {
            value: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCCCTGTGGGGCAAGGTG',
            key: 'input_text',
            entityType: 'DNA_Sequence'
          }
        },
        {
          id: n2,
          type: 'Agent',
          name: 'Microsoft Copilot ONNX',
          x: 360,
          y: 120,
          config: {
            model: 'Microsoft-Copilot-Local-ONNX',
            systemPrompt: 'You are Microsoft Copilot, an expert bioinformatician. Report time using C# provider and analyze nucleotide sequences.',
            prompt: 'Check sequence: {{input}}',
            tools: ['csharp-runtime/get_current_time', 'sequence-aligner', 'dna-bioinformatics/reverse_complement'],
            temperature: 0.7,
            maxTokens: 512
          }
        },
        {
          id: n3,
          type: 'Output',
          name: 'Backend Console Logger',
          x: 680,
          y: 120,
          config: {
            prefix: '>> BIO-PIPELINE RESULT:',
            format: 'console'
          }
        }
      ];

      connections = [
        { id: 'c1', from: n1, to: n2, sourceNodeId: n1, targetNodeId: n2 },
        { id: 'c2', from: n2, to: n3, sourceNodeId: n2, targetNodeId: n3 }
      ];
      selectedId = n2;
      message = 'Loaded Microsoft Copilot Prompt Flow Pipeline preset with C# Time Provider.';
    } else if (kind === 'aligner') {
      const n1 = 'in_seq1';
      const n2 = 'skill_aln';
      const n3 = 'out_log';

      nodes = [
        {
          id: n1,
          type: 'Input',
          name: 'Target Sequence',
          x: 60,
          y: 120,
          config: {
            value: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC',
            key: 'sequence1',
            entityType: 'DNA_Sequence'
          }
        },
        {
          id: n2,
          type: 'ProcessingSkill',
          name: 'Needleman-Wunsch Aligner',
          x: 360,
          y: 120,
          config: {
            skillId: 'sequence-aligner',
            sequence2: 'ATGGTGCACGTGACTCCTGTTGAGAAGTCTGCC'
          }
        },
        {
          id: n3,
          type: 'Output',
          name: 'Console Print Output',
          x: 680,
          y: 120,
          config: {
            prefix: '>> ALIGNMENT OUTPUT:'
          }
        }
      ];

      connections = [
        { id: 'c1', from: n1, to: n2, sourceNodeId: n1, targetNodeId: n2 },
        { id: 'c2', from: n2, to: n3, sourceNodeId: n2, targetNodeId: n3 }
      ];
      selectedId = n2;
      message = 'Loaded Needleman-Wunsch Aligner Flow preset.';
    } else if (kind === 'loop') {
      const n1 = 'in_seq';
      const n2 = 'loop_1';
      const n3 = 'out_log';

      nodes = [
        {
          id: n1,
          type: 'Input',
          name: 'Seed Sequence',
          x: 60,
          y: 120,
          config: { value: 'ATGCTAGCTAGCTA', key: 'input_text', entityType: 'DNA_Sequence' }
        },
        {
          id: n2,
          type: 'Loop',
          name: 'Batch Sequence Iterator',
          x: 360,
          y: 120,
          config: { iterations: 3, mode: 'count' }
        },
        {
          id: n3,
          type: 'Output',
          name: 'Console Print Output',
          x: 680,
          y: 120,
          config: { prefix: '>> ITERATION RESULT:' }
        }
      ];

      connections = [
        { id: 'c1', from: n1, to: n2, sourceNodeId: n1, targetNodeId: n2 },
        { id: 'c2', from: n2, to: n3, sourceNodeId: n2, targetNodeId: n3 }
      ];
      selectedId = n2;
      message = 'Loaded Loop Iterator Pipeline preset.';
    }
  }

  function addNode(type: string) {
    const id = 'n_' + Date.now().toString(36) + Math.random().toString(36).slice(2, 5);
    const x = CANVAS_PADDING + Math.floor(Math.random() * 200);
    const y = CANVAS_PADDING + Math.floor(Math.random() * 150);
    const meta = getMeta(type);

    const defaultConfig: Record<string, any> = {};
    if (type === 'Input') {
      defaultConfig.value = 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC';
      defaultConfig.key = 'input_text';
      defaultConfig.entityType = 'DNA_Sequence';
    } else if (type === 'Agent') {
      defaultConfig.model = 'Microsoft-Copilot-Local-ONNX';
      defaultConfig.systemPrompt = 'You are a Microsoft Copilot bioinformatic assistant.';
      defaultConfig.prompt = 'Analyze sequence: {{input}}';
      defaultConfig.tools = ['csharp-runtime/get_current_time', 'sequence-aligner', 'dna-bioinformatics/reverse_complement'];
    } else if (type === 'ProcessingSkill') {
      defaultConfig.skillId = 'sequence-aligner';
    } else if (type === 'Loop') {
      defaultConfig.iterations = 3;
      defaultConfig.mode = 'count';
    } else if (type === 'Conditional') {
      defaultConfig.condition = 'contains(ATG)';
    } else if (type === 'SubWorkflow') {
      defaultConfig.subWorkflowId = 'Bioinformatics-SubFlow-v1';
    } else if (type === 'CronJob') {
      defaultConfig.intervalSeconds = 30;
    } else if (type === 'StateMachine') {
      defaultConfig.currentState = 'Idle';
      defaultConfig.targetState = 'Processing';
      defaultConfig.activeLayer = 'Base Layer';
    } else if (type === 'SmartComponent') {
      defaultConfig.componentName = 'DNA Splice Junction Smart Component';
      defaultConfig.transformMode = 'dna-enhance';
    } else if (type === 'Output') {
      defaultConfig.prefix = '>> BACKEND LOG:';
    }

    nodes = [
      ...nodes,
      {
        id,
        type,
        name: meta.label,
        x,
        y,
        config: defaultConfig
      }
    ];
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
    const x = Math.max(20, e.clientX - rect.left + canvasEl.scrollLeft - NODE_W / 2);
    const y = Math.max(20, e.clientY - rect.top + canvasEl.scrollTop - NODE_H / 2);
    const id = 'n_' + Date.now().toString(36) + Math.random().toString(36).slice(2, 5);
    const meta = getMeta(type);

    const defaultConfig: Record<string, any> = {};
    if (type === 'Input') {
      defaultConfig.value = 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC';
      defaultConfig.key = 'input_text';
      defaultConfig.entityType = 'DNA_Sequence';
    } else if (type === 'Agent') {
      defaultConfig.model = 'Microsoft-Copilot-Local-ONNX';
      defaultConfig.systemPrompt = 'You are a Microsoft Copilot bioinformatic assistant.';
      defaultConfig.prompt = 'Analyze sequence: {{input}}';
      defaultConfig.tools = ['csharp-runtime/get_current_time', 'sequence-aligner'];
    } else if (type === 'Loop') {
      defaultConfig.iterations = 3;
    } else if (type === 'Conditional') {
      defaultConfig.condition = 'contains(ATG)';
    } else if (type === 'SubWorkflow') {
      defaultConfig.subWorkflowId = 'Bioinformatics-SubFlow-v1';
    } else if (type === 'CronJob') {
      defaultConfig.intervalSeconds = 30;
    } else if (type === 'StateMachine') {
      defaultConfig.currentState = 'Idle';
      defaultConfig.targetState = 'Processing';
    } else if (type === 'SmartComponent') {
      defaultConfig.componentName = 'DNA Splice Junction Smart Component';
      defaultConfig.transformMode = 'dna-enhance';
    } else if (type === 'Output') {
      defaultConfig.prefix = '>> BACKEND LOG:';
    }

    nodes = [...nodes, { id, type, name: meta.label, x, y, config: defaultConfig }];
    selectedId = id;
    selectedEdgeId = null;
    pendingFrom = null;
  }

  function onCanvasDragOver(e: DragEvent) {
    e.preventDefault();
  }

  function onNodeMouseDown(e: MouseEvent, node: any) {
    if ((e.target as HTMLElement).closest('.port')) return;
    if ((e.target as HTMLElement).closest('.node-header') || (e.target as HTMLElement).closest('.node-body')) {
      e.preventDefault();
      const rect = canvasEl.getBoundingClientRect();
      dragState = {
        id: node.id,
        offsetX: e.clientX - rect.left + canvasEl.scrollLeft - node.x,
        offsetY: e.clientY - rect.top + canvasEl.scrollTop - node.y
      };
      selectedId = node.id;
      selectedEdgeId = null;
      pendingFrom = null;
    }
  }

  function onMouseMove(e: MouseEvent) {
    if (!dragState || !canvasEl) return;
    const ds = dragState;
    const rect = canvasEl.getBoundingClientRect();
    const x = Math.max(10, e.clientX - rect.left + canvasEl.scrollLeft - ds.offsetX);
    const y = Math.max(10, e.clientY - rect.top + canvasEl.scrollTop - ds.offsetY);
    nodes = nodes.map((n) => (n.id === ds.id ? { ...n, x, y } : n));
  }

  function onMouseUp() {
    dragState = null;
  }

  function onPortClick(nodeId: string, kind: 'in' | 'out') {
    if (kind === 'out') {
      pendingFrom = nodeId;
      message = 'Connecting output port — now click a target node\'s blue input port.';
      return;
    }
    if (kind === 'in' && pendingFrom) {
      const from = pendingFrom;
      const to = nodeId;
      if (from !== to) {
        if (!connections.some((c) => (c.from === from && c.to === to) || (c.sourceNodeId === from && c.targetNodeId === to))) {
          const id = 'c_' + Date.now().toString(36) + Math.random().toString(36).slice(2, 4);
          connections = [
            ...connections,
            { id, from, to, sourceNodeId: from, targetNodeId: to }
          ];
          message = 'Flow connection established.';
        }
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

  function onKeyDown(e: KeyboardEvent) {
    if (e.key === 'Delete' || e.key === 'Backspace') {
      const activeEl = document.activeElement;
      if (activeEl && (activeEl.tagName === 'INPUT' || activeEl.tagName === 'TEXTAREA' || activeEl.tagName === 'SELECT')) {
        return;
      }
      if (selectedEdgeId) {
        connections = connections.filter((c) => c.id !== selectedEdgeId);
        selectedEdgeId = null;
      } else if (selectedId) {
        nodes = nodes.filter((n) => n.id !== selectedId);
        connections = connections.filter(
          (c) => c.from !== selectedId && c.to !== selectedId && c.sourceNodeId !== selectedId && c.targetNodeId !== selectedId
        );
        selectedId = null;
      }
    }
    if (e.key === 'Escape') {
      pendingFrom = null;
      selectedEdgeId = null;
    }
  }

  /**
   * Precise port position calculation for exact SVG arrow alignment
   */
  function getPortPos(nodeId: string, kind: 'in' | 'out') {
    const node = nodes.find((n) => n.id === nodeId);
    if (!node) return { x: 0, y: 0 };
    if (kind === 'in') {
      return { x: node.x, y: node.y + NODE_H / 2 };
    }
    return { x: node.x + NODE_W, y: node.y + NODE_H / 2 };
  }

  /**
   * Calculates smooth cubic bezier with exact port boundary alignment
   */
  function edgePath(edge: any) {
    const fromId = edge.from || edge.sourceNodeId;
    const toId = edge.to || edge.targetNodeId;
    const from = getPortPos(fromId, 'out');
    const to = getPortPos(toId, 'in');

    const dx = to.x - from.x;
    const curvature = Math.max(Math.abs(dx) * 0.45, 45);

    // Stop 4px before target port center so arrow head aligns perfectly with circle edge
    const targetX = to.x - 4;
    return `M ${from.x} ${from.y} C ${from.x + curvature} ${from.y}, ${targetX - curvature} ${to.y}, ${targetX} ${to.y}`;
  }

  function nodeStatus(nodeId: string) {
    const res = execution?.nodeResults?.find((r: any) => r.nodeId === nodeId);
    if (!res || !res.status) return null;
    const s = String(res.status).toLowerCase();
    if (s === 'completed') return { text: 'Completed', icon: '✓', color: '#10b981' };
    if (s === 'running') return { text: 'Running', icon: '▶', color: '#f59e0b' };
    if (s === 'failed') return { text: 'Failed', icon: '✕', color: '#ef4444' };
    if (s === 'skipped') return { text: 'Skipped', icon: '⊘', color: '#94a3b8' };
    return { text: res.status, icon: '•', color: '#94a3b8' };
  }

  function getNodePreview(node: any): string {
    if (node.type === 'Input') {
      const v = node.config?.value || node.config?.userInput || '';
      const et = node.config?.entityType || 'String';
      return `[${et}] "${v.slice(0, 18)}${v.length > 18 ? '…' : ''}"`;
    }
    if (node.type === 'Agent') {
      const m = node.config?.model || 'Copilot-ONNX';
      return `${m.split('-')[0]} · ${node.config?.prompt || '{{input}}'}`;
    }
    if (node.type === 'ProcessingSkill') {
      return `Skill: ${node.config?.skillId || 'unassigned'}`;
    }
    if (node.type === 'Loop') {
      return `Repeat ${node.config?.iterations || 3}x iterations`;
    }
    if (node.type === 'Conditional') {
      return `If: ${node.config?.condition || 'condition'}`;
    }
    if (node.type === 'SubWorkflow') {
      return `Sub: ${node.config?.subWorkflowId || 'pipeline'}`;
    }
    if (node.type === 'CronJob') {
      return `Every ${node.config?.intervalSeconds || 30}s`;
    }
    if (node.type === 'StateMachine') {
      return `FSM: ${node.config?.currentState || 'Idle'} → ${node.config?.targetState || 'Processing'}`;
    }
    if (node.type === 'SmartComponent') {
      return `ECS: ${node.config?.componentName || 'Smart Component'}`;
    }
    if (node.type === 'Output') {
      return 'Prints to backend console';
    }
    if (node.type === 'Value') {
      return `Val: ${node.config?.value ?? '—'}`;
    }
    return node.type;
  }

  $: currentNode = nodes.find((n) => n.id === selectedId) || null;
  $: selectedEdge = connections.find((c) => c.id === selectedEdgeId) || null;

  // Upstream & Downstream Connected Nodes for Inspector
  $: incomingNodes = currentNode
    ? connections
        .filter((c) => (c.to === currentNode.id || c.targetNodeId === currentNode.id))
        .map((c) => nodes.find((n) => n.id === (c.from || c.sourceNodeId)))
        .filter(Boolean)
    : [];

  $: outgoingNodes = currentNode
    ? connections
        .filter((c) => (c.from === currentNode.id || c.sourceNodeId === currentNode.id))
        .map((c) => nodes.find((n) => n.id === (c.to || c.targetNodeId)))
        .filter(Boolean)
    : [];

  async function onSave() {
    if (!workflow?.id) return;
    saving = true;
    error = '';
    message = '';
    try {
      const wf = {
        name: workflow.name,
        description: workflow.description,
        nodes: nodes,
        connections: connections,
        status: workflow.status
      };
      await api.updateWorkflow(workflow.id, wf);
      dispatch('saved', {});
      message = 'Workflow saved successfully.';
      setTimeout(() => (message = ''), 3000);
    } catch (e: any) {
      error = e?.message || String(e);
    } finally {
      saving = false;
    }
  }

  async function onRun() {
    executing = true;
    error = '';
    message = 'Executing flow with Microsoft Copilot ONNX & Prompt Flow...';
    try {
      const payload = {
        id: workflow?.id,
        name: workflow?.name || 'workflow',
        nodes: JSON.parse(JSON.stringify(nodes)),
        connections: JSON.parse(JSON.stringify(connections))
      };

      const result = await api.executeWorkflow(payload);
      const full = result?.fullExecution || result;
      const nodeExecs = full?.nodeExecutions || {};

      const nodeResults = nodes.map((n) => {
        const ne = nodeExecs[n.id];
        if (!ne) return { nodeId: n.id, status: 'Completed', inputs: {}, outputs: {} };
        return {
          nodeId: n.id,
          status: ne.status || 'Completed',
          inputs: ne.inputs || {},
          outputs: ne.outputs || {},
          startedAt: ne.startedAt,
          completedAt: ne.completedAt,
          error: ne.error
        };
      });

      execution = { nodeResults, status: result.status, outputs: result.outputs };
      dispatch('saved', { nodeResults });
      message = `Run ${result.status} in ${result.durationMs ?? 0}ms. Output logged to backend console stdout.`;
    } catch (e: any) {
      error = e?.message || String(e);
    } finally {
      executing = false;
    }
  }

  function toggleAgentTool(toolId: string) {
    if (!currentNode || currentNode.type !== 'Agent') return;
    const current = currentNode.config.tools || [];
    if (current.includes(toolId)) {
      currentNode.config.tools = current.filter((t: string) => t !== toolId);
    } else {
      currentNode.config.tools = [...current, toolId];
    }
    nodes = [...nodes];
  }

  function stepFsm() {
    const currentIdx = fsmStates.findIndex((s) => s.id === activeFsmState);
    const nextIdx = (currentIdx + 1) % fsmStates.length;
    activeFsmState = fsmStates[nextIdx].id;
    message = `FSM Layer [${activeFsmLayer}]: Transitioned to state "${fsmStates[nextIdx].name}"`;
    setTimeout(() => (message = ''), 3000);
  }

  function onClose() {
    dispatch('close');
  }
</script>

<div class="designer" role="application" aria-label="Workflow Designer">
  <!-- DESIGNER HEADER -->
  <div class="designer-header">
    <div class="header-left">
      <span class="brand-pill">Flow Studio</span>
      <strong>{workflow?.name || 'Flow Designer'}</strong>
    </div>

    <!-- MAIN VIEW TABS -->
    <div class="view-switch-tabs">
      <button
        type="button"
        class="view-tab"
        class:active={activeView === 'canvas'}
        on:click={() => (activeView = 'canvas')}
      >
        🔀 Flow Canvas
      </button>
      <button
        type="button"
        class="view-tab"
        class:active={activeView === 'statemachine'}
        on:click={() => (activeView = 'statemachine')}
      >
        🎮 Unity State Machine
      </button>
      <button
        type="button"
        class="view-tab"
        class:active={activeView === 'smart_component'}
        on:click={() => (activeView = 'smart_component')}
      >
        🧩 Smart Component ECS
      </button>
    </div>

    <div class="header-right">
      <button type="button" on:click={onClose}>✕ Close</button>
      <button type="button" class="save" on:click={onSave} disabled={saving}>
        💾 {saving ? 'Saving...' : 'Save'}
      </button>
      <button type="button" class="run" on:click={onRun} disabled={executing}>
        ▶ {executing ? 'Executing...' : 'Run Flow'}
      </button>
    </div>
  </div>

  {#if message}
    <div class="banner info">{message}</div>
  {/if}
  {#if error}
    <div class="banner error">{error}</div>
  {/if}

  <!-- TAB 1: FLOW CANVAS VIEW -->
  {#if activeView === 'canvas'}
    <div class="designer-subbar">
      <span class="preset-label">Templates:</span>
      <button type="button" class="preset-btn" on:click={() => loadPipelinePreset('copilot')}>
        🤖 Copilot Agent Flow
      </button>
      <button type="button" class="preset-btn" on:click={() => loadPipelinePreset('aligner')}>
        🧬 Pairwise Aligner Flow
      </button>
      <button type="button" class="preset-btn" on:click={() => loadPipelinePreset('loop')}>
        🔁 Loop Iterator Flow
      </button>
    </div>

    <div class="designer-body">
      <!-- PALETTE -->
      <aside class="palette">
        <div class="palette-title">Node Palette</div>
        <div class="palette-hint">Drag onto canvas or click to add</div>
        <div class="palette-scroll">
          {#each nodeTypes as t}
            {@const m = getMeta(t)}
            <div
              class="palette-card"
              draggable="true"
              on:dragstart={(e) => onPaletteDragStart(e, t)}
              on:click={() => addNode(t)}
              style="border-left: 3px solid {m.color}"
            >
              <span class="palette-icon">{m.icon}</span>
              <div class="palette-info">
                <span class="palette-label">{m.label}</span>
                <span class="palette-desc">{m.desc}</span>
              </div>
            </div>
          {/each}
        </div>
      </aside>

      <!-- CANVAS -->
      <main class="canvas-wrap">
        <div
          bind:this={canvasEl}
          class="canvas"
          on:drop={onCanvasDrop}
          on:dragover={onCanvasDragOver}
          on:click={onCanvasClick}
        >
          <!-- SVG CONNECTIONS WITH ACCURATE ARROW MARKER -->
          <svg class="edges" width="100%" height="100%">
            <defs>
              <marker
                id="arrow"
                viewBox="0 0 10 10"
                refX="7"
                refY="5"
                markerWidth="8"
                markerHeight="8"
                orient="auto-start-reverse"
              >
                <path d="M 0 1 L 10 5 L 0 9 z" fill="#38bdf8" />
              </marker>
              <marker
                id="arrow-selected"
                viewBox="0 0 10 10"
                refX="7"
                refY="5"
                markerWidth="9"
                markerHeight="9"
                orient="auto-start-reverse"
              >
                <path d="M 0 1 L 10 5 L 0 9 z" fill="#f59e0b" />
              </marker>
            </defs>

            {#each connections as edge}
              {@const p = edgePath(edge)}
              {@const selected = selectedEdgeId === edge.id}
              <path
                d={p}
                class="edge-line"
                class:selected
                fill="none"
                marker-end={selected ? 'url(#arrow-selected)' : 'url(#arrow)'}
              />
              <path
                d={p}
                class="connection-hit"
                on:click={(e) => onEdgeClick(e, edge.id)}
              />
            {/each}
          </svg>

          <!-- NODES -->
          {#each nodes || [] as node}
            {@const meta = getMeta(node.type)}
            {@const st = nodeStatus(node.id)}
            <div
              class="node"
              class:selected={selectedId === node.id}
              style="left:{node.x}px; top:{node.y}px; width:{NODE_W}px; height:{NODE_H}px; border-color:{meta.color}"
              on:mousedown={(e) => onNodeMouseDown(e, node)}
            >
              <div
                class="node-header"
                style="background:linear-gradient(90deg, {meta.color}26, rgba(255,255,255,0.02))"
              >
                <span class="node-icon">{meta.icon}</span>
                <span class="node-name" title={node.name}>{node.name || meta.label}</span>
                <span class="node-type" style="color:{meta.color}">{meta.label}</span>
              </div>

              <div class="node-body">
                <span class="node-preview">{getNodePreview(node)}</span>
              </div>

              {#if st}
                <div
                  class="status-chip"
                  style="color:{st.color}; border-color:{st.color}66; background:{st.color}22"
                >
                  <span class="status-icon">{st.icon}</span>
                  <span class="status-text">{st.text}</span>
                </div>
              {/if}

              <!-- Left Input Port (Blue Circle) -->
              <div
                class="port in"
                title="Input Port (Click to connect incoming data)"
                style="left:-{PORT_SIZE / 2}px; top:{NODE_H / 2 - PORT_SIZE / 2}px; width:{PORT_SIZE}px; height:{PORT_SIZE}px"
                on:click|stopPropagation={() => onPortClick(node.id, 'in')}
              ></div>

              <!-- Right Output Port (Green Circle) -->
              <div
                class="port out"
                title="Output Port (Click to connect to downstream node)"
                style="left:{NODE_W - PORT_SIZE / 2}px; top:{NODE_H / 2 - PORT_SIZE / 2}px; width:{PORT_SIZE}px; height:{PORT_SIZE}px"
                on:click|stopPropagation={() => onPortClick(node.id, 'out')}
              ></div>
            </div>
          {/each}

          {#if pendingFrom}
            <div class="pending">🎯 Click a target node's blue input port (left dot) to complete connection</div>
          {/if}
        </div>
      </main>

      <!-- INSPECTOR & PROPERTIES -->
      <aside class="inspector">
        <div class="inspector-title">Node Inspector & Properties</div>

        {#if currentNode}
          {@const meta = getMeta(currentNode.type)}

          <!-- CONNECTED NODES SECTION -->
          <div class="section connected-section">
            <div class="section-title">🔗 Flow Connections</div>

            <div class="conn-group">
              <span class="conn-label">Connected From (Inputs):</span>
              {#if incomingNodes.length > 0}
                <div class="conn-chips">
                  {#each incomingNodes as src}
                    <span class="conn-chip in">
                      {src.name} <code class="chip-type">({src.type})</code>
                    </span>
                  {/each}
                </div>
              {:else}
                <span class="conn-empty">● Entry Node (No upstream source)</span>
              {/if}
            </div>

            <div class="conn-group">
              <span class="conn-label">Connected To (Outputs):</span>
              {#if outgoingNodes.length > 0}
                <div class="conn-chips">
                  {#each outgoingNodes as tgt}
                    <span class="conn-chip out">
                      {tgt.name} <code class="chip-type">({tgt.type})</code>
                    </span>
                  {/each}
                </div>
              {:else}
                <span class="conn-empty">● Terminal Node (No downstream targets)</span>
              {/if}
            </div>
          </div>

          <!-- NODE IDENTITY -->
          <div class="section">
            <div class="field">
              <label>Node Name</label>
              <input type="text" bind:value={currentNode.name} />
            </div>
            <div class="field">
              <label>Node Type</label>
              <div
                class="badge-type"
                style="border-color:{meta.color}; color:{meta.color}; background:{meta.color}22"
              >
                {meta.icon} {meta.label}
              </div>
            </div>
          </div>

          <!-- DYNAMIC TYPE CONFIGURATION -->
          <div class="section">
            <div class="section-title">Component Entity Properties</div>

            <!-- INPUT NODE CONFIG WITH STRONGLY TYPED FIELDS -->
            {#if currentNode.type === 'Input'}
              <div class="field">
                <label>Entity Field Type (Unity-Style ECS)</label>
                <select bind:value={currentNode.config.entityType}>
                  {#each ENTITY_TYPES as et}
                    <option value={et}>{et}</option>
                  {/each}
                </select>
              </div>

              <div class="field">
                <label>Input String / User Value</label>
                <textarea
                  rows="4"
                  bind:value={currentNode.config.value}
                  placeholder="Enter DNA sequence or string value..."
                ></textarea>
              </div>

              <div class="field">
                <label>Variable / Field Identifier</label>
                <input type="text" bind:value={currentNode.config.key} placeholder="input_text" />
              </div>

              <!-- Strong typing validation badge -->
              <div class="type-badge-preview">
                <span class="type-tag">{currentNode.config.entityType || 'String'}</span>
                <span>Length: {(currentNode.config.value || '').length} characters</span>
              </div>

              <div class="field presets">
                <span class="preset-label">Insert sample:</span>
                <button
                  type="button"
                  class="small-btn"
                  on:click={() => {
                    currentNode.config.value = 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCCCTGTGGGGCAAGGTG';
                    currentNode.config.entityType = 'DNA_Sequence';
                    nodes = [...nodes];
                  }}
                >
                  DNA Beta-Globin
                </button>
                <button
                  type="button"
                  class="small-btn"
                  on:click={() => {
                    currentNode.config.value = 'Analyze the mutation rate in sample sequence';
                    currentNode.config.entityType = 'String';
                    nodes = [...nodes];
                  }}
                >
                  Copilot Prompt
                </button>
              </div>

            <!-- AGENT NODE CONFIG -->
            {:else if currentNode.type === 'Agent'}
              <div class="field">
                <label>Inference Engine & Model</label>
                <select bind:value={currentNode.config.model}>
                  <option value="Microsoft-Copilot-Local-ONNX">Microsoft Copilot ONNX (Local CPU)</option>
                  <option value="Phi-3.5-mini-ONNX">Microsoft Phi-3.5-mini ONNX</option>
                  <option value="Biomedical-Copilot-v1">Biomedical Copilot Reasoning Engine</option>
                </select>
              </div>

              <div class="field">
                <label>System Prompt (Directives)</label>
                <textarea
                  rows="3"
                  bind:value={currentNode.config.systemPrompt}
                  placeholder="Direct the Copilot persona and behavior..."
                ></textarea>
              </div>

              <div class="field">
                <label>User Prompt Template (Prompt Flow)</label>
                <textarea
                  rows="2"
                  bind:value={currentNode.config.prompt}
                  placeholder="Use &#123;&#123;input&#125;&#125; to consume upstream string"
                ></textarea>
                <span class="hint-text">Use &#123;&#123;input&#125;&#125; to interpolate string passed from Input node</span>
              </div>

              <div class="field">
                <label>Attached MCP Tools & Skills (Click to Toggle)</label>
                <div class="tool-chips">
                  {#each AVAILABLE_MCP_TOOLS as tool}
                    <button
                      type="button"
                      class="tool-chip"
                      class:active={(currentNode.config.tools || []).includes(tool.id)}
                      on:click={() => toggleAgentTool(tool.id)}
                    >
                      {(currentNode.config.tools || []).includes(tool.id) ? '✓ ' : '+ '}
                      {tool.label}
                    </button>
                  {/each}
                </div>
              </div>

              <div class="field-row">
                <div class="field half">
                  <label>Temperature</label>
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    max="1"
                    bind:value={currentNode.config.temperature}
                  />
                </div>
                <div class="field half">
                  <label>Max Tokens</label>
                  <input type="number" step="64" min="64" max="2048" bind:value={currentNode.config.maxTokens} />
                </div>
              </div>

            <!-- PROCESSING SKILL CONFIG -->
            {:else if currentNode.type === 'ProcessingSkill'}
              <div class="field">
                <label>Registered Skill / C# Clock</label>
                <select bind:value={currentNode.config.skillId}>
                  {#each AVAILABLE_SKILLS as skill}
                    <option value={skill.id}>{skill.name}</option>
                  {/each}
                </select>
              </div>

              {#if currentNode.config.skillId === 'sequence-aligner'}
                <div class="field">
                  <label>Reference Sequence 2</label>
                  <input
                    type="text"
                    bind:value={currentNode.config.sequence2}
                    placeholder="ATGGTGCACGTGACTCCTGTTGAGAAGTCTGCC"
                  />
                </div>
              {/if}

            <!-- LOOP NODE CONFIG -->
            {:else if currentNode.type === 'Loop'}
              <div class="field">
                <label>Iteration Count</label>
                <input type="number" min="1" max="10" bind:value={currentNode.config.iterations} />
              </div>
              <div class="field">
                <label>Loop Mode</label>
                <select bind:value={currentNode.config.mode}>
                  <option value="count">Count Iteration (1..N)</option>
                  <option value="array">Array Mapping</option>
                </select>
              </div>

            <!-- CONDITIONAL NODE CONFIG -->
            {:else if currentNode.type === 'Conditional'}
              <div class="field">
                <label>Branch Condition</label>
                <select bind:value={currentNode.config.condition}>
                  <option value="contains(ATG)">Contains Start Codon (ATG)</option>
                  <option value="length >= 20">Length &gt;= 20 Nucleotides</option>
                  <option value="gc >= 0.5">GC Content &gt;= 50%</option>
                  <option value="true">Always True</option>
                </select>
              </div>

            <!-- SUB-WORKFLOW CONFIG -->
            {:else if currentNode.type === 'SubWorkflow'}
              <div class="field">
                <label>Target Sub-Workflow</label>
                <select bind:value={currentNode.config.subWorkflowId}>
                  <option value="Bioinformatics-SubFlow-v1">Bioinformatics Aligner SubFlow</option>
                  <option value="Validation-SubFlow-v2">Genomic Quality Control SubFlow</option>
                  <option value="Copilot-Reasoning-SubFlow">Copilot Neural Reasoning SubFlow</option>
                </select>
              </div>

            <!-- CRON JOB CONFIG -->
            {:else if currentNode.type === 'CronJob'}
              <div class="field">
                <label>Execution Interval (Seconds)</label>
                <select bind:value={currentNode.config.intervalSeconds}>
                  <option value={10}>Every 10 seconds</option>
                  <option value={30}>Every 30 seconds</option>
                  <option value={60}>Every 1 minute</option>
                </select>
              </div>
              <div class="info-box">
                ⏰ Triggers downstream flow automatically at the specified interval.
              </div>

            <!-- STATE MACHINE CONFIG -->
            {:else if currentNode.type === 'StateMachine'}
              <div class="field">
                <label>Active FSM Layer</label>
                <select bind:value={currentNode.config.activeLayer}>
                  <option value="Base Layer">Base Layer</option>
                  <option value="QC Layer">QC Layer</option>
                  <option value="Inference Layer">Inference Layer</option>
                </select>
              </div>
              <div class="field">
                <label>Current State</label>
                <input type="text" bind:value={currentNode.config.currentState} />
              </div>
              <div class="field">
                <label>Target Transition State</label>
                <input type="text" bind:value={currentNode.config.targetState} />
              </div>

            <!-- SMART COMPONENT CONFIG -->
            {:else if currentNode.type === 'SmartComponent'}
              <div class="field">
                <label>Component Name</label>
                <input type="text" bind:value={currentNode.config.componentName} />
              </div>
              <div class="field">
                <label>Transformation Mode</label>
                <select bind:value={currentNode.config.transformMode}>
                  <option value="dna-enhance">DNA Enhance & Reverse Complement</option>
                  <option value="gc-filter">GC Filtering & Skew</option>
                  <option value="normalize">String Normalization</option>
                </select>
              </div>

            <!-- OUTPUT NODE CONFIG -->
            {:else if currentNode.type === 'Output'}
              <div class="field">
                <label>Console Print Prefix</label>
                <input type="text" bind:value={currentNode.config.prefix} placeholder=">> CONSOLE PRINT:" />
              </div>
              <div class="info-box">
                🖥️ Output is logged directly to backend terminal stdout and the Logs console page.
              </div>

            <!-- VALUE NODE CONFIG -->
            {:else if currentNode.type === 'Value'}
              <div class="field">
                <label>Constant Value</label>
                <input type="text" bind:value={currentNode.config.value} />
              </div>
            {/if}
          </div>

          <button
            type="button"
            class="danger"
            on:click={() => {
              nodes = nodes.filter((n) => n.id !== currentNode.id);
              connections = connections.filter(
                (c) => c.from !== currentNode.id && c.to !== currentNode.id
              );
              selectedId = null;
            }}
          >
            🗑 Delete Node
          </button>
        {:else if selectedEdge}
          <div class="section">
            <div class="section-title">Connection Properties</div>
            <div class="field">
              <label>Source Node</label>
              <div class="readonly">{selectedEdge.from || selectedEdge.sourceNodeId}</div>
            </div>
            <div class="field">
              <label>Target Node</label>
              <div class="readonly">{selectedEdge.to || selectedEdge.targetNodeId}</div>
            </div>
            <button
              type="button"
              class="danger"
              on:click={() => {
                connections = connections.filter((c) => c.id !== selectedEdge.id);
                selectedEdgeId = null;
              }}
            >
              🗑 Delete Connection
            </button>
          </div>
        {:else}
          <div class="hint">Click a node on the canvas to inspect and edit its properties.</div>
        {/if}
      </aside>
    </div>

  <!-- TAB 2: UNITY STYLE STATE MACHINE DESIGNER -->
  {:else if activeView === 'statemachine'}
    <div class="fsm-view">
      <div class="fsm-sidebar">
        <div class="fsm-section-title">FSM Layers (Unity-Style)</div>
        <div class="layers-list">
          {#each fsmLayers as l}
            <button
              class="layer-item"
              class:active={activeFsmLayer === l}
              on:click={() => (activeFsmLayer = l)}
            >
              🎮 {l}
            </button>
          {/each}
        </div>

        <div class="fsm-section-title" style="margin-top: 1.25rem;">Parameters & Events</div>
        <div class="params-list">
          <div class="param-row"><span>length</span><code>Int32 = 33</code></div>
          <div class="param-row"><span>gc_score</span><code>Float = 0.58</code></div>
          <div class="param-row"><span>isValid</span><code>Boolean = true</code></div>
          <div class="param-row"><span>trigger</span><code>Event = OnStart</code></div>
        </div>

        <button class="btn primary" style="margin-top: auto;" on:click={stepFsm}>
          ▶ Step State Transition
        </button>
      </div>

      <div class="fsm-canvas">
        <div class="fsm-header-bar">
          <span>Active Layer: <strong>{activeFsmLayer}</strong></span>
          <span class="active-state-tag">Active State: {fsmStates.find((s) => s.id === activeFsmState)?.name}</span>
        </div>

        <div class="fsm-graph">
          <svg class="fsm-svg" width="100%" height="100%">
            {#each fsmTransitions as t}
              {@const s1 = fsmStates.find((s) => s.id === t.from)}
              {@const s2 = fsmStates.find((s) => s.id === t.to)}
              {#if s1 && s2}
                <line
                  x1={s1.x + 80}
                  y1={s1.y + 30}
                  x2={s2.x}
                  y2={s2.y + 30}
                  stroke="#38bdf8"
                  stroke-width="2"
                  marker-end="url(#arrow)"
                />
              {/if}
            {/each}
          </svg>

          {#each fsmStates as state}
            <div
              class="fsm-node {state.type}"
              class:current={activeFsmState === state.id}
              style="left:{state.x}px; top:{state.y}px;"
              on:click={() => (activeFsmState = state.id)}
            >
              <div class="fsm-node-title">{state.name}</div>
              <div class="fsm-node-sub">{state.type.toUpperCase()}</div>
            </div>
          {/each}
        </div>
      </div>
    </div>

  <!-- TAB 3: SMART COMPONENT AUTHORING -->
  {:else if activeView === 'smart_component'}
    <div class="smart-comp-view">
      <div class="smart-comp-card">
        <h3>🧩 Author Reusable Smart Component</h3>
        <p class="subtitle">
          Construct custom reactive pipeline components conforming to Unity ECS entities, publishable to market_place.
        </p>

        <div class="comp-form">
          <div class="field">
            <label>Component Name</label>
            <input type="text" bind:value={compName} />
          </div>

          <div class="field-row">
            <div class="field half">
              <label>Input Entity Port</label>
              <select bind:value={compInputType}>
                {#each ENTITY_TYPES as et}
                  <option value={et}>{et}</option>
                {/each}
              </select>
            </div>
            <div class="field half">
              <label>Output Entity Port</label>
              <select bind:value={compOutputType}>
                {#each ENTITY_TYPES as et}
                  <option value={et}>{et}</option>
                {/each}
              </select>
            </div>
          </div>

          <div class="field">
            <label>Component Behavior Routine</label>
            <select bind:value={compTransformMode}>
              <option value="dna-enhance">DNA Enhance & Reverse Complement</option>
              <option value="gc-filter">GC Content & Codon Skew Filter</option>
              <option value="normalize">String Normalization</option>
            </select>
          </div>

          <button
            class="btn primary"
            on:click={() => {
              addNode('SmartComponent');
              activeView = 'canvas';
              message = `Smart Component "${compName}" instantiated on canvas!`;
            }}
          >
            🚀 Add Component to Canvas Flow
          </button>
        </div>
      </div>
    </div>
  {/if}
</div>

<style>
  .designer {
    display: flex;
    flex-direction: column;
    height: calc(100vh - 120px);
    background: #090e17;
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    overflow: hidden;
  }

  .designer-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.6rem 1rem;
    background: #0f172a;
    border-bottom: 1px solid var(--border);
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .brand-pill {
    background: rgba(14, 165, 233, 0.2);
    color: #38bdf8;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.72rem;
    font-weight: 700;
  }

  .view-switch-tabs {
    display: flex;
    gap: 0.4rem;
  }

  .view-tab {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    padding: 0.35rem 0.75rem;
    border-radius: 0.375rem;
    font-size: 0.78rem;
    cursor: pointer;
    font-weight: 500;
  }

  .view-tab.active {
    background: rgba(14, 165, 233, 0.2);
    border-color: #38bdf8;
    color: #38bdf8;
    font-weight: 700;
  }

  .header-right {
    display: flex;
    gap: 0.5rem;
  }

  .header-right button {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #e2e8f0;
    padding: 0.35rem 0.75rem;
    border-radius: 0.375rem;
    font-size: 0.8rem;
    cursor: pointer;
  }

  .header-right button.save {
    background: rgba(14, 165, 233, 0.2);
    border-color: rgba(14, 165, 233, 0.4);
    color: #38bdf8;
  }

  .header-right button.run {
    background: #10b981;
    border-color: #10b981;
    color: white;
    font-weight: 600;
  }

  .designer-subbar {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.35rem 1rem;
    background: rgba(15, 23, 42, 0.8);
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
    font-size: 0.75rem;
  }

  .preset-btn {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #cbd5e1;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.72rem;
    cursor: pointer;
  }

  .preset-btn:hover {
    background: rgba(255, 255, 255, 0.1);
  }

  .banner {
    padding: 0.45rem 1rem;
    font-size: 0.8rem;
  }

  .banner.info {
    background: rgba(14, 165, 233, 0.15);
    color: #38bdf8;
    border-bottom: 1px solid rgba(14, 165, 233, 0.3);
  }

  .banner.error {
    background: rgba(239, 68, 68, 0.15);
    color: #f87171;
    border-bottom: 1px solid rgba(239, 68, 68, 0.3);
  }

  .designer-body {
    display: flex;
    flex: 1;
    overflow: hidden;
  }

  /* PALETTE */
  .palette {
    width: 220px;
    background: #0c1220;
    border-right: 1px solid var(--border);
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  .palette-title {
    font-size: 0.82rem;
    font-weight: 700;
    color: #f1f5f9;
  }

  .palette-hint {
    font-size: 0.7rem;
    color: #64748b;
    margin-bottom: 0.35rem;
  }

  .palette-scroll {
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    flex: 1;
  }

  .palette-card {
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.375rem;
    padding: 0.45rem 0.6rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: grab;
    transition: all 0.15s;
  }

  .palette-card:hover {
    background: rgba(255, 255, 255, 0.05);
    border-color: rgba(255, 255, 255, 0.18);
  }

  .palette-icon {
    font-size: 1.25rem;
  }

  .palette-info {
    display: flex;
    flex-direction: column;
  }

  .palette-label {
    font-size: 0.78rem;
    color: #f1f5f9;
    font-weight: 600;
  }

  .palette-desc {
    font-size: 0.65rem;
    color: #64748b;
  }

  /* CANVAS */
  .canvas-wrap {
    flex: 1;
    position: relative;
    overflow: hidden;
  }

  .canvas {
    width: 100%;
    height: 100%;
    position: relative;
    overflow: auto;
    background-image: radial-gradient(rgba(255, 255, 255, 0.08) 1px, transparent 1px);
    background-size: 20px 20px;
  }

  .edges {
    position: absolute;
    top: 0;
    left: 0;
    width: 2500px;
    height: 2500px;
    pointer-events: none;
  }

  .edge-line {
    stroke: #38bdf8;
    stroke-width: 2.5;
    fill: none;
    transition: stroke 0.15s;
  }

  .edge-line.selected {
    stroke: #f59e0b;
    stroke-width: 3.5;
  }

  .connection-hit {
    stroke: transparent;
    stroke-width: 16;
    fill: none;
    pointer-events: stroke;
    cursor: pointer;
  }

  /* NODE CARD */
  .node {
    position: absolute;
    background: #0f172a;
    border: 1px solid;
    border-radius: 0.5rem;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
    cursor: move;
    user-select: none;
    display: flex;
    flex-direction: column;
    transition: box-shadow 0.15s;
  }

  .node.selected {
    box-shadow: 0 0 0 2px #38bdf8, 0 8px 24px rgba(56, 189, 248, 0.25);
  }

  .node-header {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.4rem 0.6rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  }

  .node-icon {
    font-size: 0.95rem;
  }

  .node-name {
    font-size: 0.78rem;
    font-weight: 600;
    color: #f1f5f9;
    flex: 1;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .node-type {
    font-size: 0.68rem;
    font-weight: 700;
  }

  .node-body {
    padding: 0.5rem 0.6rem;
    flex: 1;
  }

  .node-preview {
    font-size: 0.7rem;
    color: #94a3b8;
    font-family: ui-monospace, monospace;
    display: block;
    word-break: break-all;
  }

  .status-chip {
    position: absolute;
    bottom: -10px;
    right: 8px;
    padding: 0.1rem 0.4rem;
    border-radius: 9999px;
    font-size: 0.65rem;
    font-weight: 600;
    border: 1px solid;
  }

  /* PORTS */
  .port {
    position: absolute;
    border-radius: 50%;
    cursor: crosshair;
    transition: transform 0.15s;
  }

  .port.in {
    background: #38bdf8;
    box-shadow: 0 0 8px rgba(56, 189, 248, 0.6);
  }

  .port.out {
    background: #10b981;
    box-shadow: 0 0 8px rgba(16, 185, 129, 0.6);
  }

  .port:hover {
    transform: scale(1.4);
  }

  .pending {
    position: absolute;
    top: 15px;
    left: 50%;
    transform: translateX(-50%);
    background: rgba(14, 165, 233, 0.9);
    color: white;
    padding: 0.4rem 0.85rem;
    border-radius: 9999px;
    font-size: 0.8rem;
    font-weight: 600;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
    pointer-events: none;
  }

  /* INSPECTOR */
  .inspector {
    width: 320px;
    background: #0c1220;
    border-left: 1px solid var(--border);
    padding: 0.85rem;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
  }

  .inspector-title {
    font-size: 0.85rem;
    font-weight: 700;
    color: #f1f5f9;
  }

  .section {
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.375rem;
    padding: 0.65rem;
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
  }

  .section-title {
    font-size: 0.72rem;
    text-transform: uppercase;
    color: #64748b;
    font-weight: 700;
  }

  .connected-section {
    border-color: rgba(56, 189, 248, 0.25);
  }

  .conn-group {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .conn-label {
    font-size: 0.7rem;
    color: #94a3b8;
  }

  .conn-chips {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .conn-chip {
    padding: 0.2rem 0.45rem;
    border-radius: 0.25rem;
    font-size: 0.72rem;
    display: flex;
    justify-content: space-between;
  }

  .conn-chip.in {
    background: rgba(56, 189, 248, 0.15);
    color: #38bdf8;
    border: 1px solid rgba(56, 189, 248, 0.3);
  }

  .conn-chip.out {
    background: rgba(16, 185, 129, 0.15);
    color: #34d399;
    border: 1px solid rgba(16, 185, 129, 0.3);
  }

  .chip-type {
    font-size: 0.65rem;
    opacity: 0.8;
  }

  .conn-empty {
    font-size: 0.7rem;
    color: #64748b;
    font-style: italic;
  }

  .field {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .field label {
    font-size: 0.72rem;
    color: #94a3b8;
  }

  .field input,
  .field select,
  .field textarea {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 0.375rem;
    padding: 0.4rem 0.55rem;
    color: #f1f5f9;
    font-size: 0.8rem;
    font-family: inherit;
    box-sizing: border-box;
  }

  .type-badge-preview {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.7rem;
    color: #94a3b8;
    background: rgba(255, 255, 255, 0.04);
    padding: 0.3rem 0.5rem;
    border-radius: 0.25rem;
  }

  .type-tag {
    background: #0284c7;
    color: white;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
    font-weight: 700;
  }

  .field-row {
    display: flex;
    gap: 0.5rem;
  }

  .field.half {
    flex: 1;
  }

  .presets {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
  }

  .small-btn {
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(14, 165, 233, 0.3);
    color: #38bdf8;
    padding: 0.2rem 0.45rem;
    border-radius: 0.25rem;
    font-size: 0.7rem;
    cursor: pointer;
  }

  .tool-chips {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .tool-chip {
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.7rem;
    text-align: left;
    cursor: pointer;
  }

  .tool-chip.active {
    background: rgba(168, 85, 247, 0.2);
    border-color: #a855f7;
    color: #e9d5ff;
    font-weight: 600;
  }

  .info-box {
    background: rgba(16, 185, 129, 0.1);
    border: 1px solid rgba(16, 185, 129, 0.25);
    border-radius: 0.25rem;
    padding: 0.45rem;
    font-size: 0.72rem;
    color: #34d399;
  }

  .badge-type {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    font-weight: 600;
    border: 1px solid;
  }

  .readonly {
    padding: 0.4rem;
    background: rgba(255, 255, 255, 0.04);
    border-radius: 0.25rem;
    font-size: 0.75rem;
    color: #94a3b8;
    font-family: ui-monospace, monospace;
  }

  button.danger {
    background: rgba(239, 68, 68, 0.15);
    color: #f87171;
    border: 1px solid rgba(239, 68, 68, 0.4);
    padding: 0.4rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.78rem;
    font-weight: 600;
  }

  .hint-text {
    font-size: 0.65rem;
    color: #64748b;
  }

  .hint {
    color: #64748b;
    font-size: 0.8rem;
    font-style: italic;
    padding: 1rem 0;
  }

  /* UNITY STATE MACHINE TAB */
  .fsm-view {
    display: flex;
    flex: 1;
    overflow: hidden;
  }

  .fsm-sidebar {
    width: 250px;
    background: #0c1220;
    border-right: 1px solid var(--border);
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.65rem;
  }

  .fsm-section-title {
    font-size: 0.8rem;
    color: #94a3b8;
    font-weight: 700;
    text-transform: uppercase;
  }

  .layers-list {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .layer-item {
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.08);
    color: #cbd5e1;
    padding: 0.45rem 0.65rem;
    border-radius: 0.375rem;
    text-align: left;
    font-size: 0.8rem;
    cursor: pointer;
  }

  .layer-item.active {
    background: rgba(56, 189, 248, 0.15);
    border-color: #38bdf8;
    color: #38bdf8;
    font-weight: 700;
  }

  .params-list {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    background: #090e17;
    padding: 0.5rem;
    border-radius: 0.375rem;
    border: 1px solid rgba(255, 255, 255, 0.08);
  }

  .param-row {
    display: flex;
    justify-content: space-between;
    font-size: 0.72rem;
    color: #94a3b8;
  }

  .param-row code {
    color: #38bdf8;
  }

  .fsm-canvas {
    flex: 1;
    position: relative;
    overflow: hidden;
    background: #090e17;
  }

  .fsm-header-bar {
    display: flex;
    justify-content: space-between;
    padding: 0.5rem 1rem;
    background: rgba(15, 23, 42, 0.7);
    border-bottom: 1px solid var(--border);
    font-size: 0.8rem;
    color: #cbd5e1;
  }

  .active-state-tag {
    color: #4ade80;
    font-weight: 600;
  }

  .fsm-graph {
    position: relative;
    width: 100%;
    height: 100%;
  }

  .fsm-svg {
    position: absolute;
    inset: 0;
    pointer-events: none;
  }

  .fsm-node {
    position: absolute;
    width: 160px;
    height: 60px;
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.2);
    border-radius: 0.5rem;
    padding: 0.5rem;
    cursor: pointer;
    display: flex;
    flex-direction: column;
    justify-content: center;
    transition: all 0.15s;
  }

  .fsm-node.current {
    border-color: #4ade80;
    box-shadow: 0 0 16px rgba(74, 222, 128, 0.4);
    background: rgba(74, 222, 128, 0.08);
  }

  .fsm-node.entry {
    border-left: 4px solid #10b981;
  }

  .fsm-node.exit {
    border-left: 4px solid #ef4444;
  }

  .fsm-node-title {
    font-size: 0.75rem;
    font-weight: 700;
    color: #f1f5f9;
  }

  .fsm-node-sub {
    font-size: 0.65rem;
    color: #64748b;
  }

  /* SMART COMPONENT TAB */
  .smart-comp-view {
    padding: 2rem;
    display: flex;
    justify-content: center;
    overflow-y: auto;
  }

  .smart-comp-card {
    background: #0f172a;
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1.5rem;
    width: 600px;
    max-width: 100%;
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .smart-comp-card h3 {
    margin: 0;
    color: #f1f5f9;
  }

  .comp-form {
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
  }
</style>
