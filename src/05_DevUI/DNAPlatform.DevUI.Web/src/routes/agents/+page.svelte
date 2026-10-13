<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '$lib/services/api';
  
  let agents: any[] = [];
  let loading = true;
  let error = '';
  let showCreateModal = false;
  let newAgent = { name: '', type: 'AnalysisAgent', prompt: '' };
  
  onMount(async () => {
    await loadAgents();
  });
  
  async function loadAgents() {
    try {
      loading = true;
      agents = await api.getAgents();
      loading = false;
    } catch (e) {
      error = 'Failed to load agents';
      loading = false;
    }
  }
  
  async function createAgent() {
    if (!newAgent.name) return;
    try {
      await api.createAgent(newAgent);
      showCreateModal = false;
      newAgent = { name: '', type: 'AnalysisAgent', prompt: '' };
      await loadAgents();
    } catch (e) {
      alert('Error creating agent: ' + e);
    }
  }
  
  async function deleteAgent(id: string) {
    if (!confirm('Delete this agent?')) return;
    try {
      await api.deleteAgent(id);
      await loadAgents();
    } catch (e) {
      alert('Error deleting agent: ' + e);
    }
  }
</script>

<div class="agents-page">
  <div class="page-header">
    <h1>Agents</h1>
    <button class="btn-primary" on:click={() => showCreateModal = true}>+ New Agent</button>
  </div>
  <p class="page-description">Manage AI agents for intelligent workflow execution.</p>
  
  {#if loading}
    <div class="loading">Loading agents...</div>
  {:else if error}
    <div class="error-card">{error}</div>
  {:else if agents.length === 0}
    <div class="empty-state">
      <span class="empty-icon">🤖</span>
      <h3>No Agents Configured</h3>
      <p>Create an agent to enable AI-powered workflow execution.</p>
      <button class="btn-primary" on:click={() => showCreateModal = true}>Create First Agent</button>
    </div>
  {:else}
    <div class="agents-grid">
      {#each agents as agent}
        <div class="agent-card">
          <div class="agent-header">
            <div class="agent-avatar">🤖</div>
            <div class="agent-info">
              <h3>{agent.name}</h3>
              <span class="agent-type">{agent.type}</span>
            </div>
          </div>
          {#if agent.prompt}
            <p class="agent-prompt">{agent.prompt}</p>
          {/if}
          <div class="agent-actions">
            <button class="btn-small btn-danger" on:click={() => deleteAgent(agent.id)}>🗑 Delete</button>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

{#if showCreateModal}
  <div class="modal-overlay" on:click|self={() => showCreateModal = false}>
    <div class="modal">
      <h2>Create New Agent</h2>
      <div class="form-group">
        <label for="name">Agent Name</label>
        <input id="name" type="text" bind:value={newAgent.name} placeholder="e.g., Gene Analysis Agent" />
      </div>
      <div class="form-group">
        <label for="type">Agent Type</label>
        <select id="type" bind:value={newAgent.type}>
          <option value="AnalysisAgent">Analysis Agent</option>
          <option value="ValidationAgent">Validation Agent</option>
          <option value="TransformAgent">Transform Agent</option>
        </select>
      </div>
      <div class="form-group">
        <label for="prompt">System Prompt</label>
        <textarea id="prompt" bind:value={newAgent.prompt} placeholder="Describe the agent's role..."></textarea>
      </div>
      <div class="modal-actions">
        <button class="btn-secondary" on:click={() => showCreateModal = false}>Cancel</button>
        <button class="btn-primary" on:click={createAgent}>Create</button>
      </div>
    </div>
  </div>
{/if}

<style>
  .agents-page { max-width: 1200px; }
  .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; }
  .page-header h1 { font-size: 1.75rem; font-weight: 700; color: #f1f5f9; margin: 0; }
  .page-description { color: #94a3b8; margin: 0 0 2rem 0; }
  .btn-primary { background: var(--primary); color: white; border: none; padding: 0.625rem 1.25rem; border-radius: 0.375rem; cursor: pointer; font-weight: 500; }
  .btn-primary:hover { opacity: 0.9; }
  .btn-secondary { background: var(--bg-card); color: #e2e8f0; border: 1px solid var(--border); padding: 0.625rem 1.25rem; border-radius: 0.375rem; cursor: pointer; }
  .btn-small { background: rgba(14, 165, 233, 0.1); color: var(--primary); border: 1px solid rgba(14, 165, 233, 0.3); padding: 0.375rem 0.75rem; border-radius: 0.25rem; cursor: pointer; font-size: 0.8rem; }
  .btn-danger { background: rgba(239, 68, 68, 0.1); color: #ef4444; border-color: rgba(239, 68, 68, 0.3); }
  .loading { text-align: center; padding: 3rem; color: #94a3b8; }
  .error-card { background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); border-radius: 0.5rem; padding: 1rem; color: #ef4444; }
  .empty-state { text-align: center; padding: 4rem; background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; }
  .empty-icon { font-size: 3rem; }
  .empty-state h3 { color: #e2e8f0; margin: 1rem 0 0.5rem 0; }
  .empty-state p { color: #94a3b8; margin: 0 0 1.5rem 0; }
  .agents-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1rem; }
  .agent-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.25rem; }
  .agent-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1rem; }
  .agent-avatar { font-size: 2rem; }
  .agent-info h3 { font-size: 1rem; font-weight: 600; color: #e2e8f0; margin: 0; }
  .agent-type { font-size: 0.75rem; color: var(--primary); }
  .agent-prompt { color: #94a3b8; font-size: 0.85rem; margin: 0 0 1rem 0; line-height: 1.4; }
  .agent-actions { display: flex; gap: 0.5rem; }
  .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.7); display: flex; align-items: center; justify-content: center; z-index: 100; }
  .modal { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.5rem; width: 100%; max-width: 500px; }
  .modal h2 { font-size: 1.25rem; color: #e2e8f0; margin: 0 0 1.5rem 0; }
  .form-group { margin-bottom: 1rem; }
  .form-group label { display: block; color: #94a3b8; font-size: 0.875rem; margin-bottom: 0.5rem; }
  .form-group input, .form-group select, .form-group textarea { width: 100%; background: var(--bg-dark); border: 1px solid var(--border); border-radius: 0.375rem; padding: 0.625rem; color: #e2e8f0; font-size: 0.9rem; box-sizing: border-box; }
  .form-group textarea { min-height: 80px; resize: vertical; }
  .modal-actions { display: flex; justify-content: flex-end; gap: 0.75rem; margin-top: 1.5rem; }
</style>