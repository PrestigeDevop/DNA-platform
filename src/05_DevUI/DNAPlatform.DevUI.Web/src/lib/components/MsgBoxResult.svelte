<script lang="ts">
  export let result: any;
  export let onClose: () => void;
  
  function getColorForType(type: string): string {
    const colors: Record<string, string> = {
      'info': '#3b82f6', 'warning': '#f59e0b', 'error': '#ef4444',
      'question': '#8b5cf6', 'success': '#10b981'
    };
    return colors[type] || '#6b7280';
  }
  
  $: type = result?.data?.type || 'info';
  $: color = getColorForType(type);
</script>

{#if result}
  <div class="msgbox-overlay" on:click={onClose}>
    <div class="msgbox-container" style="border-color: {color}">
      <div class="msgbox-header" style="background-color: {color}20">
        <span class="msgbox-icon">{result.data?.icon || '💬'}</span>
        <h3>{result.data?.title || 'Alert'}</h3>
        <button class="msgbox-close" on:click={onClose}>×</button>
      </div>
      <div class="msgbox-body">
        <p class="msgbox-message">{result.data?.message}</p>
        <div class="msgbox-meta">
          <span>Type: <strong>{result.data?.type}</strong></span>
          <span>Duration: <strong>{result.durationMs}ms</strong></span>
        </div>
                        {#if result.data?.alertId}
          <div class="msgbox-id">ID: {result.data.alertId}</div>
        {/if}
      </div>
      <div class="msgbox-footer">
        <button class="btn-primary" on:click={onClose}>OK</button>
      </div>
    </div>
  </div>
{/if}

<style>
  .msgbox-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.7); display: flex; align-items: center; justify-content: center; z-index: 200; animation: fadeIn 0.2s ease; }
  .msgbox-container { background: var(--bg-card); border: 2px solid; border-radius: 0.75rem; width: 100%; max-width: 450px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5); animation: slideUp 0.3s ease; }
  .msgbox-header { display: flex; align-items: center; gap: 0.75rem; padding: 1rem 1.25rem; border-radius: 0.75rem 0.75rem 0 0; }
  .msgbox-icon { font-size: 1.5rem; }
  .msgbox-header h3 { flex: 1; font-size: 1.1rem; color: #e2e8f0; margin: 0; }
  .msgbox-close { background: none; border: none; color: #94a3b8; font-size: 1.5rem; cursor: pointer; padding: 0; line-height: 1; }
  .msgbox-close:hover { color: #e2e8f0; }
  .msgbox-body { padding: 1.25rem; }
  .msgbox-message { font-size: 1rem; color: #e2e8f0; margin: 0 0 1rem 0; line-height: 1.5; }
  .msgbox-meta { display: flex; gap: 1rem; font-size: 0.8rem; color: #94a3b8; }
  .msgbox-id { margin-top: 0.75rem; font-size: 0.7rem; color: #64748b; font-family: monospace; }
  .msgbox-footer { padding: 1rem 1.25rem; border-top: 1px solid var(--border); display: flex; justify-content: flex-end; }
  .btn-primary { background: var(--primary); color: white; border: none; padding: 0.625rem 1.25rem; border-radius: 0.375rem; cursor: pointer; font-weight: 500; }
  @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  @keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
</style>