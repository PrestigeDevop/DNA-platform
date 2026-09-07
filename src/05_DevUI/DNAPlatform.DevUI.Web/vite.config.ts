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
      '/api/workflows/execute': 'http://127.0.0.1:5254'
    }
  }
}); 
