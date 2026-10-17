import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, type Plugin } from 'vite';

function safeFetchPlugin(): Plugin {
  return {
    name: 'safe-fetch-plugin',
    enforce: 'pre',
    transform(code: string, id: string) {
      if (id.includes('fetcher.js') || code.includes('window.fetch = (input, init) => {')) {
        if (code.includes('try { window.fetch = (input, init) => {')) {
          return null;
        }
        let transformed = code.replace(
          /window\.fetch\s*=\s*\((input,\s*init)\)\s*=>\s*\{/g,
          'try { window.fetch = ($1) => {'
        );
        transformed = transformed.replace(
          /return native_fetch\(input,\s*init\);\s*\};/g,
          'return native_fetch(input, init); }; } catch (e) {}'
        );
        return { code: transformed, map: null };
      }
      return null;
    }
  };
}

export default defineConfig({
  plugins: [safeFetchPlugin(), sveltekit()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    strictPort: true
  }
});