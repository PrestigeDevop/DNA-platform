<script lang="ts">
	import { onMount } from 'svelte';

	interface Tool {
		Name: string;
		Description: string;
		InputSchema: Record<string, { propName: string }> | null;
	}

	interface Server {
		ServerId: string;
		Name: string;
		Version: string;
		Status: string;
		Message?: string;
		Tools: Tool[];
	}

	interface ApiResponse {
		Count: number;
		Servers: Server[];
	}

	interface ImportResponse {
		ServerId: string;
		Name: string;
		Version: string;
		Status: string;
		Tools: { Name: string; Description: string }[];
	}

	let servers: Server[] = [];
	let loading = true;
	let error: string | null = null;
	let showImportModal = false;
	let importError: string | null = null;
	let importSuccess = '';
	let manifestText = `{
  "ServerId": "example-server",
  "Name": "Example MCP Server",
  "Version": "1.0.0",
  "Command": "npx",
  "Args": ["-y", "@modelcontextprotocol/server-example"],
  "Env": {}
}`;

	async function fetchServers() {
		loading = true;
		error = null;
		try {
			const res = await fetch('/api/mcp/servers');
			if (!res.ok) throw new Error(`HTTP ${res.status}`);
			const data: ApiResponse = await res.json();
			servers = data.Servers || [];
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load servers';
		} finally {
			loading = false;
		}
	}

	async function importManifest() {
		importError = null;
		importSuccess = '';
		try {
			const manifest = JSON.parse(manifestText);
			const res = await fetch('/api/mcp/servers/import', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify(manifest)
			});
			if (!res.ok) {
				const err = await res.json();
				throw new Error(err.Error || `HTTP ${res.status}`);
			}
			const data: ImportResponse = await res.json();
			await fetchServers();
			importSuccess = `Imported "${data.Name}" (${data.ServerId})`;
			manifestText = `{
  "ServerId": "example-server",
  "Name": "Example MCP Server",
  "Version": "1.0.0",
  "Command": "npx",
  "Args": ["-y", "@modelcontextprotocol/server-example"],
  "Env": {}
}`;
		} catch (e) {
			importError = e instanceof Error ? e.message : 'Import failed';
		}
	}

	function handleFileImport(event: Event) {
		const input = event.target as HTMLInputElement;
		const file = input.files?.[0];
		if (!file) return;
		const reader = new FileReader();
		reader.onload = (e) => {
			manifestText = e.target?.result as string;
			showImportModal = true;
		};
		reader.readAsText(file);
		input.value = '';
	}

	function getStatusColor(status: string): string {
		const s = status.toLowerCase();
		if (s === 'running') return 'green';
		if (s === 'pending') return 'yellow';
		if (s === 'error') return 'red';
		return 'grey';
	}

	function getStatusBadge(status: string) {
		const color = getStatusColor(status);
		const bg = {
			green: 'rgba(34, 197, 94, 0.15)',
			yellow: 'rgba(234, 179, 8, 0.15)',
			red: 'rgba(239, 68, 68, 0.15)',
			grey: 'rgba(148, 163, 184, 0.15)'
		}[color];
		const text = {
			green: '#22c55e',
			yellow: '#eab308',
			red: '#ef4444',
			grey: '#94a3b8'
		}[color];
		return { bg, text };
	}

	onMount(fetchServers);
</script>

<style>
	.container {
		padding: 1.5rem;
		background: var(--bg-card);
		border-radius: 8px;
		border: 1px solid var(--border);
		min-height: 400px;
	}

	.header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1.5rem;
	}

	.title {
		font-size: 1.25rem;
		font-weight: 600;
		color: #e2e8f0;
	}

	.btn {
		padding: 0.5rem 1rem;
		border-radius: 6px;
		border: 1px solid var(--border);
		background: var(--bg-card);
		color: #e2e8f0;
		cursor: pointer;
		font-size: 0.875rem;
		transition: all 0.15s;
	}

	.btn:hover {
		background: var(--primary);
		border-color: var(--primary);
	}

	.btn-primary {
		background: var(--primary);
		border-color: var(--primary);
	}

	.btn-primary:hover {
		filter: brightness(1.1);
	}

	.btn-group {
		display: flex;
		gap: 0.5rem;
	}

	.loading,
	.empty,
	.error-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 3rem;
		color: #94a3b8;
		gap: 0.5rem;
	}

	.error-state {
		color: #ef4444;
	}

	.server-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
		gap: 1rem;
	}

	.server-card {
		background: var(--bg-card);
		border: 1px solid var(--border);
		border-radius: 8px;
		padding: 1rem;
	}

	.server-header {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		margin-bottom: 0.75rem;
	}

	.server-name {
		font-weight: 600;
		color: #e2e8f0;
		font-size: 1rem;
	}

	.server-version {
		color: #94a3b8;
		font-size: 0.75rem;
		margin-left: 0.5rem;
	}

	.status-badge {
		padding: 0.25rem 0.5rem;
		border-radius: 9999px;
		font-size: 0.7rem;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.server-message {
		color: #94a3b8;
		font-size: 0.8rem;
		margin-bottom: 0.75rem;
		font-style: italic;
	}

	.tools-section {
		border-top: 1px solid var(--border);
		padding-top: 0.75rem;
	}

	.tools-label {
		font-size: 0.75rem;
		font-weight: 600;
		color: #94a3b8;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		margin-bottom: 0.5rem;
	}

	.tool-item {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		padding: 0.5rem 0;
		border-bottom: 1px solid var(--border);
	}

	.tool-item:last-child {
		border-bottom: none;
	}

	.tool-name {
		font-weight: 500;
		color: #e2e8f0;
		font-size: 0.85rem;
	}

	.tool-description {
		color: #94a3b8;
		font-size: 0.75rem;
	}

	.tool-schema {
		display: flex;
		flex-wrap: wrap;
		gap: 0.25rem;
		margin-top: 0.25rem;
	}

	.schema-tag {
		background: var(--primary);
		color: white;
		padding: 0.125rem 0.375rem;
		border-radius: 4px;
		font-size: 0.65rem;
		font-family: monospace;
	}

	.modal-overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.6);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 100;
		padding: 1rem;
	}

	.modal {
		background: var(--bg-card);
		border: 1px solid var(--border);
		border-radius: 12px;
		width: 100%;
		max-width: 600px;
		max-height: 80vh;
		overflow: hidden;
		display: flex;
		flex-direction: column;
	}

	.modal-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 1rem 1.5rem;
		border-bottom: 1px solid var(--border);
	}

	.modal-title {
		font-weight: 600;
		color: #e2e8f0;
		font-size: 1.1rem;
	}

	.modal-close {
		background: none;
		border: none;
		color: #94a3b8;
		cursor: pointer;
		font-size: 1.5rem;
		line-height: 1;
		padding: 0;
	}

	.modal-close:hover {
		color: #e2e8f0;
	}

	.modal-body {
		padding: 1.5rem;
		overflow-y: auto;
	}

	.modal-footer {
		display: flex;
		justify-content: flex-end;
		gap: 0.75rem;
		padding: 1rem 1.5rem;
		border-top: 1px solid var(--border);
	}

	.textarea {
		width: 100%;
		min-height: 250px;
		padding: 0.75rem;
		border: 1px solid var(--border);
		border-radius: 6px;
		background: #0f172a;
		color: #e2e8f0;
		font-family: monospace;
		font-size: 0.8rem;
		line-height: 1.5;
		resize: vertical;
		box-sizing: border-box;
	}

	.textarea:focus {
		outline: none;
		border-color: var(--primary);
	}

	.modal-error {
		color: #ef4444;
		font-size: 0.85rem;
		margin-bottom: 1rem;
		padding: 0.75rem;
		background: rgba(239, 68, 68, 0.1);
		border-radius: 6px;
		border: 1px solid rgba(239, 68, 68, 0.2);
	}

	.modal-success {
		color: #22c55e;
		font-size: 0.85rem;
		margin-bottom: 1rem;
		padding: 0.75rem;
		background: rgba(34, 197, 94, 0.1);
		border-radius: 6px;
		border: 1px solid rgba(34, 197, 94, 0.2);
	}

	.file-input {
		display: none;
	}
</style>

<div class="container">
	<div class="header">
		<h2 class="title">MCP Servers</h2>
		<div class="btn-group">
			<button class="btn" on:click={() => (showImportModal = true)}>Browse marketplace</button>
			<label class="btn" for="file-import">Install from file</label>
			<input
				id="file-import"
				type="file"
				class="file-input"
				accept=".json"
				on:change={handleFileImport}
			/>
		</div>
	</div>

	{#if loading}
		<div class="loading">Loading servers�</div>
	{:else if error}
		<div class="error-state">
			<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<circle cx="12" cy="12" r="10"></circle>
				<line x1="15" y1="9" x2="9" y2="15"></line>
				<line x1="9" y1="9" x2="15" y2="15"></line>
			</svg>
			<span>{error}</span>
			<button class="btn btn-primary" on:click={fetchServers}>Retry</button>
		</div>
	{:else if servers.length === 0}
		<div class="empty">
			<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
				<rect x="2" y="3" width="20" height="14" rx="2"></rect>
				<path d="M8 21h8"></path>
				<path d="M12 17v4"></path>
			</svg>
			<span>No MCP servers configured</span>
			<button class="btn btn-primary" on:click={() => (showImportModal = true)}>Add your first server</button>
		</div>
	{:else}
		<div class="server-grid">
			{#each servers as server}
				<div class="server-card">
					<div class="server-header">
						<div>
							<span class="server-name">{server.Name}</span>
							{#if server.Version}
								<span class="server-version">v{server.Version}</span>
							{/if}
						</div>
						{#if server.Status}
							{@const badge = getStatusBadge(server.Status)}
							<span class="status-badge" style="background: {badge.bg}; color: {badge.text};">
								{server.Status}
							</span>
						{/if}
					</div>
					{#if server.Message}
						<div class="server-message">{server.Message}</div>
					{/if}
					<div class="tools-section">
						<div class="tools-label">Tools ({server.Tools.length})</div>
						{#each server.Tools as tool}
							<div class="tool-item">
								<span class="tool-name">{tool.Name}</span>
								{#if tool.Description}
									<span class="tool-description">{tool.Description}</span>
								{/if}
								{#if tool.InputSchema && Object.keys(tool.InputSchema).length > 0}
									<div class="tool-schema">
										{#each Object.keys(tool.InputSchema) as key}
											<span class="schema-tag">{key}</span>
										{/each}
									</div>
								{/if}
							</div>
						{/each}
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

{#if showImportModal}
	<!-- svelte-ignore a11y_click_events_have_key_events a11y_no_static_element_interactions -->
	<div class="modal-overlay" on:click={() => (showImportModal = false)}>
		<!-- svelte-ignore a11y_click_events_have_key_events a11y_no_static_element_interactions -->
		<div class="modal" on:click|stopPropagation>
			<div class="modal-header">
				<h3 class="modal-title">Import MCP Server</h3>
				<button class="modal-close" on:click={() => (showImportModal = false)}>&times;</button>
			</div>
			<div class="modal-body">
				{#if importError}
					<div class="modal-error">{importError}</div>
				{/if}
				{#if importSuccess}
					<div class="modal-success">{importSuccess}</div>
				{/if}
				<textarea
					class="textarea"
					bind:value={manifestText}
					placeholder='Paste manifest JSON here�'
				></textarea>
			</div>
			<div class="modal-footer">
				<button class="btn" on:click={() => (showImportModal = false)}>Cancel</button>
				<button class="btn btn-primary" on:click={importManifest} disabled={loading}>Import</button>
			</div>
		</div>
	</div>
{/if}



