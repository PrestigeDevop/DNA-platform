import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { mcpRegistry } from '$lib/server/mcp/registry';

export const GET: RequestHandler = async () => {
  const servers = mcpRegistry.listServers().map((s) => ({
    ServerId: s.serverId,
    Name: s.name,
    Version: s.version,
    Status: s.status,
    Message: s.description,
    Category: s.category,
    Tools: s.tools.map((t) => ({
      Name: t.name,
      Description: t.description,
      InputSchema: t.inputSchema?.properties
        ? Object.fromEntries(
            Object.entries(t.inputSchema.properties).map(([k, v]) => [
              k,
              { propName: v.type, description: v.description }
            ])
          )
        : null
    }))
  }));

  return json({
    Count: servers.length,
    Servers: servers
  });
};
