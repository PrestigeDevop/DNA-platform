import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// GET /api/executions/[id] — get a single execution by ID
export const GET: RequestHandler = async ({ params }) => {
  try {
    const e = await prisma.execution.findUnique({
      where: { id: params.id }
    });

    if (!e) {
      return json({ error: `Execution '${params.id}' not found` }, { status: 404 });
    }

    return json({
      id: e.id,
      workflowId: e.workflowId,
      workflowName: e.workflowName,
      status: e.status,
      startTime: e.startTime.toISOString(),
      endTime: e.endTime ? e.endTime.toISOString() : null,
      duration: e.duration || 0,
      outputs: e.outputs ? JSON.parse(e.outputs) : {},
      errors: e.errors ? JSON.parse(e.errors) : null,
      nodeResults: e.nodeResults ? JSON.parse(e.nodeResults) : [],
      version: e.version
    });
  } catch (error) {
    console.error('Error fetching execution:', error);
    return json({ error: 'Failed to fetch execution' }, { status: 500 });
  }
};
