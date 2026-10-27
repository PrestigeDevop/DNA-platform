<script lang="ts">
  import { onMount } from 'svelte';
  import MonacoEditor from '$lib/components/MonacoEditor.svelte';

  interface KernelInfo {
    id: string;
    name: string;
    language: string;
    status: 'idle' | 'busy' | 'offline';
    description: string;
  }

  interface InteractiveCell {
    id: string;
    kernel: 'csharp' | 'python' | 'javascript' | 'bash';
    code: string;
    output: string;
    running: boolean;
    durationMs?: number;
    timestamp?: string;
  }

  let serverEndpoint = 'http://localhost:8888';
  let serverStatus: 'online' | 'offline' | 'checking' = 'checking';
  let serverMessage = '';
  let serverType = '';
  let activeKernel = 'csharp';

  const AVAILABLE_KERNELS: KernelInfo[] = [
    { id: 'csharp', name: '.NET Interactive C#', language: 'csharp', status: 'idle', description: 'Roslyn Compiler & Microsoft .NET Interactive REPL' },
    { id: 'python', name: 'Python 3 CSnakes', language: 'python', status: 'idle', description: 'Zero-overhead in-process CPython bridge (BioPython, NumPy)' },
    { id: 'javascript', name: 'Node.js V8 Engine', language: 'javascript', status: 'idle', description: 'Asynchronous JavaScript stream processor' },
    { id: 'bash', name: 'POSIX Shell Sandboxed', language: 'bash', status: 'idle', description: 'System diagnostics and environment tools' }
  ];

  let cells: InteractiveCell[] = [
    {
      id: 'kcell_1',
      kernel: 'csharp',
      code: `using System;

var utcNow = DateTime.UtcNow;
Console.WriteLine($"[Microsoft Interactive Server] Session initialized at {utcNow:O}");
Console.WriteLine($"Runtime: .NET 8.0 Core CLR on {Environment.OSVersion}");

// Calculate GC ratio of test sequence
var dna = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC";
int gc = 0;
foreach (char c in dna) {
    if (c == 'G' || c == 'C') gc++;
}
double gcRatio = (double)gc / dna.Length;
Console.WriteLine($"Sequence: {dna}");
Console.WriteLine($"GC Ratio: {gcRatio:P1}");`,
      output: '',
      running: false
    },
    {
      id: 'kcell_2',
      kernel: 'python',
      code: `# CSnakes in-process polyglot execution
def analyze_sequence(seq):
    length = len(seq)
    g = seq.count('G') + seq.count('g')
    c = seq.count('C') + seq.count('c')
    skew = (g - c) / max(g + c, 1)
    return {"length": length, "gc_skew": round(skew, 4)}

result = analyze_sequence("ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC")
print(f"[CSnakes Python Engine] Computed: {result}")`,
      output: '',
      running: false
    }
  ];

  onMount(async () => {
    await checkServer();
  });

  async function checkServer() {
    serverStatus = 'checking';
    serverMessage = 'Probing Microsoft Interactive / Jupyter server...';
    try {
      const res = await fetch(`/api/interactive?endpoint=${encodeURIComponent(serverEndpoint)}`);
      const data = await res.json();
      if (data.available) {
        serverStatus = 'online';
        serverType = data.serverType || 'DotnetInteractive';
        serverMessage = data.status || 'Connected to live Microsoft Interactive server.';
      } else {
        serverStatus = 'offline';
        serverType = 'Local Roslyn & CSnakes Fallback';
        serverMessage = data.message || 'Server offline. In-process Roslyn & CSnakes execution active.';
      }
    } catch (e: any) {
      serverStatus = 'offline';
      serverType = 'In-Process Fallback Engine';
      serverMessage = `Could not reach ${serverEndpoint}. Local Roslyn engine active.`;
    }
  }

  async function runCell(cell: InteractiveCell) {
    cell.running = true;
    cell.output = '';
    const start = Date.now();

    try {
      const res = await fetch('/api/interactive', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          code: cell.code,
          kernel: cell.kernel,
          endpoint: serverEndpoint
        })
      });

      const data = await res.json();
      cell.durationMs = Date.now() - start;
      cell.timestamp = new Date().toLocaleTimeString();

      if (data.success) {
        let text = data.output || '(Execution completed with no output)';
        if (data.source) {
          text = `[Source: ${data.source}] (${cell.durationMs}ms)\n` + text;
        }
        cell.output = text;
      } else {
        cell.output = `Error: ${data.error || 'Execution failed'}`;
      }
    } catch (err: any) {
      cell.output = `Network error: ${err.message}`;
    } finally {
      cell.running = false;
      cells = [...cells];
    }
  }

  function addCell(kernel: 'csharp' | 'python' | 'javascript' | 'bash' = 'csharp') {
    const id = 'kcell_' + Date.now().toString(36);
    cells = [
      ...cells,
      {
        id,
        kernel,
        code: kernel === 'csharp' 
          ? 'using System;\nConsole.WriteLine($"UtcNow: {DateTime.UtcNow:O}");' 
          : kernel === 'python'
          ? 'print("Executing in CSnakes Python bridge")'
          : 'console.log("Interactive Node.js execution");',
        output: '',
        running: false
      }
    ];
  }

  function removeCell(id: string) {
    cells = cells.filter(c => c.id !== id);
  }

  function clearAllOutputs() {
    cells = cells.map(c => ({ ...c, output: '' }));
  }
</script>

<div class="kernel-page">
  <!-- HEADER -->
  <header class="kernel-header">
    <div class="header-info">
      <h2>⚡ Interactive Kernel & Microsoft Interactive Server</h2>
      <span class="header-desc">Execute polyglot C# (.NET 8 Roslyn), Python 3 (CSnakes Bridge), and Node.js cells</span>
    </div>

    <!-- SERVER STATUS CARD -->
    <div class="server-status-pill" class:online={serverStatus === 'online'} class:offline={serverStatus === 'offline'}>
      <span class="status-dot"></span>
      <div class="status-texts">
        <span class="status-title">
          {serverStatus === 'online' ? '● Connected' : serverStatus === 'checking' ? '◐ Checking...' : '○ Local Fallback Ready'}
        </span>
        <span class="status-detail">{serverType || 'In-Process Engine'}</span>
      </div>
      <button class="btn-probe" on:click={checkServer} title="Re-check server connection">🔄 Probe</button>
    </div>
  </header>

  <!-- CONTROLS & SERVER CONFIG BAR -->
  <div class="server-config-bar">
    <div class="config-field">
      <span class="cfg-label">Microsoft Interactive / Jupyter Endpoint:</span>
      <input type="text" bind:value={serverEndpoint} class="endpoint-input" placeholder="http://localhost:8888" />
    </div>

    <div class="kernel-chips-row">
      <span class="cfg-label">Active Kernels:</span>
      {#each AVAILABLE_KERNELS as k}
        <button 
          class="kernel-chip" 
          class:active={activeKernel === k.id}
          on:click={() => (activeKernel = k.id)}
        >
          <span class="chip-dot"></span>
          <span class="chip-name">{k.name}</span>
        </button>
      {/each}
    </div>

    <div class="bar-actions">
      <button class="btn-add-cell" on:click={() => addCell(activeKernel as any)}>
        + Add {activeKernel.toUpperCase()} Cell
      </button>
      <button class="btn-clear-all" on:click={clearAllOutputs}>
        🧹 Clear Outputs
      </button>
    </div>
  </div>

  <div class="server-info-banner">
    <span>💡 <strong>Status:</strong> {serverMessage}</span>
    <span class="tui-hint">To start live server: <code>dotnet interactive jupyter --default-kernel csharp</code> or use TUI Option 4</span>
  </div>

  <!-- CELLS WORKSPACE -->
  <div class="cells-workspace">
    {#each cells as cell, i (cell.id)}
      <div class="cell-card">
        <!-- CELL HEADER -->
        <div class="cell-head">
          <div class="cell-head-left">
            <span class="cell-num">[{i + 1}]</span>
            <select bind:value={cell.kernel} class="cell-kernel-select">
              <option value="csharp">C# (.NET 8 Roslyn)</option>
              <option value="python">Python 3 (CSnakes)</option>
              <option value="javascript">JavaScript (Node.js)</option>
              <option value="bash">POSIX Bash</option>
            </select>
            {#if cell.timestamp}
              <span class="cell-time">Last run: {cell.timestamp} ({cell.durationMs}ms)</span>
            {/if}
          </div>

          <div class="cell-head-actions">
            <button 
              class="btn-run-cell" 
              disabled={cell.running}
              on:click={() => runCell(cell)}
            >
              {cell.running ? '⏳ Executing...' : '▶ Run Cell'}
            </button>
            <button class="btn-del-cell" on:click={() => removeCell(cell.id)} title="Delete cell">✕</button>
          </div>
        </div>

        <!-- CODE EDITOR -->
        <div class="cell-editor">
          <MonacoEditor
            height="180px"
            language={cell.kernel}
            bind:value={cell.code}
          />
        </div>

        <!-- CELL OUTPUT -->
        {#if cell.output}
          <div class="cell-output-box">
            <div class="output-label">Execution Console Output:</div>
            <pre class="output-pre">{cell.output}</pre>
          </div>
        {/if}
      </div>
    {/each}
  </div>
</div>

<style>
  .kernel-page {
    max-width: 1400px;
    margin: 0 auto;
    color: #e2e8f0;
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .kernel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 1rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .header-info h2 {
    margin: 0 0 0.25rem 0;
    font-size: 1.35rem;
    color: #f8fafc;
  }

  .header-desc {
    font-size: 0.8rem;
    color: #94a3b8;
  }

  .server-status-pill {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.4rem 0.85rem;
    border-radius: 0.5rem;
    background: #0f172a;
    border: 1px solid #1e293b;
  }

  .server-status-pill.online {
    border-color: rgba(16, 185, 129, 0.4);
    background: rgba(16, 185, 129, 0.08);
  }

  .server-status-pill.offline {
    border-color: rgba(245, 158, 11, 0.3);
    background: rgba(245, 158, 11, 0.06);
  }

  .status-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: #64748b;
  }

  .online .status-dot { background: #10b981; box-shadow: 0 0 8px rgba(16, 185, 129, 0.6); }
  .offline .status-dot { background: #f59e0b; }

  .status-texts {
    display: flex;
    flex-direction: column;
  }

  .status-title {
    font-size: 0.78rem;
    font-weight: 600;
    color: #f1f5f9;
  }

  .status-detail {
    font-size: 0.68rem;
    color: #94a3b8;
  }

  .btn-probe {
    background: rgba(255, 255, 255, 0.06);
    border: 1px solid rgba(255, 255, 255, 0.12);
    color: #38bdf8;
    font-size: 0.72rem;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .server-config-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    padding: 0.75rem 1rem;
    flex-wrap: wrap;
    gap: 0.75rem;
  }

  .config-field {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .cfg-label {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .endpoint-input {
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #38bdf8;
    font-size: 0.75rem;
    font-family: ui-monospace, monospace;
    padding: 0.3rem 0.6rem;
    border-radius: 0.25rem;
    width: 190px;
  }

  .kernel-chips-row {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .kernel-chip {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.08);
    color: #94a3b8;
    font-size: 0.72rem;
    padding: 0.25rem 0.55rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .kernel-chip.active {
    background: rgba(14, 165, 233, 0.15);
    border-color: rgba(14, 165, 233, 0.4);
    color: #38bdf8;
  }

  .chip-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: currentColor;
  }

  .bar-actions {
    display: flex;
    gap: 0.5rem;
  }

  .btn-add-cell {
    background: #0284c7;
    border: none;
    color: white;
    font-size: 0.78rem;
    font-weight: 600;
    padding: 0.35rem 0.75rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .btn-add-cell:hover { background: #0369a1; }

  .btn-clear-all {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    font-size: 0.75rem;
    padding: 0.35rem 0.65rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .server-info-banner {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem 0.85rem;
    background: #0b1120;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    font-size: 0.74rem;
    color: #cbd5e1;
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .server-info-banner code {
    background: #0f172a;
    padding: 0.15rem 0.4rem;
    border-radius: 0.2rem;
    color: #38bdf8;
    font-family: ui-monospace, monospace;
  }

  .cells-workspace {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
  }

  .cell-card {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    overflow: hidden;
  }

  .cell-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.45rem 0.85rem;
    background: #0b1120;
    border-bottom: 1px solid #1e293b;
  }

  .cell-head-left {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .cell-num {
    font-size: 0.75rem;
    color: #64748b;
    font-family: ui-monospace, monospace;
    font-weight: 600;
  }

  .cell-kernel-select {
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #38bdf8;
    font-size: 0.75rem;
    padding: 0.2rem 0.45rem;
    border-radius: 0.25rem;
  }

  .cell-time {
    font-size: 0.68rem;
    color: #64748b;
    font-family: ui-monospace, monospace;
  }

  .cell-head-actions {
    display: flex;
    gap: 0.4rem;
    align-items: center;
  }

  .btn-run-cell {
    background: #10b981;
    color: #042f2e;
    border: none;
    font-size: 0.75rem;
    font-weight: 700;
    padding: 0.25rem 0.7rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .btn-run-cell:hover:not(:disabled) { background: #059669; color: white; }

  .btn-del-cell {
    background: transparent;
    border: none;
    color: #64748b;
    cursor: pointer;
    font-size: 0.8rem;
    padding: 0 0.3rem;
  }

  .btn-del-cell:hover { color: #ef4444; }

  .cell-output-box {
    background: #07090e;
    border-top: 1px solid #1e293b;
    padding: 0.65rem 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .output-label {
    font-size: 0.68rem;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }

  .output-pre {
    margin: 0;
    font-size: 0.75rem;
    color: #38bdf8;
    font-family: ui-monospace, monospace;
    white-space: pre-wrap;
    max-height: 250px;
    overflow-y: auto;
  }
</style>
