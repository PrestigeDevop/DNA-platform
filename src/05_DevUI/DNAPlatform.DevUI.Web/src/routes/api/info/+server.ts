import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

const PlatformVersion = '0.3.0-alpha';

export const GET: RequestHandler = async () => {
  return json({
    name: 'DNA Platform - DevUI API',
    version: PlatformVersion,
    description: 'Low-Code/No-Code Bioinformatic Workflow Engine with Custom Skills',
    swagger: '/swagger',
    endpoints: {
      workflows: '/api/workflows',
      executions: '/api/executions',
      agents: '/api/agents',
      skills: '/api/skills',
      snippets: '/api/snippets',
      logs: '/api/logs',
      logsStream: '/api/logs/stream',
      nodeTypes: '/api/node-types',
      statusValues: '/api/status-values',
      health: '/health'
    }
  });
};
