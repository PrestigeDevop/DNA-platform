<script lang="ts">
  import { onMount } from 'svelte';
  import WorkflowDesigner from '$lib/components/WorkflowDesigner.svelte';
  import EnvironmentConfigure from '$lib/components/EnvironmentConfigure.svelte';
  import WorkflowTestResult from '$lib/components/WorkflowTestResult.svelte';
  import { api, type ValidationResult } from '$lib/services/api';

  let workflows: any[] = [];
  let loading = true;
  let error = '';
  let showCreateModal = false;
  let newWorkflow = { name: '', description: '' };
  let editingWorkflow: any = null;
  let configuringWorkflow: any = null;

  // Test step of Configure -> Test -> Run (docs/refactor.md section 4).
  let testingWorkflowId: string | null = null;
  let testResult: ValidationResult | null = null;
  let testError = '';

  // Per-node state from the most recent Execute, handed to the designer so the
  // per-node status chips survive closing and reopening it.
  let lastExecution: { workflowId: string; nodeResults: any[] } | null = null;

  function openDesigner(workflow: any) {
    editingWorkflow = workflow;
  }

  function closeDesigner() {
    editingWorkflow = null;
    loadWorkflows();
  }

  function openConfigure(workflow: any) {
    configuringWorkflow = workflow;
  }

  function closeConfigure() {
    configuringWorkflow = null;
  }

  /**
   * Runs the stored definition through POST /api/workflows/validate. This is
   * structural validation only — no skill executes and the environment is not
   * checked — so the result panel says exactly that.
   */
  async function testWorkflow(workflow: any) {
    testingWorkflowId = workflow.id;
    testError = '';

    // The card list carries node counts but not the definitions, so re-read the
    // workflow to send the real nodes and connections.
    let definition: { name?: string; nodes?: unknown[]; connections?: unknown[] } = {
      name: workflow.name,
      nodes: workflow.nodes,
      connections: workflow.connections
    };

    try {
      const res = await fetch(`/api/workflows/${workflow.id}`);
      if (res.ok) {
        const full = await res.json();
        const wf = full.workflow ?? full;
        definition = {
          name: wf.name,
          nodes: wf.nodes ?? [],
          connections: wf.connections ?? []
        };
      }

      testResult = await api.validateWorkflow(definition);
    } catch (e: any) {
      testError = e?.message ?? String(e);
      testResult = null;
    } finally {
      testingWorkflowId = null;
    }
  }

  function closeTest() {
    testResult = null;
    testError = '';
  }

  onMount(async () => {
    await loadWorkflows();
  });
  
  async function loadWorkflows() {
    try {
      loading = true;
      const res = await fetch('/api/workflows');
      const data = await res.json();
      workflows = data.workflows || [];
      loading = false;
    } catch (e) {
      error = 'Failed to load workflows';
      loading = false;
    }
  }
  
  async function createWorkflow() {
    if (!newWorkflow.name) return;
    const workflow = {
      id: 'wf-' + Date.now(),
      name: newWorkflow.name,
      description: newWorkflow.description,
      nodes: [],
      connections: []
    };
    try {
      await fetch('/api/workflows', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(workflow)
      });
      showCreateModal = false;
      newWorkflow = { name: '', description: '' };
      await loadWorkflows();
    } catch (e) {
      alert('Error creating workflow: ' + e);
    }
  }
  
  async function deleteWorkflow(id: string) {
    if (!confirm('Delete this workflow?')) return;
    await fetch(`/api/workflows/${id}`, { method: 'DELETE' });
    await loadWorkflows();
  }
  
  async function executeWorkflow(id: string) {
    try {
      const res = await fetch(`/api/workflows/${id}/execute`, { method: 'POST' });
      const result = await res.json();
      if (!res.ok) {
        alert(`Execution failed: ${result?.error ?? res.status}`);
        return;
      }

      lastExecution = { workflowId: id, nodeResults: result?.nodeResults ?? [] };

      const failed = (result?.nodeResults ?? []).filter((r: any) => r.status === 'Failed').length;
      alert(
        `Workflow executed! Status: ${result.status}` +
          (failed ? `\n${failed} node(s) failed — open Edit for per-node state.` : '')
      );
    } catch (e) {
      alert('Error executing workflow: ' + e);
    }
  }

  function onDesignerSaved(e: CustomEvent<{ nodeResults?: any[] } | undefined>) {
    const results = e.detail?.nodeResults;
    if (results && editingWorkflow) {
      lastExecution = { workflowId: editingWorkflow.id, nodeResults: results };
    }
    loadWorkflows();
  }

  /**
   * Toggles Draft/Published (docs/refactor.md section 14). Publishing through the
   * PUT route bumps the version and snapshots the definition into
   * WorkflowVersion, so a run can be traced to the exact definition it executed.
   */
  async function togglePublished(workflow: any) {
    const next = workflow.status === 'Published' ? 'Draft' : 'Published';
    try {
      const res = await fetch(`/api/workflows/${workflow.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: workflow.name,
          description: workflow.description,
          nodes: workflow.nodes ?? [],
          connections: workflow.connections ?? [],
          status: next
        })
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        alert(`Could not change status: ${data.error ?? res.status}`);
        return;
      }
      await loadWorkflows();
    } catch (e) {
      alert('Error updating workflow status: ' + e);
    }
  }
</script>

<div class="workflows-page">
  <div class="page-header">
    <h1>Workflows</h1>
    <button class="btn-primary" on:click={() => showCreateModal = true}>+ New Workflow</button>
  </div>
  <p class="page-description">Create and manage bioinformatic workflows.</p>
  
  {#if loading}
    <div class="loading">Loading workflows...</div>
  {:else if error}
    <div class="error-card">{error}</div>
  {:else if workflows.length === 0}
    <div class="empty-state">
      <span class="empty-icon">🔄</span>
      <h3>No Workflows Yet</h3>
      <p>Create your first workflow to get started.</p>
      <button class="btn-primary" on:click={() => showCreateModal = true}>Create First Workflow</button>
    </div>
  {:else}
    <div class="workflows-grid">
      {#each workflows as workflow}
        <div class="workflow-card">
          <div class="workflow-header">
            <h3>{workflow.name}</h3>
            <span class="workflow-nodes">{workflow.nodes?.length || 0} nodes</span>
          </div>
          <p class="workflow-desc">{workflow.description || 'No description'}</p>
          <div class="workflow-badges">
            <span
              class="badge"
              class:badge-published={workflow.status === 'Published'}
              class:badge-draft={workflow.status !== 'Published'}
            >
              <span class="badge-icon">{workflow.status === 'Published' ? '●' : '○'}</span>
              {workflow.status === 'Published' ? 'Published' : 'Draft'}
            </span>
            <span class="badge-version" title="{workflow.publishedVersions ?? 0} published version(s) recorded">
              v{workflow.version ?? 1}
            </span>
          </div>
          <div class="workflow-actions">
            <button class="btn-small" on:click={() => openConfigure(workflow)}>⚙️ Configure</button>
            <button class="btn-small" on:click={() => testWorkflow(workflow)} disabled={testingWorkflowId === workflow.id}>
              {testingWorkflowId === workflow.id ? '🩺 Testing…' : '🩺 Test'}
            </button>
            <button class="btn-small" on:click={() => openDesigner(workflow)}>✏️ Edit</button>
            <button class="btn-small" on:click={() => executeWorkflow(workflow.id)}>▶ Execute</button>
            <button class="btn-small" on:click={() => togglePublished(workflow)}>
              {workflow.status === 'Published' ? '📝 Unpublish' : '📌 Publish'}
            </button>
            <button class="btn-small btn-danger" on:click={() => deleteWorkflow(workflow.id)}>🗑 Delete</button>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

{#if showCreateModal}
  <div class="modal-overlay" on:click|self={() => showCreateModal = false}>
    <div class="modal">
      <h2>Create New Workflow</h2>
      <div class="form-group">
        <label for="name">Workflow Name</label>
        <input id="name" type="text" bind:value={newWorkflow.name} placeholder="e.g., Gene Analysis Pipeline" />
      </div>
      <div class="form-group">
        <label for="description">Description (optional)</label>
        <textarea id="description" bind:value={newWorkflow.description} placeholder="Describe your workflow..."></textarea>
      </div>
      <div class="modal-actions">
        <button class="btn-secondary" on:click={() => showCreateModal = false}>Cancel</button>
        <button class="btn-primary" on:click={createWorkflow}>Create</button>
      </div>
    </div>
  </div>
{/if}

{#if editingWorkflow}
  <WorkflowDesigner
    workflow={editingWorkflow}
    execution={lastExecution?.workflowId === editingWorkflow.id ? lastExecution : null}
    on:close={closeDesigner}
    on:saved={onDesignerSaved} />
{/if}

{#if configuringWorkflow}
  <EnvironmentConfigure workflow={configuringWorkflow} on:close={closeConfigure} />
{/if}

{#if testResult || testError}
  <WorkflowTestResult result={testResult} error={testError} on:close={closeTest} />
{/if}

<style>
  .workflows-page { max-width: 1200px; }
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
  .workflows-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1rem; }
  .workflow-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.25rem; }
  .workflow-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem; }
  .workflow-header h3 { font-size: 1rem; font-weight: 600; color: #e2e8f0; margin: 0; }
  .workflow-nodes { font-size: 0.75rem; color: #64748b; background: rgba(0, 0, 0, 0.2); padding: 0.25rem 0.5rem; border-radius: 0.25rem; }
  .workflow-desc { color: #94a3b8; font-size: 0.875rem; margin: 0 0 0.5rem 0; }
  .workflow-badges { display: flex; gap: 0.4rem; margin-bottom: 0.75rem; }
  .badge {
    display: inline-flex; align-items: center; gap: 0.3rem;
    font-size: 0.7rem; padding: 0.15rem 0.4rem;
    border-radius: 0.25rem; border: 1px solid;
  }
  .badge-draft { color: #94a3b8; border-color: rgba(148, 163, 184, 0.35); background: rgba(148, 163, 184, 0.08); }
  .badge-published { color: #4ade80; border-color: rgba(74, 222, 128, 0.35); background: rgba(74, 222, 128, 0.08); }
  .badge-icon { font-size: 0.65rem; }
  .badge-version { color: #64748b; font-size: 0.7rem; padding: 0.15rem 0.35rem; background: rgba(0, 0, 0, 0.2); border-radius: 0.25rem; }
  .workflow-actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
  .btn-small:disabled { opacity: 0.55; cursor: not-allowed; }
  .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.7); display: flex; align-items: center; justify-content: center; z-index: 100; }
  .modal { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.5rem; width: 100%; max-width: 500px; }
  .modal h2 { font-size: 1.25rem; color: #e2e8f0; margin: 0 0 1.5rem 0; }
  .form-group { margin-bottom: 1rem; }
  .form-group label { display: block; color: #94a3b8; font-size: 0.875rem; margin-bottom: 0.5rem; }
  .form-group input, .form-group textarea { width: 100%; background: var(--bg-dark); border: 1px solid var(--border); border-radius: 0.375rem; padding: 0.625rem; color: #e2e8f0; font-size: 0.9rem; box-sizing: border-box; }
  .form-group textarea { min-height: 80px; resize: vertical; }
  .modal-actions { display: flex; justify-content: flex-end; gap: 0.75rem; margin-top: 1.5rem; }
</style>