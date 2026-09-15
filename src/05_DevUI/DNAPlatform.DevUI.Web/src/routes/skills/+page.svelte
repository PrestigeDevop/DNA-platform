<script lang="ts">
  import { onMount } from 'svelte';
  import MsgBoxDemo from '$lib/components/MsgBoxDemo.svelte';
  import MsgBoxResult from '$lib/components/MsgBoxResult.svelte';

  interface SkillInputField {
    name: string;
    type: string;
    label?: string;
    description?: string;
    required?: boolean;
    defaultValue?: any;
    options?: string[];
    placeholder?: string;
  }

  interface Skill {
    skillId: string;
    name: string;
    description: string;
    category: string;
    icon: string;
    inputSchema: Record<string, string>;
    inputFields?: SkillInputField[];
  }

  let skills: Skill[] = [];
  let loading = true;
  let error = '';
  let selectedSkill: Skill | null = null;
  let executionResult: any = null;
  let paramEdit: Record<string, any> = {};

  onMount(async () => {
    await loadSkills();
  });

  async function loadSkills() {
    try {
      loading = true;
      error = '';

      const res = await fetch('/api/skills');

      if (!res.ok) {
        throw new Error(`Failed to load skills: ${res.status}`);
      }

      const data = await res.json();

      skills = (data.Skills || data.skills || []).map((s: any) => ({
        skillId: s.SkillId || s.skillId || '',
        name: s.Name || s.name || '',
        description: s.Description || s.description || '',
        category: s.Category || s.category || '',
        icon: s.Icon || s.icon || '',
        inputSchema: s.InputSchema || s.inputSchema || {},
        inputFields: s.InputFields || s.inputFields || []
      }));
    } catch (err) {
      console.error(err);
      error = 'Failed to load skills';
    } finally {
      loading = false;
    }
  }

  async function executeSkill(
    skillId: string,
    inputs: Record<string, any> = {}
  ) {
    try {
      const res = await fetch(`/api/skills/${skillId}/execute`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(inputs)
      });

      const result = await res.json();

      if (result.success) {
        executionResult = result;
      } else {
        alert(
          `Skill execution failed: ${
            result.error || result.message || 'Unknown error'
          }`
        );
      }
    } catch (err) {
      console.error(err);
      alert('Skill execution failed: Network or server error');
    }
  }

  function viewDetails(skill: Skill) {
    selectedSkill = skill;
    paramEdit = {};

    if (skill.inputFields?.length) {
      for (const field of skill.inputFields) {
        if (field.defaultValue !== undefined) {
          paramEdit[field.name] = field.defaultValue;
        } else if (field.type === 'bool') {
          paramEdit[field.name] = false;
        } else {
          paramEdit[field.name] = '';
        }
      }
    } else {
      for (const [key] of Object.entries(skill.inputSchema)) {
        paramEdit[key] = '';
      }
    }
  }

  function closeDetails() {
    selectedSkill = null;
    paramEdit = {};
  }

  function closeResult() {
    executionResult = null;
  }

  async function executeFromDetails() {
    if (!selectedSkill) return;

    await executeSkill(selectedSkill.skillId, paramEdit);
    closeDetails();
  }
</script>

<div class="skills-page">
  <div class="page-header">
    <h1>Skills</h1>

    <button class="btn-primary" on:click={loadSkills}>
      🔄 Refresh
    </button>
  </div>

  <p class="page-description">
    Browse and execute bioinformatic skills.
  </p>

  <MsgBoxDemo
    onExecute={(inputs) => executeSkill('msgbox-alert', inputs)}
  />

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
      {#each skills as skill (skill.skillId)}
        <div class="skill-card">
          <div class="skill-header">
            <h3>{skill.name || skill.skillId}</h3>

            <span class="skill-category">
              {skill.category || 'General'}
            </span>
          </div>

          <p class="skill-desc">
            {skill.description || 'No description available'}
          </p>

          <div class="skill-actions">
            <button
              class="btn-small"
              on:click={() => viewDetails(skill)}
            >
              Details
            </button>

            <button
              class="btn-small btn-execute"
              on:click={() => executeSkill(skill.skillId)}
            >
              ▶ Run
            </button>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

{#if selectedSkill}
  <div
    class="modal-overlay"
    role="presentation"
    on:click|self={closeDetails}
  >
    <div
      class="modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="skill-modal-title"
      on:click|stopPropagation
    >
      <h2 id="skill-modal-title">
        {selectedSkill.name || selectedSkill.skillId}
      </h2>

      <div class="detail-section">
        <label>Skill ID</label>
        <p>{selectedSkill.skillId}</p>
      </div>

      <div class="detail-section">
        <label>Description</label>
        <p>{selectedSkill.description || 'No description'}</p>
      </div>

      <div class="detail-section">
        <label>Category</label>
        <p>{selectedSkill.category || 'General'}</p>
      </div>

      <div class="detail-section">
        <label>Parameters</label>

        <div class="params-editor">
          {#if selectedSkill.inputFields && selectedSkill.inputFields.length > 0}
            {#each selectedSkill.inputFields as field (field.name)}
              <div class="param-group">
                <label for={`field-${field.name}`}>
                  {field.label || field.name}

                  {#if field.required}
                    <span class="required-mark">*</span>
                  {/if}
                </label>

                {#if field.description}
                  <small class="field-description">
                    {field.description}
                  </small>
                {/if}

                {#if field.type === 'enum' && field.options}
                  <select
                    id={`field-${field.name}`}
                    bind:value={paramEdit[field.name]}
                  >
                    {#each field.options as option}
                      <option value={option}>{option}</option>
                    {/each}
                  </select>

                {:else if field.type === 'text'}
                  <textarea
                    id={`field-${field.name}`}
                    bind:value={paramEdit[field.name]}
                    placeholder={field.placeholder || ''}
                  ></textarea>

                {:else if field.type === 'int'}
                  <input
                    id={`field-${field.name}`}
                    type="number"
                    step="1"
                    bind:value={paramEdit[field.name]}
                    placeholder={field.placeholder || ''}
                  />

                {:else if field.type === 'float' || field.type === 'number'}
                  <input
                    id={`field-${field.name}`}
                    type="number"
                    step="any"
                    bind:value={paramEdit[field.name]}
                    placeholder={field.placeholder || ''}
                  />

                {:else if field.type === 'bool' || field.type === 'boolean'}
                  <label class="checkbox-label" for={`field-${field.name}`}>
                    <input
                      id={`field-${field.name}`}
                      type="checkbox"
                      bind:checked={paramEdit[field.name]}
                    />
                    <span>Enabled</span>
                  </label>

                {:else}
                  <input
                    id={`field-${field.name}`}
                    type="text"
                    bind:value={paramEdit[field.name]}
                    placeholder={field.placeholder || ''}
                  />
                {/if}
              </div>
            {/each}

          {:else}
            {#each Object.entries(selectedSkill.inputSchema) as [key, type]}
              <div class="param-group">
                <label for={`schema-${key}`}>{key}</label>

                <input
                  id={`schema-${key}`}
                  type="text"
                  bind:value={paramEdit[key]}
                  placeholder={type}
                />
              </div>
            {/each}
          {/if}
        </div>
      </div>

      <div class="modal-actions">
        <button class="btn-secondary" on:click={closeDetails}>
          Close
        </button>

        <button class="btn-primary" on:click={executeFromDetails}>
          Execute
        </button>
      </div>
    </div>
  </div>
{/if}

{#if executionResult}
  <MsgBoxResult result={executionResult} onClose={closeResult} />
{/if}

<style>
  .skills-page {
    max-width: 1200px;
  }

  .page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 0.5rem;
  }

  .page-header h1 {
    margin: 0;
    color: #f1f5f9;
    font-size: 1.75rem;
    font-weight: 700;
  }

  .page-description {
    margin: 0 0 2rem;
    color: #94a3b8;
  }

  .loading {
    padding: 3rem;
    color: #94a3b8;
    text-align: center;
  }

  .error-card {
    padding: 1rem;
    border: 1px solid rgba(239, 68, 68, 0.3);
    border-radius: 0.5rem;
    background: rgba(239, 68, 68, 0.1);
    color: #ef4444;
  }

  .empty-state {
    padding: 4rem;
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    background: var(--bg-card);
    text-align: center;
  }

  .empty-icon {
    font-size: 3rem;
  }

  .empty-state h3 {
    margin: 1rem 0 0.5rem;
    color: #e2e8f0;
  }

  .empty-state p {
    margin: 0;
    color: #94a3b8;
  }

  .skills-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1rem;
  }

  .skill-card {
    padding: 1.25rem;
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    background: var(--bg-card);
  }

  .skill-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.75rem;
    margin-bottom: 0.75rem;
  }

  .skill-header h3 {
    margin: 0;
    color: #e2e8f0;
    font-size: 1rem;
    font-weight: 600;
  }

  .skill-category {
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    background: rgba(0, 0, 0, 0.2);
    color: #64748b;
    font-size: 0.7rem;
    text-transform: uppercase;
    white-space: nowrap;
  }

  .skill-desc {
    margin: 0 0 1rem;
    color: #94a3b8;
    font-size: 0.85rem;
    line-height: 1.4;
  }

  .skill-actions {
    display: flex;
    gap: 0.5rem;
  }

  .btn-small {
    padding: 0.375rem 0.75rem;
    border: 1px solid rgba(14, 165, 233, 0.3);
    border-radius: 0.25rem;
    background: rgba(14, 165, 233, 0.1);
    color: var(--primary);
    cursor: pointer;
    font-size: 0.8rem;
  }

  .btn-execute {
    border-color: rgba(34, 197, 94, 0.3);
    background: rgba(34, 197, 94, 0.1);
    color: #22c55e;
  }

  .btn-small:hover,
  .btn-secondary:hover {
    filter: brightness(1.15);
  }

  .modal-overlay {
    position: fixed;
    inset: 0;
    z-index: 100;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1rem;
    background: rgba(0, 0, 0, 0.7);
  }

  .modal {
    width: 100%;
    max-width: 500px;
    max-height: 80vh;
    overflow-y: auto;
    padding: 1.5rem;
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    background: var(--bg-card);
  }

  .modal h2 {
    margin: 0 0 1rem;
    color: #e2e8f0;
    font-size: 1.25rem;
  }

  .detail-section {
    margin-bottom: 1rem;
  }

  .detail-section > label {
    display: block;
    margin-bottom: 0.25rem;
    color: #64748b;
    font-size: 0.75rem;
    text-transform: uppercase;
  }

  .detail-section p {
    margin: 0;
    color: #e2e8f0;
  }

  .params-editor {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .param-group {
    display: flex;
    flex-direction: column;
  }

  .param-group > label {
    margin-bottom: 0.25rem;
    color: #94a3b8;
    font-size: 0.8rem;
  }

  .param-group input,
  .param-group select,
  .param-group textarea {
    box-sizing: border-box;
    width: 100%;
    padding: 0.5rem;
    border: 1px solid var(--border);
    border-radius: 0.375rem;
    background: var(--bg-dark);
    color: #e2e8f0;
    font-size: 0.9rem;
  }

  .param-group textarea {
    min-height: 90px;
    resize: vertical;
  }

  .param-group input:focus,
  .param-group select:focus,
  .param-group textarea:focus {
    outline: none;
    border-color: var(--primary);
  }

  .checkbox-label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: #e2e8f0;
    cursor: pointer;
  }

  .checkbox-label input[type='checkbox'] {
    width: auto;
  }

  .field-description {
    margin: 0 0 0.35rem;
    color: #64748b;
    font-size: 0.75rem;
  }

  .required-mark {
    margin-left: 0.25rem;
    color: #ef4444;
  }

  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
    margin-top: 1.5rem;
  }

  .btn-secondary {
    padding: 0.625rem 1.25rem;
    border: 1px solid var(--border);
    border-radius: 0.375rem;
    background: var(--bg-card);
    color: #e2e8f0;
    cursor: pointer;
  }

  .btn-primary {
    padding: 0.625rem 1.25rem;
    border: none;
    border-radius: 0.375rem;
    background: var(--primary);
    color: white;
    cursor: pointer;
  }

  .btn-primary:hover {
    opacity: 0.9;
  }

  @media (max-width: 600px) {
    .page-header {
      align-items: flex-start;
      flex-direction: column;
    }

    .skills-grid {
      grid-template-columns: 1fr;
    }

    .modal {
      max-height: 90vh;
      padding: 1rem;
    }
  }
</style>