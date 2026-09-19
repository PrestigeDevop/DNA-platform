<script lang="ts">
  export let onExecute: (inputs: any) => Promise<any>;
  
  let inputs = {
    message: 'Hello from DNA Platform!',
    msgType: 'info',
    title: 'Alert',
    duration: 0
  };
  
  const msgBoxTypes = ['info', 'warning', 'error', 'question', 'success'];
  
  function getIconForType(type: string): string {
    const icons: Record<string, string> = {
      'info': 'ℹ️', 'warning': '⚠️', 'error': '❌', 'question': '❓', 'success': '✅'
    };
    return icons[type] || '💬';
  }
  
  async function execute() {
    await onExecute(inputs);
  }
</script>

<div class="demo-section">
  <h2>🎯 Quick Demo: MsgBox Alert Skill</h2>
  <p class="demo-description">Test the simplest skill - configure inputs and execute from GUI</p>
  
  <div class="demo-card">
    <div class="demo-inputs">
      <div class="form-group">
        <label for="msg-message">Message Content</label>
        <input id="msg-message" type="text" bind:value={inputs.message} placeholder="Enter your message..." />
      </div>
      
      <div class="form-group">
        <label for="msg-type">Message Type</label>
        <select id="msg-type" bind:value={inputs.msgType}>
          {#each msgBoxTypes as type}
            <option value={type}>
              {getIconForType(type)} {type.charAt(0).toUpperCase() + type.slice(1)}
            </option>
          {/each}
        </select>
      </div>
      
      <div class="form-group">
        <label for="msg-title">Title</label>
        <input id="msg-title" type="text" bind:value={inputs.title} placeholder="Alert title..." />
      </div>
      
      <div class="form-group">
        <label for="msg-duration">Auto-dismiss (ms)</label>
        <input id="msg-duration" type="number" bind:value={inputs.duration} min="0" step="1000" />
      </div>
    </div>
    
    <button class="btn-execute-large" on:click={execute}>
      ▶ Execute MsgBox Alert
    </button>
  </div>
</div>

<style>
  .demo-section { background: linear-gradient(135deg, rgba(14, 165, 233, 0.1), rgba(6, 182, 212, 0.1)); border: 1px solid rgba(14, 165, 233, 0.3); border-radius: 0.75rem; padding: 1.5rem; margin-bottom: 2rem; }
  .demo-section h2 { font-size: 1.25rem; color: #e2e8f0; margin: 0 0 0.5rem 0; }
  .demo-description { color: #94a3b8; margin: 0 0 1rem 0; }
  .demo-card { background: var(--bg-card); border-radius: 0.5rem; padding: 1.25rem; }
  .demo-inputs { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; }
  .form-group { display: flex; flex-direction: column; }
  .form-group label { color: #94a3b8; font-size: 0.8rem; margin-bottom: 0.375rem; font-weight: 500; }
  .form-group input, .form-group select { background: var(--bg-dark); border: 1px solid var(--border); border-radius: 0.375rem; padding: 0.625rem; color: #e2e8f0; font-size: 0.9rem; }
  .form-group input:focus, .form-group select:focus { outline: none; border-color: var(--primary); }
  .btn-execute-large { background: linear-gradient(135deg, #0ea5e9, #06b6d4); color: white; border: none; padding: 0.875rem 1.5rem; border-radius: 0.5rem; cursor: pointer; font-size: 1rem; font-weight: 600; width: 100%; margin-top: 1rem; }
  .btn-execute-large:hover { opacity: 0.9; transform: translateY(-1px); }
</style>