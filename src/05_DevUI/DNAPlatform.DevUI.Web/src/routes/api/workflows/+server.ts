import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// GET /api/workflows — list all workflows
export const GET: RequestHandler = async () => {
  try {
    const workflows = await prisma.workflow.findMany({
      orderBy: { updatedAt: 'desc' },
    });
    // Parse serialized JSON string fields for frontend convenience
    const result = workflows.map((w) => ({
      ...w,
      nodes: w.nodes ? JSON.parse(w.nodes) : [],
      connections: w.connections ? JSON.parse(w.connections) : [],
    }));
    return json({ workflows: result });
  } catch (error) {
    console.error('Error fetching workflows:', error);
    return json({ error: 'Failed to fetch workflows' }, { status: 500 });
  }
};

// POST /api/workflows — create a new workflow
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const workflow = await prisma.workflow.create({
      data: {
        id: body.id || undefined,
        name: body.name,
        description: body.description || null,
        nodes: body.nodes ? JSON.stringify(body.nodes) : null,
        connections: body.connections ? JSON.stringify(body.connections) : null,
      },
    });
    // Return parsed nodes/connections for frontend convenience
    const result = {
      ...workflow,
      nodes: workflow.nodes ? JSON.parse(workflow.nodes) : [],
      connections: workflow.connections ? JSON.parse(workflow.connections) : [],
    };
    return json({ workflow: result });
  } catch (error) {
    console.error('Error creating workflow:', error);
    return json({ error: 'Failed to create workflow' }, { status: 500 });
  }
};
