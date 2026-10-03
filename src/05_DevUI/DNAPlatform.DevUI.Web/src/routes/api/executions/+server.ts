import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// GET /api/executions — list all executions
export const GET: RequestHandler = async () => {
  try {
    const executions = await prisma.execution.findMany({
      orderBy: { createdAt: 'desc' },
    });
    // Parse JSON fields
    const result = executions.map(e => ({
      id: e.id,
      workflowId: e.workflowId,
      workflowName: e.workflowName,
      status: e.status,
      startTime: e.startTime.toISOString(),
      endTime: e.endTime ? e.endTime.toISOString() : null,
      duration: e.duration || 0,
      outputs: e.outputs ? JSON.parse(e.outputs) : {},
      errors: e.errors ? JSON.parse(e.errors) : null,
    }));
    return json({ executions: result });
  } catch (error) {
    console.error('Error fetching executions:', error);
    return json({ error: 'Failed to fetch executions' }, { status: 500 });
  }
};
