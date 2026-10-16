import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { mcpRegistry } from '$lib/server/mcp/registry';

// POST /api/mcp/tools/execute - execute an MCP tool
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const { serverId, toolName, args } = body;
    if (!serverId || !toolName) {
      return json({ error: 'serverId and toolName are required' }, { status: 400 });
    }

    const start = Date.now();
    const result = await mcpRegistry.callTool(serverId, toolName, args || {});
    return json({
      success: true,
      serverId,
      toolName,
      result,
      durationMs: Date.now() - start
    });
  } catch (error: any) {
    return json({ success: false, error: error.message || 'Tool execution failed' }, { status: 500 });
  }
};
