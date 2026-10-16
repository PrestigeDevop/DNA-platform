<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import type { ValidationResult } from '$lib/services/api';

  /**
   * Result panel for the Test step of Configure -> Test -> Run.
   *
   * The status chip uses colour + icon + literal word, never colour alone, so the
   * outcome is readable without relying on colour vision.
   */

  export let result: ValidationResult | null = null;
  export let error = '';

  const dispatch = createEventDispatcher();

  const CHECK_LABELS: Record<string, string> = {
    'workflow.name': 'Workflow name present',
    'workflow.hasNodes': 'At least one node',
    'graph.hasEntryNode': 'Graph has an entry node',
    'graph.acyclic': 'Graph is acyclic'
  };
</script>

<div class="overlay" on:click|self={() => dispatch('close')} role="presentation">
  <div class="panel" role="dialog" aria-modal="true" aria-label="Workflow test result">
    <header>
      <h2>🩺 Test result</h2>
      <button class="close" on:click={() => dispatch('close')} aria-label="Close">✕</button>
    </header>

    {#if error}
      <div class="chip chip-error"><span class="icon">✕</span> <span class="word">Error</span></div>
      <p class="message">{error}</p>
      <p class="scope">
        Validation could not be completed. Please review the workflow nodes and connection structure.
      </p>
    {:else if result}
      <div
        class="chip"
        class:chip-pass={result.isValid}
        class:chip-fail={!result.isValid}
      >
        <span class="icon">{result.isValid ? '✓' : '✕'}</span>
        <span class="word">{result.isValid ? 'Valid' : 'Invalid'}</span>
      </div>

      <p class="message">{result.summary}</p>

      {#if result.errors.length > 0}
        <section>
          <h3>Errors ({result.errors.length})</h3>
          <ul class="issues">
            {#each result.errors as message}
              <li><span class="bullet bullet-error">✕</span> {message}</li>
            {/each}
          </ul>
        </section>
      {/if}

      {#if result.warnings && result.warnings.length > 0}
        <section>
          <h3>Warnings ({result.warnings.length})</h3>
          <ul class="issues">
            {#each result.warnings as message}
              <li><span class="bullet bullet-warn">!</span> {message}</li>
            {/each}
          </ul>
        </section>
      {/if}

      {#if result.checks && result.checks.length > 0}
        <section>
          <h3>Structural checks</h3>
          <ul class="checks">
            {#each result.checks as check}
              <li class:check-passed={check.passed} class:check-failed={!check.passed}>
                <span class="check-icon">{check.passed ? '✓' : '✕'}</span>
                <span class="check-name">{CHECK_LABELS[check.name] ?? check.name}</span>
                {#if !check.passed && check.detail}
                  <span class="check-detail">{check.detail}</span>
                {/if}
              </li>
            {/each}
          </ul>
        </section>
      {/if}

      <p class="scope">
        Structural validation verifies node identifiers, connection references, entry points,
        and acyclic dependency order.
      </p>
    {:else}
      <p class="empty">No validation result available.</p>
    {/if}

    <footer>
      <button class="btn" on:click={() => dispatch('close')}>Close</button>
    </footer>
  </div>
</div>

<style>
  .overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 60;
    padding: 1rem;
  }
  .panel {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    max-width: 520px;
    width: 100%;
    max-height: 85vh;
    overflow-y: auto;
    padding: 1.25rem;
    color: #e2e8f0;
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.5);
  }
  header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 1rem;
  }
  h2 {
    font-size: 1.1rem;
    margin: 0;
    color: #f1f5f9;
  }
  .close {
    background: none;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    font-size: 1.1rem;
    padding: 0.25rem 0.5rem;
  }
  .close:hover {
    color: #f1f5f9;
  }
  .chip {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.3rem 0.75rem;
    border-radius: 9999px;
    font-size: 0.85rem;
    font-weight: 600;
    margin-bottom: 0.5rem;
  }
  .chip-pass {
    background: rgba(34, 197, 94, 0.15);
    color: #4ade80;
    border: 1px solid rgba(34, 197, 94, 0.3);
  }
  .chip-fail,
  .chip-error {
    background: rgba(239, 68, 68, 0.15);
    color: #f87171;
    border: 1px solid rgba(239, 68, 68, 0.3);
  }
  .message {
    font-size: 0.95rem;
    margin: 0.5rem 0 1rem;
    color: #cbd5e1;
  }
  section {
    margin-top: 1rem;
  }
  h3 {
    font-size: 0.85rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: #94a3b8;
    margin: 0 0 0.5rem;
  }
  ul.issues,
  ul.checks {
    list-style: none;
    margin: 0;
    padding: 0;
    font-size: 0.85rem;
  }
  ul.issues li,
  ul.checks li {
    padding: 0.35rem 0.5rem;
    border-radius: 0.25rem;
    margin-bottom: 0.25rem;
    background: rgba(15, 23, 42, 0.6);
  }
  .bullet {
    font-weight: bold;
    margin-right: 0.35rem;
  }
  .bullet-error {
    color: #f87171;
  }
  .bullet-warn {
    color: #fbbf24;
  }
  ul.checks li {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .check-icon {
    font-weight: bold;
  }
  .check-passed .check-icon {
    color: #4ade80;
  }
  .check-failed .check-icon {
    color: #f87171;
  }
  .check-name {
    color: #e2e8f0;
  }
  .check-detail {
    margin-left: auto;
    color: #94a3b8;
    font-size: 0.75rem;
  }
  .scope {
    font-size: 0.75rem;
    color: #64748b;
    margin-top: 1rem;
    padding-top: 0.75rem;
    border-top: 1px solid var(--border);
    line-height: 1.4;
  }
  .empty {
    color: #64748b;
    font-style: italic;
  }
  footer {
    display: flex;
    justify-content: flex-end;
    margin-top: 1.25rem;
  }
  .btn {
    background: var(--primary);
    color: white;
    border: none;
    padding: 0.45rem 1rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.85rem;
  }
  .btn:hover {
    opacity: 0.9;
  }
</style>
