<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '$lib/services/api';

  interface AgentRecord {
    id: string;
    name: string;
    type: string;
    prompt?: string | null;
    model?: string | null;
    isMcpActive?: boolean;
    assignedSkills?: string[];
    permissions?: string[];
    temperature?: number;
    maxTokens?: number;
    createdAt?: string;
  }

  const BUILTIN_MCP_AGENTS: AgentRecord[] = [
    {
      id: 'mcp-copilot-agent',
      name: 'Microsoft Copilot Bioinformatician',
      type: 'CopilotInferenceAgent',
      prompt: 'You are Microsoft Copilot, an expert bioinformatician and DNA analysis assistant. You analyze nucleotide sequences, verify reading frames, and synthesize findings using local ONNX runtime reasoning.',
      model: 'Microsoft-Copilot-Local-ONNX',
      isMcpActive: true,
      assignedSkills: [
        'csharp-runtime/get_current_time',
        'sequence-aligner',
        'dna-bioinformatics/reverse_complement',
        'dna-bioinformatics/gc_skew_analysis'
      ],
      permissions: ['Inference', 'MCP-Tool-Execute', 'FS-Read'],
      temperature: 0.7,
      maxTokens: 512
    },
    {
      id: 'mcp-kilo-agent',
      name: 'Kilo Notebook Data Agent',
      type: 'NotebookAnalysisAgent',
      prompt: 'You are Kilo, a notebook-first data analysis agent. Use workflow nodes and data cells as your active working surface. Run iterative cell checks and cite verifiable data artifacts.',
      model: 'Phi-3.5-mini-ONNX',
      isMcpActive: true,
      assignedSkills: [
        'csharp-runtime/get_current_time',
        'data-transform',
        'validation',
        'filesystem-workspace/read_file'
      ],
      permissions: ['Workspace-Storage', 'Inference', 'MCP-Tool-Execute'],
      temperature: 0.4,
      maxTokens: 768
    },
    {
      id: 'mcp-genomic-aligner',
      name: 'Genomic Sequence Aligner Agent',
      type: 'GenomicAnalysisAgent',
      prompt: 'Specialized pairwise alignment and codon translation agent. Performs Needleman-Wunsch dynamic programming and checks codon tables for open reading frames.',
      model: 'Biomedical-Copilot-v1',
      isMcpActive: true,
      assignedSkills: [
        'sequence-aligner',
        'dna-bioinformatics/dna_to_protein',
        'fasta-loader'
      ],
      permissions: ['MCP-Tool-Execute'],
      temperature: 0.2,
      maxTokens: 512
    }
  ];

  const AVAILABLE_SKILLS = [
    { id: 'csharp-runtime/get_current_time', label: '⏱️ C# System.DateTime Clock Provider' },
    { id: 'sequence-aligner', label: '🔬 Needleman-Wunsch Pairwise Aligner' },
    { id: 'dna-bioinformatics/reverse_complement', label: '🧬 DNA Reverse Complement' },
    { id: 'dna-bioinformatics/gc_skew_analysis', label: '📊 GC Skew & Percentage Analysis' },
    { id: 'dna-bioinformatics/dna_to_protein', label: '🧪 Codon Translation to Protein' },
    { id: 'fasta-loader', label: '📁 FASTA Multi-Sequence Loader' },
    { id: 'data-transform', label: '🔄 Data Transform (Upper, Lower, Clean)' },
    { id: 'validation', label: '✅ Sequence IUPAC Validator' },
    { id: 'filesystem-workspace/read_file', label: '📂 Workspace File Reader' },
    { id: 'system-runtime/system_status', label: '⚡ System Runtime Diagnostics' }
  ];

  const AVAILABLE_PERMISSIONS = [
    { id: 'Inference', label: 'Local Neural Inference (ONNX)' },
    { id: 'MCP-Tool-Execute', label: 'Execute MCP Registered Tools' },
    { id: 'FS-Read', label: 'Read Workspace Files' },
    { id: 'Workspace-Storage', label: 'Write & Save Artifacts' }
  ];

  let localAgents: any[] = [];
  let allAgents: AgentRecord[] = [];
  let loading = true;
  let error = '';
  let message = '';

  // Create Modal
  let showCreateModal = false;
  let newAgent = {
    name: '',
    type: 'AnalysisAgent',
    prompt: 'You are an intelligent bioinformatic workflow agent.',
    model: 'Microsoft-Copilot-Local-ONNX',
    assignedSkills: ['csharp-runtime/get_current_time', 'sequence-aligner'],
    permissions: ['Inference', 'MCP-Tool-Execute']
  };

  // Edit Modal
  let editingAgent: AgentRecord | null = null;
  let editForm = {
    name: '',
    type: '',
    prompt: '',
    model: 'Microsoft-Copilot-Local-ONNX',
    assignedSkills: [] as string[],
    permissions: [] as string[],
    temperature: 0.7,
    maxTokens: 512
  };

  // Test Agent Drawer
  let testingAgent: AgentRecord | null = null;
  let testPrompt = 'Report the current time using C# and analyze DNA sequence: ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC';
  let testRunning = false;
  let testResponse: any = null;
  let testError = '';

  onMount(async () => {
    await loadAgents();
  });

  async function loadAgents() {
    try {
      loading = true;
      localAgents = await api.getAgents();

      // Merge local DB agents with built-in MCP agents
      const parsedLocal: AgentRecord[] = (localAgents || []).map((a: any) => {
        let skills = ['csharp-runtime/get_current_time', 'sequence-aligner'];
        let perms = ['Inference', 'MCP-Tool-Execute'];
        let promptText = a.prompt || '';

        // Extract JSON metadata if stored in prompt
        if (promptText.startsWith('{') && promptText.includes('systemPrompt')) {
          try {
            const meta = JSON.parse(promptText);
            promptText = meta.systemPrompt || '';
            skills = meta.skills || skills;
            perms = meta.permissions || perms;
          } catch {
            // keep raw
          }
        }

        return {
          id: a.id,
          name: a.name,
          type: a.type || 'CustomAgent',
          prompt: promptText,
          model: a.model || 'Microsoft-Copilot-Local-ONNX',
          isMcpActive: false,
          assignedSkills: skills,
          permissions: perms,
          temperature: 0.7,
          maxTokens: 512,
          createdAt: a.createdAt
        };
      });

      allAgents = [...BUILTIN_MCP_AGENTS, ...parsedLocal];
    } catch (e: any) {
      error = 'Failed to load agents: ' + (e.message || String(e));
    } finally {
      loading = false;
    }
  }

  function openEdit(agent: AgentRecord) {
    editingAgent = agent;
    editForm = {
      name: agent.name,
      type: agent.type,
      prompt: agent.prompt || '',
      model: agent.model || 'Microsoft-Copilot-Local-ONNX',
      assignedSkills: [...(agent.assignedSkills || [])],
      permissions: [...(agent.permissions || ['Inference'])],
      temperature: agent.temperature ?? 0.7,
      maxTokens: agent.maxTokens ?? 512
    };
  }

  function toggleSkill(skillId: string) {
    if (editForm.assignedSkills.includes(skillId)) {
      editForm.assignedSkills = editForm.assignedSkills.filter((s) => s !== skillId);
    } else {
      editForm.assignedSkills = [...editForm.assignedSkills, skillId];
    }
  }

  function togglePermission(permId: string) {
    if (editForm.permissions.includes(permId)) {
      editForm.permissions = editForm.permissions.filter((p) => p !== permId);
    } else {
      editForm.permissions = [...editForm.permissions, permId];
    }
  }

  async function saveAgentEdit() {
    if (!editingAgent) return;

    if (editingAgent.isMcpActive) {
      // Update in-memory built-in MCP agent
      editingAgent.name = editForm.name;
      editingAgent.prompt = editForm.prompt;
      editingAgent.model = editForm.model;
      editingAgent.assignedSkills = [...editForm.assignedSkills];
      editingAgent.permissions = [...editForm.permissions];
      editingAgent.temperature = editForm.temperature;
      editingAgent.maxTokens = editForm.maxTokens;
      allAgents = [...allAgents];
      editingAgent = null;
      message = 'MCP Active Agent configuration updated!';
      setTimeout(() => (message = ''), 3000);
      return;
    }

    // Persist to SQLite DB via PUT /api/agents/[id]
    try {
      const payloadPrompt = JSON.stringify({
        systemPrompt: editForm.prompt,
        skills: editForm.assignedSkills,
        permissions: editForm.permissions
      });

      const res = await fetch(`/api/agents/${editingAgent.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: editForm.name,
          type: editForm.type,
          prompt: payloadPrompt,
          model: editForm.model
        })
      });

      if (!res.ok) throw new Error('Failed to update agent');
      editingAgent = null;
      message = 'Agent saved locally to SQLite database!';
      setTimeout(() => (message = ''), 3000);
      await loadAgents();
    } catch (e: any) {
      alert('Error updating agent: ' + e.message);
    }
  }

  async function createAgent() {
    if (!newAgent.name) return;
    try {
      const payloadPrompt = JSON.stringify({
        systemPrompt: newAgent.prompt,
        skills: newAgent.assignedSkills,
        permissions: newAgent.permissions
      });

      await api.createAgent({
        name: newAgent.name,
        type: newAgent.type,
        prompt: payloadPrompt,
        model: newAgent.model
      });

      showCreateModal = false;
      newAgent = {
        name: '',
        type: 'AnalysisAgent',
        prompt: 'You are an intelligent bioinformatic workflow agent.',
        model: 'Microsoft-Copilot-Local-ONNX',
        assignedSkills: ['csharp-runtime/get_current_time', 'sequence-aligner'],
        permissions: ['Inference', 'MCP-Tool-Execute']
      };
      message = 'New agent saved to local database!';
      setTimeout(() => (message = ''), 3000);
      await loadAgents();
    } catch (e: any) {
      alert('Error creating agent: ' + e);
    }
  }

  async function deleteAgent(id: string) {
    if (!confirm('Delete this agent from local database?')) return;
    try {
      await api.deleteAgent(id);
      await loadAgents();
    } catch (e: any) {
      alert('Error deleting agent: ' + e);
    }
  }

  async function testAgentInference(agent: AgentRecord) {
    testingAgent = agent;
    testRunning = true;
    testError = '';
    testResponse = null;

    try {
      const res = await fetch('/api/copilot/infer', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          model: agent.model || 'Microsoft-Copilot-Local-ONNX',
          systemPrompt: agent.prompt || 'You are an AI assistant',
          userPrompt: testPrompt,
          availableTools: agent.assignedSkills || [],
          temperature: agent.temperature || 0.7,
          maxTokens: agent.maxTokens || 512
        })
      });

      const data = await res.json();
      if (!res.ok || !data.success) {
        testError = data.error || 'Inference failed';
      } else {
        testResponse = data.result;
      }
    } catch (e: any) {
      testError = e.message || 'Network failure during inference';
    } finally {
      testRunning = false;
    }
  }
</script>

<div class="agents-page">
  <div class="page-header">
    <div>
      <h1>Agents & Microsoft Copilot</h1>
      <p class="page-description">
        Manage intelligent Copilot agents, assign real MCP skills & C# clock providers, and configure permissions.
      </p>
    </div>
    <div class="header-actions">
      <button class="btn primary" on:click={() => (showCreateModal = true)}>+ Create New Agent</button>
    </div>
  </div>

  {#if message}
    <div class="alert info">{message}</div>
  {/if}
  {#if error}
    <div class="alert error">{error}</div>
  {/if}

  {#if loading}
    <div class="loading">Loading agents...</div>
  {:else}
    <div class="agents-grid">
      {#each allAgents as agent}
        <div class="agent-card" class:mcp-active={agent.isMcpActive}>
          <div class="agent-top">
            <div class="avatar-wrap">
              <span class="avatar">🤖</span>
            </div>
            <div class="agent-title-info">
              <h3>{agent.name}</h3>
              <div class="meta-row">
                <span class="agent-type">{agent.type}</span>
                {#if agent.isMcpActive}
                  <span class="badge active-mcp">● Active (MCP Server)</span>
                {:else}
                  <span class="badge local-db">💾 Local DB</span>
                {/if}
              </div>
            </div>
          </div>

          <div class="model-badge">
            <span class="model-icon">⚡</span>
            <span>{agent.model || 'Microsoft-Copilot-Local-ONNX'}</span>
          </div>

          <div class="agent-prompt-box">
            <span class="section-label">System Directives:</span>
            <p>{agent.prompt || 'No system prompt configured.'}</p>
          </div>

          <!-- ASSIGNED MCP SKILLS -->
          <div class="skills-box">
            <span class="section-label">Assigned MCP Tools & Skills:</span>
            <div class="skill-chips">
              {#if agent.assignedSkills && agent.assignedSkills.length > 0}
                {#each agent.assignedSkills as skill}
                  <span class="skill-chip">
                    {skill === 'csharp-runtime/get_current_time' ? '⏱️ C# Time' : skill}
                  </span>
                {/each}
              {:else}
                <span class="no-skills">No tools attached</span>
              {/if}
            </div>
          </div>

          <!-- PERMISSIONS -->
          <div class="perms-box">
            {#each agent.permissions || [] as perm}
              <span class="perm-tag">✓ {perm}</span>
            {/each}
          </div>

          <div class="agent-actions">
            <button class="btn secondary small" on:click={() => openEdit(agent)}>
              ⚙️ Edit & Assign Skills
            </button>
            <button class="btn primary small" on:click={() => testAgentInference(agent)}>
              ▶ Test Agent
            </button>
            {#if !agent.isMcpActive}
              <button class="btn danger small" on:click={() => deleteAgent(agent.id)}>
                🗑
              </button>
            {/if}
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

<!-- EDIT AGENT MODAL -->
{#if editingAgent}
  <div class="modal-backdrop" on:click|self={() => (editingAgent = null)}>
    <div class="modal-card wide">
      <div class="modal-head">
        <h3>Configure Agent: {editingAgent.name}</h3>
        <button class="btn-close" on:click={() => (editingAgent = null)}>✕</button>
      </div>

      <div class="form-grid">
        <div class="field">
          <label>Agent Name</label>
          <input type="text" bind:value={editForm.name} />
        </div>

        <div class="field">
          <label>Inference Model</label>
          <select bind:value={editForm.model}>
            <option value="Microsoft-Copilot-Local-ONNX">Microsoft Copilot ONNX (Local CPU)</option>
            <option value="Phi-3.5-mini-ONNX">Microsoft Phi-3.5-mini ONNX</option>
            <option value="Biomedical-Copilot-v1">Biomedical Copilot Reasoning Engine</option>
          </select>
        </div>
      </div>

      <div class="field">
        <label>System Prompt (Persona & Instructions)</label>
        <textarea rows="3" bind:value={editForm.prompt}></textarea>
      </div>

      <!-- ASSIGN MCP SKILLS CHECKLIST -->
      <div class="field">
        <label>Assign MCP Tools & Bioinformatic Skills</label>
        <div class="skills-checklist">
          {#each AVAILABLE_SKILLS as skill}
            <label class="check-item">
              <input
                type="checkbox"
                checked={editForm.assignedSkills.includes(skill.id)}
                on:change={() => toggleSkill(skill.id)}
              />
              <span>{skill.label}</span>
              <code class="tool-id">{skill.id}</code>
            </label>
          {/each}
        </div>
      </div>

      <!-- PERMISSIONS CHECKLIST -->
      <div class="field">
        <label>Agent Execution Permissions</label>
        <div class="perms-checklist">
          {#each AVAILABLE_PERMISSIONS as perm}
            <label class="check-item inline">
              <input
                type="checkbox"
                checked={editForm.permissions.includes(perm.id)}
                on:change={() => togglePermission(perm.id)}
              />
              <span>{perm.label}</span>
            </label>
          {/each}
        </div>
      </div>

      <div class="modal-actions">
        <button class="btn secondary" on:click={() => (editingAgent = null)}>Cancel</button>
        <button class="btn primary" on:click={saveAgentEdit}>Save Agent Configuration</button>
      </div>
    </div>
  </div>
{/if}

<!-- CREATE MODAL -->
{#if showCreateModal}
  <div class="modal-backdrop" on:click|self={() => (showCreateModal = false)}>
    <div class="modal-card">
      <div class="modal-head">
        <h3>Create New Intelligent Agent</h3>
        <button class="btn-close" on:click={() => (showCreateModal = false)}>✕</button>
      </div>

      <div class="field">
        <label>Agent Name</label>
        <input type="text" bind:value={newAgent.name} placeholder="e.g. Gene Variant Specialist" />
      </div>

      <div class="field">
        <label>Agent Type</label>
        <select bind:value={newAgent.type}>
          <option value="AnalysisAgent">Bioinformatic Analysis Agent</option>
          <option value="ValidationAgent">Quality & Validation Agent</option>
          <option value="TransformAgent">Data Transformation Agent</option>
          <option value="CopilotInferenceAgent">Copilot ONNX Agent</option>
        </select>
      </div>

      <div class="field">
        <label>System Prompt (Directives)</label>
        <textarea rows="3" bind:value={newAgent.prompt}></textarea>
      </div>

      <div class="field">
        <label>Inference Model</label>
        <select bind:value={newAgent.model}>
          <option value="Microsoft-Copilot-Local-ONNX">Microsoft Copilot ONNX</option>
          <option value="Phi-3.5-mini-ONNX">Phi-3.5-mini ONNX</option>
          <option value="Biomedical-Copilot-v1">Biomedical Copilot</option>
        </select>
      </div>

      <div class="modal-actions">
        <button class="btn secondary" on:click={() => (showCreateModal = false)}>Cancel</button>
        <button class="btn primary" on:click={createAgent}>Save to Database</button>
      </div>
    </div>
  </div>
{/if}

<!-- TEST DRAWER -->
{#if testingAgent}
  <div class="modal-backdrop" on:click|self={() => (testingAgent = null)}>
    <div class="modal-card wide">
      <div class="modal-head">
        <h3>▶ Test Agent: {testingAgent.name}</h3>
        <button class="btn-close" on:click={() => (testingAgent = null)}>✕</button>
      </div>

      <div class="field">
        <label>Test User Prompt</label>
        <textarea rows="3" bind:value={testPrompt}></textarea>
      </div>

      <button
        class="btn primary"
        on:click={() => testAgentInference(testingAgent!)}
        disabled={testRunning}
      >
        {testRunning ? 'Running Copilot ONNX Inference...' : '🚀 Execute Inference'}
      </button>

      {#if testError}
        <div class="alert error">{testError}</div>
      {/if}

      {#if testResponse}
        <div class="response-panel">
          <div class="section-label">Copilot Response:</div>
          <pre class="response-text">{testResponse.response}</pre>

          {#if testResponse.toolCalls && testResponse.toolCalls.length > 0}
            <div class="section-label">Invoked MCP Tools ({testResponse.toolCalls.length}):</div>
            <div class="tool-calls-grid">
              {#each testResponse.toolCalls as tc}
                <div class="tool-call-card">
                  <strong>{tc.toolName}</strong>
                  <pre>{JSON.stringify(tc.result, null, 2)}</pre>
                </div>
              {/each}
            </div>
          {/if}

          {#if testResponse.onnxExecution}
            <div class="onnx-meta">
              <span>Runtime: {testResponse.onnxExecution.runtime}</span>
              <span>FLOPs: {testResponse.onnxExecution.flopsEstimated}</span>
              <span>Tokens/s: {testResponse.onnxExecution.tokensPerSecond}</span>
            </div>
          {/if}
        </div>
      {/if}
    </div>
  </div>
{/if}

<style>
  .agents-page {
    max-width: 1200px;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
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
    margin: 0.25rem 0 0 0;
    font-size: 0.88rem;
  }

  .agents-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
    gap: 1.25rem;
  }

  .agent-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
    transition: all 0.15s ease;
  }

  .agent-card:hover {
    border-color: rgba(56, 189, 248, 0.4);
    transform: translateY(-2px);
  }

  .agent-card.mcp-active {
    border-color: rgba(168, 85, 247, 0.35);
  }

  .agent-top {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .avatar-wrap {
    font-size: 2rem;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 0.5rem;
    width: 48px;
    height: 48px;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .agent-title-info h3 {
    margin: 0;
    font-size: 1.05rem;
    color: #f1f5f9;
  }

  .meta-row {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    margin-top: 0.25rem;
  }

  .agent-type {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .badge {
    font-size: 0.68rem;
    padding: 0.15rem 0.45rem;
    border-radius: 9999px;
    font-weight: 600;
  }

  .badge.active-mcp {
    background: rgba(168, 85, 247, 0.2);
    color: #c084fc;
    border: 1px solid rgba(168, 85, 247, 0.4);
  }

  .badge.local-db {
    background: rgba(34, 197, 94, 0.15);
    color: #4ade80;
    border: 1px solid rgba(34, 197, 94, 0.3);
  }

  .model-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    background: rgba(14, 165, 233, 0.12);
    color: #38bdf8;
    border: 1px solid rgba(14, 165, 233, 0.25);
    border-radius: 0.375rem;
    padding: 0.25rem 0.6rem;
    font-size: 0.75rem;
    font-family: ui-monospace, monospace;
    align-self: flex-start;
  }

  .section-label {
    font-size: 0.7rem;
    text-transform: uppercase;
    color: #64748b;
    font-weight: 600;
    display: block;
    margin-bottom: 0.25rem;
  }

  .agent-prompt-box p {
    margin: 0;
    font-size: 0.8rem;
    color: #cbd5e1;
    line-height: 1.4;
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .skill-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
  }

  .skill-chip {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #e2e8f0;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.72rem;
  }

  .no-skills {
    color: #64748b;
    font-size: 0.75rem;
    font-style: italic;
  }

  .perms-box {
    display: flex;
    gap: 0.4rem;
    flex-wrap: wrap;
  }

  .perm-tag {
    font-size: 0.68rem;
    color: #10b981;
    background: rgba(16, 185, 129, 0.1);
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
  }

  .agent-actions {
    display: flex;
    gap: 0.5rem;
    margin-top: auto;
    padding-top: 0.75rem;
    border-top: 1px solid rgba(255, 255, 255, 0.05);
  }

  .btn {
    padding: 0.4rem 0.8rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.82rem;
    font-weight: 500;
    transition: all 0.15s;
    border: none;
  }

  .btn.small {
    padding: 0.35rem 0.65rem;
    font-size: 0.75rem;
  }

  .btn.primary {
    background: var(--primary);
    color: white;
  }

  .btn.secondary {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.15);
    color: #e2e8f0;
  }

  .btn.danger {
    background: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.3);
    color: #f87171;
  }

  /* MODAL */
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.75);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 60;
    padding: 1rem;
  }

  .modal-card {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1.5rem;
    width: 480px;
    max-width: 95vw;
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .modal-card.wide {
    width: 650px;
    max-height: 90vh;
    overflow-y: auto;
  }

  .modal-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .modal-head h3 {
    margin: 0;
    color: #f1f5f9;
    font-size: 1.15rem;
  }

  .btn-close {
    background: transparent;
    border: none;
    color: #94a3b8;
    font-size: 1.1rem;
    cursor: pointer;
  }

  .form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem;
  }

  .field {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .field label {
    font-size: 0.78rem;
    color: #94a3b8;
    font-weight: 500;
  }

  .field input,
  .field select,
  .field textarea {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.15);
    border-radius: 0.375rem;
    padding: 0.5rem 0.75rem;
    color: #f1f5f9;
    font-size: 0.82rem;
    box-sizing: border-box;
  }

  .skills-checklist {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    max-height: 180px;
    overflow-y: auto;
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 0.375rem;
    padding: 0.5rem;
  }

  .perms-checklist {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .check-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.8rem;
    color: #cbd5e1;
    cursor: pointer;
  }

  .check-item.inline {
    background: rgba(255, 255, 255, 0.04);
    padding: 0.3rem 0.6rem;
    border-radius: 0.25rem;
    border: 1px solid rgba(255, 255, 255, 0.08);
  }

  .tool-id {
    margin-left: auto;
    font-size: 0.68rem;
    color: #64748b;
  }

  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    margin-top: 0.5rem;
  }

  .response-panel {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 0.375rem;
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .response-text {
    margin: 0;
    color: #38bdf8;
    font-size: 0.82rem;
    font-family: ui-monospace, monospace;
    white-space: pre-wrap;
  }

  .tool-calls-grid {
    display: grid;
    grid-template-columns: 1fr;
    gap: 0.5rem;
  }

  .tool-call-card {
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.25rem;
    padding: 0.4rem 0.6rem;
  }

  .tool-call-card strong {
    color: #a855f7;
    font-size: 0.75rem;
  }

  .tool-call-card pre {
    margin: 0.2rem 0 0 0;
    font-size: 0.72rem;
    color: #94a3b8;
  }

  .onnx-meta {
    display: flex;
    gap: 1rem;
    font-size: 0.72rem;
    color: #64748b;
    border-top: 1px solid rgba(255, 255, 255, 0.05);
    padding-top: 0.4rem;
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
</style>
