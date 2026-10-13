<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import type { ValidationResult } from '$lib/services/api';

  /**
   * Result panel for the Test step of Configure -> Test -> Run.
   *
   * The status chip uses colour + icon + literal word, never colour alone, so the
   * outcome is readable without relying on colour vision (same convention as
   * EnvironmentConfigure.svelte).
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
        The test endpoint could not be reached. It is served by the .NET API on
        port 5254 through the Vite proxy — check that the backend is running.
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

      {#if result.warnings.length > 0}
        <section>
          <h3>Warnings ({result.warnings.length})</h3>
          <ul class="issues">
            {#each result.warnings as message}
              <li><span class="bullet bullet-warn">!</span> {message}</li>
            {/each}
          </ul>
        </section>
      {/if}

      {#if result.checks.length > 0}
        <section>
          <h3>Checks performed ({result.checks.length})</h3>
          <ul class="checks">
            {#each result.checks as check}
              <li>
                <span class="bullet" class:bullet-pass={check.passed} class:bullet-fail={!check.passed}>
                  {check.passed ? '✓' : '✕'}
                </span>
                <span class="check-name">{CHECK_LABELS[check.name] ?? check.name}</span>
                <span class="check-state">{check.passed ? 'passed' : 'failed'}</span>
                {#if !check.passed && check.detail}
                  <span class="check-detail">{check.detail}</span>
                {/if}
              </li>
            {/each}
          </ul>
        </section>
      {/if}

      <p class="scope">
        Structural checks only. This does not execute skills, verify the
        environment, or validate data — it confirms the definition is shaped
        correctly and that the graph can start and finish.
      </p>
    {/if}

    <footer>
      <button class="btn-primary" on:click={() => dispatch('close')}>Close</button>
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
    z-index: 100;
    padding: 1rem;
  }
  .panel {
    background: var(--bg-card);
    border: 1px solid var(--border);
    border-radius: 0.5rem;
    padding: 1.5rem;
    width: 100%;
    max-width: 620px;
    max-height: 85vh;
    overflow-y: auto;
  }
  header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
  }
  h2 { font-size: 1.125rem; color: #e2e8f0; margin: 0; }
  .close {
    background: transparent;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    font-size: 1rem;
  }
  .close:hover { color: #e2e8f0; }

  /* colour + icon + word: never colour alone */
  .chip {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.375rem 0.75rem;
    border-radius: 0.375rem;
    font-weight: 600;
    font-size: 0.875rem;
    border: 1px solid;
  }
  .chip .icon { font-size: 1rem; }
  .chip-pass { background: rgba(34, 197, 94, 0.1); border-color: rgba(34, 197, 94, 0.35); color: #4ade80; }
  .chip-fail { background: rgba(239, 68, 68, 0.1); border-color: rgba(239, 68, 68, 0.35); color: #f87171; }
  .chip-error { background: rgba(239, 68, 68, 0.1); border-color: rgba(239, 68, 68, 0.35); color: #f87171; }

  .message { color: #cbd5e1; font-size: 0.9rem; margin: 0.75rem 0 0; }

  section { margin-top: 1.25rem; }
  h3 { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.04em; color: #94a3b8; margin: 0 0 0.5rem; }

  ul { list-style: none; margin: 0; padding: 0; }
  .issues li, .checks li {
    display: flex;
    align-items: baseline;
    gap: 0.5rem;
    font-size: 0.85rem;
    color: #cbd5e1;
    padding: 0.35rem 0;
    border-bottom: 1px solid rgba(148, 163, 184, 0.1);
  }
  .bullet { font-size: 0.8rem; width: 1rem; flex-shrink: 0; }
  .bullet-error, .bullet-fail { color: #f87171; }
  .bullet-pass { color: #4ade80; }
  .bullet-warn { color: #fbbf24; }

  .check-name { color: #e2e8f0; }
  .check-state { font-size: 0.75rem; color: #64748b; }
  .check-detail { font-size: 0.75rem; color: #94a3b8; }

  .scope {
    margin-top: 1.25rem;
    padding: 0.625rem 0.75rem;
    background: rgba(0, 0, 0, 0.2);
    border-left: 2px solid var(--border);
    border-radius: 0.25rem;
    font-size: 0.8rem;
    color: #94a3b8;
  }

  footer { display: flex; justify-content: flex-end; margin-top: 1.5rem; }
  .btn-primary {
    background: var(--primary);
    color: white;
    border: none;
    padding: 0.5rem 1.25rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-weight: 500;
  }
  .btn-primary:hover { opacity: 0.9; }
</style>
