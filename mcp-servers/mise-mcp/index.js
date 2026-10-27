#!/usr/bin/env node

/**
 * Genuine Mise MCP Server
 * A dependency-free Model Context Protocol server that exposes the mise toolchain
 * to any MCP client over stdio.
 *
 * It is a thin, genuine wrapper around the real `mise` CLI: every tool shells out
 * to `mise` and returns the real output. Nothing is faked.
 */

import { spawn } from 'node:child_process';
import * as readline from 'node:readline';

const MISE_BIN = process.env.MISE_BIN || 'mise';

function runMiseCommand(args, cwd = process.cwd()) {
  return new Promise((resolve) => {
    const start = Date.now();
    let stdout = '';
    let stderr = '';

    const proc = spawn(MISE_BIN, args, {
      cwd,
      env: { ...process.env, NO_COLOR: '1' }
    });

    proc.stdout.on('data', (d) => {
      stdout += d.toString();
    });

    proc.stderr.on('data', (d) => {
      stderr += d.toString();
    });

    proc.on('close', (code) => {
      const durationMs = Date.now() - start;
      resolve({
        code,
        stdout: stdout.trim(),
        stderr: stderr.trim(),
        durationMs
      });
    });

    proc.on('error', (err) => {
      resolve({
        code: -1,
        stdout: '',
        stderr: err.message,
        durationMs: Date.now() - start
      });
    });
  });
}

const TOOLS = [
  {
    name: 'mise_list',
    description: 'Lists all installed language toolchains, versions, and paths managed by mise.',
    inputSchema: {
      type: 'object',
      properties: {
        json: {
          type: 'boolean',
          description: 'Return output as structured JSON',
          default: true
        }
      }
    }
  },
  {
    name: 'mise_current',
    description: 'Queries active toolchain versions currently in use in the active project directory.',
    inputSchema: {
      type: 'object',
      properties: {
        plugin: {
          type: 'string',
          description: 'Optional plugin name (e.g. node, python, dotnet) to filter by'
        }
      }
    }
  },
  {
    name: 'mise_install',
    description: 'Installs tools or packages matching requirements from .mise.toml, or a specific tool@version.',
    inputSchema: {
      type: 'object',
      properties: {
        tool: {
          type: 'string',
          description: 'Tool and version to install (e.g. "python@3.11", "node@20", or empty to install all from config)'
        },
        force: {
          type: 'boolean',
          description: 'Force reinstallation even if already present',
          default: false
        }
      }
    }
  },
  {
    name: 'mise_doctor',
    description: 'Checks mise toolchain health, paths, shims, permissions, and environment variables.',
    inputSchema: {
      type: 'object',
      properties: {}
    }
  },
  {
    name: 'mise_plugins',
    description: 'Lists registered mise plugins, available backends (asdf, aqua, vfox, ubi), and repository URLs.',
    inputSchema: {
      type: 'object',
      properties: {
        urls: {
          type: 'boolean',
          description: 'Show git/registry URLs for plugins',
          default: true
        }
      }
    }
  },
  {
    name: 'mise_run',
    description: 'Executes a task configured in mise.toml or task files.',
    inputSchema: {
      type: 'object',
      properties: {
        task: {
          type: 'string',
          description: 'The task name to execute'
        },
        args: {
          type: 'array',
          items: { type: 'string' },
          description: 'Arguments to pass to the task'
        }
      },
      required: ['task']
    }
  }
];

async function handleToolCall(name, args = {}) {
  switch (name) {
    case 'mise_list': {
      const cliArgs = ['ls'];
      if (args.json !== false) cliArgs.push('--json');
      const res = await runMiseCommand(cliArgs);
      return res.stdout || res.stderr || 'No tools installed yet.';
    }

    case 'mise_current': {
      const cliArgs = ['current'];
      if (args.plugin) cliArgs.push(args.plugin);
      const res = await runMiseCommand(cliArgs);
      return res.stdout || res.stderr || 'No active tools configured.';
    }

    case 'mise_install': {
      const cliArgs = ['install'];
      if (args.force) cliArgs.push('-f');
      if (args.tool) cliArgs.push(args.tool);
      const res = await runMiseCommand(cliArgs);
      const output = [res.stdout, res.stderr].filter(Boolean).join('\n');
      return output || `mise install completed with code ${res.code} in ${res.durationMs}ms`;
    }

    case 'mise_doctor': {
      const res = await runMiseCommand(['doctor']);
      return res.stdout || res.stderr || 'mise doctor check passed';
    }

    case 'mise_plugins': {
      const cliArgs = ['plugins', 'ls'];
      if (args.urls !== false) cliArgs.push('--urls');
      const res = await runMiseCommand(cliArgs);
      return res.stdout || res.stderr || 'No third-party plugins installed.';
    }

    case 'mise_run': {
      const cliArgs = ['run', args.task];
      if (Array.isArray(args.args) && args.args.length > 0) {
        cliArgs.push('--', ...args.args);
      }
      const res = await runMiseCommand(cliArgs);
      return [res.stdout, res.stderr].filter(Boolean).join('\n') || `Task executed with code ${res.code}`;
    }

    default:
      throw new Error(`Unknown tool: ${name}`);
  }
}

// JSON-RPC stdio transport loop
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  terminal: false
});

function sendResponse(id, result) {
  process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id, result }) + '\n');
}

function sendError(id, code, message, data = null) {
  process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id, error: { code, message, data } }) + '\n');
}

rl.on('line', async (line) => {
  const trimmed = line.trim();
  if (!trimmed) return;

  let msg;
  try {
    msg = JSON.parse(trimmed);
  } catch (err) {
    sendError(null, -32700, 'Parse error: invalid JSON');
    return;
  }

  const { id, method, params } = msg;

  if (method === 'initialize') {
    sendResponse(id, {
      protocolVersion: '2024-11-05',
      serverInfo: {
        name: 'mise-mcp-server',
        version: '1.0.0'
      },
      capabilities: {
        tools: {}
      }
    });
    return;
  }

  if (method === 'notifications/initialized') {
    // Notification from client, no response required
    return;
  }

  if (method === 'ping') {
    sendResponse(id, {});
    return;
  }

  if (method === 'tools/list') {
    sendResponse(id, { tools: TOOLS });
    return;
  }

  if (method === 'tools/call') {
    try {
      const { name, arguments: toolArgs } = params || {};
      const outputText = await handleToolCall(name, toolArgs || {});
      sendResponse(id, {
        content: [
          {
            type: 'text',
            text: typeof outputText === 'string' ? outputText : JSON.stringify(outputText, null, 2)
          }
        ],
        isError: false
      });
    } catch (err) {
      sendResponse(id, {
        content: [
          {
            type: 'text',
            text: `Error executing ${params?.name}: ${err.message}`
          }
        ],
        isError: true
      });
    }
    return;
  }

  if (id !== undefined && id !== null) {
    sendError(id, -32601, `Method not found: ${method}`);
  }
});

process.stderr.write('mise MCP server running on stdio (PID: ' + process.pid + ')\n');
