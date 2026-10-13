<script lang="ts">
  import { createEventDispatcher, onDestroy, onMount } from "svelte";
  import {
    environmentApi,
    type EnvironmentStatus,
    type RuntimeStatus
  } from "$lib/services/environment";

  /** Workflow whose execution environment is being configured (page-owned). */
  export let workflow: any = null;

  const dispatch = createEventDispatcher();
  // Aborts every in-flight call when the view is closed.
  const controller = new AbortController();

  // Overall environment state
  let status: EnvironmentStatus | null = null;
  let loading = true;
  let verifying = false;
  let error = "";
  let notice = "";

  // Export panel
  let exportContent = "";
  let exportMissing = false;
  let exportMissingMessage = "";
  let exportError = "";
  let exporting = false;
  let copyState = "";

  // Import panel
  let importContent = "";
  let importError = "";
  let importing = false;

  interface StatusVisual {
    label: string;
    icon: string;
    className: string;
  }

  /** GREEN / YELLOW / RED. The literal word is always rendered next to the colour. */
  const STATUS_META: Record<string, StatusVisual> = {
    green: { label: "GREEN", icon: "🟢", className: "green" },
    yellow: { label: "YELLOW", icon: "🟡", className: "yellow" },
    red: { label: "RED", icon: "🔴", className: "red" }
  };

  const UNKNOWN_STATUS: StatusVisual = {
    label: "UNKNOWN",
    icon: "⚪",
    className: "unknown"
  };

  const OVERALL_COPY: Record<string, { title: string; body: string }> = {
    green: {
      title: "Environment ready",
      body: "Every runtime this project requests resolves through mise and is active. The workflow will run on the versions listed below."
    },
    yellow: {
      title: "Falling back to platform defaults",
      body: "At least one requested runtime is not provided by mise. The platform-provided default is used instead, which is not necessarily the version the workflow was written against. Verify it still fits before running."
    },
    red: {
      title: "Environment cannot run this workflow",
      body: "At least one requested runtime is unavailable, incompatible, or cannot be resolved, and the platform has no default for it."
    },
    unknown: {
      title: "Environment state unknown",
      body: "The backend reported a status this view does not recognise. Verify the environment to refresh it."
    }
  };

  const SOURCE_META: Record<string, { label: string; icon: string }> = {
    mise: { label: "mise.toml", icon: "📦" },
    "platform-default": { label: "Platform default", icon: "🧱" },
    none: { label: "No provider", icon: "⛔" }
  };

  function statusVisual(value: string | null | undefined): StatusVisual {
    return STATUS_META[(value || "").trim().toLowerCase()] || UNKNOWN_STATUS;
  }

  function overallCopy(value: string | null | undefined): { title: string; body: string } {
    return OVERALL_COPY[(value || "").trim().toLowerCase()] || OVERALL_COPY.unknown;
  }

  function sourceVisual(value: string | null | undefined): { label: string; icon: string } {
    const key = (value || "none").trim().toLowerCase();
    return SOURCE_META[key] || { label: value || "unknown", icon: "❔" };
  }

  function sourceNote(runtime: RuntimeStatus): string {
    if (runtime.source === "mise") return "Version comes from mise.toml.";
    if (runtime.source === "platform-default") {
      return "Not in mise.toml, so the platform-provided default is used instead.";
    }
    return "No runtime provider is available for this tool.";
  }

  function versionText(value: string | null | undefined, fallback: string): string {
    const trimmed = (value || "").trim();
    return trimmed ? trimmed : fallback;
  }

  function formatCheckedAt(value: string | null | undefined): string {
    if (!value) return "unknown";
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return value;
    return parsed.toLocaleString();
  }

  async function loadEnvironment(forceVerify: boolean) {
    if (forceVerify) {
      verifying = true;
    } else {
      loading = true;
    }
    error = "";
    try {
      status = forceVerify
        ? await environmentApi.verifyEnvironment({ signal: controller.signal })
        : await environmentApi.getEnvironment({ signal: controller.signal });
    } catch (e: any) {
      if (controller.signal.aborted) return;
      error = e?.message || "Failed to read the local execution environment.";
    } finally {
      loading = false;
      verifying = false;
    }
  }

  function verify() {
    notice = "";
    void loadEnvironment(true);
  }

  function retry() {
    void loadEnvironment(false);
  }

  async function loadExport() {
    exporting = true;
    exportError = "";
    copyState = "";
    try {
      const result = await environmentApi.exportMise({ signal: controller.signal });
      if (result.found) {
        exportContent = result.content;
        exportMissing = false;
        exportMissingMessage = "";
      } else {
        exportContent = "";
        exportMissing = true;
        exportMissingMessage = result.message || "No mise.toml was found for this project.";
      }
    } catch (e: any) {
      if (controller.signal.aborted) return;
      exportContent = "";
      exportMissing = false;
      exportError = e?.message || "Failed to export the mise configuration.";
    } finally {
      exporting = false;
    }
  }

  async function copyExport() {
    copyState = "";
    try {
      if (!navigator.clipboard) {
        copyState = "Clipboard unavailable in this browser - select the text and copy it manually.";
        return;
      }
      await navigator.clipboard.writeText(exportContent);
      copyState = "Copied to clipboard.";
    } catch (e: any) {
      copyState = e?.message || "Copy failed - select the text and copy it manually.";
    }
  }

  async function runImport() {
    importError = "";
    notice = "";
    if (!importContent.trim()) {
      importError = "Paste the mise configuration text before importing.";
      return;
    }
    importing = true;
    try {
      const result = await environmentApi.importMise(importContent, { signal: controller.signal });
      notice = "mise.toml written to " + (result.path || "the project root") + ".";
      // A new mise.toml changes every resolved runtime, so re-check both views.
      await loadEnvironment(true);
      await loadExport();
    } catch (e: any) {
      if (controller.signal.aborted) return;
      importError = e?.message || "Failed to import the mise configuration.";
    } finally {
      importing = false;
    }
  }

  $: overall = statusVisual(status?.overallStatus);
  $: overallInfo = overallCopy(status?.overallStatus);
  $: runtimes = status?.runtimes ?? [];
  $: tasks = status?.tasks ?? [];
  $: workflowLine = workflow?.name
    ? workflow.name + " will execute in this environment"
    : "This workflow will execute in the local environment shown here.";

  onMount(() => {
    void loadEnvironment(false);
  });

  onDestroy(() => {
    controller.abort();
  });
</script>

<div class="configure">
  <header class="configure-header">
    <div class="title">
      <h2>⚙️ Configure</h2>
      <span class="subtitle">{workflowLine}</span>
    </div>
    <div class="header-actions">
      {#if notice}<span class="msg ok">{notice}</span>{/if}
      {#if error}<span class="msg err">{error}</span>{/if}
      <button class="btn primary" on:click={verify} disabled={verifying || loading}>
        {#if verifying}<span class="spinner small" aria-hidden="true"></span>{/if}
        {verifying ? "Verifying…" : "🩺 Verify environment"}
      </button>
      <button class="btn ghost" on:click={() => dispatch("close")}>✕ Close</button>
    </div>
  </header>

  <div class="configure-body">
    {#if loading}
      <section class="panel">
        <div class="panel-loading">
          <span class="spinner" aria-hidden="true"></span>
          <span>Reading the local execution environment…</span>
        </div>
      </section>
    {:else if !status}
      <section class="panel">
        <div class="error-card">
          <h3>⚠️ Environment unavailable</h3>
          <p>{error || "The backend did not return an environment status."}</p>
          <p class="hint">The .NET API must be running on port 5254 for this view to resolve.</p>
          <button class="btn" on:click={retry}>↻ Try again</button>
        </div>
      </section>
    {:else}
      <section class="panel" aria-busy={verifying}>
        <div class="panel-header">
          <h3>Execution environment</h3>
          <span class="chip {overall.className}">
            <span class="chip-icon" aria-hidden="true">{overall.icon}</span>
            <span>{overall.label}</span>
          </span>
        </div>

        <div class="banner {overall.className}" role="status">
          <span class="banner-icon" aria-hidden="true">{overall.icon}</span>
          <div class="banner-text">
            <strong>{overallInfo.title}</strong>
            <span>{overallInfo.body}</span>
            {#if verifying}<span class="banner-note">Re-checking the environment…</span>{/if}
          </div>
        </div>

        <dl class="facts">
          <div class="fact">
            <dt>Environment</dt>
            <dd>{status.name || "local polyglot environment"}</dd>
          </div>
          <div class="fact">
            <dt>Project root</dt>
            <dd>{versionText(status.projectRoot, "not detected - no mise.toml found above the API output")}</dd>
          </div>
          <div class="fact">
            <dt>mise CLI</dt>
            <dd>{status.miseInstalled ? "mise " + versionText(status.miseVersion, "installed") : "not installed"}</dd>
          </div>
          <div class="fact">
            <dt>Last checked</dt>
            <dd>{formatCheckedAt(status.checkedAt)}</dd>
          </div>
        </dl>
      </section>

      <section class="panel">
        {#if status.miseInstalled}
          <div class="banner green" role="status">
            <span class="banner-icon" aria-hidden="true">📦</span>
            <div class="banner-text">
              <strong>mise {versionText(status.miseVersion, "installed")} manages this project</strong>
              <span>
                {#if status.experimentalEnabled}
                  MISE_EXPERIMENTAL is set, so the experimental mise MCP server (mise mcp) is allowed to start. It is not wired into this platform.
                {:else}
                  MISE_EXPERIMENTAL is not set. The experimental mise MCP server (mise mcp) refuses to start without MISE_EXPERIMENTAL=1, and it is not wired into this platform.
                {/if}
              </span>
            </div>
          </div>
        {:else}
          <div class="banner yellow" role="status">
            <span class="banner-icon" aria-hidden="true">🟡</span>
            <div class="banner-text">
              <strong>mise is not installed</strong>
              <span>
                No mise CLI was found on this machine, so nothing here is managed by mise.toml. The
                platform-provided defaults (.NET 8 and Node.js LTS) are being used instead - a runtime
                marked "platform default" is not the version mise.toml would have pinned.
              </span>
              <span class="banner-note">
                Install mise, then import a mise.toml below and verify again. The experimental mise
                MCP server (mise mcp) additionally requires MISE_EXPERIMENTAL=1 and is not wired
                into this platform.
              </span>
            </div>
          </div>
        {/if}
      </section>

      <section class="panel">
        <div class="panel-header">
          <h3 id="runtimes-heading">Runtimes</h3>
          <span class="hint">{runtimes.length} requested by this project</span>
        </div>

        {#if runtimes.length === 0}
          <p class="empty">The backend reported no runtimes for this project.</p>
        {:else}
          <table class="runtime-table" aria-labelledby="runtimes-heading">
            <thead>
              <tr>
                <th scope="col">Runtime</th>
                <th scope="col">Requested</th>
                <th scope="col">Resolved</th>
                <th scope="col">Provided by</th>
                <th scope="col">Status</th>
              </tr>
            </thead>
            <tbody>
              {#each runtimes as runtime (runtime.id)}
                <tr>
                  <th scope="row">
                    <span class="runtime-name">{runtime.name || runtime.id}</span>
                    <span class="runtime-id">{runtime.id}</span>
                  </th>
                  <td>{versionText(runtime.requestedVersion, "—")}</td>
                  <td>{versionText(runtime.resolvedVersion, "not resolved")}</td>
                  <td>
                    <span class="source {runtime.source}">
                      <span aria-hidden="true">{sourceVisual(runtime.source).icon}</span>
                      <span>{sourceVisual(runtime.source).label}</span>
                    </span>
                    <span class="cell-note">{sourceNote(runtime)}</span>
                  </td>
                  <td>
                    <span class="chip {statusVisual(runtime.status).className}">
                      <span class="chip-icon" aria-hidden="true">{statusVisual(runtime.status).icon}</span>
                      <span>{statusVisual(runtime.status).label}</span>
                    </span>
                    <span class="cell-note flags">
                      {runtime.installed ? "installed" : "not installed"} ·
                      {runtime.active ? "active" : "not active"}
                    </span>
                    {#if runtime.message}
                      <span class="cell-note">{runtime.message}</span>
                    {/if}
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        {/if}
      </section>

      <section class="panel">
        <div class="panel-header">
          <h3 id="tasks-heading">Mise tasks</h3>
          <span class="hint">{tasks.length} declared</span>
        </div>
        {#if tasks.length === 0}
          <p class="empty">
            No mise tasks are declared for this project. Tasks come from mise.toml (or an inherited
            parent/global config) and are listed once mise can read it.
          </p>
        {:else}
          <ul class="task-list">
            {#each tasks as task (task.name)}
              <li>
                <span class="task-name">{task.name}</span>
                {#if task.description}
                  <span class="task-desc">{task.description}</span>
                {/if}
              </li>
            {/each}
          </ul>
        {/if}
      </section>

      <div class="split">
        <section class="panel">
          <div class="panel-header">
            <h3>Export mise configuration</h3>
            <button class="btn" on:click={loadExport} disabled={exporting}>
              {exporting ? "Exporting…" : "⬇ Export"}
            </button>
          </div>

          {#if exportError}
            <div class="error-card">
              <p>{exportError}</p>
              <button class="btn" on:click={loadExport} disabled={exporting}>↻ Retry export</button>
            </div>
          {:else if exportMissing}
            <p class="empty">
              {exportMissingMessage} Export has nothing to show until a mise.toml exists - import one
              below, or add it to the project root.
            </p>
          {:else if exportContent}
            <label class="field-label" for="export-content">mise.toml (read-only)</label>
            <textarea
              id="export-content"
              class="code-input"
              rows="12"
              readonly
              spellcheck="false"
              bind:value={exportContent}
            ></textarea>
            <div class="row-actions">
              <button class="btn" on:click={copyExport}>📋 Copy to clipboard</button>
              {#if copyState}<span class="msg ok">{copyState}</span>{/if}
            </div>
          {:else}
            <p class="empty">
              Export reads the project mise.toml and shows it here, read-only, so it can be backed up
              or pasted elsewhere.
            </p>
          {/if}
        </section>

        <section class="panel">
          <div class="panel-header">
            <h3>Import mise configuration</h3>
            <button class="btn primary" on:click={runImport} disabled={importing}>
              {importing ? "Importing…" : "⬆ Import"}
            </button>
          </div>

          <p class="warn-note">
            ⚠️ Importing overwrites mise.toml in the project root. Existing content is replaced, so
            export first if you need to keep it.
          </p>

          <label class="field-label" for="import-content">mise.toml to write (TOML)</label>
          <textarea
            id="import-content"
            class="code-input"
            rows="12"
            spellcheck="false"
            bind:value={importContent}
            placeholder={'[tools]\ndotnet = "8"\nnode = "lts"'}
          ></textarea>

          {#if importError}
            <div class="error-card" role="alert">
              <strong>Import rejected:</strong> {importError}
            </div>
          {/if}
          <p class="hint">
            On success the environment is verified and the export panel refreshes automatically.
          </p>
        </section>
      </div>
    {/if}
  </div>
</div>

<style>
  .configure {
    position: fixed;
    inset: 0;
    z-index: 200;
    display: flex;
    flex-direction: column;
    background: var(--bg-dark, #0f172a);
  }
  .configure-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    flex-wrap: wrap;
    padding: 0.75rem 1.25rem;
    border-bottom: 1px solid var(--border, #1e293b);
  }
  .configure-header h2 {
    margin: 0;
    font-size: 1.1rem;
    color: #f1f5f9;
  }
  .subtitle {
    color: #64748b;
    font-size: 0.8rem;
  }
  .header-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
  }
  .msg {
    font-size: 0.8rem;
  }
  .msg.ok {
    color: #10b981;
  }
  .msg.err {
    color: #ef4444;
  }
  .btn {
    background: var(--bg-card, #1e293b);
    color: #e2e8f0;
    border: 1px solid var(--border, #334155);
    padding: 0.4rem 0.8rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.85rem;
  }
  .btn:hover {
    opacity: 0.9;
  }
  .btn.primary {
    background: #0ea5e9;
    border-color: #0ea5e9;
    color: white;
  }
  .btn.ghost {
    background: transparent;
  }
  .btn:disabled {
    opacity: 0.5;
    cursor: default;
  }
  .configure-body {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 1.25rem;
  }
  .panel {
    padding: 1rem 1.25rem;
    border: 1px solid var(--border, #334155);
    border-radius: 0.5rem;
    background: var(--bg-card, #1e293b);
  }
  .panel-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    flex-wrap: wrap;
    margin-bottom: 0.75rem;
  }
  .panel-header h3 {
    margin: 0;
    color: #64748b;
    font-size: 0.8rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
  .panel-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    padding: 2.5rem 0;
    color: #94a3b8;
    font-size: 0.9rem;
  }
  .spinner {
    width: 20px;
    height: 20px;
    border: 2px solid var(--border, #334155);
    border-top-color: var(--primary, #0ea5e9);
    border-radius: 50%;
    animation: spin 1s linear infinite;
  }
  .spinner.small {
    width: 12px;
    height: 12px;
    display: inline-block;
    margin-right: 0.35rem;
  }
  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }
  .banner {
    display: flex;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    border: 1px solid;
    border-radius: 0.5rem;
  }
  .banner.green {
    background: rgba(34, 197, 94, 0.1);
    border-color: rgba(34, 197, 94, 0.35);
    color: #bbf7d0;
  }
  .banner.yellow {
    background: rgba(245, 158, 11, 0.1);
    border-color: rgba(245, 158, 11, 0.35);
    color: #fde68a;
  }
  .banner.red {
    background: rgba(239, 68, 68, 0.1);
    border-color: rgba(239, 68, 68, 0.35);
    color: #fecaca;
  }
  .banner.unknown {
    background: rgba(148, 163, 184, 0.1);
    border-color: rgba(148, 163, 184, 0.35);
    color: #e2e8f0;
  }
  .banner-icon {
    font-size: 1.1rem;
  }
  .banner-text {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
    font-size: 0.85rem;
    line-height: 1.45;
  }
  .banner-note {
    font-size: 0.78rem;
    opacity: 0.9;
  }
  .chip {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.15rem 0.55rem;
    border: 1px solid;
    border-radius: 0.75rem;
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.05em;
    white-space: nowrap;
  }
  .chip.green {
    background: rgba(34, 197, 94, 0.15);
    border-color: rgba(34, 197, 94, 0.45);
    color: #4ade80;
  }
  .chip.yellow {
    background: rgba(245, 158, 11, 0.15);
    border-color: rgba(245, 158, 11, 0.45);
    color: #fbbf24;
  }
  .chip.red {
    background: rgba(239, 68, 68, 0.15);
    border-color: rgba(239, 68, 68, 0.45);
    color: #f87171;
  }
  .chip.unknown {
    background: rgba(148, 163, 184, 0.15);
    border-color: rgba(148, 163, 184, 0.45);
    color: #cbd5e1;
  }
  .facts {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 0.75rem;
    margin: 0.75rem 0 0;
  }
  .fact {
    padding: 0.6rem 0.75rem;
    border-radius: 0.375rem;
    background: rgba(0, 0, 0, 0.2);
  }
  .fact dt {
    color: #64748b;
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
  .fact dd {
    margin: 0.25rem 0 0;
    color: #e2e8f0;
    font-size: 0.85rem;
    word-break: break-word;
  }
  .runtime-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.82rem;
  }
  .runtime-table th,
  .runtime-table td {
    padding: 0.6rem 0.5rem;
    border-bottom: 1px solid var(--border, #334155);
    color: #e2e8f0;
    text-align: left;
    vertical-align: top;
  }
  .runtime-table thead th {
    color: #64748b;
    font-size: 0.7rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
  .runtime-table tbody tr:last-child th,
  .runtime-table tbody tr:last-child td {
    border-bottom: none;
  }
  .runtime-name {
    display: block;
    font-weight: 600;
  }
  .runtime-id {
    display: block;
    color: #64748b;
    font-size: 0.72rem;
  }
  .source {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    padding: 0.15rem 0.45rem;
    border: 1px solid;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    white-space: nowrap;
  }
  .source.mise {
    background: rgba(14, 165, 233, 0.12);
    border-color: rgba(14, 165, 233, 0.4);
    color: #7dd3fc;
  }
  .source.platform-default {
    background: rgba(245, 158, 11, 0.12);
    border-color: rgba(245, 158, 11, 0.4);
    color: #fcd34d;
  }
  .source.none {
    background: rgba(239, 68, 68, 0.12);
    border-color: rgba(239, 68, 68, 0.4);
    color: #fca5a5;
  }
  .cell-note {
    display: block;
    margin-top: 0.3rem;
    color: #64748b;
    font-size: 0.72rem;
  }
  .cell-note.flags {
    color: #94a3b8;
  }
  .task-list {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    margin: 0;
    padding: 0;
    list-style: none;
  }
  .task-list li {
    display: flex;
    align-items: baseline;
    gap: 0.6rem;
    padding: 0.5rem 0.75rem;
    border-radius: 0.375rem;
    background: rgba(0, 0, 0, 0.2);
  }
  .task-name {
    color: #7dd3fc;
    font-family: ui-monospace, monospace;
    font-size: 0.82rem;
  }
  .task-desc {
    color: #94a3b8;
    font-size: 0.78rem;
  }
  .split {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 1rem;
    align-items: start;
  }
  .field-label {
    display: block;
    margin: 0 0 0.35rem;
    color: #94a3b8;
    font-size: 0.78rem;
  }
  .code-input {
    box-sizing: border-box;
    width: 100%;
    padding: 0.6rem;
    border: 1px solid var(--border, #334155);
    border-radius: 0.375rem;
    background: #0b1220;
    color: #e2e8f0;
    font-family: ui-monospace, monospace;
    font-size: 0.8rem;
    resize: vertical;
  }
  .code-input:focus {
    outline: none;
    border-color: var(--primary, #0ea5e9);
  }
  .row-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
    margin-top: 0.6rem;
  }
  .warn-note {
    margin: 0 0 0.75rem;
    padding: 0.5rem 0.75rem;
    border: 1px solid rgba(245, 158, 11, 0.3);
    border-radius: 0.375rem;
    background: rgba(245, 158, 11, 0.1);
    color: #fcd34d;
    font-size: 0.78rem;
  }
  .error-card {
    padding: 1rem;
    border: 1px solid rgba(239, 68, 68, 0.3);
    border-radius: 0.5rem;
    background: rgba(239, 68, 68, 0.1);
    color: #fca5a5;
    font-size: 0.85rem;
  }
  .error-card h3 {
    margin: 0 0 0.5rem;
    color: #ef4444;
  }
  .error-card p {
    margin: 0 0 0.5rem;
  }
  .hint {
    margin: 0.5rem 0 0;
    color: #64748b;
    font-size: 0.75rem;
  }
  .empty {
    margin: 0;
    color: #64748b;
    font-size: 0.85rem;
    line-height: 1.45;
  }
  @media (max-width: 700px) {
    .configure-header {
      align-items: flex-start;
      flex-direction: column;
    }
  }
</style>