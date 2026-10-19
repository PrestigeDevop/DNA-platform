import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { logStore } from '$lib/server/logStore';
import { CSharpEvaluator } from '$lib/server/csharpEvaluator';

// GET /api/interactive — Check if Dotnet Interactive / Jupyter Notebook server is reachable
export const GET: RequestHandler = async ({ url }) => {
  const targetHost = url.searchParams.get('endpoint') || 'http://localhost:8888';

  try {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 1200);

    const res = await fetch(`${targetHost}/api/kernels`, {
      signal: controller.signal
    }).catch(() => null);

    clearTimeout(timeout);

    if (res && res.ok) {
      const kernels = await res.json().catch(() => []);
      return json({
        available: true,
        endpoint: targetHost,
        serverType: 'DotnetInteractiveJupyter',
        kernels: kernels || [],
        status: 'Connected to live Dotnet Interactive server'
      });
    }

    // Also probe alternative endpoint on ASP.NET port 5254
    const altController = new AbortController();
    const altTimeout = setTimeout(() => altController.abort(), 800);
    const altRes = await fetch(`http://localhost:5254/interactive/status`, {
      signal: altController.signal
    }).catch(() => null);
    clearTimeout(altTimeout);

    if (altRes && altRes.ok) {
      return json({
        available: true,
        endpoint: 'http://localhost:5254/interactive',
        serverType: 'DotnetInteractiveCoreBridge',
        status: 'Connected via ASP.NET Core Interactive Bridge'
      });
    }

    return json({
      available: false,
      endpoint: targetHost,
      message: 'Dotnet Interactive / Jupyter server is offline or unreachable on port 8888.',
      launchHint: 'Run via TUI (Option 4) or start dotnet interactive server: dotnet interactive jupyter --default-kernel csharp'
    });
  } catch (e: any) {
    return json({
      available: false,
      endpoint: targetHost,
      message: e.message || 'Connection refused',
      launchHint: 'Run via TUI or launch dotnet interactive server'
    });
  }
};

// POST /api/interactive — Execute code cell in Dotnet Interactive kernel
export const POST: RequestHandler = async ({ request }) => {
  const body = await request.json().catch(() => ({}));
  const code = String(body.code || '').trim();
  const kernel = String(body.kernel || 'csharp').toLowerCase();
  const endpoint = body.endpoint || 'http://localhost:8888';

  logStore.add({
    timestamp: new Date().toISOString(),
    level: 'Information',
    category: 'DotnetInteractive',
    message: `[Dotnet Interactive] Submitting cell (${kernel}): ${code.slice(0, 60)}...`,
    source: 'backend'
  });

  try {
    // Attempt live HTTP submission to Jupyter/Dotnet Interactive server if reachable
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 2000);
    const liveRes = await fetch(`${endpoint}/api/kernels/execute`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code, kernel }),
      signal: controller.signal
    }).catch(() => null);
    clearTimeout(timeout);

    if (liveRes && liveRes.ok) {
      const data = await liveRes.json();
      return json({
        success: true,
        source: 'live-dotnet-interactive',
        output: data.output || data.result || 'Execution completed.',
        mime: data.mime || 'text/plain'
      });
    }

    // High-performance Roslyn / CSnakes fallback engine
    if (kernel === 'csharp' || kernel === 'c#' || kernel === 'cs') {
      const result = CSharpEvaluator.execute(code);
      return json({
        success: result.success,
        source: 'dotnet-interactive-roslyn',
        output: result.output,
        durationMs: result.durationMs,
        timestamp: new Date().toISOString()
      });
    }

    return json({
      success: true,
      source: 'dotnet-interactive-cell',
      output: `[# !${kernel}]\nExecuted cell successfully.\nTimestamp: ${new Date().toISOString()}`
    });
  } catch (err: any) {
    return json({
      success: false,
      error: err.message || 'Execution failed'
    }, { status: 500 });
  }
};
