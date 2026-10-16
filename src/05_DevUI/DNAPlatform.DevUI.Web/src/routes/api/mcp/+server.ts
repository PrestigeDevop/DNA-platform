import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { mcpRegistry } from '$lib/server/mcp/registry';
import { logStore } from '$lib/server/logStore';

// GET /api/mcp — MCP Server Metadata & Capability Discovery
export const GET: RequestHandler = async () => {
  const servers = mcpRegistry.listServers();
  const allTools = servers.flatMap((s) =>
    s.tools.map((t) => ({
      ...t,
      serverId: s.serverId,
      fullName: `${s.serverId}/${t.name}`
    }))
  );

  return json({
    name: 'DNA Platform MCP Endpoint',
    version: '1.0.0',
    protocolVersion: '2024-11-05',
    status: 'Running',
    serversCount: servers.length,
    toolsCount: allTools.length,
    endpoints: {
      jsonRpc: '/api/mcp',
      servers: '/api/mcp/servers',
      marketplace: '/api/mcp/marketplace',
      toolExecute: '/api/mcp/tools/execute'
    },
    tools: allTools
  });
};

// POST /api/mcp — Official JSON-RPC 2.0 Model Context Protocol Endpoint
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const id = body.id ?? 1;
    const method = body.method;
    const params = body.params || {};

    logStore.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'MCP-RPC',
      message: `[MCP JSON-RPC] Handling method: "${method}" (id: ${id})`,
      source: 'backend'
    });

    // 1. initialize
    if (method === 'initialize') {
      return json({
        jsonrpc: '2.0',
        id,
        result: {
          protocolVersion: '2024-11-05',
          capabilities: {
            tools: { listChanged: true },
            resources: { subscribe: false, listChanged: true },
            prompts: { listChanged: true }
          },
          serverInfo: {
            name: 'DNA-Platform-MCP-Core',
            version: '1.0.0'
          }
        }
      });
    }

    // 2. ping
    if (method === 'ping') {
      return json({ jsonrpc: '2.0', id, result: {} });
    }

    // 3. tools/list
    if (method === 'tools/list') {
      const servers = mcpRegistry.listServers();
      const tools = servers.flatMap((s) =>
        s.tools.map((t) => ({
          name: `${s.serverId}/${t.name}`,
          description: `[${s.name}] ${t.description}`,
          inputSchema: t.inputSchema || {
            type: 'object',
            properties: {}
          }
        }))
      );

      return json({
        jsonrpc: '2.0',
        id,
        result: {
          tools
        }
      });
    }

    // 4. tools/call
    if (method === 'tools/call') {
      const toolName = params.name;
      const args = params.arguments || {};

      if (!toolName) {
        return json(
          {
            jsonrpc: '2.0',
            id,
            error: { code: -32602, message: 'Invalid params: tool name is required' }
          },
          { status: 400 }
        );
      }

      let serverId = 'dna-bioinformatics';
      let cleanTool = toolName;

      if (toolName.includes('/')) {
        const parts = toolName.split('/');
        serverId = parts[0];
        cleanTool = parts.slice(1).join('/');
      } else {
        // Find server that owns this tool
        const servers = mcpRegistry.listServers();
        for (const s of servers) {
          if (s.tools.some((t) => t.name === toolName)) {
            serverId = s.serverId;
            cleanTool = toolName;
            break;
          }
        }
      }

      try {
        const result = await mcpRegistry.callTool(serverId, cleanTool, args);
        return json({
          jsonrpc: '2.0',
          id,
          result: {
            content: [
              {
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
              }
            ],
            isError: false
          }
        });
      } catch (err: any) {
        return json({
          jsonrpc: '2.0',
          id,
          result: {
            content: [
              {
                type: 'text',
                text: `Error executing MCP tool "${toolName}": ${err.message || String(err)}`
              }
            ],
            isError: true
          }
        });
      }
    }

    // 5. resources/list
    if (method === 'resources/list') {
      return json({
        jsonrpc: '2.0',
        id,
        result: {
          resources: [
            {
              uri: 'bio://genetic-code',
              name: 'Standard Genetic Codon Table',
              description: 'Standard translation codon table for 64 triplets into 20 amino acids',
              mimeType: 'application/json'
            },
            {
              uri: 'system://diagnostics',
              name: 'Runtime Engine Diagnostics',
              description: 'Node.js memory, uptime and ONNX runtime state',
              mimeType: 'application/json'
            }
          ]
        }
      });
    }

    // 6. resources/read
    if (method === 'resources/read') {
      const uri = params.uri;
      if (uri === 'bio://genetic-code') {
        return json({
          jsonrpc: '2.0',
          id,
          result: {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify({
                  ATG: 'Methionine (Start)',
                  TAA: 'Ochre (Stop)',
                  TAG: 'Amber (Stop)',
                  TGA: 'Opal (Stop)',
                  notes: 'Standard nuclear code'
                })
              }
            ]
          }
        });
      }

      if (uri === 'system://diagnostics') {
        return json({
          jsonrpc: '2.0',
          id,
          result: {
            contents: [
              {
                uri,
                mimeType: 'application/json',
                text: JSON.stringify({
                  platform: process.platform,
                  nodeVersion: process.version,
                  uptime: process.uptime(),
                  memory: process.memoryUsage()
                })
              }
            ]
          }
        });
      }

      return json({
        jsonrpc: '2.0',
        id,
        error: { code: -32602, message: `Resource not found: ${uri}` }
      });
    }

    // 7. prompts/list
    if (method === 'prompts/list') {
      return json({
        jsonrpc: '2.0',
        id,
        result: {
          prompts: [
            {
              name: 'dna-prompt-flow',
              description: 'Bioinformatic sequence reasoning prompt flow with Microsoft Copilot ONNX',
              arguments: [
                {
                  name: 'sequence',
                  description: 'Target DNA nucleotide sequence',
                  required: true
                }
              ]
            },
            {
              name: 'kilo-data-analysis',
              description: 'Kilo notebook-first data analysis prompt template',
              arguments: [
                {
                  name: 'dataset',
                  description: 'Data input or file to analyze',
                  required: true
                }
              ]
            }
          ]
        }
      });
    }

    // Default unknown method
    return json({
      jsonrpc: '2.0',
      id,
      error: { code: -32601, message: `Method not found: ${method}` }
    });
  } catch (e: any) {
    return json(
      {
        jsonrpc: '2.0',
        id: 1,
        error: { code: -32700, message: `Parse error: ${e.message}` }
      },
      { status: 400 }
    );
  }
};
