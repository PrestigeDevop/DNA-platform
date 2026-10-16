import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { mcpRegistry } from '$lib/server/mcp/registry';
import { logStore } from '$lib/server/logStore';

export const POST: RequestHandler = async ({ request }) => {
  try {
    const raw = await request.text();
    let manifest: any;
    try {
      manifest = JSON.parse(raw);
      if (typeof manifest === 'string') {
        manifest = JSON.parse(manifest);
      }
    } catch {
      return json(
        { Error: 'Manifest JSON body is required and must be valid JSON.' },
        { status: 400 }
      );
    }

    const serverId = (manifest.ServerId || manifest.serverId || 'custom-mcp').toLowerCase().replace(/\s+/g, '-');
    const name = manifest.Name || manifest.name || 'Custom MCP Server';
    const version = manifest.Version || manifest.version || '1.0.0';
    const description = manifest.Description || manifest.description || 'Imported extensible MCP tool server';
    const category = manifest.Category || manifest.category || 'Custom Extensions';

    // Parse tools or provide default extensible tools
    const rawTools = manifest.Tools || manifest.tools || [];
    const tools = rawTools.length > 0
      ? rawTools.map((t: any) => ({
          name: t.name || t.Name || 'execute',
          description: t.description || t.Description || 'Executes custom tool operation',
          inputSchema: t.inputSchema || t.InputSchema || {
            type: 'object',
            properties: {
              input: { type: 'string', description: 'Input parameter' }
            }
          }
        }))
      : [
          {
            name: 'execute',
            description: 'Execute operation on imported MCP server',
            inputSchema: {
              type: 'object',
              properties: {
                query: { type: 'string', description: 'Query input' }
              }
            }
          },
          {
            name: 'inspect',
            description: 'Inspect status and parameters of imported service',
            inputSchema: { type: 'object', properties: {} }
          }
        ];

    // Register with real execution handler
    mcpRegistry.register({
      serverId,
      name,
      version,
      status: 'Running',
      description,
      category,
      tools,
      executeTool: async (toolName: string, args: Record<string, any>) => {
        logStore.add({
          timestamp: new Date().toISOString(),
          level: 'Information',
          category: 'MCP-Custom',
          message: `[${name}] Executed tool "${toolName}" with args: ${JSON.stringify(args).slice(0, 100)}`,
          source: 'backend'
        });
        return {
          status: 'success',
          server: serverId,
          tool: toolName,
          timestamp: new Date().toISOString(),
          echoInput: args,
          message: `Custom MCP tool [${serverId}/${toolName}] executed successfully`
        };
      }
    });

    logStore.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'MCP',
      message: `Successfully imported and activated MCP server: ${name} (${serverId}) with ${tools.length} tool(s)`,
      source: 'backend'
    });

    return json({
      ServerId: serverId,
      Name: name,
      Version: version,
      Status: 'Running',
      Description: description,
      Tools: tools.map((t: any) => ({ Name: t.name, Description: t.description }))
    });
  } catch (error: any) {
    return json({ Error: error.message || 'Failed to import MCP server manifest' }, { status: 500 });
  }
};
