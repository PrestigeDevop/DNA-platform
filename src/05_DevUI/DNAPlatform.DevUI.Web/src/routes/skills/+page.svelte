<script lang="ts">
  import { onMount } from 'svelte';
  import MsgBoxDemo from '$lib/components/MsgBoxDemo.svelte';
  import MsgBoxResult from '$lib/components/MsgBoxResult.svelte';
  
  let skills: any[] = [];
  let loading = true;
  let error = '';
  let selectedSkill: any = null;
  let executionResult: any = null;
  
  onMount(async () => {
    await loadSkills();
  });
  
  async function loadSkills() {
    try {
      loading = true;
      const res = await fetch('/api/skills');
      const data = await res.json();
      skills = data.skills || [];
      loading = false;
    } catch (e) {
      error = 'Failed to load skills';
      loading = false;
    }
  }
  
  async function executeSkill(skillId: string, inputs: any = {}) {
    try {
      const res = await fetch(`/api/skills/${skillId}/execute`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(inputs)
      });
      const result = await res.json();
      
      if (result.success) {
        executionResult = result;
      } else {
        alert(`Skill execution failed: ${result.error}`);
      }
    } catch (e) {
      alert('Error executing skill: ' + e);
    }
  }
  
  function viewDetails(skill: any) {
    selectedSkill = skill;
  }
  
  function closeResult() {
    executionResult = null;
  }
</script>

<div class="skills-page">
  <div class="page-header">
    <h1>Skills</h1>
    <button class="btn-primary" on:click={loadSkills}>🔄 Refresh</button>
  </div>
  <p class="page-description">Browse and execute bioinformatic skills.</p>
  
  <!-- MsgBox Alert Skill Demo -->
  <MsgBoxDemo onExecute={(inputs) => executeSkill('msgbox-alert', inputs)} />
  
  
  {#if loading}
    <div class="loading">Loading skills...</div>
  {:else if error}
    <div class="error-card">{error}</div>
  {:else if skills.length === 0}
    <div class="empty-state">
      <span class="empty-icon">🧩</span>
      <h3>No Skills Available</h3>
      <p>Skills will appear here once registered.</p>
    </div>
  {:else}
    <div class="skills-grid">
      {#each skills as skill}
        <div class="skill-card">
          <div class="skill-header">
            <h3>{skill.name || skill.skillId}</h3>
            <span class="skill-category">{skill.category || 'General'}</span>
          </div>
          <p class="skill-desc">{skill.description || 'No description available'}</p>
          <div class="skill-actions">
            <button class="btn-small" on:click={() => viewDetails(skill)}>Details</button>
            <button class="btn-small btn-execute" on:click={() => executeSkill(skill.skillId)}>▶ Run</button>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

{#if selectedSkill}
  <div class="modal-overlay" on:click|self={() => selectedSkill = null}>
    <div class="modal">
      <h2>{selectedSkill.name || selectedSkill.skillId}</h2>
      <div class="detail-section">
        <label>Skill ID</label>
        <p>{selectedSkill.skillId}</p>
      </div>
      <div class="detail-section">
        <label>Description</label>
        <p>{selectedSkill.description || 'No description'}</p>
      </div>
      <div class="modal-actions">
        <button class="btn-secondary" on:click={() => selectedSkill = null}>Close</button>
  
  <!-- MsgBox Result Display -->
  <MsgBoxResult result={executionResult} onClose={closeResult} />
  
        <button class="btn-primary" on:click={() => { executeSkill(selectedSkill.skillId); selectedSkill = null; }}>Execute</button>
      </div>
    </div>
  </div>
{/if}

<style>
  .skills-page { max-width: 1200px; }
  .page-header { margin-bottom: 0.5rem; }
  .page-header h1 { font-size: 1.75rem; font-weight: 700; color: #f1f5f9; margin: 0; }
  .page-description { color: #94a3b8; margin: 0 0 2rem 0; }
  .loading { text-align: center; padding: 3rem; color: #94a3b8; }
  .error-card { background: rgba(239, 68, 68, 0.1); border: 1px solid rgba(239, 68, 68, 0.3); border-radius: 0.5rem; padding: 1rem; color: #ef4444; }
  .empty-state { text-align: center; padding: 4rem; background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; }
  .empty-icon { font-size: 3rem; }
  .empty-state h3 { color: #e2e8f0; margin: 1rem 0 0.5rem 0; }
  .empty-state p { color: #94a3b8; margin: 0; }
  .skills-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1rem; }
  .skill-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.25rem; }
  .skill-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem; }
  .skill-header h3 { font-size: 1rem; font-weight: 600; color: #e2e8f0; margin: 0; }
  .skill-category { font-size: 0.7rem; color: #64748b; background: rgba(0, 0, 0, 0.2); padding: 0.2rem 0.5rem; border-radius: 0.25rem; text-transform: uppercase; }
  .skill-desc { color: #94a3b8; font-size: 0.85rem; margin: 0 0 1rem 0; line-height: 1.4; }
  .skill-actions { display: flex; gap: 0.5rem; }
  .btn-small { background: rgba(14, 165, 233, 0.1); color: var(--primary); border: 1px solid rgba(14, 165, 233, 0.3); padding: 0.375rem 0.75rem; border-radius: 0.25rem; cursor: pointer; font-size: 0.8rem; }
  .btn-execute { background: rgba(34, 197, 94, 0.1); color: #22c55e; border-color: rgba(34, 197, 94, 0.3); }
  .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.7); display: flex; align-items: center; justify-content: center; z-index: 100; }
  .modal { background: var(--bg-card); border: 1px solid var(--border); border-radius: 0.5rem; padding: 1.5rem; width: 100%; max-width: 500px; max-height: 80vh; overflow-y: auto; }
  .modal h2 { font-size: 1.25rem; color: #e2e8f0; margin: 0 0 1rem 0; }
  .detail-section { margin-bottom: 1rem; }
  .detail-section label { display: block; color: #64748b; font-size: 0.75rem; text-transform: uppercase; margin-bottom: 0.25rem; }
  .detail-section p { color: #e2e8f0; margin: 0; }
  .modal-actions { display: flex; justify-content: flex-end; gap: 0.75rem; margin-top: 1.5rem; }
  .btn-secondary { background: var(--bg-card); color: #e2e8f0; border: 1px solid var(--border); padding: 0.625rem 1.25rem; border-radius: 0.375rem; cursor: pointer; }
  .btn-primary { background: var(--primary); color: white; border: none; padding: 0.625rem 1.25rem; border-radius: 0.375rem; cursor: pointer; }
</style>