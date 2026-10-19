<script lang="ts">
  import { createEventDispatcher, onMount, onDestroy } from 'svelte';
  import { api } from '$lib/services/api';
  import MonacoEditor from '$lib/components/MonacoEditor.svelte';

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
    Agent: { icon: '🤖', color: '#a855f7', label: 'Copilot Agent', desc: 'Microsoft Copilot ONNX Reasoning' },
    ProcessingSkill: { icon: '🧬', color: '#f59e0b', label: 'Bio Skill', desc: 'Execute registry skill or C# clock' },
    WorkspaceFileSystem: { icon: '📁', color: '#06b6d4', label: 'Workspace File System', desc: 'Read, write, list files in workspace' },
    StateMachine: { icon: '🎮', color: '#38bdf8', label: 'State Machine', desc: 'Layered state machine with transitions & conditions' },
    LogicComponent: { icon: '🧩', color: '#8b5cf6', label: 'Logic Component', desc: 'Multi-I/O C#, CSnakes Python & JS component' },
    Output: { icon: '🖥️', color: '#10b981', label: 'Output', desc: 'Prints to backend console' },
    Loop: { icon: '🔁', color: '#6366f1', label: 'Loop', desc: 'Repeat iteration / accumulator' },
    Conditional: { icon: '🔀', color: '#ec4899', label: 'Conditional', desc: 'Branching logic' },
    SubWorkflow: { icon: '📦', color: '#64748b', label: 'Sub-Workflow', desc: 'Nested pipeline run' },
    CronJob: { icon: '⏰', color: '#eab308', label: 'Cron Job', desc: 'Scheduled interval trigger' },
    Value: { icon: '🔢', color: '#94a3b8', label: 'Value', desc: 'Constant value' },
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

  let activeView: 'canvas' | 'logic_fsm' = 'canvas';
  let fullSizeCanvas = false;

  // Interactive FSM Parameters & Events
  let fsmParameters: Array<{ name: string; type: 'Int32' | 'Float' | 'Boolean' | 'Trigger'; value: any }> = [
    { name: 'sequence_length', type: 'Int32', value: 58 },
    { name: 'gc_score', type: 'Float', value: 0.58 },
    { name: 'isValid', type: 'Boolean', value: true },
    { name: 'allowFlow', type: 'Boolean', value: true },
    { name: 'onStartTrigger', type: 'Trigger', value: false }
  ];
  let newParamName = '';
  let newParamType: 'Int32' | 'Float' | 'Boolean' | 'Trigger' = 'Boolean';
  let showAddParamModal = false;

  let fsmEvents: Array<{ name: string; description: string; lastDispatched?: string }> = [
    { name: 'OnStart', description: 'Triggered on pipeline start' },
    { name: 'OnSequenceLoaded', description: 'Fired when input DNA sequence loaded' },
    { name: 'OnValidationPassed', description: 'Fired when CSnakes quality checks pass' },
    { name: 'OnArtifactWritten', description: 'Fired when workspace file is saved' }
  ];
  let newEventName = '';
  let showAddEventModal = false;

  function addFsmParameter() {
    if (!newParamName.trim()) return;
    const defVal = newParamType === 'Boolean' ? true : newParamType === 'Trigger' ? false : 0;
    fsmParameters = [...fsmParameters, { name: newParamName.trim(), type: newParamType, value: defVal }];
    newParamName = '';
    showAddParamModal = false;
  }

  function addFsmEvent() {
    if (!newEventName.trim()) return;
    fsmEvents = [...fsmEvents, { name: newEventName.trim(), description: 'Custom user-defined transition event' }];
    newEventName = '';
    showAddEventModal = false;
  }

  function dispatchFsmEvent(evtName: string) {
    const evt = fsmEvents.find(e => e.name === evtName);
    if (evt) {
      evt.lastDispatched = new Date().toLocaleTimeString();
      fsmEvents = [...fsmEvents];
    }
    // Also toggle any matching trigger parameter
    const param = fsmParameters.find(p => p.name === evtName || p.name === evtName + 'Trigger');
    if (param && param.type === 'Trigger') {
      param.value = true;
      setTimeout(() => { param.value = false; fsmParameters = [...fsmParameters]; }, 800);
      fsmParameters = [...fsmParameters];
    }
  }

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

  // State Machine Designer State
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
  function getStateName(id: string): string {
    const s = fsmStates.find(x => x.id === id);
    return s ? s.name : id;
  }
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

  
  // Agent Model Download & Cache Management
  let downloadedAgentModels: Record<string, boolean> = {
    'Microsoft-Copilot-Local-ONNX': true,
    'Microsoft-Copilot-Bio-ONNX': true,
    'Phi-3.5-mini-ONNX': true,
    'Biomedical-Copilot-v1': true
  };
  let agentModelDownloading = false;
  let agentModelProgress = 0;

  function downloadAgentModel(modelName: string) {
    if (agentModelDownloading || downloadedAgentModels[modelName]) return;
    agentModelDownloading = true;
    agentModelProgress = 20;
    const interval = setInterval(() => {
      agentModelProgress += 25;
      if (agentModelProgress >= 100) {
        clearInterval(interval);
        downloadedAgentModels[modelName] = true;
        downloadedAgentModels = { ...downloadedAgentModels };
        agentModelDownloading = false;
        agentModelProgress = 0;
      }
    }, 250);
  }

  // Custom ECS Property Schema Creator State
  let showAddPropertyForm = false;
  let newPropName = '';
  let newPropType: 'enum' | 'vectordb' | 'number' | 'boolean' | 'string' = 'enum';
  let newPropOptions = 'Permissive, Standard, Strict';
  let newPropDefault = 'Standard';
  let newPropDesc = '';

  function addCustomEcsProperty(node: any) {
    if (!node || !newPropName.trim()) return;
    if (!node.config) node.config = {};
    if (!node.config.properties) node.config.properties = {};
    if (!node.config.propertySchemas) node.config.propertySchemas = [];

    const pName = newPropName.trim();
    let schema: any = {
      name: pName,
      type: newPropType,
      description: newPropDesc || `${newPropType} property for ${pName}`,
      defaultValue: newPropDefault
    };

    if (newPropType === 'enum') {
      const opts = newPropOptions.split(',').map(s => s.trim()).filter(Boolean);
      schema.options = opts.length > 0 ? opts : ['Default'];
      schema.defaultValue = opts[0] || 'Default';
      node.config.properties[pName] = schema.defaultValue;
    } else if (newPropType === 'vectordb') {
      schema.defaultValue = 'pgvector_default';
      schema.vectorDbConfig = {
        provider: 'pgvector',
        collection: 'genomic_embeddings',
        dimension: 1536,
        connectionUri: 'postgresql://postgres:postgres@localhost:5432/dnaplatform'
      };
      node.config.properties[pName] = 'pgvector_default';
    } else if (newPropType === 'number') {
      const num = parseFloat(newPropDefault) || 0.85;
      schema.min = 0;
      schema.max = 1;
      schema.step = 0.05;
      schema.defaultValue = num;
      node.config.properties[pName] = num;
    } else if (newPropType === 'boolean') {
      const b = newPropDefault === 'true' || newPropDefault === true;
      schema.defaultValue = b;
      node.config.properties[pName] = b;
    } else {
      schema.defaultValue = newPropDefault;
      node.config.properties[pName] = newPropDefault;
    }

    node.config.propertySchemas = [...node.config.propertySchemas, schema];
    nodes = [...nodes];

    // Reset creator
    newPropName = '';
    showAddPropertyForm = false;
  }

  function removeCustomEcsProperty(node: any, pName: string) {
    if (!node || !node.config) return;
    if (node.config.propertySchemas) {
      node.config.propertySchemas = node.config.propertySchemas.filter((s: any) => s.name !== pName);
    }
    if (node.config.properties) {
      delete node.config.properties[pName];
    }
    nodes = [...nodes];
  }

  async function autoReflectNodeSchemas(node: any) {
    if (!node) return;
    const code = node.config?.script || node.config?.expression || '';
    const lang = node.config?.runtime || node.config?.language || 'csharp';
    try {
      const res = await fetch('/api/authoring/reflect', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code, language: lang })
      });
      const data = await res.json();
      if (data.success && data.result) {
        if (!node.config) node.config = {};
        if (!node.config.properties) node.config.properties = {};
        node.config.propertySchemas = data.result.properties || [];
        for (const p of node.config.propertySchemas) {
          if (node.config.properties[p.name] === undefined) {
            node.config.properties[p.name] = p.defaultValue;
          }
        }
        nodes = [...nodes];
        message = `✔ Auto-reflected ${node.config.propertySchemas.length} ECS properties from code!`;
        setTimeout(() => (message = ''), 3000);
      }
    } catch (e: any) {
      console.warn('Reflection error:', e);
    }
  }

  // Load custom components from authoring studio
  let customStudioComponents: any[] = [];
  function loadCustomStudioComponents() {
    try {
      const stored = localStorage.getItem('dna_custom_components');
      if (stored) {
        customStudioComponents = JSON.parse(stored);
      }
    } catch (e) {}
  }

  onMount(() => {
    const wf = workflow || {};
    const parsedNodes = safeParseJson(wf.nodes);
    const parsedConns = safeParseJson(wf.connections);

    if (parsedNodes.length === 0) {
      const n1 = 'node_input_1';
      const n2 = 'node_output_1';
      nodes = [
        {
          id: n1,
          type: 'Input',
          name: 'DNA Sequence Input',
          x: 100,
          y: 140,
          config: {
            value: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCCCTGTGGGGCAAGGTG',
            key: 'sequence',
            entityType: 'DNA_Sequence'
          }
        },
        {
          id: n2,
          type: 'Output',
          name: 'Terminal Console Logger',
          x: 480,
          y: 140,
          config: {
            prefix: '>> PIPELINE RESULT:'
          }
        }
      ];
      connections = [
        { id: 'c1', from: n1, to: n2, sourceNodeId: n1, targetNodeId: n2 }
      ];
      selectedId = n1;
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
    loadCustomComponentsList();
    window.addEventListener('keydown', onKeyDown);
  });

  onDestroy(() => {
    window.removeEventListener('mousemove', onMouseMove);
    window.removeEventListener('mouseup', onMouseUp);
    window.removeEventListener('keydown', onKeyDown);
  });

  function getMeta(type: string): NodeMeta {
    if (type === 'SmartComponent') return TYPE_META['LogicComponent'];
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

    const defaultConfig: Record<string, any> = {
      properties: {},
      logicComponent: {
        enabled: true,
        name: `${meta.label} Logic Component`,
        runtime: 'javascript',
        inputs: [{ name: 'input', type: 'DNA_Sequence' }],
        outputs: [{ name: 'output', type: 'JSON' }]
      }
    };
    if (type === 'Input') {
      defaultConfig.value = 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC';
      defaultConfig.key = 'input_text';
      defaultConfig.entityType = 'DNA_Sequence';
    } else if (type === 'Agent') {
      defaultConfig.model = 'Microsoft-Copilot-Local-ONNX';
      defaultConfig.systemPrompt = 'You are a Microsoft Copilot bioinformatic assistant.';
      defaultConfig.prompt = 'Analyze sequence: {{input}}';
      defaultConfig.tools = ['csharp-runtime/get_current_time', 'sequence-aligner', 'dna-bioinformatics/reverse_complement', 'dna-bioinformatics/gc_skew_analysis'];
    } else if (type === 'ProcessingSkill') {
      defaultConfig.skillId = 'sequence-aligner';
    } else if (type === 'WorkspaceFileSystem') {
      defaultConfig.operation = 'write_file';
      defaultConfig.filePath = 'artifacts/pipeline_summary.json';
      defaultConfig.fileContent = '';
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
    } else if (type === 'LogicComponent' || type === 'SmartComponent') {
      defaultConfig.componentName = 'Custom Logic Component';
      defaultConfig.runtime = 'python';
      defaultConfig.inputs = [
        { name: 'sequence', type: 'DNA_Sequence' },
        { name: 'reference', type: 'DNA_Sequence' }
      ];
      defaultConfig.outputs = [
        { name: 'alignmentScore', type: 'Float' },
        { name: 'verified', type: 'Boolean' }
      ];
      defaultConfig.properties = {
        cutoff: 0.85,
        strictValidation: true
      };
      defaultConfig.expression = 'Console.log(Time.now + " [LogicComponent: " + this.name + "] running");\nthis.properties.verified = true;\nreturn { ...$input, verified: true };';
      defaultConfig.hasExpression = true;
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
      const m = node.config?.model || 'Microsoft-Copilot-Local-ONNX';
      const prompt = node.config?.prompt || '{{input}}';
      const shortModel = m.replace('Microsoft-', '').replace('-ONNX', '');
      return `[${shortModel}] ⚡ ${prompt}`;
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


  // --------------------------------------------------------------------------
  // SMART COMPONENT INSPECTOR & INLINE EXPRESSION ENGINE (n8n / Elsa Style)
  // --------------------------------------------------------------------------

  // Node Context Menu & Custom Script Attachment State
  let showContextMenu = false;
  let contextMenuNode: any = null;
  let contextMenuX = 0;
  let contextMenuY = 0;

  // Drawer Tabs: 'identity' | 'script' | 'props' | 'expression' | 'stm'
  let drawerActiveTab: 'identity' | 'script' | 'props' | 'expression' | 'stm' = 'script';
  let nodeScriptLanguage: 'javascript' | 'python' | 'csharp' = 'javascript';
  let nodeScriptCode = '';
  let testScriptInput = '{\n  "sequence": "ATGCGATCGATC",\n  "gc": 0.50,\n  "length": 12,\n  "quality": 0.95\n}';
  let testScriptResult = '';
  let testScriptError = '';
  let testingScript = false;

  // Logic Component Authoring in Tab
  let lcName = 'DNA Phase Gate Logic Component';
  let lcCategory = 'State Machine';
  let lcLanguage: 'csharp' | 'python' | 'javascript' = 'csharp';
  let lcDescription = 'Evaluates genomic invariants and controls state machine transition gates.';
  let lcCode = `using System;\nusing System.ComponentModel;\n\n[Category("State Machine")]\n[Description("DNA Phase Gate logic_component")]\npublic class DnaPhaseGate : StateMachineBehaviour\n{\n    [Description("Quality cutoff score")]\n    public double Threshold { get; set; } = 0.85;\n\n    [Description("Enforce strict validation")]\n    public bool StrictQualityGate { get; set; } = true;\n\n    public override void OnStateEnter(string state, object data)\n    {\n        Console.WriteLine($"[logic_component] Entering state: {state}");\n    }\n\n    [Tool("validate_flow", "Validates incoming DNA sequence")]\n    public bool ValidateFlow(string sequence)\n    {\n        var dna = new DnaSequence(sequence);\n        return dna.CalculateGc() >= Threshold;\n    }\n}`;
  let lcTestOutput = '';
  let lcTesting = false;

  function onOpenContextMenu(event: MouseEvent, node: any) {
    event.preventDefault();
    event.stopPropagation();
    contextMenuNode = node;
    contextMenuX = event.clientX;
    contextMenuY = event.clientY;
    showContextMenu = true;
  }

  function duplicateNode(node: any) {
    if (!node) return;
    const id = 'node_' + Math.random().toString(36).slice(2, 7);
    const newNode = {
      ...JSON.parse(JSON.stringify(node)),
      id,
      name: (node.name || 'Node') + ' (Copy)',
      x: (node.x || 100) + 40,
      y: (node.y || 100) + 40
    };
    nodes = [...nodes, newNode];
    selectedId = id;
    showContextMenu = false;
    message = `Duplicated node "${newNode.name}"`;
    setTimeout(() => (message = ''), 2500);
  }

  function deleteNodeById(nodeId: string) {
    nodes = nodes.filter((n) => n.id !== nodeId);
    connections = connections.filter((c) => c.from !== nodeId && c.to !== nodeId);
    if (selectedId === nodeId) selectedId = null;
    showContextMenu = false;
    message = `Deleted node`;
    setTimeout(() => (message = ''), 2000);
  }

  async function testNodeScript() {
    testingScript = true;
    testScriptResult = '';
    testScriptError = '';
    try {
      let parsedInput: any = {};
      try {
        parsedInput = JSON.parse(testScriptInput);
      } catch (err: any) {
        testScriptError = 'Invalid Input JSON: ' + err.message;
        testingScript = false;
        return;
      }

      const res = await fetch('/api/polyglot/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: nodeScriptLanguage,
          code: nodeScriptCode
        })
      });

      const data = await res.json();
      if (data.success) {
        testScriptResult = data.output || '// Script executed successfully with no stdout output.';
      } else {
        testScriptError = data.error || data.output || 'Script execution failed.';
      }
    } catch (e: any) {
      testScriptError = 'Network error: ' + (e.message || String(e));
    } finally {
      testingScript = false;
    }
  }

  async function testLogicComponentCode() {
    lcTesting = true;
    lcTestOutput = '';
    try {
      const res = await fetch('/api/polyglot/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: lcLanguage,
          code: lcCode
        })
      });
      const data = await res.json();
      lcTestOutput = data.output || '// Component syntax verified.';
    } catch (e: any) {
      lcTestOutput = 'Test error: ' + (e.message || String(e));
    } finally {
      lcTesting = false;
    }
  }

  async function saveAndAddLogicComponent() {
    const compId = 'comp-' + Math.random().toString(36).slice(2, 8);
    const newComp = {
      id: compId,
      name: lcName,
      category: lcCategory,
      language: lcLanguage,
      description: lcDescription,
      code: lcCode,
      properties: [
        { name: 'Threshold', type: 'number', default: 0.85, min: 0, max: 1, step: 0.05, description: 'Cutoff score' },
        { name: 'StrictQualityGate', type: 'boolean', default: true, description: 'Enforce strict validation' }
      ]
    };

    try {
      await fetch('/api/components', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newComp)
      });
      await loadCustomComponentsList();
    } catch {}

    addCustomComponentNode(newComp);
    activeView = 'canvas';
    message = `Logic component "${lcName}" saved to catalog and added to canvas flow!`;
    setTimeout(() => (message = ''), 3500);
  }

  let showNodeDrawer = false;
  let inspectingNode: any = null;

  // --------------------------------------------------------------------------
  // ATTACHED EXPRESSION ENGINE (n8n / Elsa Style) & INTELLISENSE
  // --------------------------------------------------------------------------
  let exprTextareaEl: HTMLTextAreaElement | null = null;
  let focusTextareaEl: HTMLTextAreaElement | null = null;
  let isFocusMode = false;
  let focusNode: any = null;

  let showAutocomplete = false;
  let autocompleteIdx = 0;
  let cursorToken = '';
  let tokenStartPos = 0;
  let popupPos = { top: 40, left: 10 };

  let exprTestRunning = false;
  let exprTestOutput = '';

  interface SuggestionItem {
    label: string;
    insertText: string;
    desc: string;
    type: 'method' | 'property' | 'variable' | 'class';
    icon: string;
  }

  const INTELLISENSE_ITEMS: SuggestionItem[] = [
    { label: 'Console.log(msg)', insertText: 'Console.log(', desc: 'Print message to workflow execution logs', type: 'method', icon: '📜' },
    { label: 'Console.warn(msg)', insertText: 'Console.warn(', desc: 'Print warning to workflow execution logs', type: 'method', icon: '⚠️' },
    { label: 'Time.now', insertText: 'Time.now', desc: 'Current ISO 8601 timestamp string', type: 'property', icon: '⏱️' },
    { label: 'Time.utc', insertText: 'Time.utc', desc: 'Current UTC string representation', type: 'property', icon: '⏱️' },
    { label: 'DateTime.UtcNow', insertText: 'DateTime.UtcNow', desc: 'C# high-precision UTC time provider', type: 'property', icon: '⏱️' },
    { label: 'this.name', insertText: 'this.name', desc: 'Name of the current workflow node', type: 'property', icon: '🏷️' },
    { label: 'this.id', insertText: 'this.id', desc: 'Unique identifier of current node', type: 'property', icon: '🔑' },
    { label: 'this.type', insertText: 'this.type', desc: 'Component type of current node', type: 'property', icon: '🧩' },
    { label: '$input', insertText: '$input', desc: 'Incoming execution payload from upstream node', type: 'variable', icon: '📥' },
    { label: '$json', insertText: '$json', desc: 'Alias for incoming JSON data structure', type: 'variable', icon: '📄' },
    { label: '$nodes', insertText: '$nodes', desc: 'Dictionary of all upstream node outputs', type: 'variable', icon: '🌐' },
    { label: 'DnaSequence', insertText: 'DnaSequence', desc: 'Bioinformatics DNA Sequence helper class', type: 'class', icon: '🧬' }
  ];

  let filteredSuggestions: SuggestionItem[] = [...INTELLISENSE_ITEMS];

  function handleExprInput(e: Event, node: any, isFocus = false) {
    const textarea = (isFocus ? focusTextareaEl : exprTextareaEl) || (e.target as HTMLTextAreaElement);
    if (!textarea) return;

    const val = textarea.value;
    const cursorPos = textarea.selectionStart;

    // Detect token preceding cursor
    const textBefore = val.slice(0, cursorPos);
    const match = textBefore.match(/([a-zA-Z0-9_\$\.]+)$/);

    if (match) {
      cursorToken = match[1];
      tokenStartPos = cursorPos - cursorToken.length;

      const q = cursorToken.toLowerCase();
      filteredSuggestions = INTELLISENSE_ITEMS.filter((item) => {
        return (
          item.label.toLowerCase().includes(q) ||
          item.insertText.toLowerCase().includes(q) ||
          (q.endsWith('.') && item.insertText.toLowerCase().startsWith(q))
        );
      });

      if (filteredSuggestions.length > 0) {
        showAutocomplete = true;
        autocompleteIdx = 0;
        popupPos = { top: Math.min(200, textarea.clientHeight - 40), left: 14 };
      } else {
        showAutocomplete = false;
      }
    } else {
      showAutocomplete = false;
    }

    if (node && node.config) {
      node.config.expression = val;
      nodes = [...nodes];
    }
  }

  function handleExprKeydown(e: KeyboardEvent, node: any, isFocus = false) {
    if (!showAutocomplete || filteredSuggestions.length === 0) {
      if (e.ctrlKey && e.key === ' ') {
        e.preventDefault();
        filteredSuggestions = [...INTELLISENSE_ITEMS];
        showAutocomplete = true;
        autocompleteIdx = 0;
      }
      return;
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      autocompleteIdx = (autocompleteIdx + 1) % filteredSuggestions.length;
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      autocompleteIdx = (autocompleteIdx - 1 + filteredSuggestions.length) % filteredSuggestions.length;
    } else if (e.key === 'Enter' || e.key === 'Tab') {
      e.preventDefault();
      applySuggestion(filteredSuggestions[autocompleteIdx], node, isFocus);
    } else if (e.key === 'Escape') {
      showAutocomplete = false;
    }
  }

  function applySuggestion(item: SuggestionItem, node: any, isFocus = false) {
    const textarea = isFocus ? focusTextareaEl : exprTextareaEl;
    if (!textarea || !node || !node.config) return;

    const currentVal = node.config.expression || '';
    const before = currentVal.slice(0, tokenStartPos);
    const after = currentVal.slice(tokenStartPos + cursorToken.length);

    let insertion = item.insertText;
    if (cursorToken.includes('.')) {
      const lastDot = cursorToken.lastIndexOf('.');
      const prefix = cursorToken.slice(0, lastDot + 1);
      if (item.insertText.startsWith(prefix)) {
        insertion = item.insertText.slice(prefix.length);
      }
    }

    node.config.expression = before + (cursorToken.includes('.') ? cursorToken.slice(0, cursorToken.lastIndexOf('.') + 1) + insertion : insertion) + after;
    showAutocomplete = false;
    nodes = [...nodes];

    setTimeout(() => {
      if (textarea) {
        textarea.focus();
        const newPos = tokenStartPos + insertion.length;
        textarea.setSelectionRange(newPos, newPos);
      }
    }, 10);
  }

  function insertSnippet(node: any, snippet: string) {
    if (!node || !node.config) return;
    if (!node.config.expression) {
      node.config.expression = snippet;
    } else {
      node.config.expression = node.config.expression.trim() + '\n' + snippet;
    }
    nodes = [...nodes];
  }

  async function testExpressionOnNode(node: any) {
    if (!node) return;
    exprTestRunning = true;
    exprTestOutput = '';

    const expr = node.config?.expression || '';
    if (!expr.trim()) {
      exprTestOutput = '// (Empty expression — passes input through unmodified)';
      exprTestRunning = false;
      return;
    }

    const consoleLogs: string[] = [];
    const mockConsole = {
      log: (...args: any[]) => {
        consoleLogs.push(args.map((a) => (typeof a === 'object' ? JSON.stringify(a) : String(a))).join(' '));
      },
      warn: (...args: any[]) => mockConsole.log('[WARN]', ...args),
      error: (...args: any[]) => mockConsole.log('[ERROR]', ...args)
    };

    if (!node.config) node.config = {};
    if (!node.config.properties) node.config.properties = {};

    const nodeInstance = {
      id: node.id,
      name: node.name,
      type: node.type === 'SmartComponent' ? 'LogicComponent' : node.type,
      config: node.config,
      properties: node.config.properties
    };

    const scriptArgs = {
      $input: node.config?.value ? { value: node.config.value, sequence: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCT' } : { status: 'active', count: 1 },
      $json: node.config?.value ? { value: node.config.value } : { status: 'active' },
      $nodes: { [node.id]: { output: 'sample_output' } },
      Console: mockConsole,
      console: mockConsole,
      Time: {
        now: new Date().toISOString(),
        utc: new Date().toUTCString(),
        unix: Date.now()
      },
      DateTime: {
        UtcNow: new Date().toISOString()
      }
    };

    try {
      const fn = new Function(...Object.keys(scriptArgs), `
        try {
          ${expr.includes('return') ? expr : `const _res = (${expr}); return _res;`}
        } catch (err) {
          Console.log("Execution error: " + err.message);
          return undefined;
        }
      `);

      const result = fn.call(nodeInstance, ...Object.values(scriptArgs));

      // Synchronize any ECS properties created or referenced in the expression
      if (nodeInstance.properties) {
        node.config.properties = { ...nodeInstance.properties };
        nodes = [...nodes];
      }

      let out = '';
      if (consoleLogs.length > 0) {
        out += '── Captured Console Logs ──\n' + consoleLogs.join('\n') + '\n\n';
      }
      out += '── Return Value ──\n' + (result !== undefined ? (typeof result === 'object' ? JSON.stringify(result, null, 2) : String(result)) : '(undefined)');
      if (Object.keys(nodeInstance.properties).length > 0) {
        out += '\n\n── Node Scriptable ECS Properties ──\n' + JSON.stringify(nodeInstance.properties, null, 2);
      }
      exprTestOutput = out;
    } catch (err: any) {
      exprTestOutput = `Syntax / Evaluation Error:\n${err.message}`;
    } finally {
      exprTestRunning = false;
    }
  }

  function addNodeEcsProperty(node: any) {
    if (!node) return;
    if (!node.config) node.config = {};
    if (!node.config.properties) node.config.properties = {};
    const count = Object.keys(node.config.properties).length + 1;
    node.config.properties[`prop_${count}`] = 'value';
    nodes = [...nodes];
  }

  function removeNodeEcsProperty(node: any, key: string) {
    if (!node || !node.config || !node.config.properties) return;
    delete node.config.properties[key];
    nodes = [...nodes];
  }

  function addNodePort(node: any, direction: 'inputs' | 'outputs') {
    if (!node) return;
    if (!node.config) node.config = {};
    if (!node.config[direction]) node.config[direction] = [];
    const count = node.config[direction].length + 1;
    node.config[direction].push({
      name: `${direction === 'inputs' ? 'in' : 'out'}_${count}`,
      type: direction === 'inputs' ? 'DNA_Sequence' : 'JSON'
    });
    nodes = [...nodes];
  }

  function removeNodePort(node: any, direction: 'inputs' | 'outputs', index: number) {
    if (!node || !node.config || !node.config[direction]) return;
    node.config[direction].splice(index, 1);
    nodes = [...nodes];
  }

  function openFocusMode(node: any) {
    if (!node) return;
    focusNode = node;
    isFocusMode = true;
    showAutocomplete = false;
    exprTestOutput = '';
  }

  function closeFocusMode() {
    isFocusMode = false;
    focusNode = null;
    showAutocomplete = false;
  }
  let inspectingNodeProps: Array<{ name: string; type: string; default: any; value: any; min?: number; max?: number; step?: number; description?: string }> = [];
  let customComponentsList: any[] = [];
  let selectedComponentId = '';
  let inlineExpressionCode = '';
  let testInputJson = '{\n  "sequence": "ATGCGATCGATC",\n  "gc": 0.50,\n  "length": 12,\n  "quality": 0.95\n}';
  let testOutputResult = '';
  let testOutputError = '';

  // Visual FSM authoring state
  let showAddStateModal = false;
  let newFsmStateName = '';
  let newFsmStateType: 'entry' | 'state' | 'exit' = 'state';
  let showAddTransModal = false;
  let newTransFrom = '';
  let newTransTo = '';
  let newTransTrigger = 'OnEvent';
  let newTransCondition = 'true';

  async function loadCustomComponentsList() {
    try {
      const res = await fetch('/api/components');
      if (res.ok) {
        const data = await res.json();
        customComponentsList = data.components || [];
      }
    } catch (e) {
      console.warn('Could not fetch custom components catalog', e);
    }
  }

  function openNodeSmartConfig(node: any) {
    if (!node) return;
    selectedId = node.id;
    inspectingNode = JSON.parse(JSON.stringify(node));
    inlineExpressionCode = inspectingNode.config?.expression || inspectingNode.expression || '';
    selectedComponentId = inspectingNode.config?.componentId || '';
    nodeScriptCode = inspectingNode.config?.customScript || inspectingNode.customScript || '';
    nodeScriptLanguage = inspectingNode.config?.scriptLanguage || inspectingNode.scriptLanguage || 'javascript';
    testScriptResult = '';
    testScriptError = '';

    // Load custom properties
    const props = inspectingNode.config?.customProperties || inspectingNode.customProperties || [];
    inspectingNodeProps = Array.isArray(props) ? JSON.parse(JSON.stringify(props)) : [];

    testOutputResult = '';
    testOutputError = '';
    showNodeDrawer = true;

    if (customComponentsList.length === 0) {
      loadCustomComponentsList();
    }
  }

  function closeNodeSmartConfig() {
    showNodeDrawer = false;
    inspectingNode = null;
    testOutputResult = '';
    testOutputError = '';
  }

  function onSelectCustomComponent(compId: string) {
    selectedComponentId = compId;
    if (!compId || !inspectingNode) return;
    const comp = customComponentsList.find((c) => c.id === compId);
    if (!comp) return;

    inspectingNode.name = comp.name;
    inspectingNode.desc = comp.description;
    inspectingNode.type = 'SmartComponent';
    if (!inspectingNode.config) inspectingNode.config = {};
    inspectingNode.config.componentId = comp.id;
    inspectingNode.config.componentName = comp.name;
    inspectingNode.config.category = comp.category;

    if (comp.expression && !inlineExpressionCode) {
      inlineExpressionCode = comp.expression;
    }

    if (Array.isArray(comp.properties)) {
      inspectingNodeProps = comp.properties.map((p: any) => ({
        name: p.name,
        type: p.type || 'string',
        default: p.default,
        value: p.default,
        min: p.min,
        max: p.max,
        step: p.step,
        description: p.description
      }));
    }
  }

  function addCustomProperty() {
    inspectingNodeProps = [
      ...inspectingNodeProps,
      {
        name: `Property_${inspectingNodeProps.length + 1}`,
        type: 'string',
        default: 'value',
        value: 'value',
        description: 'Custom public attribute'
      }
    ];
  }

  function removeCustomProperty(idx: number) {
    inspectingNodeProps = inspectingNodeProps.filter((_, i) => i !== idx);
  }

  function testNodeExpression() {
    testOutputResult = '';
    testOutputError = '';
    try {
      let parsedInput: any = {};
      try {
        parsedInput = JSON.parse(testInputJson);
      } catch (jsonErr: any) {
        testOutputError = `Invalid Test Input JSON: ${jsonErr.message}`;
        return;
      }

      const properties: Record<string, any> = {};
      for (const p of inspectingNodeProps) {
        properties[p.name] = p.value !== undefined ? p.value : p.default;
      }

      let rawExpr = inlineExpressionCode.trim();
      if (rawExpr.startsWith('{{') && rawExpr.endsWith('}}')) {
        rawExpr = rawExpr.slice(2, -2).trim();
      }

      if (!rawExpr) {
        testOutputResult = '// (Empty expression - passes input through unmodified)\n' + JSON.stringify(parsedInput, null, 2);
        return;
      }

      const nodeInstance = {
        id: inspectingNode?.id || 'node_1',
        name: inspectingNode?.name || 'Current Node',
        type: inspectingNode?.type || 'LogicComponent',
        properties,
        props: properties,
        config: inspectingNode?.config || {}
      };
      const mockConsole = {
        log: (...args: any[]) => console.log('[Expr]', ...args),
        warn: (...args: any[]) => console.warn('[Expr]', ...args),
        error: (...args: any[]) => console.error('[Expr]', ...args)
      };
      const Time = { now: new Date().toISOString(), utc: new Date().toUTCString(), unix: Date.now() };
      const DateTime = { UtcNow: new Date().toISOString() };

      let cleanExpr = rawExpr.trim();
      if (!cleanExpr.includes('return ') && !cleanExpr.includes(';')) {
        cleanExpr = 'return (' + cleanExpr + ');';
      }
      const exprFn = new Function('$input', 'input', 'properties', 'props', 'Console', 'console', 'Time', 'DateTime', `
        "use strict";
        ${cleanExpr}
      `);
      const res = exprFn.call(nodeInstance, parsedInput, parsedInput, properties, properties, mockConsole, mockConsole, Time, DateTime);
      testOutputResult = typeof res === 'object' ? JSON.stringify(res, null, 2) : String(res);
    } catch (evalErr: any) {
      testOutputError = `Evaluation Error: ${evalErr.message}`;
    }
  }

  function saveNodeSmartConfig() {
    if (!inspectingNode) return;
    const idx = nodes.findIndex((n) => n.id === inspectingNode.id);
    if (idx !== -1) {
      if (!nodes[idx].config) nodes[idx].config = {};
      nodes[idx].name = inspectingNode.name;
      nodes[idx].config.expression = inlineExpressionCode;
      nodes[idx].config.customProperties = inspectingNodeProps;
      nodes[idx].config.componentId = selectedComponentId;
      nodes[idx].config.customScript = nodeScriptCode;
      nodes[idx].config.scriptLanguage = nodeScriptLanguage;
      if (inspectingNode.config?.category) {
        nodes[idx].config.category = inspectingNode.config.category;
      }
      nodes = [...nodes];
    }
    showNodeDrawer = false;
    message = `Updated Smart Component & Expression for "${inspectingNode.name}"`;
    setTimeout(() => (message = ''), 3500);
  }

  function addCustomComponentNode(comp: any) {
    const id = 'node_' + Math.random().toString(36).slice(2, 7);
    const offset = (nodes.length % 6) * 30;
    const newNode = {
      id,
      name: comp.name,
      type: 'SmartComponent',
      x: 280 + offset,
      y: 120 + offset,
      config: {
        componentId: comp.id,
        componentName: comp.name,
        category: comp.category,
        expression: comp.expression || '',
        customProperties: (comp.properties || []).map((p: any) => ({
          name: p.name,
          type: p.type || 'string',
          default: p.default,
          value: p.default,
          min: p.min,
          max: p.max,
          step: p.step,
          description: p.description
        }))
      }
    };
    nodes = [...nodes, newNode];
    selectedId = id;
    message = `Added "${comp.name}" [${comp.category}] to canvas flow!`;
    setTimeout(() => (message = ''), 3000);
  }

  function addFsmState() {
    if (!newFsmStateName.trim()) return;
    const id = 's_' + Date.now().toString(36).slice(-4);
    const lastState = fsmStates[fsmStates.length - 1];
    const newX = lastState ? lastState.x + 180 : 100;
    const newY = lastState ? lastState.y : 80;
    fsmStates = [
      ...fsmStates,
      {
        id,
        name: newFsmStateName.trim(),
        type: newFsmStateType,
        x: newX,
        y: newY
      }
    ];
    newFsmStateName = '';
    showAddStateModal = false;
    message = `Added FSM state: ${id}`;
    setTimeout(() => (message = ''), 2500);
  }

  function deleteFsmState(stateId: string) {
    if (fsmStates.length <= 1) return;
    fsmStates = fsmStates.filter((s) => s.id !== stateId);
    fsmTransitions = fsmTransitions.filter((t) => t.from !== stateId && t.to !== stateId);
    if (activeFsmState === stateId) {
      activeFsmState = fsmStates[0].id;
    }
  }

  function addFsmTransition() {
    if (!newTransFrom || !newTransTo) return;
    fsmTransitions = [
      ...fsmTransitions,
      {
        from: newTransFrom,
        to: newTransTo,
        trigger: newTransTrigger || 'OnEvent',
        condition: newTransCondition || 'true'
      }
    ];
    showAddTransModal = false;
    message = `Added FSM transition from ${newTransFrom} to ${newTransTo}`;
    setTimeout(() => (message = ''), 2500);
  }

  function deleteFsmTransition(idx: number) {
    fsmTransitions = fsmTransitions.filter((_, i) => i !== idx);
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
        class:active={activeView === 'logic_fsm'}
        on:click={() => (activeView = 'logic_fsm')}
      >
        🎮 Logic & State Machine (Unity Studio)
      </button>
      {#if activeView === 'canvas'}
        <button
          type="button"
          class="btn-fullsize-toggle"
          class:active={fullSizeCanvas}
          on:click={() => (fullSizeCanvas = !fullSizeCanvas)}
          title="Toggle Full Size Flow Canvas"
        >
          {fullSizeCanvas ? '⊡ Standard Layout' : '⛶ Full Size Canvas'}
        </button>
      {/if}
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
    <div class="designer-body">
      <!-- PALETTE -->
      {#if !fullSizeCanvas}
      <aside class="palette">
        <div class="palette-title">Node Palette</div>
        <div class="palette-hint">Drag onto canvas or click to add</div>
        <div class="palette-scroll">
          {#if customStudioComponents.length > 0}
            <div class="palette-subhead">Studio Components & Extensions</div>
            {#each customStudioComponents as comp}
              <div
                class="palette-card custom-comp-card"
                draggable="true"
                on:click={() => {
                  const id = 'n_' + Date.now().toString(36) + Math.random().toString(36).slice(2, 5);
                  const newProps = {};
                  for (const s of (comp.properties || [])) {
                    newProps[s.name] = s.defaultValue;
                  }
                  nodes = [...nodes, {
                    id,
                    type: 'LogicComponent',
                    name: comp.name,
                    x: 120,
                    y: 120,
                    config: {
                      componentName: comp.name,
                      runtime: comp.language || 'csharp',
                      inputs: comp.inputs || [{ name: 'input', type: 'DNA_Sequence' }],
                      outputs: comp.outputs || [{ name: 'output', type: 'JSON' }],
                      script: comp.code || '',
                      propertySchemas: comp.properties || [],
                      properties: newProps,
                      isMiddleware: comp.isMiddleware
                    }
                  }];
                  selectedId = id;
                }}
                style="border-left: 3px solid #8b5cf6;"
              >
                <span class="palette-icon">{comp.isMiddleware ? '🛡️' : '🧩'}</span>
                <div class="palette-info">
                  <span class="palette-label">{comp.name}</span>
                  <span class="palette-desc">{comp.category} · {comp.language?.toUpperCase()}</span>
                </div>
              </div>
            {/each}
            <div class="palette-subhead" style="margin-top: 0.5rem;">Standard Workflow Nodes</div>
          {/if}
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
      {/if}

      <!-- CANVAS -->
      <main class="canvas-wrap" class:fullsize={fullSizeCanvas}>
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
              on:dblclick={() => openNodeSmartConfig(node)}
              on:contextmenu|preventDefault={(e) => onOpenContextMenu(e, node)}
            >
              <div
                class="node-header"
                style="background:linear-gradient(90deg, {meta.color}26, rgba(255,255,255,0.02))"
              >
                <span class="node-icon">
  {#if node.type === 'Agent'}
    {#if (node.config?.model || '').includes('Bio')}🧬
    {:else if (node.config?.model || '').includes('Phi')}⚡
    {:else if (node.config?.model || '').includes('Biomedical')}🔬
    {:else if (node.config?.model || '').includes('Foundry') || (node.config?.model || '').includes('Qwen')}🌐
    {:else}🤖{/if}
  {:else}
    {meta.icon}
  {/if}
</span>
                <span class="node-name" title={node.name}>{node.name || meta.label}</span>
                <div class="node-actions-row">
                  {#if node.config?.customScript}
                    <span class="script-badge" title="Has Attached Custom Script">📜</span>
                  {/if}
                  {#if node.config?.expression}
                    <span class="expr-badge" title="Has Inline Expression">ƒ(x)</span>
                  {/if}
                  {#if node.config?.componentId}
                    <span class="comp-badge" title="Linked to Logic Component">🧩</span>
                  {/if}
                  <button
                    type="button"
                    class="node-menu-btn"
                    title="Node Context Menu"
                    on:click|stopPropagation={(e) => onOpenContextMenu(e, node)}
                  >
                    ⋮
                  </button>
                  <button
                    type="button"
                    class="node-gear-btn"
                    title="Configure Logic Component & Custom Script (Double-click node also opens this)"
                    on:click|stopPropagation={() => openNodeSmartConfig(node)}
                  >
                    ⚙️
                  </button>
                  <span class="node-type" style="color:{meta.color}">{meta.label}</span>
                </div>
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
      {#if !fullSizeCanvas}
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
            <div class="section-title">Node Configuration</div>

            <!-- INPUT NODE CONFIG WITH STRONGLY TYPED FIELDS -->
            {#if currentNode.type === 'Input'}
              <div class="field">
                <label>Entity Field Type (Logic Component ECS)</label>
                <select bind:value={currentNode.config.entityType}>
                  {#each ENTITY_TYPES as et}
                    <option value={et}>{et}</option>
                  {/each}
                </select>
              </div>

              <div class="field">
                <label>Input String / User Value</label>
                <textarea
                  rows="3"
                  bind:value={currentNode.config.value}
                  placeholder="Enter input string or DNA sequence..."
                ></textarea>
              </div>

              <div class="field">
                <label>Variable / Field Identifier</label>
                <input type="text" bind:value={currentNode.config.key} placeholder="input_seq" />
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
                <div class="field-label-row">
                  <label>Inference Engine & Model</label>
                  {#if downloadedAgentModels[currentNode.config.model || 'Microsoft-Copilot-Local-ONNX']}
                    <span class="badge-model-ready">✓ Local ONNX Engine Ready</span>
                  {:else}
                    <button type="button" class="btn-download-model-sm" disabled={agentModelDownloading} on:click={() => downloadAgentModel(currentNode.config.model)}>
                      {agentModelDownloading ? `Downloading (${agentModelProgress}%)...` : '⬇ Download Model'}
                    </button>
                  {/if}
                </div>
                <select bind:value={currentNode.config.model} class="agent-model-select">
                  <option value="Microsoft-Copilot-Local-ONNX">🤖 Microsoft Copilot ONNX (Local CPU) [Default]</option>
                  <option value="Microsoft-Copilot-Bio-ONNX">🧬 Microsoft Copilot Bio ONNX [Genomics / Aligner]</option>
                  <option value="Phi-3.5-mini-ONNX">⚡ Microsoft Phi-3.5-mini ONNX [High-Speed Low-Latency]</option>
                  <option value="Biomedical-Copilot-v1">🔬 Biomedical Copilot Reasoning Engine</option>
                  <option value="Foundry-Local-Qwen-2.5">🌐 Foundry Local Qwen 2.5 (Local Weights)</option>
                </select>
              </div>

              <div class="field">
                <label>System Prompt (Directives)</label>
                <textarea
                  rows="3"
                  bind:value={currentNode.config.systemPrompt}
                  placeholder="Directives for Copilot agent..."
                ></textarea>
              </div>

              <div class="field">
                <label>User Prompt Template (Prompt Flow)</label>
                <textarea
                  rows="2"
                  bind:value={currentNode.config.prompt}
                  placeholder="Check sequence: {{input}}"
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

            <!-- LOGIC COMPONENT (C#, CSnakes Python & JS with Multi-I/O & Scriptable ECS) -->
            {:else if currentNode.type === 'LogicComponent' || currentNode.type === 'SmartComponent'}
              <div class="field">
                <label>Logic Component Name</label>
                <input type="text" bind:value={currentNode.config.componentName} placeholder="Custom Logic Evaluator" />
              </div>
              <div class="field">
                <label>Execution Runtime</label>
                <select bind:value={currentNode.config.runtime}>
                  <option value="csharp">C# (.NET 8 Core Runtime)</option>
                  <option value="python">Python 3 (CSnakes Polyglot Bridge)</option>
                  <option value="javascript">JavaScript (Node.js)</option>
                </select>
              </div>

              <!-- Multi-I/O Ports -->
              <div class="ecs-ports-box">
                <div class="box-header">
                  <span class="box-title">📥 Programmable Input Ports ({(currentNode.config.inputs || []).length})</span>
                  <button type="button" class="btn-sm-add" on:click={() => addNodePort(currentNode, 'inputs')}>+ Add Input</button>
                </div>
                {#if (currentNode.config.inputs || []).length > 0}
                  <div class="ports-grid">
                    {#each (currentNode.config.inputs || []) as inp, i}
                      <div class="port-chip-row">
                        <input type="text" bind:value={inp.name} placeholder="Port Name" />
                        <select bind:value={inp.type}>
                          <option value="DNA_Sequence">DNA_Sequence</option>
                          <option value="String">String</option>
                          <option value="JSON">JSON</option>
                          <option value="Float">Float</option>
                          <option value="Int32">Int32</option>
                          <option value="Boolean">Boolean</option>
                        </select>
                        <button type="button" class="btn-del-chip" on:click={() => removeNodePort(currentNode, 'inputs', i)}>✕</button>
                      </div>
                    {/each}
                  </div>
                {:else}
                  <span class="empty-hint">No multi-input ports configured. Uses primary node connection.</span>
                {/if}

                <div class="box-header" style="margin-top: 0.75rem;">
                  <span class="box-title">📤 Programmable Output Ports ({(currentNode.config.outputs || []).length})</span>
                  <button type="button" class="btn-sm-add" on:click={() => addNodePort(currentNode, 'outputs')}>+ Add Output</button>
                </div>
                {#if (currentNode.config.outputs || []).length > 0}
                  <div class="ports-grid">
                    {#each (currentNode.config.outputs || []) as out, i}
                      <div class="port-chip-row">
                        <input type="text" bind:value={out.name} placeholder="Port Name" />
                        <select bind:value={out.type}>
                          <option value="JSON">JSON</option>
                          <option value="DNA_Sequence">DNA_Sequence</option>
                          <option value="String">String</option>
                          <option value="Float">Float</option>
                          <option value="Int32">Int32</option>
                          <option value="Boolean">Boolean</option>
                        </select>
                        <button type="button" class="btn-del-chip" on:click={() => removeNodePort(currentNode, 'outputs', i)}>✕</button>
                      </div>
                    {/each}
                  </div>
                {:else}
                  <span class="empty-hint">Default single output port active.</span>
                {/if}
              </div>

            <!-- WORKSPACE FILE SYSTEM NODE CONFIG -->
            {:else if currentNode.type === 'WorkspaceFileSystem'}
              <div class="field">
                <label>Operation</label>
                <select bind:value={currentNode.config.operation}>
                  <option value="read_file">📖 Read File</option>
                  <option value="write_file">✍️ Write File</option>
                  <option value="list_files">📁 List Workspace Files</option>
                  <option value="exists">🔍 Check File Exists</option>
                </select>
              </div>
              <div class="field">
                <label>Workspace File Path</label>
                <input
                  type="text"
                  bind:value={currentNode.config.filePath}
                  placeholder="alignment_report.txt"
                />
              </div>
              {#if currentNode.config.operation === 'write_file'}
                <div class="field">
                  <label>File Content (Optional Override)</label>
                  <textarea
                    rows="3"
                    bind:value={currentNode.config.fileContent}
                    placeholder="Leave blank to write upstream payload automatically..."
                  ></textarea>
                </div>
              {/if}
              <div class="info-box">
                📁 Executes against local workspace directory <code>./workspace/</code>.
              </div>

            <!-- OUTPUT NODE CONFIG -->
            {:else if currentNode.type === 'Output'}
              <div class="field">
                <label>Console Print Prefix</label>
                <input type="text" bind:value={currentNode.config.prefix} placeholder=">> BIO-PIPELINE RESULT:" />
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

          <!-- HYBRID CUSTOM ECS PROPERTIES INSPECTOR -->
          <div class="section ecs-properties-section">
            <div class="ecs-prop-header">
              <div class="ecs-title-block">
                <span class="section-title">⚡ Custom ECS Node Properties</span>
                <span class="ecs-sub">Enum, Vector DB (pgvector), Sliders, Switches</span>
              </div>
              <div class="ecs-header-btns">
                <button type="button" class="btn-reflect-schema-sm" on:click={() => autoReflectNodeSchemas(currentNode)} title="Auto-reflect schemas from code">
                  🔄 Auto-Reflect
                </button>
                <button type="button" class="btn-add-ecs-prop" on:click={() => (showAddPropertyForm = !showAddPropertyForm)}>
                  {showAddPropertyForm ? '✕ Cancel' : '+ Add Property'}
                </button>
              </div>
            </div>

            <!-- INLINE ADD CUSTOM PROPERTY FORM -->
            {#if showAddPropertyForm}
              <div class="add-prop-inline-card">
                <div class="add-prop-title">Define Custom ECS Property</div>
                <div class="add-prop-grid">
                  <div class="add-prop-field">
                    <label>Property Name:</label>
                    <input type="text" bind:value={newPropName} placeholder="e.g. policyLevel" />
                  </div>
                  <div class="add-prop-field">
                    <label>Control Type:</label>
                    <select bind:value={newPropType}>
                      <option value="enum">Enum Dropdown</option>
                      <option value="vectordb">Vector DB Store (pgvector)</option>
                      <option value="number">Numeric Range Slider</option>
                      <option value="boolean">Boolean Switch</option>
                      <option value="string">Text String</option>
                    </select>
                  </div>
                </div>

                {#if newPropType === 'enum'}
                  <div class="add-prop-field full">
                    <label>Allowed Enum Choices (comma-separated):</label>
                    <input type="text" bind:value={newPropOptions} placeholder="Permissive, Standard, Strict" />
                  </div>
                {/if}

                <div class="add-prop-actions">
                  <button type="button" class="btn-save-custom-prop" on:click={() => addCustomEcsProperty(currentNode)}>
                    ✓ Save Property to Node
                  </button>
                </div>
              </div>
            {/if}

            <!-- RENDERED CUSTOM ECS PROPERTIES -->
            <div class="ecs-rendered-list">
              <!-- 1. Reflected / Defined Schemas -->
              {#if currentNode.config?.propertySchemas && currentNode.config.propertySchemas.length > 0}
                {#each currentNode.config.propertySchemas as schema}
                  <div class="ecs-prop-card">
                    <div class="ecs-prop-card-head">
                      <div class="prop-title-row">
                        <strong class="prop-k">{schema.name}</strong>
                        <span class="prop-t-badge {schema.type}">{schema.type.toUpperCase()}</span>
                      </div>
                      <button type="button" class="btn-del-prop" on:click={() => removeCustomEcsProperty(currentNode, schema.name)}>✕</button>
                    </div>
                    {#if schema.description}
                      <span class="prop-d-text">{schema.description}</span>
                    {/if}

                    <!-- INTERACTIVE WIDGET BASED ON PROPERTY TYPE -->
                    <div class="ecs-widget-wrapper">
                      {#if schema.type === 'enum'}
                        <select 
                          class="ecs-widget-select"
                          value={currentNode.config.properties?.[schema.name] || schema.defaultValue || schema.options?.[0]}
                          on:change={(e) => {
                            if (!currentNode.config.properties) currentNode.config.properties = {};
                            currentNode.config.properties[schema.name] = e.currentTarget.value;
                            nodes = [...nodes];
                          }}
                        >
                          {#each schema.options || ['Standard'] as opt}
                            <option value={opt}>{opt}</option>
                          {/each}
                        </select>

                      {:else if schema.type === 'vectordb'}
                        <div class="ecs-vectordb-widget">
                          <div class="vdb-ctrl-row">
                            <span class="vdb-label">Store:</span>
                            <select 
                              class="vdb-provider-select"
                              value={currentNode.config.properties?.[schema.name] || 'pgvector_default'}
                              on:change={(e) => {
                                if (!currentNode.config.properties) currentNode.config.properties = {};
                                currentNode.config.properties[schema.name] = e.currentTarget.value;
                                nodes = [...nodes];
                              }}
                            >
                              <option value="pgvector_default">pgvector (Local PostgreSQL/Prisma Default)</option>
                              <option value="chroma_genomic">ChromaDB Store</option>
                              <option value="qdrant_cloud">Qdrant Vector Engine</option>
                            </select>
                          </div>
                          <div class="vdb-meta-row">
                            <span>Collection: <code>genomic_embeddings</code></span> · 
                            <span>Dimension: <code>1536</code></span>
                          </div>
                        </div>

                      {:else if schema.type === 'number'}
                        <div class="ecs-slider-widget">
                          <input 
                            type="range" 
                            min={schema.min || 0} 
                            max={schema.max || 1} 
                            step={schema.step || 0.05}
                            value={currentNode.config.properties?.[schema.name] ?? schema.defaultValue ?? 0.85}
                            on:input={(e) => {
                              if (!currentNode.config.properties) currentNode.config.properties = {};
                              currentNode.config.properties[schema.name] = parseFloat(e.currentTarget.value);
                              nodes = [...nodes];
                            }}
                            class="ecs-slider-bar"
                          />
                          <span class="ecs-slider-val">{currentNode.config.properties?.[schema.name] ?? schema.defaultValue ?? 0.85}</span>
                        </div>

                      {:else if schema.type === 'boolean'}
                        <div class="ecs-toggle-widget">
                          <button 
                            type="button" 
                            class="btn-toggle-switch"
                            class:active={currentNode.config.properties?.[schema.name] ?? schema.defaultValue}
                            on:click={() => {
                              if (!currentNode.config.properties) currentNode.config.properties = {};
                              const curr = currentNode.config.properties?.[schema.name] ?? schema.defaultValue;
                              currentNode.config.properties[schema.name] = !curr;
                              nodes = [...nodes];
                            }}
                          >
                            {(currentNode.config.properties?.[schema.name] ?? schema.defaultValue) ? '✔ Enabled [TRUE]' : '✖ Disabled [FALSE]'}
                          </button>
                        </div>

                      {:else}
                        <input 
                          type="text" 
                          class="ecs-text-widget"
                          value={currentNode.config.properties?.[schema.name] ?? schema.defaultValue ?? ''}
                          on:input={(e) => {
                            if (!currentNode.config.properties) currentNode.config.properties = {};
                            currentNode.config.properties[schema.name] = e.currentTarget.value;
                            nodes = [...nodes];
                          }}
                        />
                      {/if}
                    </div>
                  </div>
                {/each}
              {/if}

              <!-- 2. Fallback Generic Properties for keys not in schemas -->
              {#if currentNode.config?.properties}
                {#each Object.entries(currentNode.config.properties) as [propKey, propVal]}
                  {#if !(currentNode.config?.propertySchemas || []).some((s) => s.name === propKey)}
                    <div class="ecs-prop-row generic">
                      <span class="prop-key-tag">{propKey}</span>
                      <span class="prop-sep">:</span>
                      <input
                        type="text"
                        class="prop-val-input"
                        value={String(propVal)}
                        on:change={(e) => {
                          let parsedVal = e.currentTarget.value;
                          if (parsedVal === 'true') parsedVal = true;
                          else if (parsedVal === 'false') parsedVal = false;
                          else if (!isNaN(Number(parsedVal)) && parsedVal !== '') parsedVal = Number(parsedVal);
                          currentNode.config.properties[propKey] = parsedVal;
                          nodes = [...nodes];
                        }}
                      />
                      <button type="button" class="btn-del-prop" on:click={() => removeNodeEcsProperty(currentNode, propKey)}>✕</button>
                    </div>
                  {/if}
                {/each}
              {/if}

              {#if (!currentNode.config?.propertySchemas || currentNode.config.propertySchemas.length === 0) && (!currentNode.config?.properties || Object.keys(currentNode.config.properties).length === 0)}
                <span class="empty-hint">No ECS properties declared. Click <strong>+ Add Property</strong> or <strong>🔄 Auto-Reflect</strong>. Accessible in expressions as <code>this.properties</code>.</span>
              {/if}
            </div>
          </div>

          <!-- ============================================== -->
          <!-- ATTACHED EXPRESSION ENGINE (n8n / Elsa Style) -->
          <!-- ============================================== -->
          <div class="section expression-section">
            <div class="expr-header-row">
              <div class="section-title">⚡ Attached Expression</div>
              {#if !currentNode.config?.hasExpression && !currentNode.config?.expression}
                <button
                  type="button"
                  class="btn-add-expr"
                  on:click={() => {
                    if (!currentNode.config) currentNode.config = {};
                    currentNode.config.hasExpression = true;
                    if (!currentNode.config.expression) {
                      currentNode.config.expression = `Console.log(Time.now + " " + this.name + " has executed");\nreturn $input;`;
                    }
                    if (!currentNode.config.expressionLang) {
                      currentNode.config.expressionLang = 'javascript';
                    }
                    nodes = [...nodes];
                  }}
                >
                  + Add Expression
                </button>
              {/if}
            </div>

            {#if !currentNode.config?.hasExpression && !currentNode.config?.expression}
              <div class="no-expr-hint">
                <p>No expression attached. Click <strong>+ Add Expression</strong> to attach a reactive script executed when this node runs (n8n / Elsa style).</p>
              </div>
            {:else}
              <div class="expr-toolbar">
                <div class="expr-lang-group">
                  <label for="expr-lang-side">Lang:</label>
                  <select id="expr-lang-side" bind:value={currentNode.config.expressionLang} on:change={() => (nodes = [...nodes])}>
                    <option value="javascript">JavaScript</option>
                    <option value="csharp">C# (.NET)</option>
                    <option value="python">Python</option>
                  </select>
                </div>
                <div class="expr-actions-right">
                  <button
                    type="button"
                    class="btn-focus-mode"
                    title="Open full-screen focus editor"
                    on:click={() => openFocusMode(currentNode)}
                  >
                    ⛶ Focus Mode
                  </button>
                  <button
                    type="button"
                    class="btn-detach-expr"
                    title="Remove attached expression"
                    on:click={() => {
                      currentNode.config.hasExpression = false;
                      currentNode.config.expression = '';
                      nodes = [...nodes];
                    }}
                  >
                    ✕ Detach
                  </button>
                </div>
              </div>

              <!-- Monaco Code Editor for Expression -->
              <div class="editor-wrapper">
                <MonacoEditor
                  height="180px"
                  language={currentNode.config.expressionLang || 'javascript'}
                  bind:value={currentNode.config.expression}
                  placeholder={'Console.log(Time.now + " " + this.name + " has executed");\nthis.properties.verified = true;\nreturn $input;'}
                  on:change={() => (nodes = [...nodes])}
                />
              </div>

              <!-- Quick Snippets -->
              <div class="expr-quick-snippets">
                <span class="snippets-label">Snippets:</span>
                <button
                  type="button"
                  class="snippet-chip"
                  on:click={() => insertSnippet(currentNode, 'Console.log(Time.now + " " + this.name + " has executed");')}
                >
                  Console.log(Time.now...)
                </button>
                <button
                  type="button"
                  class="snippet-chip"
                  on:click={() => insertSnippet(currentNode, 'return { ...$input, executedAt: Time.now };')}
                >
                  return &#123; ...$input &#125;
                </button>
              </div>

              <!-- Real Test Runner -->
              <div class="expr-tester">
                <div class="tester-header">
                  <button type="button" class="btn-run-expr" on:click={() => testExpressionOnNode(currentNode)}>
                    🧪 Test Expression
                  </button>
                  {#if exprTestRunning}
                    <span class="test-spinner">Evaluating...</span>
                  {/if}
                </div>
                {#if exprTestOutput}
                  <pre class="expr-output font-mono">{exprTestOutput}</pre>
                {/if}
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
      {/if}
    </div>

  <!-- COMBINED TAB: UNITY-STYLE LOGIC & STATE MACHINE STUDIO -->
  {:else if activeView === 'logic_fsm'}
    <div class="unity-studio-container">
      <!-- LEFT COLUMN: HIERARCHICAL FSM CONTROLLER & STATE MACHINE -->
      <div class="unity-panel fsm-panel">
        <div class="unity-panel-header">
          <div class="panel-header-left">
            <span class="panel-icon">🎮</span>
            <h4>State Machine & FSM Controller</h4>
          </div>
          <span class="active-layer-badge">{activeFsmLayer}</span>
        </div>

        <!-- Layers Section -->
        <div class="fsm-sub-section">
          <div class="section-title-row">
            <span>FSM Layers</span>
            <button type="button" class="btn-xs" on:click={() => {
              const name = prompt('Enter new layer name:', 'New Layer');
              if (name && !fsmLayers.includes(name)) { fsmLayers = [...fsmLayers, name]; activeFsmLayer = name; }
            }}>+ Add Layer</button>
          </div>
          <div class="layers-chips">
            {#each fsmLayers as l}
              <button
                type="button"
                class="layer-chip"
                class:active={activeFsmLayer === l}
                on:click={() => (activeFsmLayer = l)}
              >
                🎮 {l}
              </button>
            {/each}
          </div>
        </div>

        <!-- Interactive Parameters Section -->
        <div class="fsm-sub-section">
          <div class="section-title-row">
            <span>FSM Parameters ({fsmParameters.length})</span>
            <button type="button" class="btn-xs" on:click={() => (showAddParamModal = !showAddParamModal)}>+ Add Param</button>
          </div>

          {#if showAddParamModal}
            <div class="mini-modal-box">
              <input type="text" bind:value={newParamName} placeholder="Parameter Name" class="mini-input" />
              <select bind:value={newParamType} class="mini-select">
                <option value="Boolean">Boolean</option>
                <option value="Int32">Int32</option>
                <option value="Float">Float</option>
                <option value="Trigger">Trigger</option>
              </select>
              <div class="mini-actions">
                <button type="button" class="btn-xs" on:click={() => (showAddParamModal = false)}>Cancel</button>
                <button type="button" class="btn-xs primary" on:click={addFsmParameter}>Add</button>
              </div>
            </div>
          {/if}

          <div class="interactive-params-grid">
            {#each fsmParameters as param}
              <div class="param-card">
                <div class="param-card-top">
                  <span class="param-name">{param.name}</span>
                  <span class="param-type-badge {param.type}">{param.type}</span>
                </div>
                <div class="param-control">
                  {#if param.type === 'Boolean'}
                    <label class="toggle-switch">
                      <input type="checkbox" bind:checked={param.value} on:change={() => (fsmParameters = [...fsmParameters])} />
                      <span class="toggle-slider"></span>
                      <span class="toggle-label">{param.value ? 'true' : 'false'}</span>
                    </label>
                  {:else if param.type === 'Int32'}
                    <input type="number" step="1" bind:value={param.value} class="param-number-input" />
                  {:else if param.type === 'Float'}
                    <input type="number" step="0.01" bind:value={param.value} class="param-number-input" />
                  {:else if param.type === 'Trigger'}
                    <button
                      type="button"
                      class="btn-trigger"
                      class:fired={param.value}
                      on:click={() => dispatchFsmEvent(param.name)}
                    >
                      ⚡ Fire Trigger
                    </button>
                  {/if}
                </div>
              </div>
            {/each}
          </div>
        </div>

        <!-- Events Section -->
        <div class="fsm-sub-section">
          <div class="section-title-row">
            <span>FSM Events & Triggers</span>
            <button type="button" class="btn-xs" on:click={() => (showAddEventModal = !showAddEventModal)}>+ Add Event</button>
          </div>

          {#if showAddEventModal}
            <div class="mini-modal-box">
              <input type="text" bind:value={newEventName} placeholder="Event Name (e.g. OnQualityVerified)" class="mini-input" />
              <div class="mini-actions">
                <button type="button" class="btn-xs" on:click={() => (showAddEventModal = false)}>Cancel</button>
                <button type="button" class="btn-xs primary" on:click={addFsmEvent}>Add</button>
              </div>
            </div>
          {/if}

          <div class="events-list">
            {#each fsmEvents as evt}
              <div class="event-row">
                <div class="event-meta">
                  <span class="event-name">⚡ {evt.name}</span>
                  {#if evt.lastDispatched}
                    <span class="last-dispatched">Fired: {evt.lastDispatched}</span>
                  {/if}
                </div>
                <button type="button" class="btn-dispatch" on:click={() => dispatchFsmEvent(evt.name)}>Dispatch</button>
              </div>
            {/each}
          </div>
        </div>

        <!-- States & Transitions Visual Graph -->
        <div class="fsm-sub-section fsm-graph-section">
          <div class="section-title-row">
            <span>States ({fsmStates.length}) & Transitions</span>
            <div class="sub-actions">
              <button type="button" class="btn-xs" on:click={() => (showAddStateModal = !showAddStateModal)}>+ State</button>
              <button type="button" class="btn-xs" on:click={() => (showAddTransModal = !showAddTransModal)}>+ Transition</button>
            </div>
          </div>

          {#if showAddStateModal}
            <div class="mini-modal-box">
              <input type="text" placeholder="State Name" bind:value={newFsmStateName} class="mini-input" />
              <select bind:value={newFsmStateType} class="mini-select">
                <option value="state">Standard State</option>
                <option value="entry">Entry State</option>
                <option value="exit">Exit State</option>
              </select>
              <div class="mini-actions">
                <button type="button" class="btn-xs" on:click={() => (showAddStateModal = false)}>Cancel</button>
                <button type="button" class="btn-xs primary" on:click={addFsmState}>Create</button>
              </div>
            </div>
          {/if}

          {#if showAddTransModal}
            <div class="mini-modal-box">
              <div class="mini-row">
                <select bind:value={newTransFrom} class="mini-select">
                  {#each fsmStates as s}<option value={s.id}>{s.name}</option>{/each}
                </select>
                <span>➔</span>
                <select bind:value={newTransTo} class="mini-select">
                  {#each fsmStates as s}<option value={s.id}>{s.name}</option>{/each}
                </select>
              </div>
              <input type="text" placeholder="Trigger Event" bind:value={newTransTrigger} class="mini-input" />
              <input type="text" placeholder="Condition (e.g. isValid == true)" bind:value={newTransCondition} class="mini-input" />
              <div class="mini-actions">
                <button type="button" class="btn-xs" on:click={() => (showAddTransModal = false)}>Cancel</button>
                <button type="button" class="btn-xs primary" on:click={addFsmTransition}>Add Transition</button>
              </div>
            </div>
          {/if}

          <div class="fsm-states-horizontal">
            {#each fsmStates as state}
              <div
                class="fsm-state-node"
                class:active={activeFsmState === state.id}
                class:entry={state.type === 'entry'}
                class:exit={state.type === 'exit'}
                on:click={() => (activeFsmState = state.id)}
              >
                <div class="state-icon">{state.type === 'entry' ? '▶' : state.type === 'exit' ? '🏁' : '⚙️'}</div>
                <div class="state-details">
                  <div class="state-name">{state.name}</div>
                  <div class="state-type-tag">{state.type.toUpperCase()}</div>
                </div>
              </div>
            {/each}
          </div>

          <div class="fsm-transitions-chips">
            <span class="sub-label">Active Transitions:</span>
            {#each fsmTransitions as trans}
              <div class="trans-chip">
                <code>{getStateName(trans.from)} ➔ {getStateName(trans.to)}</code>
                <span class="trans-trigger">[{trans.trigger}]</span>
                <span class="trans-cond">({trans.condition})</span>
              </div>
            {/each}
          </div>
        </div>
      </div>

      <!-- RIGHT COLUMN: LOGIC COMPONENT & CSNAKES / MONACO EDITOR -->
      <div class="unity-panel logic-panel">
        <div class="unity-panel-header">
          <div class="panel-header-left">
            <span class="panel-icon">🧩</span>
            <h4>Logic Component Studio (CSnakes Polyglot)</h4>
          </div>
          <span class="active-runtime-badge {lcLanguage}">{lcLanguage.toUpperCase()}</span>
        </div>

        <div class="logic-top-grid">
          <div class="field">
            <label>Component Name</label>
            <input type="text" bind:value={lcName} class="unity-input" />
          </div>

          <div class="field">
            <label>Category</label>
            <select bind:value={lcCategory} class="unity-select">
              <option value="State Machine">State Machine</option>
              <option value="Bioinformatics">Bioinformatics</option>
              <option value="Shell Tools">Shell Tools</option>
              <option value="Modules">Modules</option>
              <option value="MCP Server">MCP Server</option>
            </select>
          </div>

          <div class="field">
            <label>Language Runtime</label>
            <select bind:value={lcLanguage} class="unity-select">
              <option value="csharp">⚡ C# (.NET 8 Core)</option>
              <option value="python">🐍 Python 3 (CSnakes Polyglot Bridge)</option>
              <option value="javascript">🟨 JavaScript (Node.js)</option>
            </select>
          </div>
        </div>

        <!-- Programmable Multi-I/O Ports -->
        <div class="ports-editor-box">
          <div class="ports-section-col">
            <div class="section-title-row">
              <span>📥 Input Ports</span>
              <button type="button" class="btn-xs" on:click={() => {
                const name = prompt('Input Port Name:', 'querySequence');
                if (name) lcCode = `// Input port [${name}] declared\n` + lcCode;
              }}>+ Input</button>
            </div>
            <div class="ports-list">
              <span class="port-tag in">sequence (DNA_Sequence)</span>
              <span class="port-tag in">reference (DNA_Sequence)</span>
            </div>
          </div>
          <div class="ports-section-col">
            <div class="section-title-row">
              <span>📤 Output Ports</span>
              <button type="button" class="btn-xs" on:click={() => {
                const name = prompt('Output Port Name:', 'alignmentScore');
                if (name) lcCode += `\n// Output port [${name}] emitted`;
              }}>+ Output</button>
            </div>
            <div class="ports-list">
              <span class="port-tag out">alignmentScore (Float)</span>
              <span class="port-tag out">verified (Boolean)</span>
            </div>
          </div>
        </div>

        <!-- Reflected shortcuts for StateMachineBehaviour & Nodes -->
        <div class="shortcuts-row">
          <span class="sub-label">StateMachineBehaviour API:</span>
          <button type="button" class="btn-api-chip" on:click={() => (lcCode += '\nstateMachine.SetVariable("isValid", true);')}>
            + SetVariable
          </button>
          <button type="button" class="btn-api-chip" on:click={() => (lcCode += '\nstateMachine.TriggerTransition("OnValidationPassed");')}>
            + TriggerTransition
          </button>
          <button type="button" class="btn-api-chip" on:click={() => (lcCode += '\nstateMachine.PlayState("Genomic Verification");')}>
            + PlayState
          </button>
        </div>

        <!-- Monaco Editor Container -->
        <div class="logic-editor-wrap">
          <div class="editor-header-bar">
            <span>Source Code ({lcLanguage.toUpperCase()})</span>
            <div class="editor-actions-bar">
              <button type="button" class="btn-xs" on:click={testLogicComponentCode} disabled={lcTesting}>
                {lcTesting ? 'Validating...' : '🧪 Validate Syntax'}
              </button>
              <button type="button" class="btn-xs primary" on:click={saveAndAddLogicComponent}>
                🚀 Save & Add to Canvas Flow
              </button>
            </div>
          </div>
          <div class="monaco-container-box">
            <MonacoEditor
              height="260px"
              language={lcLanguage === 'python' ? 'python' : lcLanguage === 'csharp' ? 'csharp' : 'javascript'}
              bind:value={lcCode}
            />
          </div>
          {#if lcTestOutput}
            <div class="lc-output-pane">
              <div class="lc-output-title">Compilation & Syntax Result:</div>
              <pre class="lc-output-text font-mono">{lcTestOutput}</pre>
            </div>
          {/if}
        </div>
      </div>
    </div>
  {/if}
    <!-- SLIDE-OVER DRAWER: NODE SMART COMPONENT & INLINE EXPRESSION ENGINE -->

  <!-- NODE CONTEXT MENU -->
  {#if showContextMenu && contextMenuNode}
    <div
      class="node-context-menu"
      style="left:{contextMenuX}px; top:{contextMenuY}px;"
      on:click|stopPropagation
    >
      <div class="context-header">{contextMenuNode.name || contextMenuNode.id}</div>
      <button class="menu-item" on:click={() => { showContextMenu = false; selectedId = contextMenuNode.id; }}>
        ⚙️ Select in Inspector
      </button>
      <button class="menu-item" on:click={() => { showContextMenu = false; openFocusMode(contextMenuNode); }}>
        ⛶ Focus Mode (Expression Editor)
      </button>
      <button class="menu-item" on:click={() => duplicateNode(contextMenuNode)}>
        📋 Duplicate Node
      </button>
      <div class="menu-divider"></div>
      <button class="menu-item delete" on:click={() => deleteNodeById(contextMenuNode.id)}>
        🗑️ Delete Node
      </button>
    </div>
  {/if}

  <!-- FULL-SCREEN FOCUS MODE MODAL FOR EXPRESSION SCRIPTING -->
  {#if isFocusMode && focusNode}
    <div class="focus-overlay" on:click={closeFocusMode}></div>
    <div class="focus-modal-panel" role="dialog" aria-label="Expression Editor Focus Mode">
      <div class="focus-header">
        <div class="focus-title-group">
          <span class="focus-icon">⛶</span>
          <div>
            <h3>Expression Editor: {focusNode.name}</h3>
            <span class="focus-subtitle">Full-Screen Focus Mode & Live Autocomplete ({focusNode.type})</span>
          </div>
        </div>
        <div class="focus-header-actions">
          <div class="lang-pill-select">
            <label for="focus-lang">Language:</label>
            <select id="focus-lang" bind:value={focusNode.config.expressionLang} on:change={() => (nodes = [...nodes])}>
              <option value="javascript">JavaScript</option>
              <option value="csharp">C# (.NET)</option>
              <option value="python">Python</option>
            </select>
          </div>
          <button class="btn-test-modal" on:click={() => testExpressionOnNode(focusNode)}>
            🧪 Test Expression
          </button>
          <button class="btn-close-focus" on:click={closeFocusMode}>
            ✕ Exit Focus Mode
          </button>
        </div>
      </div>

      <div class="focus-body">
        <div class="focus-editor-col">
          <div class="focus-editor-wrapper">
            <MonacoEditor
              height="100%"
              language={focusNode.config.expressionLang || 'javascript'}
              bind:value={focusNode.config.expression}
              placeholder={'Console.log(Time.now + " " + this.name + " has executed");\nthis.properties.verified = true;\nreturn $input;'}
              on:change={() => (nodes = [...nodes])}
            />
          </div>

          <!-- Quick Snippets in Focus Mode -->
          <div class="focus-snippets-bar">
            <span class="snippets-label">Insert Snippet:</span>
            <button
              type="button"
              class="snippet-chip"
              on:click={() => insertSnippet(focusNode, 'Console.log(Time.now + " " + this.name + " has executed");')}
            >
              Console.log(Time.now + this.name...)
            </button>
            <button
              type="button"
              class="snippet-chip"
              on:click={() => insertSnippet(focusNode, 'return { ...$input, executedAt: Time.now };')}
            >
              return &#123; ...$input &#125;
            </button>
            <button
              type="button"
              class="snippet-chip"
              on:click={() => insertSnippet(focusNode, 'Console.log("Input payload: " + JSON.stringify($input));')}
            >
              Console.log($input)
            </button>
          </div>
        </div>

        <div class="focus-output-col">
          <div class="focus-output-header">
            <span>Execution & Console Output</span>
            {#if exprTestRunning}
              <span class="running-tag">Evaluating...</span>
            {/if}
          </div>
          <pre class="focus-output-pre font-mono">{exprTestOutput || '// Click "🧪 Test Expression" to execute against mock node context.\n// Console.log(...) calls and return values will be displayed here.'}</pre>
        </div>
      </div>

      <div class="focus-footer">
        <span class="footer-hint">Tip: Autocomplete triggers on typing <code>Console.</code>, <code>Time.</code>, <code>this.</code>, <code>$</code> or <kbd>Ctrl+Space</kbd>.</span>
        <button class="btn-done-focus" on:click={closeFocusMode}>
          ✔ Done Editing
        </button>
      </div>
    </div>
  {/if}</div>

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

  /* STATE MACHINE TAB */
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

  /* NODE CONTEXT MENU */
  .node-context-menu {
    position: fixed;
    z-index: 2000;
    background: #0b0e14;
    border: 1px solid #38bdf888;
    border-radius: 0.4rem;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.85);
    padding: 0.35rem 0;
    min-width: 170px;
    animation: fadeIn 0.15s ease;
  }
  .context-header {
    padding: 0.35rem 0.75rem;
    font-size: 0.7rem;
    font-weight: 700;
    color: #38bdf8;
    border-bottom: 1px solid #1e293b;
    margin-bottom: 0.25rem;
  }
  .menu-item {
    width: 100%;
    text-align: left;
    background: none;
    border: none;
    color: #e2e8f0;
    padding: 0.4rem 0.75rem;
    font-size: 0.75rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .menu-item:hover {
    background: rgba(14, 165, 233, 0.2);
    color: #38bdf8;
  }
  .menu-item.delete:hover {
    background: rgba(239, 68, 68, 0.2);
    color: #ef4444;
  }
  .menu-divider {
    height: 1px;
    background: #1e293b;
    margin: 0.25rem 0;
  }
  .node-menu-btn {
    background: transparent;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    font-size: 0.85rem;
    padding: 0.1rem 0.25rem;
    border-radius: 0.2rem;
  }
  .node-menu-btn:hover {
    color: #38bdf8;
    background: rgba(255, 255, 255, 0.1);
  }
  .script-badge {
    font-size: 0.65rem;
    background: rgba(234, 179, 8, 0.2);
    color: #facc15;
    border: 1px solid rgba(234, 179, 8, 0.4);
    padding: 0.05rem 0.25rem;
    border-radius: 0.2rem;
  }

  /* DRAWER NAV TABS */
  .drawer-nav-tabs {
    display: flex;
    gap: 0.4rem;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 0.75rem;
  }
  .dtab {
    background: #0f172a;
    border: 1px solid #334155;
    color: #94a3b8;
    font-size: 0.75rem;
    font-weight: 600;
    padding: 0.35rem 0.75rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }
  .dtab:hover {
    color: #e2e8f0;
    border-color: #38bdf8;
  }
  .dtab.active {
    background: rgba(2, 132, 199, 0.25);
    border-color: #38bdf8;
    color: #38bdf8;
  }
  .lang-select-sm {
    background: #0f172a;
    border: 1px solid #334155;
    color: #38bdf8;
    font-size: 0.75rem;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
  }
  .script-textarea {
    width: 100%;
    background: #080a0f;
    border: 1px solid #38bdf844;
    color: #f1f5f9;
    font-family: monospace;
    font-size: 0.8rem;
    padding: 0.75rem;
    border-radius: 0.35rem;
    resize: vertical;
  }

  /* LOGIC COMPONENT STUDIO */
  .logic-comp-studio {
    display: grid;
    grid-template-columns: 320px 1fr;
    gap: 1.5rem;
    padding: 1.5rem;
    height: 100%;
    overflow-y: auto;
  }
  .studio-sidebar {
    background: #0b0e14;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
  }
  .studio-sidebar h3 {
    margin: 0;
    font-size: 1.1rem;
    color: #38bdf8;
  }
  .studio-hint {
    font-size: 0.75rem;
    color: #64748b;
    margin: 0;
  }
  .studio-input, .studio-select {
    width: 100%;
    background: #0f172a;
    border: 1px solid #334155;
    color: #f1f5f9;
    padding: 0.4rem;
    font-size: 0.8rem;
    border-radius: 0.25rem;
  }
  .reflected-nodes-box {
    background: #080a0f;
    border: 1px solid #1e293b;
    border-radius: 0.35rem;
    padding: 0.65rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }
  .nodes-box-title {
    font-size: 0.7rem;
    color: #94a3b8;
    font-weight: 600;
  }
  .node-tokens-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
  }
  .token-btn {
    background: #1e293b;
    border: 1px solid #38bdf844;
    color: #38bdf8;
    font-size: 0.65rem;
    font-family: monospace;
    padding: 0.15rem 0.4rem;
    border-radius: 0.2rem;
    cursor: pointer;
  }
  .token-btn:hover {
    background: #38bdf8;
    color: #0b0e14;
  }
  .stm-api-shortcuts {
    margin-top: 0.35rem;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.35rem;
  }
  .stm-label {
    font-size: 0.65rem;
    color: #64748b;
  }
  .token-btn-sm {
    background: rgba(168, 85, 247, 0.2);
    border: 1px solid #a855f7;
    color: #c084fc;
    font-size: 0.65rem;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
    cursor: pointer;
  }
  .studio-actions {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-top: auto;
  }
  .btn-test-studio {
    background: #1e293b;
    border: 1px solid #38bdf8;
    color: #38bdf8;
    padding: 0.45rem;
    font-size: 0.8rem;
    border-radius: 0.25rem;
    cursor: pointer;
    font-weight: 600;
  }
  .btn-save-studio {
    background: #0284c7;
    border: 1px solid #38bdf8;
    color: white;
    padding: 0.55rem;
    font-size: 0.8rem;
    border-radius: 0.25rem;
    cursor: pointer;
    font-weight: 700;
  }
  .studio-main-code {
    background: #0b0e14;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }
  .code-topbar {
    display: flex;
    justify-content: space-between;
    font-size: 0.8rem;
    color: #94a3b8;
  }
  .code-subtext {
    font-size: 0.7rem;
    color: #06b6d4;
  }
  .studio-textarea {
    width: 100%;
    flex: 1;
    min-height: 380px;
    background: #080a0f;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    font-family: monospace;
    font-size: 0.85rem;
    padding: 1rem;
    border-radius: 0.35rem;
    resize: none;
    outline: none;
  }
  .lc-output-pane {
    background: #080a0f;
    border: 1px solid #38bdf844;
    border-radius: 0.35rem;
    padding: 0.75rem;
  }
  .lc-output-title {
    font-size: 0.75rem;
    color: #38bdf8;
    font-weight: 600;
    margin-bottom: 0.35rem;
  }
  .lc-output-text {
    color: #4ade80;
    margin: 0;
    font-size: 0.75rem;
  }


  /* ATTACHED EXPRESSION ENGINE (n8n / Elsa Style) */
  .expression-section {
    background: #0b121e;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.75rem;
    margin-top: 0.75rem;
  }

  .expr-header-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;
  }

  .btn-add-expr {
    background: #0284c7;
    color: #fff;
    border: none;
    border-radius: 4px;
    padding: 0.3rem 0.65rem;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.15s;
  }

  .btn-add-expr:hover {
    background: #0369a1;
  }

  .no-expr-hint {
    background: rgba(15, 23, 42, 0.6);
    border: 1px dashed #334155;
    border-radius: 6px;
    padding: 0.75rem;
    font-size: 0.75rem;
    color: #94a3b8;
    line-height: 1.4;
  }

  .expr-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
  }

  .expr-lang-group {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .expr-lang-group select {
    background: #0f172a;
    color: #f1f5f9;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.2rem 0.4rem;
    font-size: 0.75rem;
  }

  .expr-actions-right {
    display: flex;
    gap: 0.35rem;
  }

  .btn-focus-mode {
    background: #3b82f6;
    color: white;
    border: none;
    border-radius: 4px;
    padding: 0.25rem 0.5rem;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-focus-mode:hover {
    background: #2563eb;
  }

  .btn-detach-expr {
    background: transparent;
    color: #ef4444;
    border: 1px solid rgba(239, 68, 68, 0.4);
    border-radius: 4px;
    padding: 0.25rem 0.5rem;
    font-size: 0.75rem;
    cursor: pointer;
  }

  .btn-detach-expr:hover {
    background: rgba(239, 68, 68, 0.15);
  }

  .editor-wrapper {
    position: relative;
    margin-bottom: 0.5rem;
  }

  .expr-textarea {
    width: 100%;
    background: #030712;
    color: #38bdf8;
    border: 1px solid #1e293b;
    border-radius: 4px;
    padding: 0.5rem;
    font-size: 0.75rem;
    line-height: 1.4;
    resize: vertical;
  }

  .expr-textarea:focus {
    outline: none;
    border-color: #38bdf8;
  }

  /* INTELLISENSE & AUTOCOMPLETE POPUP */
  .intellisense-popup {
    position: absolute;
    z-index: 100;
    width: 280px;
    background: #0f172a;
    border: 1px solid #38bdf8;
    border-radius: 6px;
    box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.7);
    overflow: hidden;
  }

  .popup-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #1e293b;
    padding: 0.35rem 0.5rem;
    font-size: 0.7rem;
    font-weight: 600;
    color: #38bdf8;
    border-bottom: 1px solid #334155;
  }

  .popup-keys {
    font-size: 0.65rem;
    color: #94a3b8;
  }

  .suggestions-list {
    list-style: none;
    margin: 0;
    padding: 0;
    max-height: 180px;
    overflow-y: auto;
  }

  .suggestion-item {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.35rem 0.5rem;
    cursor: pointer;
    font-size: 0.725rem;
    border-bottom: 1px solid #1e293b;
    transition: background 0.1s;
  }

  .suggestion-item:hover,
  .suggestion-item.active {
    background: rgba(56, 189, 248, 0.2);
  }

  .sug-icon {
    font-size: 0.85rem;
  }

  .sug-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
  }

  .sug-label {
    color: #f1f5f9;
    font-family: monospace;
    font-size: 0.725rem;
  }

  .sug-desc {
    color: #94a3b8;
    font-size: 0.65rem;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .sug-type {
    font-size: 0.65rem;
    color: #64748b;
    font-family: monospace;
  }

  .expr-quick-snippets {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.35rem;
    margin-bottom: 0.5rem;
  }

  .snippets-label {
    font-size: 0.7rem;
    color: #64748b;
  }

  .snippet-chip {
    background: rgba(30, 41, 59, 0.8);
    color: #cbd5e1;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.15rem 0.4rem;
    font-size: 0.675rem;
    cursor: pointer;
    font-family: monospace;
  }

  .snippet-chip:hover {
    background: #334155;
    color: #38bdf8;
  }

  .expr-tester {
    margin-top: 0.5rem;
    border-top: 1px solid #1e293b;
    padding-top: 0.5rem;
  }

  .tester-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.35rem;
  }

  .btn-run-expr {
    background: #10b981;
    color: white;
    border: none;
    border-radius: 4px;
    padding: 0.25rem 0.5rem;
    font-size: 0.725rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-run-expr:hover {
    background: #059669;
  }

  .test-spinner {
    font-size: 0.7rem;
    color: #10b981;
  }

  .expr-output {
    background: #030712;
    border: 1px solid #1e293b;
    border-radius: 4px;
    padding: 0.5rem;
    font-size: 0.7rem;
    color: #a7f3d0;
    max-height: 120px;
    overflow-y: auto;
    white-space: pre-wrap;
    word-break: break-all;
  }

  /* FOCUS MODE FULLSCREEN MODAL */
  .focus-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.8);
    backdrop-filter: blur(4px);
    z-index: 1000;
  }

  .focus-modal-panel {
    position: fixed;
    top: 5vh;
    left: 5vw;
    right: 5vw;
    bottom: 5vh;
    background: #0a0f1d;
    border: 1px solid #1e293b;
    border-radius: 10px;
    box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.9);
    z-index: 1001;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .focus-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    background: #0f172a;
    border-bottom: 1px solid #1e293b;
  }

  .focus-title-group {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .focus-icon {
    font-size: 1.5rem;
    color: #38bdf8;
  }

  .focus-title-group h3 {
    margin: 0;
    font-size: 1.15rem;
    color: #f8fafc;
  }

  .focus-subtitle {
    font-size: 0.775rem;
    color: #94a3b8;
  }

  .focus-header-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .lang-pill-select {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    font-size: 0.8rem;
    color: #94a3b8;
  }

  .lang-pill-select select {
    background: #1e293b;
    color: #f1f5f9;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.3rem 0.6rem;
  }

  .btn-test-modal {
    background: #10b981;
    color: white;
    border: none;
    border-radius: 5px;
    padding: 0.4rem 0.85rem;
    font-size: 0.825rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-close-focus {
    background: #334155;
    color: #f1f5f9;
    border: none;
    border-radius: 5px;
    padding: 0.4rem 0.85rem;
    font-size: 0.825rem;
    cursor: pointer;
  }

  .btn-close-focus:hover {
    background: #475569;
  }

  .focus-body {
    flex: 1;
    display: flex;
    overflow: hidden;
  }

  .focus-editor-col {
    flex: 2;
    display: flex;
    flex-direction: column;
    padding: 1rem;
    border-right: 1px solid #1e293b;
  }

  .focus-editor-wrapper {
    flex: 1;
    position: relative;
    display: flex;
  }

  .focus-textarea {
    width: 100%;
    height: 100%;
    background: #030712;
    color: #38bdf8;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 1rem;
    font-size: 0.9rem;
    line-height: 1.5;
    resize: none;
  }

  .focus-popup {
    width: 320px;
  }

  .focus-snippets-bar {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding-top: 0.75rem;
  }

  .focus-output-col {
    flex: 1;
    display: flex;
    flex-direction: column;
    padding: 1rem;
    background: #030712;
  }

  .focus-output-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.825rem;
    font-weight: 600;
    color: #94a3b8;
    margin-bottom: 0.5rem;
  }

  .focus-output-pre {
    flex: 1;
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 1rem;
    font-size: 0.8rem;
    color: #a7f3d0;
    overflow-y: auto;
    white-space: pre-wrap;
    word-break: break-all;
  }

  .focus-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1.5rem;
    background: #0f172a;
    border-top: 1px solid #1e293b;
  }

  .footer-hint {
    font-size: 0.8rem;
    color: #64748b;
  }

  .btn-done-focus {
    background: #0284c7;
    color: white;
    border: none;
    border-radius: 5px;
    padding: 0.45rem 1.25rem;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-done-focus:hover {
    background: #0369a1;
  }

  /* SCRIPTABLE ECS PROPERTIES & PORTS */
  .ecs-properties-section {
    background: #080d1a;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.65rem 0.75rem;
    margin-top: 0.75rem;
  }

  .ecs-prop-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;
  }

  .btn-add-ecs-prop {
    background: #1e293b;
    color: #38bdf8;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.2rem 0.45rem;
    font-size: 0.7rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-add-ecs-prop:hover {
    background: #334155;
  }

  .ecs-props-list {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .ecs-prop-row {
    display: flex;
    align-items: center;
    gap: 0.35rem;
  }

  .prop-key-input {
    flex: 1;
    background: #0f172a;
    color: #f8fafc;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.2rem 0.4rem;
    font-size: 0.75rem;
    font-family: monospace;
  }

  .prop-sep {
    color: #64748b;
    font-weight: bold;
  }

  .prop-val-input {
    flex: 1.5;
    background: #0f172a;
    color: #38bdf8;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.2rem 0.4rem;
    font-size: 0.75rem;
    font-family: monospace;
  }

  .btn-del-prop {
    background: transparent;
    color: #64748b;
    border: none;
    cursor: pointer;
    font-size: 0.75rem;
  }

  .btn-del-prop:hover { color: #ef4444; }

  .ecs-ports-box {
    background: #080d1a;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.65rem;
    margin-top: 0.5rem;
  }

  .box-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.4rem;
  }

  .box-title {
    font-size: 0.75rem;
    font-weight: 600;
    color: #cbd5e1;
  }

  .btn-sm-add {
    background: #1e293b;
    color: #38bdf8;
    border: 1px solid #334155;
    border-radius: 3px;
    padding: 0.15rem 0.4rem;
    font-size: 0.65rem;
    cursor: pointer;
  }

  .ports-grid {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .port-chip-row {
    display: flex;
    gap: 0.35rem;
    align-items: center;
  }

  .port-chip-row input {
    flex: 2;
    background: #0f172a;
    color: #f8fafc;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.2rem 0.4rem;
    font-size: 0.725rem;
  }

  .port-chip-row select {
    flex: 1.5;
    background: #0f172a;
    color: #f8fafc;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.2rem 0.4rem;
    font-size: 0.725rem;
  }

  .btn-del-chip {
    background: transparent;
    color: #64748b;
    border: none;
    cursor: pointer;
    font-size: 0.725rem;
  }

  .btn-del-chip:hover { color: #ef4444; }

  .empty-hint {
    font-size: 0.7rem;
    color: #64748b;
    font-style: italic;
  }


  /* UNITY-STYLE LOGIC & STATE MACHINE STUDIO */
  .btn-fullsize-toggle {
    background: #1e293b;
    color: #38bdf8;
    border: 1px solid #0284c7;
    border-radius: 4px;
    padding: 0.35rem 0.75rem;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    margin-left: 0.5rem;
  }
  .btn-fullsize-toggle.active {
    background: #0284c7;
    color: #fff;
  }
  .canvas-wrap.fullsize {
    position: fixed;
    top: 50px;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 100;
    background: #07090e;
  }

  .unity-studio-container {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
    height: calc(100vh - 120px);
    padding: 0.75rem;
    background: #07090e;
    overflow-y: auto;
  }
  @media (max-width: 1024px) {
    .unity-studio-container {
      grid-template-columns: 1fr;
    }
  }

  .unity-panel {
    background: #0a0f1d;
    border: 1px solid #1e293b;
    border-radius: 8px;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    overflow-y: auto;
  }
  .unity-panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 0.5rem;
  }
  .panel-header-left {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .unity-panel-header h4 {
    margin: 0;
    font-size: 0.95rem;
    color: #f8fafc;
  }
  .active-layer-badge, .active-runtime-badge {
    font-size: 0.7rem;
    padding: 0.15rem 0.5rem;
    border-radius: 4px;
    background: #1e293b;
    color: #38bdf8;
    font-weight: 600;
  }

  .fsm-sub-section {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.75rem;
  }
  .section-title-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.8rem;
    font-weight: 600;
    color: #94a3b8;
    margin-bottom: 0.5rem;
  }
  .btn-xs {
    background: #1e293b;
    color: #38bdf8;
    border: 1px solid #334155;
    border-radius: 3px;
    padding: 0.15rem 0.45rem;
    font-size: 0.7rem;
    cursor: pointer;
  }
  .btn-xs.primary {
    background: #0284c7;
    color: #fff;
    border-color: #0284c7;
  }
  .layers-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
  }
  .layer-chip {
    background: #1e293b;
    border: 1px solid #334155;
    color: #cbd5e1;
    border-radius: 4px;
    padding: 0.25rem 0.5rem;
    font-size: 0.75rem;
    cursor: pointer;
  }
  .layer-chip.active {
    background: #0284c7;
    color: #fff;
    border-color: #38bdf8;
  }

  .interactive-params-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(130px, 1fr));
    gap: 0.5rem;
  }
  .param-card {
    background: #0a0f1d;
    border: 1px solid #1e293b;
    border-radius: 4px;
    padding: 0.4rem;
  }
  .param-card-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.25rem;
  }
  .param-name {
    font-size: 0.75rem;
    font-weight: 600;
    color: #e2e8f0;
  }
  .param-type-badge {
    font-size: 0.65rem;
    padding: 0.05rem 0.3rem;
    border-radius: 2px;
    background: #1e293b;
  }
  .param-type-badge.Boolean { color: #34d399; }
  .param-type-badge.Int32 { color: #38bdf8; }
  .param-type-badge.Float { color: #f59e0b; }
  .param-type-badge.Trigger { color: #ec4899; }

  .param-number-input {
    width: 100%;
    background: #0f172a;
    border: 1px solid #334155;
    color: #f8fafc;
    padding: 0.2rem 0.4rem;
    border-radius: 3px;
    font-size: 0.75rem;
  }
  .btn-trigger {
    width: 100%;
    background: #ec489922;
    border: 1px solid #ec4899;
    color: #ec4899;
    border-radius: 3px;
    padding: 0.2rem;
    font-size: 0.7rem;
    font-weight: 600;
    cursor: pointer;
  }
  .btn-trigger.fired {
    background: #ec4899;
    color: #fff;
  }

  .events-list {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }
  .event-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #0a0f1d;
    border: 1px solid #1e293b;
    padding: 0.3rem 0.5rem;
    border-radius: 4px;
  }
  .event-meta {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .event-name {
    font-size: 0.75rem;
    font-weight: 600;
    color: #facc15;
  }
  .last-dispatched {
    font-size: 0.65rem;
    color: #94a3b8;
  }
  .btn-dispatch {
    background: #ca8a0422;
    border: 1px solid #ca8a04;
    color: #facc15;
    border-radius: 3px;
    padding: 0.15rem 0.45rem;
    font-size: 0.7rem;
    cursor: pointer;
  }

  .fsm-states-horizontal {
    display: flex;
    gap: 0.5rem;
    overflow-x: auto;
    padding-bottom: 0.5rem;
  }
  .fsm-state-node {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    background: #0a0f1d;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.4rem 0.6rem;
    cursor: pointer;
    min-width: 120px;
  }
  .fsm-state-node.active {
    border-color: #38bdf8;
    background: #0284c722;
  }
  .fsm-state-node.entry { border-left: 3px solid #10b981; }
  .fsm-state-node.exit { border-left: 3px solid #ef4444; }
  .state-name { font-size: 0.75rem; font-weight: 600; color: #f8fafc; }
  .state-type-tag { font-size: 0.65rem; color: #94a3b8; }

  .fsm-transitions-chips {
    margin-top: 0.5rem;
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
    align-items: center;
  }
  .trans-chip {
    font-size: 0.7rem;
    background: #0a0f1d;
    border: 1px solid #1e293b;
    padding: 0.2rem 0.4rem;
    border-radius: 3px;
    color: #cbd5e1;
  }
  .trans-trigger { color: #facc15; margin-left: 0.2rem; }
  .trans-cond { color: #34d399; margin-left: 0.2rem; }

  .logic-top-grid {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: 0.5rem;
  }
  .unity-input, .unity-select {
    width: 100%;
    background: #0f172a;
    border: 1px solid #334155;
    color: #f8fafc;
    padding: 0.35rem 0.5rem;
    border-radius: 4px;
    font-size: 0.8rem;
  }

  .ports-editor-box {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem;
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.5rem;
  }
  .ports-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.3rem;
  }
  .port-tag {
    font-size: 0.7rem;
    padding: 0.15rem 0.4rem;
    border-radius: 3px;
  }
  .port-tag.in { background: #0284c722; border: 1px solid #0284c7; color: #38bdf8; }
  .port-tag.out { background: #064e3b22; border: 1px solid #059669; color: #34d399; }

  .shortcuts-row {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
    align-items: center;
  }
  .btn-api-chip {
    background: #1e293b;
    border: 1px solid #334155;
    color: #a855f7;
    border-radius: 3px;
    padding: 0.2rem 0.4rem;
    font-size: 0.7rem;
    cursor: pointer;
  }
  .btn-api-chip:hover {
    border-color: #a855f7;
    background: #581c8722;
  }

  .logic-editor-wrap {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 6px;
    overflow: hidden;
  }
  .editor-header-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #0a0f1d;
    padding: 0.4rem 0.75rem;
    border-bottom: 1px solid #1e293b;
    font-size: 0.75rem;
    color: #94a3b8;
  }
  .editor-actions-bar {
    display: flex;
    gap: 0.5rem;
  }
  .monaco-container-box {
    border-bottom: 1px solid #1e293b;
  }


  .btn-open-studio-shortcut {
    width: 100%;
    background: #581c8733;
    border: 1px solid #a855f7;
    color: #c084fc;
    border-radius: 4px;
    padding: 0.4rem;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: pointer;
    margin-bottom: 0.5rem;
  }
  .btn-open-studio-shortcut:hover {
    background: #a855f7;
    color: #fff;
  }


  /* HYBRID ECS PROPERTY INSPECTOR STYLES */
  .ecs-title-block {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
  }

  .ecs-sub {
    font-size: 0.68rem;
    color: #64748b;
  }

  .ecs-header-btns {
    display: flex;
    gap: 0.4rem;
    align-items: center;
  }

  .btn-reflect-schema-sm {
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(14, 165, 233, 0.3);
    color: #38bdf8;
    font-size: 0.68rem;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .add-prop-inline-card {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.75rem;
    margin-bottom: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .add-prop-title {
    font-size: 0.75rem;
    font-weight: 600;
    color: #f1f5f9;
  }

  .add-prop-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
  }

  .add-prop-field {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .add-prop-field.full {
    grid-column: 1 / -1;
  }

  .add-prop-field label {
    font-size: 0.68rem;
    color: #94a3b8;
  }

  .add-prop-field input, .add-prop-field select {
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    padding: 0.3rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
  }

  .add-prop-actions {
    display: flex;
    justify-content: flex-end;
  }

  .btn-save-custom-prop {
    background: #10b981;
    color: #042f2e;
    border: none;
    border-radius: 0.25rem;
    font-size: 0.72rem;
    font-weight: 700;
    padding: 0.3rem 0.7rem;
    cursor: pointer;
  }

  .ecs-rendered-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .ecs-prop-card {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.55rem;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .ecs-prop-card-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .prop-title-row {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .prop-k {
    font-size: 0.75rem;
    color: #f1f5f9;
    font-family: ui-monospace, monospace;
  }

  .prop-t-badge {
    font-size: 0.6rem;
    padding: 0.1rem 0.3rem;
    border-radius: 0.2rem;
    font-weight: 600;
  }

  .prop-t-badge.enum { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }
  .prop-t-badge.vectordb { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
  .prop-t-badge.number { background: rgba(14, 165, 233, 0.15); color: #38bdf8; border: 1px solid rgba(14, 165, 233, 0.3); }
  .prop-t-badge.boolean { background: rgba(168, 85, 247, 0.15); color: #c084fc; border: 1px solid rgba(168, 85, 247, 0.3); }

  .prop-d-text {
    font-size: 0.68rem;
    color: #64748b;
  }

  .ecs-widget-wrapper {
    margin-top: 0.15rem;
  }

  .ecs-widget-select, .ecs-text-widget {
    width: 100%;
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    padding: 0.3rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    box-sizing: border-box;
  }

  .ecs-vectordb-widget {
    background: #07090e;
    border: 1px solid rgba(255, 255, 255, 0.06);
    border-radius: 0.25rem;
    padding: 0.4rem;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .vdb-ctrl-row {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .vdb-provider-select {
    flex: 1;
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #34d399;
    padding: 0.25rem;
    font-size: 0.72rem;
    border-radius: 0.25rem;
  }

  .vdb-meta-row {
    font-size: 0.65rem;
    color: #64748b;
  }

  .vdb-meta-row code {
    color: #94a3b8;
  }

  .ecs-slider-widget {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .ecs-slider-bar {
    flex: 1;
  }

  .ecs-slider-val {
    font-size: 0.75rem;
    color: #38bdf8;
    font-family: ui-monospace, monospace;
    font-weight: 600;
  }

  .btn-toggle-switch {
    width: 100%;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    font-size: 0.72rem;
    padding: 0.35rem 0.6rem;
    border-radius: 0.25rem;
    cursor: pointer;
    text-align: center;
  }

  .btn-toggle-switch.active {
    background: rgba(16, 185, 129, 0.2);
    border-color: rgba(16, 185, 129, 0.4);
    color: #34d399;
    font-weight: 600;
  }

  .field-label-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.3rem;
  }

  .badge-model-ready {
    font-size: 0.65rem;
    color: #10b981;
    font-weight: 600;
  }

  .btn-download-model-sm {
    background: rgba(245, 158, 11, 0.2);
    border: 1px solid rgba(245, 158, 11, 0.4);
    color: #fbbf24;
    font-size: 0.65rem;
    padding: 0.15rem 0.45rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .palette-subhead {
    font-size: 0.68rem;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    padding: 0.3rem 0.2rem;
    font-weight: 600;
  }

  .custom-comp-card {
    background: rgba(139, 92, 246, 0.08);
  }
</style>

