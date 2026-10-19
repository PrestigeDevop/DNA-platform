<script lang="ts">
  import { onMount } from 'svelte';
  import MonacoEditor from '$lib/components/MonacoEditor.svelte';

  interface NotebookCell {
    id: string;
    language: 'csharp' | 'python' | 'javascript' | 'bash';
    code: string;
    output: string;
    error?: string;
    running: boolean;
    durationMs?: number;
    timestamp?: string;
  }

  let activeView: 'notebook' | 'dotnet_interactive' = 'notebook';

  // Live Dotnet Interactive status
  let interactiveAvailable = false;
  let interactiveEndpoint = 'http://localhost:8888';
  let interactiveChecking = false;
  let interactiveStatusMsg = '';

  let cells: NotebookCell[] = [
    {
      id: 'cell_cs_1',
      language: 'csharp',
      code: `using System;\n\nvar now = DateTime.UtcNow;\nConsole.WriteLine($"[C# Roslyn Engine] Current UTC Time: {now:yyyy-MM-dd HH:mm:ss.fff}");\n\nvar seq = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC";\nvar gcCount = 0;\nforeach (var c in seq) if (c == 'G' || c == 'C') gcCount++;\nvar gcPercent = (double)gcCount / seq.Length * 100;\nConsole.WriteLine($"Sequence Length: {seq.Length} bp, GC Content: {gcPercent:F2}%");`,
      output: '',
      running: false
    },
    {
      id: 'cell_py_1',
      language: 'python',
      code: `# CSnakes High-Speed In-Process Python Bridge\nimport sys\n\nsequence = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC"\ntranslation_table = str.maketrans("ATGC", "TACG")\nrev_comp = sequence.translate(translation_table)[::-1]\n\nprint(f"[Python 3 CSnakes] Python Runtime: {sys.version.split()[0]}")\nprint(f"Target Sequence:      {sequence}")\nprint(f"Reverse Complement:   {rev_comp}")`,
      output: '',
      running: false
    },
    {
      id: 'cell_js_1',
      language: 'javascript',
      code: `// Node.js Execution Kernel\nconst sampleSeq = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC";\nconst codonCount = Math.floor(sampleSeq.length / 3);\n\nconsole.log("[JavaScript Kernel] Processing Sequence Analysis");\nconsole.log({ totalCodons: codonCount, timestamp: new Date().toISOString() });`,
      output: '',
      running: false
    },
    {
      id: 'cell_sh_1',
      language: 'bash',
      code: `# Shell Environment Inspection\nuname -m && date && pwd`,
      output: '',
      running: false
    }
  ];

  // Dotnet interactive cells
  let interactiveCells: NotebookCell[] = [
    {
      id: 'di_cell_1',
      language: 'csharp',
      code: `#!csharp\n#r "nuget: Microsoft.DotNet.Interactive, *"\n\nConsole.WriteLine("Hello from Live Dotnet Interactive Notebook!");\nDateTime.UtcNow`,
      output: '',
      running: false
    }
  ];

  onMount(async () => {
    await checkInteractiveServer();
  });

  async function checkInteractiveServer() {
    interactiveChecking = true;
    try {
      const res = await fetch(`/api/interactive?endpoint=${encodeURIComponent(interactiveEndpoint)}`);
      const data = await res.json();
      interactiveAvailable = !!data.available;
      interactiveStatusMsg = data.message || data.status || '';
    } catch {
      interactiveAvailable = false;
      interactiveStatusMsg = 'Server unreachable';
    } finally {
      interactiveChecking = false;
    }
  }

  async function runCell(cell: NotebookCell) {
    cell.running = true;
    cell.output = '';
    cell.error = undefined;
    cells = [...cells];

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
      cell.durationMs = data.durationMs;
      cell.timestamp = new Date().toLocaleTimeString();

      if (data.success) {
        cell.output = data.output || '(Execution finished with no output)';
      } else {
        cell.error = data.error || data.output || 'Execution failed';
        cell.output = data.output || '';
      }
    } catch (e: any) {
      cell.error = e.message || 'Execution error';
    } finally {
      cell.running = false;
      cells = [...cells];
    }
  }

  async function runInteractiveCell(cell: NotebookCell) {
    cell.running = true;
    cell.output = '';
    cell.error = undefined;
    interactiveCells = [...interactiveCells];

    try {
      const res = await fetch('/api/interactive', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          kernel: cell.language,
          code: cell.code,
          endpoint: interactiveEndpoint
        })
      });

      const data = await res.json();
      if (data.success) {
        cell.output = data.output || 'Execution complete.';
      } else {
        cell.error = data.error || 'Failed to execute interactive cell';
      }
    } catch (e: any) {
      cell.error = e.message || 'Execution request failed';
    } finally {
      cell.running = false;
      interactiveCells = [...interactiveCells];
    }
  }

  async function runAllCells() {
    for (const cell of cells) {
      await runCell(cell);
    }
  }

  function clearAllOutputs() {
    cells = cells.map((c) => ({ ...c, output: '', error: undefined, durationMs: undefined }));
  }

  function addCell(lang: 'csharp' | 'python' | 'javascript' | 'bash') {
    const id = 'cell_' + Math.random().toString(36).slice(2, 7);
    const newCell: NotebookCell = {
      id,
      language: lang,
      code: lang === 'csharp'
        ? '// C# Roslyn Cell\nConsole.WriteLine("Executing C# cell...");'
        : lang === 'python'
        ? '# Python 3 CSnakes Cell\nprint("Executing Python cell...")'
        : lang === 'javascript'
        ? '// JavaScript Cell\nconsole.log("Executing JavaScript cell...");'
        : '# Shell Script\necho "Running shell command..."',
      output: '',
      running: false
    };
    cells = [...cells, newCell];
  }

  function removeCell(id: string) {
    cells = cells.filter((c) => c.id !== id);
  }
</script>

<div class="polyglot-page">
  <!-- Header Bar -->
  <header class="polyglot-header">
    <div class="header-left">
      <h2>⚡ Polyglot Kernel Notebook</h2>
      <span class="header-sub">Interactive Multi-Language Runtime (C# Roslyn · Python CSnakes · Node.js · Shell)</span>
    </div>
    <div class="header-right">
      <div class="view-switch" role="tablist">
        <button
          type="button"
          class="switch-btn"
          class:active={activeView === 'notebook'}
          on:click={() => (activeView = 'notebook')}
        >
          📓 Notebook Cells
        </button>
        {#if interactiveAvailable}
          <button
            type="button"
            class="switch-btn live"
            class:active={activeView === 'dotnet_interactive'}
            on:click={() => (activeView = 'dotnet_interactive')}
          >
            ● Dotnet Interactive
          </button>
        {/if}
      </div>
      <button type="button" class="btn-action run-all" on:click={runAllCells}>
        ▶ Run All Cells
      </button>
      <button type="button" class="btn-action clear-all" on:click={clearAllOutputs}>
        🧹 Clear Outputs
      </button>
    </div>
  </header>

  <!-- Interactive Server Status Bar -->
  <div class="interactive-banner" class:active={interactiveAvailable}>
    <div class="banner-info">
      {#if interactiveAvailable}
        <span class="status-indicator online">●</span>
        <span>Dotnet Interactive server detected and running on <strong>{interactiveEndpoint}</strong>. Live notebook mode available!</span>
      {:else}
        <span class="status-indicator offline">○</span>
        <span>Dotnet Interactive / Jupyter server is currently offline on port 8888. Run via TUI (Option 4) or <code>dotnet interactive jupyter</code> to launch.</span>
      {/if}
    </div>
    <button type="button" class="btn-refresh-interactive" disabled={interactiveChecking} on:click={checkInteractiveServer}>
      {interactiveChecking ? 'Checking...' : '🔄 Re-Check Server'}
    </button>
  </div>

  <!-- TAB 1: POLYGLOT NOTEBOOK CELLS -->
  {#if activeView === 'notebook'}
    <div class="notebook-body">
      {#each cells as cell, idx (cell.id)}
        <div class="notebook-cell">
          <div class="cell-header">
            <div class="cell-title">
              <span class="cell-index">[{idx + 1}]</span>
              <span class="cell-lang-tag {cell.language}">{cell.language.toUpperCase()}</span>
            </div>
            <div class="cell-actions">
              <button
                type="button"
                class="btn-run-cell"
                disabled={cell.running}
                on:click={() => runCell(cell)}
              >
                {cell.running ? '⏳ Running...' : '▶ Run Cell'}
              </button>
              <button
                type="button"
                class="btn-delete-cell"
                on:click={() => removeCell(cell.id)}
                title="Delete Cell"
              >
                ✕
              </button>
            </div>
          </div>

          <!-- Cell Monaco Editor -->
          <div class="cell-editor-container">
            <MonacoEditor
              height="160px"
              language={cell.language}
              bind:value={cell.code}
            />
          </div>

          <!-- Cell Output -->
          {#if cell.output || cell.error}
            <div class="cell-output-box" class:has-error={!!cell.error}>
              <div class="output-top-bar">
                <span>Output {cell.durationMs ? `(${cell.durationMs}ms)` : ''}</span>
                {#if cell.timestamp}
                  <span class="output-time">{cell.timestamp}</span>
                {/if}
              </div>
              <pre class="output-pre font-mono">{cell.error ? `Error:\n${cell.error}\n\n${cell.output}` : cell.output}</pre>
            </div>
          {/if}
        </div>
      {/each}

      <!-- Add Cell Bar -->
      <div class="add-cell-strip">
        <span class="strip-label">+ Add Cell:</span>
        <button type="button" class="add-btn" on:click={() => addCell('csharp')}>C# (.NET 8)</button>
        <button type="button" class="add-btn" on:click={() => addCell('python')}>Python 3 (CSnakes)</button>
        <button type="button" class="add-btn" on:click={() => addCell('javascript')}>JavaScript</button>
        <button type="button" class="add-btn" on:click={() => addCell('bash')}>Bash / Shell</button>
      </div>
    </div>
  {:else if activeView === 'dotnet_interactive'}
    <!-- TAB 2: DOTNET INTERACTIVE LIVE NOTEBOOK -->
    <div class="interactive-body">
      <div class="interactive-toolbar">
        <h3>Live Dotnet Interactive Notebook Session</h3>
        <span class="tag-endpoint">Connected to: {interactiveEndpoint}</span>
      </div>

      {#each interactiveCells as diCell (diCell.id)}
        <div class="notebook-cell">
          <div class="cell-header">
            <span class="cell-lang-tag csharp">DOTNET INTERACTIVE (# !csharp)</span>
            <button
              type="button"
              class="btn-run-cell"
              disabled={diCell.running}
              on:click={() => runInteractiveCell(diCell)}
            >
              {diCell.running ? '⏳ Executing...' : '▶ Execute Cell'}
            </button>
          </div>
          <div class="cell-editor-container">
            <MonacoEditor
              height="200px"
              language="csharp"
              bind:value={diCell.code}
            />
          </div>
          {#if diCell.output || diCell.error}
            <div class="cell-output-box" class:has-error={!!diCell.error}>
              <pre class="output-pre font-mono">{diCell.error || diCell.output}</pre>
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .polyglot-page {
    max-width: 1400px;
    margin: 0 auto;
    color: #e2e8f0;
  }

  .polyglot-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 1rem;
    margin-bottom: 1rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .header-left h2 {
    margin: 0 0 0.25rem 0;
    font-size: 1.4rem;
    color: #f8fafc;
  }

  .header-sub {
    font-size: 0.8rem;
    color: #94a3b8;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .view-switch {
    display: flex;
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 2px;
  }

  .switch-btn {
    background: transparent;
    border: none;
    color: #94a3b8;
    padding: 0.4rem 0.85rem;
    font-size: 0.8rem;
    cursor: pointer;
    border-radius: 4px;
    transition: all 0.15s;
  }

  .switch-btn.active {
    background: #0284c7;
    color: #ffffff;
    font-weight: 600;
  }

  .switch-btn.live {
    color: #10b981;
  }

  .btn-action {
    border: none;
    border-radius: 5px;
    padding: 0.4rem 0.85rem;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-action.run-all {
    background: #10b981;
    color: white;
  }

  .btn-action.run-all:hover {
    background: #059669;
  }

  .btn-action.clear-all {
    background: #334155;
    color: #cbd5e1;
  }

  .interactive-banner {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: rgba(15, 23, 42, 0.7);
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 0.6rem 1rem;
    margin-bottom: 1.25rem;
    font-size: 0.8rem;
    color: #94a3b8;
  }

  .interactive-banner.active {
    border-color: rgba(16, 185, 129, 0.4);
    background: rgba(16, 185, 129, 0.05);
    color: #cbd5e1;
  }

  .status-indicator.online {
    color: #10b981;
    margin-right: 0.35rem;
  }

  .status-indicator.offline {
    color: #f59e0b;
    margin-right: 0.35rem;
  }

  .btn-refresh-interactive {
    background: #1e293b;
    color: #e2e8f0;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.25rem 0.6rem;
    font-size: 0.75rem;
    cursor: pointer;
  }

  .notebook-body {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
  }

  .notebook-cell {
    background: #0a0f1d;
    border: 1px solid #1e293b;
    border-radius: 8px;
    overflow: hidden;
  }

  .cell-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem 0.75rem;
    background: #0f172a;
    border-bottom: 1px solid #1e293b;
  }

  .cell-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .cell-index {
    color: #64748b;
    font-family: monospace;
    font-size: 0.8rem;
  }

  .cell-lang-tag {
    font-size: 0.7rem;
    font-weight: 700;
    padding: 0.15rem 0.45rem;
    border-radius: 4px;
  }

  .cell-lang-tag.csharp { background: rgba(56, 189, 248, 0.2); color: #38bdf8; }
  .cell-lang-tag.python { background: rgba(245, 158, 11, 0.2); color: #f59e0b; }
  .cell-lang-tag.javascript { background: rgba(234, 179, 8, 0.2); color: #eab308; }
  .cell-lang-tag.bash { background: rgba(168, 85, 247, 0.2); color: #c084fc; }

  .cell-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .btn-run-cell {
    background: #0284c7;
    color: white;
    border: none;
    border-radius: 4px;
    padding: 0.25rem 0.65rem;
    font-size: 0.75rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-run-cell:hover {
    background: #0369a1;
  }

  .btn-delete-cell {
    background: transparent;
    color: #64748b;
    border: none;
    cursor: pointer;
    font-size: 0.85rem;
  }

  .btn-delete-cell:hover {
    color: #ef4444;
  }

  .cell-output-box {
    background: #030712;
    border-top: 1px solid #1e293b;
    padding: 0.75rem;
  }

  .cell-output-box.has-error {
    border-left: 3px solid #ef4444;
  }

  .output-top-bar {
    display: flex;
    justify-content: space-between;
    font-size: 0.7rem;
    color: #64748b;
    margin-bottom: 0.35rem;
  }

  .output-pre {
    margin: 0;
    font-size: 0.8rem;
    color: #a7f3d0;
    white-space: pre-wrap;
    word-break: break-all;
    max-height: 220px;
    overflow-y: auto;
  }

  .add-cell-strip {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem;
    background: #0a0f1d;
    border: 1px dashed #334155;
    border-radius: 8px;
    margin-top: 0.5rem;
  }

  .strip-label {
    font-size: 0.8rem;
    color: #94a3b8;
  }

  .add-btn {
    background: #1e293b;
    color: #cbd5e1;
    border: 1px solid #334155;
    border-radius: 4px;
    padding: 0.3rem 0.6rem;
    font-size: 0.75rem;
    cursor: pointer;
  }

  .add-btn:hover {
    background: #334155;
    color: #38bdf8;
  }

  .interactive-body {
    background: #0a0f1d;
    border: 1px solid #1e293b;
    border-radius: 8px;
    padding: 1rem;
  }

  .interactive-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 0.75rem;
    margin-bottom: 1rem;
  }

  .tag-endpoint {
    font-family: monospace;
    font-size: 0.75rem;
    background: #1e293b;
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    color: #10b981;
  }
</style>
