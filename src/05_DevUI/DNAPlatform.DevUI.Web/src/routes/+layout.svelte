<script lang="ts">
  import '../app.css';
  import { page } from '$app/stores';
  
  let sidebarOpen = true;
  
  const navItems = [
    { path: '/', label: 'Dashboard', icon: '📊' },
    { path: '/workflows', label: 'Workflows', icon: '🔄' },
    { path: '/skills', label: 'Skills', icon: '🧩' },
    { path: '/executions', label: 'Executions', icon: '⚡' },
    { path: '/agents', label: 'Agents', icon: '🤖' }
  ];
  
  $: currentPath = $page.url.pathname;
</script>

<div class="app-container">
  <!-- Sidebar -->
  <aside class="sidebar" class:collapsed={!sidebarOpen}>
    <div class="sidebar-header">
      <h1 class="logo">🧬 DNA Platform</h1>
      <button class="toggle-btn" on:click={() => sidebarOpen = !sidebarOpen}>
        {sidebarOpen ? '◀' : '▶'}
      </button>
    </div>
    
    <nav class="sidebar-nav">
      {#each navItems as item}
        <a 
          href={item.path} 
          class="nav-item"
          class:active={currentPath === item.path}
        >
          <span class="nav-icon">{item.icon}</span>
          {#if sidebarOpen}
            <span class="nav-label">{item.label}</span>
          {/if}
        </a>
      {/each}
    </nav>
    
    {#if sidebarOpen}
      <div class="sidebar-footer">
        <p class="version">v0.1.0-alpha</p>
        <p class="status">● Backend Connected</p>
      </div>
    {/if}
  </aside>
  
  <!-- Main Content -->
  <main class="main-content">
    <header class="top-bar">
      <h2 class="page-title">
        {navItems.find(i => i.path === currentPath)?.label || 'Dashboard'}
      </h2>
      <div class="top-bar-actions">
        <a href="http://localhost:5254/swagger" target="_blank" class="api-link">
          API Docs
        </a>
      </div>
    </header>
    
    <div class="content-area">
      <slot />
    </div>
  </main>
</div>

<style>
  .app-container {
    display: flex;
    min-height: 100vh;
    background: var(--bg-dark);
  }
  
  .sidebar {
    width: 240px;
    background: var(--bg-card);
    border-right: 1px solid var(--border);
    display: flex;
    flex-direction: column;
    transition: width 0.2s ease;
  }
  
  .sidebar.collapsed {
    width: 60px;
  }
  
  .sidebar-header {
    padding: 1rem;
    border-bottom: 1px solid var(--border);
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  
  .logo {
    font-size: 1rem;
    font-weight: 600;
    color: var(--primary);
    margin: 0;
  }
  
  .toggle-btn {
    background: none;
    border: none;
    color: #94a3b8;
    cursor: pointer;
    padding: 0.25rem;
  }
  
  .toggle-btn:hover {
    color: var(--primary);
  }
  
  .sidebar-nav {
    flex: 1;
    padding: 1rem 0;
  }
  
  .nav-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    color: #94a3b8;
    text-decoration: none;
    transition: all 0.2s ease;
  }
  
  .nav-item:hover {
    background: rgba(14, 165, 233, 0.1);
    color: #e2e8f0;
  }
  
  .nav-item.active {
    background: rgba(14, 165, 233, 0.2);
    color: var(--primary);
    border-right: 3px solid var(--primary);
  }
  
  .nav-icon {
    font-size: 1.25rem;
  }
  
  .nav-label {
    font-size: 0.9rem;
  }
  
  .sidebar-footer {
    padding: 1rem;
    border-top: 1px solid var(--border);
  }
  
  .version {
    font-size: 0.75rem;
    color: #64748b;
    margin: 0;
  }
  
  .status {
    font-size: 0.75rem;
    color: #22c55e;
    margin: 0.25rem 0 0 0;
  }
  
  .main-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }
  
  .top-bar {
    height: 60px;
    background: var(--bg-card);
    border-bottom: 1px solid var(--border);
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 1.5rem;
  }
  
  .page-title {
    font-size: 1.25rem;
    font-weight: 600;
    color: #e2e8f0;
    margin: 0;
  }
  
  .api-link {
    color: var(--primary);
    text-decoration: none;
    font-size: 0.875rem;
    padding: 0.5rem 1rem;
    border: 1px solid var(--primary);
    border-radius: 0.375rem;
    transition: all 0.2s ease;
  }
  
  .api-link:hover {
    background: var(--primary);
    color: white;
  }
  
  .content-area {
    flex: 1;
    padding: 1.5rem;
    overflow-y: auto;
  }
</style>