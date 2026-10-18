<script lang="ts">
  import { onMount } from 'svelte';
  import {
    getCompletions,
    validateCSharpSyntax,
    CSHARP_COMPLETIONS,
    type CompletionItem,
    type DiagnosticIssue
  } from '$lib/services/csharpIntelliSense';

  // Active Top Tab: 'kernel' | 'author' | 'agent'
  let activeTab: 'kernel' | 'author' | 'agent' = 'kernel';

  // --------------------------------------------------------------------------
  // TAB 1: POLYGLOT KERNEL STATE
  // --------------------------------------------------------------------------
  interface NotebookCell {
    id: string;
    language: 'csharp' | 'python' | 'javascript' | 'bash';
    code: string;
    output: string;
    durationMs?: number;
    executing: boolean;
    timestamp?: string;
  }

  let cells: NotebookCell[] = [
    {
      id: 'cell-1',
      language: 'csharp',
      code: `using System;
using DNAPlatform.Core;

// Real C# Execution with Preloaded Biological Types
var dna = new DnaSequence("ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCC");
Console.WriteLine($"DNA Sequence: {dna}");
Console.WriteLine($"Length:       {dna.Length} bp");
Console.WriteLine($"GC Fraction:  {dna.CalculateGc():P2}");
Console.WriteLine($"GC Skew:      {dna.CalculateGcSkew():F4}");
Console.WriteLine($"Antiparallel: {dna.ReverseComplement()}");
Console.WriteLine($"RNA Transcript: {dna.Transcribe()}");`,
      output: '',
      executing: false
    },
    {
      id: 'cell-2',
      language: 'csharp',
      code: `// Needleman-Wunsch Global Pairwise Sequence Alignment
var s1 = "ATGGTGCACCTGACTCCT";
var s2 = "ATGGTGCACGTGACTCCT";
var alignment = NeedlemanWunschAligner.Align(s1, s2);

Console.WriteLine($"Alignment Score:     {alignment.Score}");
Console.WriteLine($"Sequence Identity:   {alignment.IdentityPercentage}%");
Console.WriteLine($"Aligned Sequence 1:  {alignment.AlignedSequence1}");
Console.WriteLine($"Aligned Sequence 2:  {alignment.AlignedSequence2}");`,
      output: '',
      executing: false
    },
    {
      id: 'cell-3',
      language: 'python',
      code: `# CSnakes Polyglot Python Kernel
import math

sequence = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCC"
gc_count = sequence.count('G') + sequence.count('C')
gc_pct = (gc_count / len(sequence)) * 100

print(f"[CSnakes Python Engine] Sequence Length: {len(sequence)} bp")
print(f"[CSnakes Python Engine] GC Content:      {gc_pct:.2f}%")
print(f"[CSnakes Python Engine] Melting Temp Tm:  {64.9 + 41 * (gc_count - 16.4) / len(sequence):.1f}°C")`,
      output: '',
      executing: false
    }
  ];

  let selectedCellId = 'cell-1';
  let showIntelliSense = false;
  let completionsList: CompletionItem[] = [];
  let diagnostics: DiagnosticIssue[] = [];

  // --------------------------------------------------------------------------
  // TAB 2: SMART COMPONENT AUTHORING STATE
  // --------------------------------------------------------------------------
  const componentCategories = [
    'State Machine',
    'Shell Tools',
    'Modules',
    'MCP Server',
    'Agent as MCP'
  ];

  let authorCategory = 'State Machine';
  let authorLanguage = 'csharp';
  let authorName = 'Custom Mutation Gate';
  let authorDescription = 'Autonomous validation state machine transitioning through sequence verification.';
  let authorExpression = '$input.gc >= 0.45 && $input.gc <= 0.65';

  let authorCode = `using System;
using System.ComponentModel;

[Category("State Machine")]
[Description("DNA Phase Gate State Machine for validating genomic transitions")]
public class MutationGateComponent
{
    [Description("Strict GC fraction threshold required to proceed")]
    public double GcThreshold { get; set; } = 0.55;

    [Description("Maximum allowable single nucleotide variations")]
    public int MaxMutations { get; set; } = 3;

    [Description("Enforce strict IUPAC nucleotide validation")]
    public bool StrictIupacValidation { get; set; } = true;

    [Description("Target reference gene symbol")]
    public string TargetGene { get; set; } = "HBB_HUMAN";

    [Tool("validate_mutation", "Validates candidate variant against clinical tolerance criteria")]
    public bool ValidateVariant(string sequence, int position)
    {
        var dna = new DnaSequence(sequence);
        Console.WriteLine($"Evaluating {TargetGene} at pos {position}: GC={dna.CalculateGc():P2}");
        return dna.CalculateGc() >= GcThreshold;
    }
}`;

  let reflectedProperties: any[] = [];
  let reflectedTools: any[] = [];
  let authorSaveSuccess = false;
  let authorTestRunning = false;
  let authorTestOutput = '';
  let liveParamValues: Record<string, any> = {};

  // --------------------------------------------------------------------------
  // TAB 3: AGENT LAB (MICROSOFT AGENT FRAMEWORK & AG-UI) STATE
  // --------------------------------------------------------------------------
  let agentPrompt = 'Review mutation E6V on HBB sequence ATGGTGCACCTGACTCCTGAG and apply clinical edit.';
  let agentRunning = false;
  let agentEvents: Array<{ event: string; data: any; time: string }> = [];
  let pendingApproval: any = null;
  let agentStateSnapshot: any = {
    activeState: 'IDLE',
    targetGene: 'HBB_HUMAN',
    currentSequence: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCC',
    mutationsApplied: 0,
    pipelinePhase: 'Inspection'
  };

  onMount(async () => {
    updateReflectedSchema();
    await loadAuthoredComponents();
  });

  // --------------------------------------------------------------------------
  // TAB 1 ACTIONS: POLYGLOT KERNEL
  // --------------------------------------------------------------------------
  async function executeCell(cell: NotebookCell) {
    cell.executing = true;
    cell.output = '';

    try {
      const res = await fetch('/api/polyglot/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: cell.language,
          code: cell.code
        })
      });

      const data = await res.json();
      cell.executing = false;
      cell.durationMs = data.durationMs;
      cell.timestamp = new Date().toLocaleTimeString();

      if (data.success) {
        cell.output = data.output;
      } else {
        cell.output = `[ERROR] ${data.error || 'Execution failed'}\n${data.output || ''}`;
      }
      cells = [...cells];
    } catch (err: any) {
      cell.executing = false;
      cell.output = `[NETWORK ERROR] ${err.message}`;
      cells = [...cells];
    }
  }

  function addCell(lang: 'csharp' | 'python' | 'javascript' | 'bash' = 'csharp') {
    const newId = `cell-${Date.now()}`;
    const defaultCodes: Record<string, string> = {
      csharp: `var seq = new DnaSequence("ATGCGATCGATCG");\nConsole.WriteLine($"Sequence: {seq} (Length: {seq.Length}, GC: {seq.CalculateGc():P2})");`,
      python: `print("Python CSnakes Bridge Active")\nimport sys\nprint(f"Python Runtime: {sys.version.split()[0]}")`,
      javascript: `console.log("JavaScript Engine Online:", new Date().toISOString());`,
      bash: `echo "Host Architecture: $(uname -m)"\necho "Workspace: $(pwd)"`
    };

    cells = [
      ...cells,
      {
        id: newId,
        language: lang,
        code: defaultCodes[lang] || '// New code block',
        output: '',
        executing: false
      }
    ];
    selectedCellId = newId;
  }

  function removeCell(id: string) {
    if (cells.length > 1) {
      cells = cells.filter(c => c.id !== id);
    }
  }

  function handleEditorInput(cell: NotebookCell, event: any) {
    cell.code = event.target.value;
    if (cell.language === 'csharp') {
      diagnostics = validateCSharpSyntax(cell.code);
      const cursor = event.target.selectionStart || 0;
      completionsList = getCompletions(cell.code, cursor);
    }
  }

  function insertCompletion(item: CompletionItem) {
    const activeCell = cells.find(c => c.id === selectedCellId);
    if (!activeCell) return;

    activeCell.code += '\n' + item.insertText.replace(/\$\{[0-9]+:([^}]+)\}/g, '$1');
    showIntelliSense = false;
    cells = [...cells];
  }

  // --------------------------------------------------------------------------
  // TAB 2 ACTIONS: SMART COMPONENT AUTHORING
  // --------------------------------------------------------------------------
  function updateReflectedSchema() {
    diagnostics = validateCSharpSyntax(authorCode);

    // Extract public properties
    const propRegex = /(?:\[Description\(["']([^"']+)["']\)\]\s*)?public\s+(double|float|int|long|string|bool|boolean)\s+([A-Za-z0-9_]+)\s*\{\s*get;\s*set;\s*\}\s*(?:=\s*([^;]+);)?/g;
    const props: any[] = [];
    let match;

    while ((match = propRegex.exec(authorCode)) !== null) {
      const propDesc = match[1] || '';
      const rawType = match[2].toLowerCase();
      const name = match[3];
      const rawDefault = match[4]?.trim();

      let type = 'string';
      let defaultValue: any = '';
      let min: number | undefined;
      let max: number | undefined;
      let step: number | undefined;

      if (['double', 'float', 'int', 'long'].includes(rawType)) {
        type = 'number';
        defaultValue = rawDefault ? parseFloat(rawDefault) : 0;
        if (name.toLowerCase().includes('threshold') || name.toLowerCase().includes('rate')) {
          min = 0;
          max = 1;
          step = 0.05;
        } else {
          min = 0;
          max = 100;
          step = 1;
        }
      } else if (['bool', 'boolean'].includes(rawType)) {
        type = 'boolean';
        defaultValue = rawDefault ? rawDefault.toLowerCase() === 'true' : false;
      } else {
        type = 'string';
        defaultValue = rawDefault ? rawDefault.replace(/^["']|["']$/g, '') : '';
      }

      props.push({ name, type, rawType: match[2], default: defaultValue, description: propDesc, min, max, step });
      if (liveParamValues[name] === undefined) {
        liveParamValues[name] = defaultValue;
      }
    }
    reflectedProperties = props;

    // Extract tools
    const toolRegex = /\[(?:Tool|McpTool)\((?:["']([^"']+)["'])?(?:,\s*["']([^"']+)["'])?\)\s*\]\s*public\s+[A-Za-z0-9_<>[\]]+\s+([A-Za-z0-9_]+)/g;
    const tools: any[] = [];
    let tMatch;
    while ((tMatch = toolRegex.exec(authorCode)) !== null) {
      tools.push({ name: tMatch[3], description: tMatch[1] || tMatch[2] || `Tool ${tMatch[3]}` });
    }
    reflectedTools = tools;
  }

  async function testAuthoredComponent() {
    authorTestRunning = true;
    authorTestOutput = '';

    try {
      const res = await fetch('/api/polyglot/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: authorLanguage,
          code: authorCode
        })
      });
      const data = await res.json();
      authorTestRunning = false;
      authorTestOutput = data.output || 'Component compiled with no errors.';
    } catch (err: any) {
      authorTestRunning = false;
      authorTestOutput = `Error: ${err.message}`;
    }
  }

  let authoredComponentsList: any[] = [];

  async function loadAuthoredComponents() {
    try {
      const res = await fetch('/api/components');
      const data = await res.json();
      if (data.components) {
        authoredComponentsList = data.components;
      }
    } catch (e) {
      console.warn('Failed loading components:', e);
    }
  }

  async function saveAuthoredComponent() {
    try {
      const res = await fetch('/api/components', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: authorName,
          category: authorCategory,
          language: authorLanguage,
          description: authorDescription,
          code: authorCode,
          properties: reflectedProperties,
          expression: authorExpression
        })
      });

      const data = await res.json();
      if (data.success) {
        authorSaveSuccess = true;
        await loadAuthoredComponents();
        setTimeout(() => (authorSaveSuccess = false), 3500);
      }
    } catch (err: any) {
      alert(`Save failed: ${err.message}`);
    }
  }

  function loadPresetToAuthor(preset: any) {
    authorName = preset.name;
    authorCategory = preset.category;
    authorLanguage = preset.language || 'csharp';
    authorDescription = preset.description;
    authorCode = preset.code;
    authorExpression = preset.expression || '$input.value > 0';
    updateReflectedSchema();
  }

  // --------------------------------------------------------------------------
  // TAB 3 ACTIONS: AGENT LAB (AG-UI STREAM & HITL)
  // --------------------------------------------------------------------------
  async function runAgentLab(withApproval = false, approvedToolCallId?: string) {
    agentRunning = true;
    if (!withApproval) {
      agentEvents = [];
      pendingApproval = null;
    }

    try {
      const bodyPayload: any = {
        prompt: agentPrompt,
        agentId: 'mcp-copilot-agent',
        requireApproval: true
      };

      if (approvedToolCallId) {
        bodyPayload.approvals = [
          { toolCallId: approvedToolCallId, toolName: 'sequence-mutation', approved: true }
        ];
      }

      const response = await fetch('/api/agui', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(bodyPayload)
      });

      const reader = response.body?.getReader();
      const decoder = new TextDecoder();

      if (!reader) {
        agentRunning = false;
        return;
      }

      let buffer = '';

      while (true) {
        const { value, done } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n\n');
        buffer = lines.pop() || '';

        for (const block of lines) {
          if (!block.trim()) continue;
          let eventName = 'MESSAGE';
          let eventData: any = {};

          for (const line of block.split('\n')) {
            if (line.startsWith('event:')) {
              eventName = line.replace('event:', '').trim();
            } else if (line.startsWith('data:')) {
              try {
                eventData = JSON.parse(line.replace('data:', '').trim());
              } catch {
                eventData = line.replace('data:', '').trim();
              }
            }
          }

          agentEvents = [
            ...agentEvents,
            { event: eventName, data: eventData, time: new Date().toLocaleTimeString() }
          ];

          // Handle HITL Interrupt
          if (eventName === 'APPROVAL_REQUESTED') {
            pendingApproval = eventData;
            agentStateSnapshot.activeState = 'WAITING_FOR_APPROVAL';
            agentStateSnapshot.pipelinePhase = 'Human Authorization Required';
          }

          if (eventName === 'RUN_FINISHED') {
            if (!pendingApproval) {
              agentStateSnapshot.activeState = 'COMPLETED';
              agentStateSnapshot.mutationsApplied += 1;
              agentStateSnapshot.pipelinePhase = 'Execution Verified';
            }
          }
        }
      }

      agentRunning = false;
    } catch (err: any) {
      agentRunning = false;
      agentEvents = [
        ...agentEvents,
        { event: 'ERROR', data: { error: err.message }, time: new Date().toLocaleTimeString() }
      ];
    }
  }

  function handleAuthorizeInterrupt() {
    if (!pendingApproval) return;
    const callId = pendingApproval.toolCallId || 'call_e2e_1';
    pendingApproval = null;
    agentStateSnapshot.activeState = 'RESUMING';
    runAgentLab(true, callId);
  }

  function handleDenyInterrupt() {
    pendingApproval = null;
    agentStateSnapshot.activeState = 'DENIED';
    agentEvents = [
      ...agentEvents,
      {
        event: 'APPROVAL_DENIED',
        data: { message: 'Operator declined mutation approval.' },
        time: new Date().toLocaleTimeString()
      }
    ];
  }
</script>

<svelte:head>
  <title>Kernel & Studio — DeNovoAutomata</title>
</svelte:head>

<div class="studio-page">
  <!-- Top Navigation & Tab Bar -->
  <header class="studio-header">
    <div class="header-branding">
      <h1>Polyglot Kernel & Authoring Studio</h1>
      <span class="sub-badge">Real C# Execution · Unity-Style Inspector · AG-UI Protocol</span>
    </div>

    <!-- Segmented Tab Controls -->
    <div class="tab-controls" role="tablist">
      <button
        role="tab"
        aria-selected={activeTab === 'kernel'}
        class="tab-btn"
        class:active={activeTab === 'kernel'}
        on:click={() => (activeTab = 'kernel')}
      >
        💻 Polyglot Kernel
      </button>
      <button
        role="tab"
        aria-selected={activeTab === 'author'}
        class="tab-btn"
        class:active={activeTab === 'author'}
        on:click={() => (activeTab = 'author')}
      >
        ✍️ Component Authoring
      </button>
      <button
        role="tab"
        aria-selected={activeTab === 'agent'}
        class="tab-btn"
        class:active={activeTab === 'agent'}
        on:click={() => (activeTab = 'agent')}
      >
        🤖 Real-Time Agent Lab
      </button>
    </div>
  </header>

  <!-- ====================================================================== -->
  <!-- TAB 1: POLYGLOT KERNEL & NOTEBOOK                                      -->
  <!-- ====================================================================== -->
  {#if activeTab === 'kernel'}
    <main class="kernel-view">
      <div class="kernel-toolbar">
        <div class="toolbar-left">
          <span class="toolbar-label">Add Cell:</span>
          <button class="btn-tool" on:click={() => addCell('csharp')}>+ C# (.NET 8)</button>
          <button class="btn-tool" on:click={() => addCell('python')}>+ Python (CSnakes)</button>
          <button class="btn-tool" on:click={() => addCell('javascript')}>+ JavaScript</button>
          <button class="btn-tool" on:click={() => addCell('bash')}>+ Bash / Shell</button>
        </div>

        <div class="toolbar-right">
          <button
            class="btn-tool"
            class:active-tool={showIntelliSense}
            on:click={() => (showIntelliSense = !showIntelliSense)}
          >
            ⚡ IntelliSense Helper {showIntelliSense ? '▲' : '▼'}
          </button>
          <button class="btn-tool" on:click={() => (cells = cells.map(c => ({ ...c, output: '' })))}>
            Clear Outputs
          </button>
        </div>
      </div>

      <!-- IntelliSense Quick Snippet Drawer -->
      {#if showIntelliSense}
        <div class="intellisense-drawer">
          <div class="drawer-header">
            <h4>C# & Biological Type IntelliSense Suggestions</h4>
            <span class="drawer-note">Click any item to insert snippet into selected cell</span>
          </div>
          <div class="completions-grid">
            {#each CSHARP_COMPLETIONS as comp}
              <button class="comp-card" on:click={() => insertCompletion(comp)}>
                <span class="comp-label">{comp.label}</span>
                <span class="comp-kind">{comp.kind}</span>
                <span class="comp-detail">{comp.detail}</span>
              </button>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Notebook Cells -->
      <div class="cells-container">
        {#each cells as cell (cell.id)}
          <section class="notebook-cell" class:cell-selected={selectedCellId === cell.id}>
            <header class="cell-header">
              <div class="cell-meta">
                <span class="lang-tag lang-{cell.language}">
                  {cell.language.toUpperCase()}
                </span>
                <span class="cell-id">Cell {cell.id}</span>
                {#if cell.durationMs !== undefined}
                  <span class="cell-time">⏱ {cell.durationMs}ms</span>
                {/if}
                {#if cell.timestamp}
                  <span class="cell-timestamp">· {cell.timestamp}</span>
                {/if}
              </div>

              <div class="cell-actions">
                <button
                  class="btn-run"
                  disabled={cell.executing}
                  on:click={() => executeCell(cell)}
                >
                  {cell.executing ? '⏳ Running...' : '▶ Run Cell'}
                </button>
                <button class="btn-del" on:click={() => removeCell(cell.id)}>✕</button>
              </div>
            </header>

            <div class="cell-body">
              <textarea
                class="code-editor font-mono"
                value={cell.code}
                rows={Math.max(4, cell.code.split('\n').length + 1)}
                on:focus={() => (selectedCellId = cell.id)}
                on:input={(e) => handleEditorInput(cell, e)}
              ></textarea>
            </div>

            {#if cell.output}
              <div class="cell-output-pane">
                <div class="output-banner">
                  <span>Standard Output</span>
                  <button class="btn-tiny" on:click={() => (cell.output = '')}>Clear</button>
                </div>
                <pre class="output-text font-mono">{cell.output}</pre>
              </div>
            {/if}
          </section>
        {/each}
      </div>
    </main>

  <!-- ====================================================================== -->
  <!-- TAB 2: SMART COMPONENT AUTHORING (NODE COMPATIBLE)                     -->
  <!-- ====================================================================== -->
  {:else if activeTab === 'author'}
    <main class="author-view">
      <!-- Preset Components Sidebar -->
      <aside class="author-sidebar">
        <h3>Component Presets</h3>
        <p class="sidebar-help">Select a standard component to inspect or customize:</p>

        <div class="presets-list">
          {#each authoredComponentsList as comp}
            <button
              class="preset-item"
              class:selected={authorName === comp.name}
              on:click={() => loadPresetToAuthor(comp)}
            >
              <div class="preset-top">
                <span class="preset-name">{comp.name}</span>
                <span class="preset-cat">{comp.category}</span>
              </div>
              <p class="preset-desc">{comp.description}</p>
            </button>
          {/each}
        </div>
      </aside>

      <!-- Main Authoring Studio -->
      <section class="author-editor-stage">
        <div class="author-config-bar">
          <div class="field-group">
            <label for="author-comp-name">Component Name</label>
            <input id="author-comp-name" type="text" bind:value={authorName} />
          </div>

          <div class="field-group">
            <label for="author-comp-cat">Node Category</label>
            <select id="author-comp-cat" bind:value={authorCategory}>
              {#each componentCategories as cat}
                <option value={cat}>{cat}</option>
              {/each}
            </select>
          </div>

          <div class="field-group">
            <label for="author-comp-lang">Language</label>
            <select id="author-comp-lang" bind:value={authorLanguage}>
              <option value="csharp">C# (.NET 8.0)</option>
              <option value="python">Python (CSnakes)</option>
              <option value="javascript">JavaScript</option>
              <option value="bash">Bash / Shell</option>
            </select>
          </div>

          <div class="field-actions">
            <button
              class="btn-primary"
              disabled={authorTestRunning}
              on:click={testAuthoredComponent}
            >
              {authorTestRunning ? '⏳ Compiling...' : '⚡ Test & Reflect'}
            </button>
            <button class="btn-success" on:click={saveAuthoredComponent}>
              💾 Save to Nodes
            </button>
          </div>
        </div>

        {#if authorSaveSuccess}
          <div class="alert-success">
            ✔ Component '{authorName}' saved! Now available as a draggable node in Workflow Canvas.
          </div>
        {/if}

        <div class="author-split">
          <!-- Code Editor -->
          <div class="editor-pane">
            <div class="pane-header">
              <span>C# Source Code (Unity-Style Auto-Property Attributes)</span>
              <span class="syntax-info">{diagnostics.length === 0 ? '✔ Valid Syntax' : `⚠ ${diagnostics.length} issues`}</span>
            </div>
            <textarea
              class="editor-textarea font-mono"
              bind:value={authorCode}
              on:input={updateReflectedSchema}
            ></textarea>

            {#if diagnostics.length > 0}
              <div class="diagnostics-box">
                {#each diagnostics as diag}
                  <div class="diag-item diag-{diag.severity}">
                    Line {diag.line}: {diag.message}
                  </div>
                {/each}
              </div>
            {/if}

            {#if authorTestOutput}
              <div class="test-output-box">
                <div class="test-header">Compiler & Runtime Output</div>
                <pre class="font-mono">{authorTestOutput}</pre>
              </div>
            {/if}
          </div>

          <!-- Unity-Style Live Inspector Preview -->
          <div class="inspector-pane">
            <div class="pane-header">
              <span>Unity-Style Live Inspector Preview</span>
              <span class="pill-count">{reflectedProperties.length} Properties</span>
            </div>

            <div class="inspector-card">
              <div class="inspector-title">
                <h4>{authorName}</h4>
                <span class="cat-label">{authorCategory}</span>
              </div>
              <p class="inspector-desc">{authorDescription}</p>

              <hr class="inspector-divider" />

              <div class="properties-list">
                {#if reflectedProperties.length === 0}
                  <div class="empty-props">
                    <p>No public properties detected.</p>
                    <small>Add <code>public double Threshold &#123; get; set; &#125; = 0.75;</code> to expose property controls.</small>
                  </div>
                {:else}
                  {#each reflectedProperties as prop}
                    <div class="prop-row">
                      <div class="prop-label-col">
                        <span class="prop-name">{prop.name}</span>
                        <span class="prop-type">{prop.rawType}</span>
                        {#if prop.description}
                          <span class="prop-hint">{prop.description}</span>
                        {/if}
                      </div>

                      <div class="prop-control-col">
                        {#if prop.type === 'boolean'}
                          <label class="switch">
                            <input
                              type="checkbox"
                              bind:checked={liveParamValues[prop.name]}
                            />
                            <span class="slider"></span>
                          </label>
                        {:else if prop.type === 'number'}
                          <div class="number-control">
                            <input
                              type="range"
                              min={prop.min ?? 0}
                              max={prop.max ?? 100}
                              step={prop.step ?? 1}
                              bind:value={liveParamValues[prop.name]}
                            />
                            <input
                              type="number"
                              class="number-input font-mono"
                              bind:value={liveParamValues[prop.name]}
                            />
                          </div>
                        {:else}
                          <input
                            type="text"
                            class="text-input"
                            bind:value={liveParamValues[prop.name]}
                          />
                        {/if}
                      </div>
                    </div>
                  {/each}
                {/if}
              </div>

              {#if reflectedTools.length > 0}
                <hr class="inspector-divider" />
                <div class="tools-section">
                  <h5>Exposed MCP & Agent Tools</h5>
                  {#each reflectedTools as tool}
                    <div class="tool-tag">
                      <strong>{tool.name}</strong>: {tool.description}
                    </div>
                  {/each}
                </div>
              {/if}

              <hr class="inspector-divider" />
              <div class="expression-section">
                <label for="author-node-expr">Node Inline Expression (n8n Syntax)</label>
                <input
                  id="author-node-expr"
                  type="text"
                  class="font-mono text-xs"
                  bind:value={authorExpression}
                  placeholder="e.g. $input.gc > 0.50"
                />
              </div>
            </div>
          </div>
        </div>
      </section>
    </main>

  <!-- ====================================================================== -->
  <!-- TAB 3: REAL-TIME AGENT INTERACTION & TESTING LAB                       -->
  <!-- ====================================================================== -->
  {:else if activeTab === 'agent'}
    <main class="agent-lab-view">
      <div class="lab-split">
        <!-- Agent Controls & Input -->
        <section class="agent-controls-pane">
          <div class="controls-header">
            <h3>Microsoft Agent Framework Testing Console</h3>
            <span class="sub-badge">Provider Support Matrix · Local ONNX & Function Tools</span>
          </div>

          <div class="agent-meta-card">
            <div class="meta-row">
              <span class="meta-label">Hosting Protocol:</span>
              <span class="meta-val">AG-UI v1.0.0 (Server-Sent Events)</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Inference Engine:</span>
              <span class="meta-val">Microsoft-Copilot-Local-ONNX (Phi-3.5)</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Function Tools:</span>
              <span class="meta-val">csharp-runtime/get_current_time · sequence-mutation · IUPAC</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Governance:</span>
              <span class="meta-val">Human-in-the-Loop (HITL) Interrupt Active</span>
            </div>
          </div>

          <div class="input-card">
            <label for="agent-prompt-input">Agent Instructions / Task</label>
            <textarea
              id="agent-prompt-input"
              class="agent-prompt-textarea"
              bind:value={agentPrompt}
              rows="4"
              placeholder="Enter instructions for the agent..."
            ></textarea>

            <div class="prompt-presets">
              <span>Presets:</span>
              <button
                class="btn-preset"
                on:click={() => (agentPrompt = 'Apply sequence mutation E6V to HBB_HUMAN at position 6 with precision timing.')}
              >
                Mutation (Triggers HITL Interrupt)
              </button>
              <button
                class="btn-preset"
                on:click={() => (agentPrompt = 'Analyze GC skew on nucleotide string ATGCGATCGATCGATCGATCGA.')}
              >
                Safe Read-Only Analysis
              </button>
            </div>

            <div class="action-strip">
              <button
                class="btn-primary"
                disabled={agentRunning}
                on:click={() => runAgentLab(false)}
              >
                {agentRunning ? '⏳ Streaming Agent Deliberation...' : '▶ Run Agent Task'}
              </button>
            </div>
          </div>

          <!-- Live Agent State Card (Subscriber Protocol) -->
          <div class="state-card">
            <h4>Live Agent State (AG-UI Subscriber)</h4>
            <div class="state-grid font-mono">
              <div class="state-cell">
                <span class="sc-label">Active State</span>
                <span class="sc-val status-{agentStateSnapshot.activeState.toLowerCase()}">
                  {agentStateSnapshot.activeState}
                </span>
              </div>
              <div class="state-cell">
                <span class="sc-label">Target Gene</span>
                <span class="sc-val">{agentStateSnapshot.targetGene}</span>
              </div>
              <div class="state-cell">
                <span class="sc-label">Mutations Applied</span>
                <span class="sc-val">{agentStateSnapshot.mutationsApplied}</span>
              </div>
              <div class="state-cell">
                <span class="sc-label">Pipeline Phase</span>
                <span class="sc-val">{agentStateSnapshot.pipelinePhase}</span>
              </div>
            </div>
          </div>
        </section>

        <!-- Real-Time SSE Stream Display -->
        <section class="agent-stream-pane">
          <div class="stream-header">
            <h3>Real-Time AG-UI Stream Events</h3>
            <span class="event-count">{agentEvents.length} Events Received</span>
          </div>

          <!-- Human-in-the-Loop Interrupt Card -->
          {#if pendingApproval}
            <div class="hitl-approval-card">
              <div class="hitl-header">
                <span class="hitl-icon">⚠️</span>
                <div class="hitl-title-box">
                  <h4>Human-in-the-Loop Approval Required</h4>
                  <p>Agent is requesting authorization before applying genetic mutation.</p>
                </div>
              </div>

              <div class="hitl-details font-mono">
                <div><strong>Tool Name:</strong> {pendingApproval.toolName}</div>
                <div><strong>Call ID:</strong> {pendingApproval.toolCallId}</div>
                <div><strong>Target:</strong> {pendingApproval.arguments?.sequence} (Pos: {pendingApproval.arguments?.targetPosition})</div>
                <div><strong>Mutation:</strong> {pendingApproval.arguments?.mutation}</div>
                <div><strong>Note:</strong> {pendingApproval.arguments?.description}</div>
              </div>

              <div class="hitl-actions">
                <button class="btn-approve" on:click={handleAuthorizeInterrupt}>
                  ✔ Authorize Operation
                </button>
                <button class="btn-deny" on:click={handleDenyInterrupt}>
                  ✕ Reject Operation
                </button>
              </div>
            </div>
          {/if}

          <!-- Events Feed -->
          <div class="events-feed">
            {#if agentEvents.length === 0}
              <div class="empty-stream">
                <span>⚡</span>
                <p>Click 'Run Agent Task' to stream real-time reasoning traces and tool executions over AG-UI.</p>
              </div>
            {:else}
              {#each agentEvents as ev}
                <div class="event-item event-{ev.event.toLowerCase()}">
                  <div class="event-top">
                    <span class="event-badge">{ev.event}</span>
                    <span class="event-time font-mono">{ev.time}</span>
                  </div>
                  <pre class="event-payload font-mono">{JSON.stringify(ev.data, null, 2)}</pre>
                </div>
              {/each}
            {/if}
          </div>
        </section>
      </div>
    </main>
  {/if}
</div>

<style>
  .studio-page {
    padding: 1.5rem 2rem;
    max-width: 1600px;
    margin: 0 auto;
    color: #e2e8f0;
  }

  .studio-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 1.25rem;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .header-branding h1 {
    font-size: 1.6rem;
    font-weight: 700;
    margin: 0 0 0.25rem 0;
    color: #f8fafc;
  }

  .sub-badge {
    font-size: 0.825rem;
    color: #94a3b8;
  }

  .tab-controls {
    display: flex;
    gap: 0.5rem;
    background: #0f172a;
    padding: 0.25rem;
    border-radius: 8px;
    border: 1px solid #1e293b;
  }

  .tab-btn {
    padding: 0.5rem 1rem;
    font-size: 0.875rem;
    font-weight: 500;
    color: #94a3b8;
    background: transparent;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .tab-btn:hover {
    color: #f8fafc;
  }

  .tab-btn.active {
    background: #0284c7;
    color: #ffffff;
    font-weight: 600;
  }

  /* ----------------- Polyglot Kernel Styles ----------------- */
  .kernel-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 8px;
    padding: 0.75rem 1rem;
    margin-bottom: 1rem;
    flex-wrap: wrap;
    gap: 0.75rem;
  }

  .toolbar-left, .toolbar-right {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .toolbar-label {
    font-size: 0.85rem;
    color: #94a3b8;
    margin-right: 0.25rem;
  }

  .btn-tool {
    background: #1e293b;
    color: #cbd5e1;
    border: 1px solid #334155;
    padding: 0.4rem 0.75rem;
    border-radius: 6px;
    font-size: 0.825rem;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .btn-tool:hover {
    background: #334155;
    color: #f8fafc;
  }

  .btn-tool.active-tool {
    background: #0369a1;
    color: #ffffff;
    border-color: #0284c7;
  }

  .intellisense-drawer {
    background: #0f172a;
    border: 1px solid #0284c7;
    border-radius: 8px;
    padding: 1rem;
    margin-bottom: 1.25rem;
  }

  .drawer-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.75rem;
  }

  .drawer-header h4 {
    margin: 0;
    font-size: 0.95rem;
    color: #38bdf8;
  }

  .drawer-note {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .completions-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 0.5rem;
  }

  .comp-card {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    background: #1e293b;
    border: 1px solid #334155;
    padding: 0.5rem 0.75rem;
    border-radius: 6px;
    cursor: pointer;
    text-align: left;
    transition: all 0.15s ease;
  }

  .comp-card:hover {
    background: #334155;
    border-color: #38bdf8;
  }

  .comp-label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #38bdf8;
  }

  .comp-kind {
    font-size: 0.7rem;
    color: #a855f7;
    text-transform: uppercase;
  }

  .comp-detail {
    font-size: 0.75rem;
    color: #94a3b8;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 100%;
  }

  .notebook-cell {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 8px;
    margin-bottom: 1rem;
    overflow: hidden;
  }

  .cell-selected {
    border-color: #0284c7;
  }

  .cell-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem 1rem;
    background: #1e293b;
    border-bottom: 1px solid #334155;
  }

  .cell-meta {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.8rem;
  }

  .lang-tag {
    font-size: 0.7rem;
    font-weight: 700;
    padding: 0.15rem 0.4rem;
    border-radius: 4px;
  }

  .lang-csharp { background: #68217a; color: #fff; }
  .lang-python { background: #3776ab; color: #fff; }
  .lang-javascript { background: #f7df1e; color: #000; }
  .lang-bash { background: #4eaa25; color: #fff; }

  .cell-time { color: #38bdf8; font-family: monospace; }
  .cell-timestamp { color: #64748b; }

  .cell-actions {
    display: flex;
    gap: 0.5rem;
  }

  .btn-run {
    background: #059669;
    color: #ffffff;
    border: none;
    padding: 0.35rem 0.75rem;
    border-radius: 4px;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-run:disabled { opacity: 0.6; cursor: not-allowed; }
  .btn-del {
    background: transparent;
    color: #64748b;
    border: none;
    cursor: pointer;
  }
  .btn-del:hover { color: #ef4444; }

  .code-editor {
    width: 100%;
    background: #090d16;
    color: #e2e8f0;
    border: none;
    padding: 1rem;
    font-size: 0.875rem;
    line-height: 1.5;
    resize: vertical;
    outline: none;
  }

  .cell-output-pane {
    background: #020617;
    border-top: 1px solid #1e293b;
    padding: 0.75rem 1rem;
  }

  .output-banner {
    display: flex;
    justify-content: space-between;
    font-size: 0.75rem;
    color: #64748b;
    text-transform: uppercase;
    margin-bottom: 0.4rem;
  }

  .btn-tiny {
    background: transparent;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    font-size: 0.7rem;
  }

  .output-text {
    margin: 0;
    color: #38bdf8;
    font-size: 0.825rem;
    white-space: pre-wrap;
  }

  /* ----------------- Smart Component Authoring Styles ----------------- */
  .author-view {
    display: grid;
    grid-template-columns: 280px 1fr;
    gap: 1.5rem;
  }

  .author-sidebar {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 8px;
    padding: 1rem;
  }

  .author-sidebar h3 {
    margin: 0 0 0.25rem 0;
    font-size: 1rem;
    color: #f8fafc;
  }

  .sidebar-help {
    font-size: 0.775rem;
    color: #94a3b8;
    margin-bottom: 1rem;
  }

  .presets-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .preset-item {
    background: #1e293b;
    border: 1px solid #334155;
    padding: 0.6rem;
    border-radius: 6px;
    cursor: pointer;
    text-align: left;
    transition: all 0.15s ease;
  }

  .preset-item:hover, .preset-item.selected {
    border-color: #0284c7;
    background: #1e3a5f;
  }

  .preset-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.25rem;
  }

  .preset-name {
    font-size: 0.825rem;
    font-weight: 600;
    color: #f8fafc;
  }

  .preset-cat {
    font-size: 0.675rem;
    padding: 0.1rem 0.35rem;
    background: #0f172a;
    color: #38bdf8;
    border-radius: 3px;
  }

  .preset-desc {
    margin: 0;
    font-size: 0.75rem;
    color: #94a3b8;
    line-height: 1.3;
  }

  .author-config-bar {
    display: flex;
    gap: 1rem;
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 8px;
    padding: 0.75rem 1rem;
    margin-bottom: 1rem;
    align-items: flex-end;
    flex-wrap: wrap;
  }

  .field-group {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .field-group label {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .field-group input, .field-group select {
    background: #1e293b;
    color: #f8fafc;
    border: 1px solid #334155;
    padding: 0.45rem 0.65rem;
    border-radius: 6px;
    font-size: 0.85rem;
  }

  .field-actions {
    display: flex;
    gap: 0.5rem;
    margin-left: auto;
  }

  .btn-primary {
    background: #0284c7;
    color: #fff;
    border: none;
    padding: 0.45rem 0.9rem;
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-success {
    background: #059669;
    color: #fff;
    border: none;
    padding: 0.45rem 0.9rem;
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
  }

  .alert-success {
    background: #064e3b;
    color: #a7f3d0;
    padding: 0.6rem 1rem;
    border-radius: 6px;
    font-size: 0.85rem;
    margin-bottom: 1rem;
  }

  .author-split {
    display: grid;
    grid-template-columns: 1fr 420px;
    gap: 1rem;
  }

  .editor-pane, .inspector-pane {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 8px;
    display: flex;
    flex-direction: column;
  }

  .pane-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.6rem 1rem;
    background: #1e293b;
    border-bottom: 1px solid #334155;
    font-size: 0.825rem;
    font-weight: 600;
  }

  .syntax-info { font-size: 0.75rem; color: #10b981; }
  .pill-count { font-size: 0.75rem; color: #38bdf8; }

  .editor-textarea {
    width: 100%;
    min-height: 480px;
    background: #090d16;
    color: #e2e8f0;
    border: none;
    padding: 1rem;
    font-size: 0.85rem;
    line-height: 1.5;
    resize: vertical;
    outline: none;
  }

  .test-output-box {
    background: #020617;
    border-top: 1px solid #1e293b;
    padding: 0.75rem 1rem;
  }

  .test-header {
    font-size: 0.75rem;
    color: #64748b;
    text-transform: uppercase;
    margin-bottom: 0.25rem;
  }

  .test-output-box pre {
    margin: 0;
    font-size: 0.8rem;
    color: #38bdf8;
    white-space: pre-wrap;
  }

  /* Inspector Styles */
  .inspector-card {
    padding: 1rem;
  }

  .inspector-title h4 {
    margin: 0;
    font-size: 1.1rem;
    color: #f8fafc;
  }

  .cat-label {
    display: inline-block;
    font-size: 0.725rem;
    color: #38bdf8;
    background: #0284c7/20;
    margin-top: 0.25rem;
  }

  .inspector-desc {
    font-size: 0.8rem;
    color: #94a3b8;
    margin: 0.4rem 0 0.75rem 0;
  }

  .inspector-divider {
    border: 0;
    border-top: 1px solid #1e293b;
    margin: 0.75rem 0;
  }

  .properties-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .prop-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #1e293b;
    padding: 0.5rem 0.75rem;
    border-radius: 6px;
    border: 1px solid #334155;
  }

  .prop-label-col {
    display: flex;
    flex-direction: column;
  }

  .prop-name {
    font-size: 0.825rem;
    font-weight: 600;
    color: #f8fafc;
  }

  .prop-type {
    font-size: 0.7rem;
    color: #a855f7;
    font-family: monospace;
  }

  .prop-hint {
    font-size: 0.7rem;
    color: #94a3b8;
  }

  .number-control {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .number-input {
    width: 60px;
    background: #090d16;
    border: 1px solid #334155;
    color: #38bdf8;
    padding: 0.25rem;
    border-radius: 4px;
    text-align: right;
  }

  .text-input {
    background: #090d16;
    border: 1px solid #334155;
    color: #f8fafc;
    padding: 0.35rem 0.5rem;
    border-radius: 4px;
    font-size: 0.8rem;
  }

  .tools-section h5 {
    margin: 0 0 0.5rem 0;
    font-size: 0.8rem;
    color: #38bdf8;
  }

  .tool-tag {
    font-size: 0.75rem;
    color: #cbd5e1;
    background: #090d16;
    padding: 0.35rem 0.5rem;
    border-radius: 4px;
    border: 1px solid #1e293b;
    margin-bottom: 0.35rem;
  }

  .expression-section {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .expression-section label {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .expression-section input {
    background: #090d16;
    border: 1px solid #334155;
    color: #10b981;
    padding: 0.4rem;
    border-radius: 4px;
  }

  /* ----------------- Real-Time Agent Lab Styles ----------------- */
  .lab-split {
    display: grid;
    grid-template-columns: 460px 1fr;
    gap: 1.5rem;
  }

  .agent-controls-pane, .agent-stream-pane {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 8px;
    padding: 1.25rem;
  }

  .controls-header h3, .stream-header h3 {
    margin: 0 0 0.25rem 0;
    font-size: 1.1rem;
    color: #f8fafc;
  }

  .agent-meta-card {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 6px;
    padding: 0.75rem;
    margin: 1rem 0;
  }

  .meta-row {
    display: flex;
    justify-content: space-between;
    font-size: 0.775rem;
    margin-bottom: 0.25rem;
  }

  .meta-label { color: #94a3b8; }
  .meta-val { color: #38bdf8; font-weight: 500; }

  .input-card {
    margin-bottom: 1rem;
  }

  .input-card label {
    display: block;
    font-size: 0.8rem;
    color: #94a3b8;
    margin-bottom: 0.4rem;
  }

  .agent-prompt-textarea {
    width: 100%;
    background: #090d16;
    color: #f8fafc;
    border: 1px solid #334155;
    border-radius: 6px;
    padding: 0.75rem;
    font-size: 0.85rem;
    resize: vertical;
    outline: none;
    box-sizing: border-box;
  }

  .prompt-presets {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
    align-items: center;
    margin: 0.6rem 0;
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .btn-preset {
    background: #1e293b;
    border: 1px solid #334155;
    color: #cbd5e1;
    font-size: 0.725rem;
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    cursor: pointer;
  }

  .btn-preset:hover {
    color: #38bdf8;
    border-color: #38bdf8;
  }

  .action-strip {
    margin-top: 0.75rem;
  }

  .state-card {
    background: #1e293b;
    border: 1px solid #334155;
    border-radius: 6px;
    padding: 0.75rem;
  }

  .state-card h4 {
    margin: 0 0 0.5rem 0;
    font-size: 0.85rem;
    color: #38bdf8;
  }

  .state-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
  }

  .state-cell {
    background: #090d16;
    padding: 0.5rem;
    border-radius: 4px;
    display: flex;
    flex-direction: column;
  }

  .sc-label { font-size: 0.7rem; color: #64748b; }
  .sc-val { font-size: 0.8rem; font-weight: 600; color: #f8fafc; }

  .status-waiting_for_approval { color: #f59e0b; }
  .status-completed { color: #10b981; }

  /* HITL Card */
  .hitl-approval-card {
    background: #451a03;
    border: 1px solid #f59e0b;
    border-radius: 8px;
    padding: 1rem;
    margin-bottom: 1rem;
    animation: pulseBorder 2s infinite ease-in-out;
  }

  .hitl-header {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    margin-bottom: 0.75rem;
  }

  .hitl-icon { font-size: 1.5rem; }
  .hitl-title-box h4 { margin: 0; color: #fef3c7; font-size: 0.95rem; }
  .hitl-title-box p { margin: 0; color: #fde68a; font-size: 0.775rem; }

  .hitl-details {
    background: #1c0b02;
    padding: 0.6rem;
    border-radius: 6px;
    font-size: 0.75rem;
    color: #fed7aa;
    margin-bottom: 0.75rem;
    line-height: 1.4;
  }

  .hitl-actions {
    display: flex;
    gap: 0.75rem;
  }

  .btn-approve {
    background: #15803d;
    color: #fff;
    border: none;
    padding: 0.45rem 1rem;
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-deny {
    background: #b91c1c;
    color: #fff;
    border: none;
    padding: 0.45rem 1rem;
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
  }

  .stream-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 0.75rem;
  }

  .event-count {
    font-size: 0.75rem;
    color: #38bdf8;
  }

  .events-feed {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    max-height: 650px;
    overflow-y: auto;
  }

  .empty-stream {
    text-align: center;
    padding: 3rem 1rem;
    color: #64748b;
  }

  .empty-stream span { font-size: 2rem; display: block; margin-bottom: 0.5rem; }

  .event-item {
    background: #090d16;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.65rem 0.85rem;
  }

  .event-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.4rem;
  }

  .event-badge {
    font-size: 0.7rem;
    font-weight: 700;
    padding: 0.15rem 0.4rem;
    border-radius: 4px;
    background: #1e293b;
    color: #38bdf8;
  }

  .event-run_started .event-badge { color: #10b981; }
  .event-text_delta .event-badge { color: #38bdf8; }
  .event-tool_call_started .event-badge { color: #f59e0b; }
  .event-approval_requested .event-badge { color: #ef4444; background: #451a03; }
  .event-run_finished .event-badge { color: #10b981; }

  .event-time { font-size: 0.7rem; color: #64748b; }

  .event-payload {
    margin: 0;
    font-size: 0.775rem;
    color: #cbd5e1;
    white-space: pre-wrap;
    max-height: 180px;
    overflow-y: auto;
  }

  @keyframes pulseBorder {
    0%, 100% { border-color: #f59e0b; }
    50% { border-color: #ef4444; }
  }
</style>
