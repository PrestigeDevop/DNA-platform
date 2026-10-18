<script lang="ts">
  import { onMount } from 'svelte';

  interface Tool {
    Name: string;
    Description: string;
    InputSchema?: Record<string, { propName: string; description?: string }> | null;
  }

  interface Server {
    ServerId: string;
    Name: string;
    Version: string;
    Status: string;
    Category?: string;
    Message?: string;
    Tools: Tool[];
  }

  interface MarketplaceItem {
    id: string;
    name: string;
    author: string;
    version: string;
    description: string;
    category: string;
    icon: string;
    installed: boolean;
    featured: boolean;
    tags: string[];
    toolsCount: number;
    agentTemplate?: any;
  }

  interface NotebookCell {
    id: string;
    language: string;
    source: string;
    outputs?: any[];
    output?: string;
    running?: boolean;
    durationMs?: number;
  }

  let activeTab: 'marketplace' | 'servers' | 'test' | 'polyglot' | 'author' = 'marketplace';
  let servers: Server[] = [];
  let marketplaceItems: MarketplaceItem[] = [];
  let loading = true;
  let error: string | null = null;
  let message = '';

  // Filter & Search
  let searchQuery = '';
  let selectedCategory = 'All';
  const categories = ['All', 'AI & Inference', 'Bioinformatics', 'System', 'Agents', 'Smart Component', 'Polyglot'];

  // Test tool state
  let selectedServerId = '';
  let selectedToolName = '';
  let toolArgsText = '{\n  "sequence": "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC"\n}';
  let toolRunning = false;
  let toolResult: any = null;
  let toolError = '';

  // Import modal
  let showImportModal = false;
  let importText = `{
  "serverId": "custom-dna-annotator",
  "name": "Custom DNA Annotator",
  "version": "1.0.0",
  "category": "Extensions",
  "description": "High-throughput genomic feature annotation",
  "tools": [
    { "name": "annotate_genes", "description": "Annotate coding sequences" }
  ]
}`;

  // Polyglot Notebook State
  let notebookCells: NotebookCell[] = [];
  let notebookTitle = 'DryLab_Poc_.ipynb (Jupyter Notebook)';
  let activeKernel = 'Polyglot (.NET C# / Python / Node)';
  let loadingNotebook = false;

  // Authoring Studio State
  let authorName = 'DNA Mutation Analyzer';
  let authorCategory = 'Bioinformatics';
  let authorIcon = '🔬';
  let authorScript = `// Custom DNA Mutation Analyzer
const seq = String(input).toUpperCase();
const mutations = [];
for (let i = 0; i < seq.length; i++) {
  if (seq[i] === 'G' && i % 3 === 0) {
    mutations.push({ position: i + 1, original: 'A', mutated: 'G', type: 'transition' });
  }
}
return {
  analyzed_sequence: seq,
  mutation_count: mutations.length,
  mutations,
  summary: mutations.length > 0 ? "Potential genomic variants detected" : "Conserved sequence"
};`;
  let authoringSaving = false;

  onMount(async () => {
    await refreshAll();
    await loadNotebook();
  });

  async function refreshAll() {
    loading = true;
    error = null;
    try {
      const [resServers, resMarket] = await Promise.all([
        fetch('/api/mcp/servers'),
        fetch('/api/mcp/marketplace')
      ]);

      if (resServers.ok) {
        const data = await resServers.json();
        servers = data.Servers || [];
        if (servers.length > 0 && !selectedServerId) {
          selectedServerId = servers[0].ServerId;
          if (servers[0].Tools.length > 0) {
            selectedToolName = servers[0].Tools[0].Name;
          }
        }
      }

      if (resMarket.ok) {
        const mData = await resMarket.json();
        marketplaceItems = mData.plugins || [];
      }
    } catch (e: any) {
      error = e.message || 'Failed to load MCP servers';
    } finally {
      loading = false;
    }
  }

  async function loadNotebook() {
    loadingNotebook = true;
    try {
      const res = await fetch('/api/polyglot/notebook');
      if (res.ok) {
        const data = await res.json();
        notebookTitle = data.title || notebookTitle;
        activeKernel = data.kernel || activeKernel;
        notebookCells = data.cells || [];
      }
    } catch {
      // ignore
    } finally {
      loadingNotebook = false;
    }
  }

  async function runCell(cell: NotebookCell) {
    cell.running = true;
    cell.output = '';
    try {
      const res = await fetch('/api/polyglot/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: cell.language,
          code: cell.source
        })
      });
      const data = await res.json();
      if (res.ok && data.success) {
        cell.output = data.output;
        cell.durationMs = data.durationMs;
      } else {
        cell.output = `Error: ${data.error || 'Execution failed'}`;
      }
    } catch (e: any) {
      cell.output = `Network error: ${e.message}`;
    } finally {
      cell.running = false;
      notebookCells = [...notebookCells];
    }
  }

  function addCell(lang: 'csharp' | 'python' | 'javascript') {
    const templates: Record<string, string> = {
      csharp: '// C# DateTime Provider\nusing System;\nConsole.WriteLine($"Current Time (UTC): {DateTime.UtcNow:o}");\nConsole.WriteLine($"Ticks: {DateTime.UtcNow.Ticks}");',
      python: '# Python BioInformatics Cell\nseq = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC"\nprint(f"Sequence: {seq} (Length: {len(seq)} bp)")',
      javascript: '// JavaScript / Node.js Cell\nconst mem = process.memoryUsage();\nconsole.log(`Heap Used: ${(mem.heapUsed / 1024 / 1024).toFixed(2)} MB`);'
    };

    notebookCells = [
      ...notebookCells,
      {
        id: `cell-${Date.now()}`,
        language: lang,
        source: templates[lang] || '// New cell',
        output: ''
      }
    ];
  }

  async function toggleInstall(plugin: MarketplaceItem) {
    try {
      const action = plugin.installed ? 'uninstall' : 'install';
      const res = await fetch('/api/mcp/marketplace', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: plugin.id, action })
      });

      if (res.ok) {
        plugin.installed = !plugin.installed;
        marketplaceItems = [...marketplaceItems];
        message = `${plugin.name} ${action === 'install' ? 'installed and active' : 'uninstalled'}.`;
        setTimeout(() => (message = ''), 3000);
        await refreshAll();
      }
    } catch (e: any) {
      error = e.message || 'Failed to update plugin';
    }
  }

  async function runToolTest() {
    if (!selectedServerId || !selectedToolName) return;
    toolRunning = true;
    toolError = '';
    toolResult = null;

    try {
      let parsedArgs = {};
      try {
        parsedArgs = JSON.parse(toolArgsText);
      } catch {
        toolError = 'Arguments must be valid JSON';
        toolRunning = false;
        return;
      }

      const res = await fetch('/api/mcp/tools/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          serverId: selectedServerId,
          toolName: selectedToolName,
          args: parsedArgs
        })
      });

      const data = await res.json();
      if (!res.ok || !data.success) {
        toolError = data.error || 'Execution failed';
      } else {
        toolResult = data.result;
      }
    } catch (e: any) {
      toolError = e.message || 'Tool execution network failure';
    } finally {
      toolRunning = false;
    }
  }

  async function onImportManifest() {
    try {
      const res = await fetch('/api/mcp/servers/import', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: importText
      });
      if (!res.ok) throw new Error('Import failed');
      showImportModal = false;
      message = 'MCP server manifest imported successfully!';
      setTimeout(() => (message = ''), 3000);
      await refreshAll();
    } catch (e: any) {
      error = e.message || 'Failed to import server';
    }
  }

  async function onAuthorSkill() {
    if (!authorName.trim()) return;
    authoringSaving = true;
    try {
      const res = await fetch('/api/skills/author', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: authorName,
          category: authorCategory,
          icon: authorIcon,
          script: authorScript
        })
      });
      const data = await res.json();
      if (res.ok && data.success) {
        message = `Skill "${authorName}" created and annotated with Microsoft Copilot! Active in MCP registry.`;
        setTimeout(() => (message = ''), 4000);
        await refreshAll();
      } else {
        error = data.error || 'Failed to author skill';
      }
    } catch (e: any) {
      error = e.message || 'Error saving skill';
    } finally {
      authoringSaving = false;
    }
  }

  $: currentServerTools = servers.find((s) => s.ServerId === selectedServerId)?.Tools || [];

  $: filteredMarketplace = marketplaceItems.filter((item) => {
    if (selectedCategory !== 'All' && item.category !== selectedCategory) return false;
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      return (
        item.name.toLowerCase().includes(q) ||
        item.description.toLowerCase().includes(q) ||
        item.category.toLowerCase().includes(q) ||
        item.tags.some((t) => t.toLowerCase().includes(q))
      );
    }
    return true;
  });
</script>

<div class="mcp-shell">
  <div class="mcp-header">
    <div class="title-area">
      <div class="badge">Model Context Protocol</div>
      <h2>MCP Ecosystem & market_place</h2>
      <p class="subtitle">
        Extensible MCP tools, Microsoft Copilot local reasoning, C# .NET 8 runtime, and Polyglot Jupyter notebook kernel.
      </p>
    </div>
    <div class="top-actions">
      <button class="btn secondary" on:click={() => (showImportModal = true)}>➕ Import Manifest</button>
      <button class="btn primary" on:click={refreshAll}>🔄 Refresh Registry</button>
    </div>
  </div>

  {#if message}
    <div class="alert info">{message}</div>
  {/if}
  {#if error}
    <div class="alert error">{error}</div>
  {/if}

  <!-- TABS -->
  <div class="tabs">
    <button
      class="tab-btn"
      class:active={activeTab === 'marketplace'}
      on:click={() => (activeTab = 'marketplace')}
    >
      🛒 market_place ({marketplaceItems.length})
    </button>
    <button
      class="tab-btn"
      class:active={activeTab === 'servers'}
      on:click={() => (activeTab = 'servers')}
    >
      ⚡ Active MCP Servers ({servers.length})
    </button>
    <button
      class="tab-btn"
      class:active={activeTab === 'test'}
      on:click={() => (activeTab = 'test')}
    >
      🔬 Execute MCP Tool
    </button>
    <a
      href="/kernel"
      class="tab-btn"
      style="color: #38bdf8; text-decoration: none; border-color: #0284c7; font-weight: 600;"
    >
      💻 Kernel & Component Studio ↗
    </a>
  </div>

  <!-- TAB 1: market_place -->
  {#if activeTab === 'marketplace'}
    <div class="market-controls">
      <div class="search-bar">
        <span class="search-icon">🔍</span>
        <input
          type="text"
          bind:value={searchQuery}
          placeholder="Filter extensions, agents, smart components, bioinformatics..."
        />
      </div>
      <div class="category-chips">
        {#each categories as cat}
          <button
            class="cat-chip"
            class:active={selectedCategory === cat}
            on:click={() => (selectedCategory = cat)}
          >
            {cat}
          </button>
        {/each}
      </div>
    </div>

    <div class="marketplace-grid">
      {#each filteredMarketplace as item}
        <div class="market-card" class:installed={item.installed}>
          <div class="market-top">
            <span class="market-icon">{item.icon}</span>
            <div class="market-meta">
              <h3>{item.name}</h3>
              <span class="author">by {item.author} · v{item.version}</span>
            </div>
            {#if item.featured}
              <span class="pill featured">Featured</span>
            {/if}
          </div>

          <p class="market-desc">{item.description}</p>

          {#if item.agentTemplate}
            <div class="kilo-agent-box">
              <span class="kilo-badge">Agent Spec</span>
              <code>Mode: {item.agentTemplate.mode}</code>
              <p class="kilo-prompt">{item.agentTemplate.systemPrompt.slice(0, 85)}...</p>
            </div>
          {/if}

          <div class="market-tags">
            <span class="tag category">{item.category}</span>
            {#each item.tags as tag}
              <span class="tag">#{tag}</span>
            {/each}
          </div>

          <div class="market-footer">
            <span class="tools-count">{item.toolsCount} registered tool(s)</span>
            <button
              class="install-btn"
              class:installed={item.installed}
              on:click={() => toggleInstall(item)}
            >
              {item.installed ? '✓ Active' : '+ Install Plugin'}
            </button>
          </div>
        </div>
      {/each}
    </div>

  <!-- TAB 2: ACTIVE SERVERS -->
  {:else if activeTab === 'servers'}
    <div class="servers-list">
      {#each servers as s}
        <div class="server-item">
          <div class="server-head">
            <div>
              <h3>{s.Name}</h3>
              <span class="server-id"><code>{s.ServerId}</code> · v{s.Version}</span>
            </div>
            <span class="status-indicator live">● {s.Status}</span>
          </div>

          {#if s.Message}
            <p class="server-msg">{s.Message}</p>
          {/if}

          <div class="tools-container">
            <h4>Exposed MCP Tools:</h4>
            <div class="tools-grid">
              {#each s.Tools as t}
                <div class="tool-badge">
                  <strong>{t.Name}</strong>
                  <span>{t.Description}</span>
                </div>
              {/each}
            </div>
          </div>
        </div>
      {/each}
    </div>

  <!-- TAB 3: TOOL TEST CONSOLE -->
  {:else if activeTab === 'test'}
    <div class="test-console">
      <div class="test-panel inputs">
        <h3>Execute MCP Tool</h3>
        <div class="field">
          <label>Target MCP Server</label>
          <select
            bind:value={selectedServerId}
            on:change={() => (selectedToolName = currentServerTools[0]?.Name || '')}
          >
            {#each servers as s}
              <option value={s.ServerId}>{s.Name} ({s.ServerId})</option>
            {/each}
          </select>
        </div>

        <div class="field">
          <label>Select Tool</label>
          <select bind:value={selectedToolName}>
            {#each currentServerTools as t}
              <option value={t.Name}>{t.Name} — {t.Description}</option>
            {/each}
          </select>
        </div>

        <div class="field">
          <label>Tool Arguments (JSON)</label>
          <textarea rows="7" bind:value={toolArgsText}></textarea>
        </div>

        <div class="field presets-row">
          <span class="label-sub">Quick Arguments:</span>
          <button
            type="button"
            class="mini-btn"
            on:click={() => {
              if (selectedToolName === 'get_current_time') {
                toolArgsText = '{\n  "format": "o"\n}';
              } else if (selectedToolName === 'data_transform') {
                toolArgsText = '{\n  "data": "atggtgcacctgactcctgaggagaagtctgcc",\n  "mode": "reverse"\n}';
              } else if (selectedToolName === 'reverse_complement') {
                toolArgsText = '{\n  "sequence": "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC"\n}';
              } else {
                toolArgsText = '{\n  "sequence": "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC"\n}';
              }
            }}
          >
            Load Sample
          </button>
        </div>

        <button class="btn primary execute" on:click={runToolTest} disabled={toolRunning}>
          {toolRunning ? 'Executing...' : '▶ Run Tool Call'}
        </button>
      </div>

      <div class="test-panel results">
        <h3>Execution Output</h3>
        {#if toolError}
          <div class="alert error">{toolError}</div>
        {:else if toolResult}
          <pre class="result-code">{JSON.stringify(toolResult, null, 2)}</pre>
        {:else}
          <div class="empty-state">Select a tool and click "Run Tool Call" to inspect live response.</div>
        {/if}
      </div>
    </div>

  <!-- REDIRECT BANNER TO DEDICATED /kernel PAGE -->
  {:else if activeTab === 'polyglot' || activeTab === 'author'}
    <div class="polyglot-view" style="text-align: center; padding: 4rem 1rem;">
      <h3 style="font-size: 1.4rem; color: #f8fafc; margin-bottom: 0.5rem;">
        💻 Polyglot Kernel & Component Studio has moved!
      </h3>
      <p style="color: #94a3b8; max-width: 600px; margin: 0 auto 1.5rem auto;">
        The interactive multi-language kernel (real C#, Python CSnakes, JavaScript, Shell), Unity-style Smart Component authoring, and real-time Agent testing lab are now consolidated on the dedicated <strong>/kernel</strong> page.
      </p>
      <a href="/kernel" class="btn primary" style="display: inline-block; text-decoration: none; padding: 0.75rem 1.75rem; font-weight: 600; border-radius: 6px;">
        Open Kernel & Studio ↗
      </a>
    </div>
  {/if}
</div>

<style>
  .mcp-shell {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    margin-top: 1rem;
  }

  .mcp-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
  }

  .title-area h2 {
    font-size: 1.4rem;
    color: #f1f5f9;
    margin: 0.35rem 0 0.2rem 0;
  }

  .subtitle {
    color: #94a3b8;
    font-size: 0.85rem;
    margin: 0;
  }

  .badge {
    display: inline-block;
    padding: 0.15rem 0.5rem;
    background: rgba(14, 165, 233, 0.15);
    color: #38bdf8;
    border: 1px solid rgba(14, 165, 233, 0.3);
    border-radius: 9999px;
    font-size: 0.7rem;
    font-weight: 600;
  }

  .top-actions {
    display: flex;
    gap: 0.5rem;
  }

  .tabs {
    display: flex;
    gap: 0.5rem;
    border-bottom: 1px solid var(--border);
    padding-bottom: 0.5rem;
    flex-wrap: wrap;
  }

  .tab-btn {
    background: transparent;
    border: 1px solid transparent;
    color: #94a3b8;
    padding: 0.45rem 0.85rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.82rem;
    transition: all 0.15s;
  }

  .tab-btn:hover {
    color: #f1f5f9;
    background: rgba(255, 255, 255, 0.04);
  }

  .tab-btn.active {
    background: rgba(14, 165, 233, 0.15);
    color: #38bdf8;
    border-color: rgba(14, 165, 233, 0.3);
    font-weight: 600;
  }

  /* MARKETPLACE */
  .market-controls {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .search-bar {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: #0f172a;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 0.375rem;
    padding: 0.4rem 0.75rem;
  }

  .search-bar input {
    background: transparent;
    border: none;
    color: #f1f5f9;
    font-size: 0.85rem;
    width: 100%;
  }

  .search-bar input:focus {
    outline: none;
  }

  .category-chips {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;
  }

  .cat-chip {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    padding: 0.25rem 0.6rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    cursor: pointer;
  }

  .cat-chip.active {
    background: rgba(14, 165, 233, 0.2);
    border-color: #38bdf8;
    color: #38bdf8;
  }

  .marketplace-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    gap: 1rem;
  }

  .market-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    transition: transform 0.15s, border-color 0.15s;
  }

  .market-card:hover {
    border-color: rgba(56, 189, 248, 0.4);
    transform: translateY(-2px);
  }

  .market-card.installed {
    border-color: rgba(34, 197, 94, 0.4);
  }

  .market-top {
    display: flex;
    align-items: center;
    gap: 0.65rem;
  }

  .market-icon {
    font-size: 1.75rem;
  }

  .market-meta h3 {
    margin: 0;
    font-size: 0.95rem;
    color: #f1f5f9;
  }

  .market-meta .author {
    font-size: 0.72rem;
    color: #64748b;
  }

  .market-desc {
    font-size: 0.8rem;
    color: #94a3b8;
    line-height: 1.4;
    margin: 0;
  }

  .pill.featured {
    margin-left: auto;
    font-size: 0.65rem;
    padding: 0.15rem 0.4rem;
    background: rgba(245, 158, 11, 0.2);
    color: #fbbf24;
    border-radius: 9999px;
  }

  .kilo-agent-box {
    background: rgba(15, 23, 42, 0.6);
    border: 1px dashed rgba(255, 255, 255, 0.12);
    border-radius: 0.375rem;
    padding: 0.45rem;
    font-size: 0.72rem;
  }

  .kilo-badge {
    background: #0284c7;
    color: white;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
    font-weight: 600;
    margin-right: 0.4rem;
  }

  .kilo-prompt {
    margin: 0.3rem 0 0 0;
    color: #cbd5e1;
    font-style: italic;
  }

  .market-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 0.3rem;
  }

  .tag {
    font-size: 0.68rem;
    padding: 0.15rem 0.4rem;
    background: rgba(255, 255, 255, 0.04);
    border-radius: 0.25rem;
    color: #94a3b8;
  }

  .tag.category {
    background: rgba(14, 165, 233, 0.15);
    color: #38bdf8;
  }

  .market-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: auto;
    padding-top: 0.5rem;
    border-top: 1px solid rgba(255, 255, 255, 0.05);
  }

  .tools-count {
    font-size: 0.72rem;
    color: #64748b;
  }

  .install-btn {
    background: rgba(14, 165, 233, 0.2);
    color: #38bdf8;
    border: 1px solid rgba(14, 165, 233, 0.4);
    padding: 0.3rem 0.7rem;
    border-radius: 0.375rem;
    font-size: 0.75rem;
    cursor: pointer;
    font-weight: 600;
  }

  .install-btn.installed {
    background: rgba(34, 197, 94, 0.2);
    border-color: rgba(34, 197, 94, 0.4);
    color: #4ade80;
  }

  /* SERVERS LIST */
  .servers-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .server-item {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1rem;
  }

  .server-head {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
  }

  .server-head h3 {
    margin: 0;
    color: #f1f5f9;
    font-size: 1rem;
  }

  .server-id {
    font-size: 0.75rem;
    color: #64748b;
  }

  .status-indicator.live {
    color: #4ade80;
    font-size: 0.8rem;
  }

  .tools-container {
    margin-top: 0.85rem;
  }

  .tools-container h4 {
    margin: 0 0 0.5rem 0;
    font-size: 0.75rem;
    color: #94a3b8;
    text-transform: uppercase;
  }

  .tools-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    gap: 0.5rem;
  }

  .tool-badge {
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.375rem;
    padding: 0.45rem 0.6rem;
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
  }

  .tool-badge strong {
    color: #38bdf8;
    font-size: 0.78rem;
    font-family: ui-monospace, monospace;
  }

  .tool-badge span {
    color: #94a3b8;
    font-size: 0.72rem;
  }

  /* TEST CONSOLE */
  .test-console {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.25rem;
  }

  .test-panel {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .test-panel h3 {
    margin: 0;
    color: #f1f5f9;
    font-size: 0.95rem;
  }

  .field {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .field label {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .field select,
  .field input,
  .field textarea {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.15);
    border-radius: 0.375rem;
    padding: 0.45rem 0.6rem;
    color: #f1f5f9;
    font-size: 0.8rem;
    font-family: ui-monospace, monospace;
  }

  .presets-row {
    flex-direction: row;
    align-items: center;
    gap: 0.5rem;
  }

  .label-sub {
    font-size: 0.72rem;
    color: #64748b;
  }

  .mini-btn {
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(14, 165, 233, 0.3);
    color: #38bdf8;
    padding: 0.2rem 0.45rem;
    border-radius: 0.25rem;
    font-size: 0.7rem;
    cursor: pointer;
  }

  .result-code {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 0.375rem;
    padding: 0.75rem;
    font-size: 0.78rem;
    color: #38bdf8;
    overflow: auto;
    max-height: 380px;
  }

  /* POLYGLOT JUPYTER VIEW */
  .polyglot-view {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .polyglot-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 0.85rem 1.25rem;
  }

  .polyglot-header h3 {
    margin: 0;
    color: #f1f5f9;
    font-size: 1.05rem;
  }

  .polyglot-actions {
    display: flex;
    gap: 0.5rem;
  }

  .notebook-cells {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .cell-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 0.75rem 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .cell-head {
    display: flex;
    align-items: center;
    gap: 0.65rem;
  }

  .cell-index {
    color: #64748b;
    font-family: ui-monospace, monospace;
    font-size: 0.8rem;
  }

  .cell-lang {
    font-size: 0.68rem;
    padding: 0.15rem 0.4rem;
    border-radius: 0.2rem;
    font-weight: 700;
  }

  .badge-csharp {
    background: rgba(168, 85, 247, 0.2);
    color: #c084fc;
    border: 1px solid rgba(168, 85, 247, 0.4);
  }

  .badge-python {
    background: rgba(245, 158, 11, 0.2);
    color: #fbbf24;
    border: 1px solid rgba(245, 158, 11, 0.4);
  }

  .badge-javascript,
  .badge-node {
    background: rgba(34, 197, 94, 0.2);
    color: #4ade80;
    border: 1px solid rgba(34, 197, 94, 0.4);
  }

  .cell-dur {
    font-size: 0.72rem;
    color: #64748b;
  }

  .btn-run {
    margin-left: auto;
    background: rgba(34, 197, 94, 0.15);
    border: 1px solid rgba(34, 197, 94, 0.4);
    color: #4ade80;
    padding: 0.25rem 0.6rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    cursor: pointer;
    font-weight: 600;
  }

  .cell-editor {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 0.375rem;
    padding: 0.5rem 0.75rem;
    color: #f1f5f9;
    font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
    font-size: 0.82rem;
    line-height: 1.45;
  }

  .cell-output {
    background: rgba(0, 0, 0, 0.4);
    border-left: 3px solid #38bdf8;
    border-radius: 0.25rem;
    padding: 0.5rem 0.75rem;
  }

  .output-label {
    font-size: 0.68rem;
    color: #64748b;
    text-transform: uppercase;
  }

  .cell-output pre {
    margin: 0.25rem 0 0 0;
    color: #e2e8f0;
    font-family: ui-monospace, monospace;
    font-size: 0.8rem;
    white-space: pre-wrap;
  }

  /* AUTHORING STUDIO */
  .author-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.9rem;
  }

  .author-card h3 {
    margin: 0;
    color: #f1f5f9;
    font-size: 1.1rem;
  }

  .form-grid {
    display: grid;
    grid-template-columns: 2fr 1fr auto;
    gap: 0.75rem;
  }

  .code-textarea {
    font-family: ui-monospace, monospace;
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.15);
    border-radius: 0.375rem;
    padding: 0.6rem;
    color: #f1f5f9;
    font-size: 0.82rem;
  }

  .hint-text {
    font-size: 0.72rem;
    color: #64748b;
  }

  /* COMMON BUTTONS & ALERTS */
  .btn {
    padding: 0.45rem 0.9rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.82rem;
    font-weight: 500;
    transition: all 0.15s;
  }

  .btn.primary {
    background: var(--primary);
    color: white;
    border: none;
  }

  .btn.secondary {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #e2e8f0;
  }

  .alert {
    padding: 0.5rem 0.85rem;
    border-radius: 0.375rem;
    font-size: 0.82rem;
  }

  .alert.info {
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(14, 165, 233, 0.3);
    color: #38bdf8;
  }

  .alert.error {
    background: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.3);
    color: #f87171;
  }

  .empty-state {
    color: #64748b;
    font-style: italic;
    font-size: 0.82rem;
    text-align: center;
    padding: 2rem 0;
  }

  /* MODAL */
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 50;
  }

  .modal-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1.25rem;
    width: 480px;
    max-width: 90vw;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .modal-card h3 {
    margin: 0;
    color: #f1f5f9;
  }

  .modal-card textarea {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.15);
    border-radius: 0.375rem;
    padding: 0.5rem;
    color: #f1f5f9;
    font-family: ui-monospace, monospace;
    font-size: 0.8rem;
  }

  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
  }
</style>
