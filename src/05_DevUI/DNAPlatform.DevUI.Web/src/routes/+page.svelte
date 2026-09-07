<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '$lib/services/api';
  
  let health: any = null;
  let skills: any[] = [];
  let workflows: any[] = [];
  let agents: any[] = [];
  let executions: any[] = [];
  let loading = true;
  let error = '';
  
  onMount(async () => {
    try {
      // Fetch from .NET backend
      health = await api.health();
      skills = await api.getSkills();
      
      // Fetch from SvelteKit server endpoints (Prisma)
      workflows = await api.getWorkflows();
      agents = await api.getAgents();
      executions = await api.getExecutions();
      
      loading = false;
    } catch (e) {
      error = 'Failed to connect to backend. Make sure the API is running on port 5254.';
      loading = false;
    }
  });
  
  async function createSampleWorkflow() {
    const sampleWorkflow = {
      id: 'sample-' + Date.now(),
      name: 'Sample Bioinformatic Pipeline',
      nodes: [
        { id: 'input', name: 'FASTA Input', nodeType: 0, config: { source: 'sequences.fasta' } },
        { id: 'align', name: 'Sequence Alignment', nodeType: 2, config: { operation: 'align' } },
        { id: 'output', name: 'Results Output', nodeType: 4, config: { destination: 'results.json' } }
      ],
      connections: [
        { sourceNodeId: 'input', targetNodeId: 'align', outputKey: 'data', inputKey: 'input' },
        { sourceNodeId: 'align', targetNodeId: 'output', outputKey: 'result', inputKey: 'input' }
      ]
    };
    
    try {
      await api.executeWorkflow(sampleWorkflow);
      alert('Sample workflow created and executed successfully!');
    } catch (e) {
      alert('Error creating workflow: ' + e);
    }
  }
</script>

<div class="dashboard">
  <h1 class="page-heading">Dashboard</h1>
  <p class="subtitle">Welcome to DNA Platform - Low-Code Bioinformatic Workflow Engine</p>
  
  {#if loading}
    <div class="loading">
      <div class="spinner"></div>
      <p>Connecting to backend...</p>
    </div>
  {:else if error}
    <div class="error-card">
      <h3>⚠️ Connection Error</h3>
      <p>{error}</p>
      <p class="hint">Run <code>run-all.bat</code> to start both backend and frontend.</p>
    </div>
  {:else}
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">🔄</div>
        <div class="stat-info">
                    <span class="stat-value">{workflows.length}</span>
          <span class="stat-label">Workflows</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">🧩</div>
        <div class="stat-info">
          <span class="stat-value">{skills.length}</span>
          <span class="stat-label">Skills</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">⚡</div>
        <div class="stat-info">
                    <span class="stat-value">{executions.length}</span>
          <span class="stat-label">Executions</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">🤖</div>
        <div class="stat-info">
                    <span class="stat-value">{agents.length}</span>
          <span class="stat-label">Agents</span>
        </div>
      </div>
    </div>
    
    <div class="card">
      <h3>System Status</h3>
      <div class="status-grid">
        <div class="status-item">
          <span class="status-dot healthy"></span>
          <span>Backend API</span>
          <span class="status-url">http://localhost:5254</span>
        </div>
        <div class="status-item">
          <span class="status-dot healthy"></span>
          <span>Health Check</span>
          <span class="status-url">{health?.Status || 'Healthy'}</span>
        </div>
      </div>
    </div>
    
    <div class="card">
      <h3>Quick Actions</h3>
      <div class="actions-grid">
        <a href="/workflows" class="action-btn">
          <span class="action-icon">➕</span>
          <span>Create Workflow</span>
        </a>
        <a href="/skills" class="action-btn">
          <span class="action-icon">🔍</span>
          <span>Browse Skills</span>
        </a>
        <button class="action-btn" on:click={createSampleWorkflow}>
          <span class="action-icon">🚀</span>
          <span>Run Sample</span>
        </button>
      </div>
    </div>
    
    {#if skills.length > 0}
      <div class="card">
        <h3>Available Skills</h3>
        <div class="skills-preview">
          {#each skills.slice(0, 5) as skill}
            <div class="skill-chip">
              <span class="skill-name">{skill.name || skill.skillId}</span>
            </div>
          {/each}
        </div>
      </div>
    {/if}
  {/if}
</div>

<style>
  .dashboard { max-width: 1200px; }
  .page-heading { font-size: 1.75rem; font-weight: 700; color: #f1f5f9; margin: 0 0 0.5rem 0; }
  .subtitle { color: #94a3b8; margin: 0 0 2rem 0; }
  .loading { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 4rem; }
  .spinner { width: 40px; height: 40px; border: 3px solid var(--border); border-top-color: var(--primary); border-radius: 50%; animation: spin 1s linear infinite; }
  @keyframes spin { to { transform: rotate(360deg); } }
  .error-card { background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); border-radius: 0.5rem; padding: 1.5rem; }
  .error-card h3 { color: #ef4444; margin: 0 0 0.5rem 0; }
  .error-card p { color: #fca5a5; margin: 0; }
  .hint { margin-top: 1rem !important; font-size: 0.875rem; }
  .hint code { background: rgba(0, 0, 0, 0.3); padding: 0.25rem 0.5rem; border-radius: 0.25rem; }
  .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }
  .stat-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.25rem; display: flex; align-items: center; gap: 1rem; }
  .stat-icon { font-size: 2rem; }
  .stat-info { display: flex; flex-direction: column; }
  .stat-value { font-size: 1.5rem; font-weight: 700; color: #f1f5f9; }
  .stat-label { font-size: 0.875rem; color: #94a3b8; }
  .card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.5rem; margin-bottom: 1.5rem; }
  .card h3 { font-size: 1.1rem; font-weight: 600; color: #e2e8f0; margin: 0 0 1rem 0; }
  .status-grid { display: flex; flex-direction: column; gap: 0.75rem; }
  .status-item { display: flex; align-items: center; gap: 0.75rem; padding: 0.5rem; background: rgba(0, 0, 0, 0.2); border-radius: 0.375rem; }
  .status-dot { width: 10px; height: 10px; border-radius: 50%; }
  .status-dot.healthy { background: #22c55e; box-shadow: 0 0 8px #22c55e; }
  .status-url { margin-left: auto; font-size: 0.8rem; color: #64748b; }
  .actions-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 1rem; }
  .action-btn { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 1.25rem; background: rgba(14, 165, 233, 0.1); border: 1px solid rgba(14, 165, 233, 0.3); border-radius: 0.5rem; color: var(--primary); text-decoration: none; cursor: pointer; transition: all 0.2s ease; }
  .action-btn:hover { background: rgba(14, 165, 233, 0.2); transform: translateY(-2px); }
  .action-icon { font-size: 1.5rem; }
  .skills-preview { display: flex; flex-wrap: wrap; gap: 0.5rem; }
  .skill-chip { background: rgba(14, 165, 233, 0.15); border: 1px solid rgba(14, 165, 233, 0.3); border-radius: 1rem; padding: 0.375rem 0.75rem; }
  .skill-name { font-size: 0.85rem; color: var(--primary); }
</style>