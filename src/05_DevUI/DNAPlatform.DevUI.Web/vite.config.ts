import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [sveltekit()],
  server: {
    port: 5173,
    proxy: {
      // Routes handled by SvelteKit server endpoints (Prisma) are NOT proxied:
      //   /api/workflows       -> SvelteKit (CRUD via Prisma)
      //   /api/workflows/:id   -> SvelteKit (DELETE)
      //   /api/workflows/:id/execute -> SvelteKit (execute + store in Prisma)
      //   /api/agents          -> SvelteKit (CRUD via Prisma)
      //   /api/agents/:id      -> SvelteKit (DELETE)
      //   /api/executions      -> SvelteKit (list via Prisma)
      // Routes proxied to .NET backend (port 5254, IPv4 for deterministic binding):
      '/health': 'http://127.0.0.1:5254',
      '/api/skills': 'http://127.0.0.1:5254',
      '/api/node-types': 'http://127.0.0.1:5254',
      '/api/status-values': 'http://127.0.0.1:5254',
      '/api/info': 'http://127.0.0.1:5254',
      // Ad-hoc execute: the definition comes from the request body (designer).
      '/api/workflows/execute': 'http://127.0.0.1:5254',
      // Workflow-level Test step of Configure -> Test -> Run
      // (docs/refactor.md section 4). The action exists on WorkflowsController but
      // was unreachable from the browser until this entry was added.
      '/api/workflows/validate': 'http://127.0.0.1:5254',
      // Environment/mise introspection for the workflow Configure view
      // (docs/refactor.md section 4) - served by EnvironmentController on :5254.
      '/api/environment': 'http://127.0.0.1:5254',
      // MCP plugin discovery (McpServersController on :5254)
      '/api/mcp': 'http://127.0.0.1:5254',
      // SSE log stream (LogStreamService on :5254) - NOT handled by SvelteKit,
      // so the Vite proxy passes it through to the .NET backend.
      '/api/logs/stream': 'http://127.0.0.1:5254'
    }
  }
});