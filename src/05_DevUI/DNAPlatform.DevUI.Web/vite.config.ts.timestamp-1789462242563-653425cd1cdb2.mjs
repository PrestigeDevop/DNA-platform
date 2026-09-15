// vite.config.ts
import { sveltekit } from "file:///C:/Users/prest/OneDrive/Desktop/VibeCoding/DNA-platform%20Cat/src/05_DevUI/DNAPlatform.DevUI.Web/node_modules/@sveltejs/kit/src/exports/vite/index.js";
import { defineConfig } from "file:///C:/Users/prest/OneDrive/Desktop/VibeCoding/DNA-platform%20Cat/src/05_DevUI/DNAPlatform.DevUI.Web/node_modules/vite/dist/node/index.js";
var vite_config_default = defineConfig({
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
      "/health": "http://127.0.0.1:5254",
      "/api/skills": "http://127.0.0.1:5254",
      "/api/node-types": "http://127.0.0.1:5254",
      "/api/status-values": "http://127.0.0.1:5254",
      "/api/info": "http://127.0.0.1:5254",
      "/api/workflows/execute": "http://127.0.0.1:5254"
    }
  }
});
export {
  vite_config_default as default
};
//# sourceMappingURL=data:application/json;base64,ewogICJ2ZXJzaW9uIjogMywKICAic291cmNlcyI6IFsidml0ZS5jb25maWcudHMiXSwKICAic291cmNlc0NvbnRlbnQiOiBbImNvbnN0IF9fdml0ZV9pbmplY3RlZF9vcmlnaW5hbF9kaXJuYW1lID0gXCJDOlxcXFxVc2Vyc1xcXFxwcmVzdFxcXFxPbmVEcml2ZVxcXFxEZXNrdG9wXFxcXFZpYmVDb2RpbmdcXFxcRE5BLXBsYXRmb3JtIENhdFxcXFxzcmNcXFxcMDVfRGV2VUlcXFxcRE5BUGxhdGZvcm0uRGV2VUkuV2ViXCI7Y29uc3QgX192aXRlX2luamVjdGVkX29yaWdpbmFsX2ZpbGVuYW1lID0gXCJDOlxcXFxVc2Vyc1xcXFxwcmVzdFxcXFxPbmVEcml2ZVxcXFxEZXNrdG9wXFxcXFZpYmVDb2RpbmdcXFxcRE5BLXBsYXRmb3JtIENhdFxcXFxzcmNcXFxcMDVfRGV2VUlcXFxcRE5BUGxhdGZvcm0uRGV2VUkuV2ViXFxcXHZpdGUuY29uZmlnLnRzXCI7Y29uc3QgX192aXRlX2luamVjdGVkX29yaWdpbmFsX2ltcG9ydF9tZXRhX3VybCA9IFwiZmlsZTovLy9DOi9Vc2Vycy9wcmVzdC9PbmVEcml2ZS9EZXNrdG9wL1ZpYmVDb2RpbmcvRE5BLXBsYXRmb3JtJTIwQ2F0L3NyYy8wNV9EZXZVSS9ETkFQbGF0Zm9ybS5EZXZVSS5XZWIvdml0ZS5jb25maWcudHNcIjtpbXBvcnQgeyBzdmVsdGVraXQgfSBmcm9tICdAc3ZlbHRlanMva2l0L3ZpdGUnO1xyXG5pbXBvcnQgeyBkZWZpbmVDb25maWcgfSBmcm9tICd2aXRlJztcclxuXHJcbmV4cG9ydCBkZWZhdWx0IGRlZmluZUNvbmZpZyh7XHJcbiAgcGx1Z2luczogW3N2ZWx0ZWtpdCgpXSxcclxuICBzZXJ2ZXI6IHtcclxuICAgIHBvcnQ6IDUxNzMsXHJcbiAgICBwcm94eToge1xyXG4gICAgICAvLyBSb3V0ZXMgaGFuZGxlZCBieSBTdmVsdGVLaXQgc2VydmVyIGVuZHBvaW50cyAoUHJpc21hKSBhcmUgTk9UIHByb3hpZWQ6XHJcbiAgICAgIC8vICAgL2FwaS93b3JrZmxvd3MgICAgICAgLT4gU3ZlbHRlS2l0IChDUlVEIHZpYSBQcmlzbWEpXHJcbiAgICAgIC8vICAgL2FwaS93b3JrZmxvd3MvOmlkICAgLT4gU3ZlbHRlS2l0IChERUxFVEUpXHJcbiAgICAgIC8vICAgL2FwaS93b3JrZmxvd3MvOmlkL2V4ZWN1dGUgLT4gU3ZlbHRlS2l0IChleGVjdXRlICsgc3RvcmUgaW4gUHJpc21hKVxyXG4gICAgICAvLyAgIC9hcGkvYWdlbnRzICAgICAgICAgIC0+IFN2ZWx0ZUtpdCAoQ1JVRCB2aWEgUHJpc21hKVxyXG4gICAgICAvLyAgIC9hcGkvYWdlbnRzLzppZCAgICAgIC0+IFN2ZWx0ZUtpdCAoREVMRVRFKVxyXG4gICAgICAvLyAgIC9hcGkvZXhlY3V0aW9ucyAgICAgIC0+IFN2ZWx0ZUtpdCAobGlzdCB2aWEgUHJpc21hKVxyXG4gICAgICAvLyBSb3V0ZXMgcHJveGllZCB0byAuTkVUIGJhY2tlbmQgKHBvcnQgNTI1NCwgSVB2NCBmb3IgZGV0ZXJtaW5pc3RpYyBiaW5kaW5nKTpcclxuICAgICAgJy9oZWFsdGgnOiAnaHR0cDovLzEyNy4wLjAuMTo1MjU0JyxcclxuICAgICAgJy9hcGkvc2tpbGxzJzogJ2h0dHA6Ly8xMjcuMC4wLjE6NTI1NCcsXHJcbiAgICAgICcvYXBpL25vZGUtdHlwZXMnOiAnaHR0cDovLzEyNy4wLjAuMTo1MjU0JyxcclxuICAgICAgJy9hcGkvc3RhdHVzLXZhbHVlcyc6ICdodHRwOi8vMTI3LjAuMC4xOjUyNTQnLFxyXG4gICAgICAnL2FwaS9pbmZvJzogJ2h0dHA6Ly8xMjcuMC4wLjE6NTI1NCcsXHJcbiAgICAgICcvYXBpL3dvcmtmbG93cy9leGVjdXRlJzogJ2h0dHA6Ly8xMjcuMC4wLjE6NTI1NCdcclxuICAgIH1cclxuICB9XHJcbn0pOyBcclxuIl0sCiAgIm1hcHBpbmdzIjogIjtBQUFrZSxTQUFTLGlCQUFpQjtBQUM1ZixTQUFTLG9CQUFvQjtBQUU3QixJQUFPLHNCQUFRLGFBQWE7QUFBQSxFQUMxQixTQUFTLENBQUMsVUFBVSxDQUFDO0FBQUEsRUFDckIsUUFBUTtBQUFBLElBQ04sTUFBTTtBQUFBLElBQ04sT0FBTztBQUFBO0FBQUE7QUFBQTtBQUFBO0FBQUE7QUFBQTtBQUFBO0FBQUE7QUFBQSxNQVNMLFdBQVc7QUFBQSxNQUNYLGVBQWU7QUFBQSxNQUNmLG1CQUFtQjtBQUFBLE1BQ25CLHNCQUFzQjtBQUFBLE1BQ3RCLGFBQWE7QUFBQSxNQUNiLDBCQUEwQjtBQUFBLElBQzVCO0FBQUEsRUFDRjtBQUNGLENBQUM7IiwKICAibmFtZXMiOiBbXQp9Cg==
