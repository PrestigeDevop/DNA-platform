import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// GET /api/workflows/{id} — get a single workflow
export const GET: RequestHandler = async ({ params }) => {
  try {
    const workflow = await prisma.workflow.findUnique({
      where: { id: params.id },
    });
    if (!workflow) {
      return json({ error: `Workflow '${params.id}' not found` }, { status: 404 });
    }
    const result = {
      ...workflow,
      nodes: workflow.nodes ? JSON.parse(workflow.nodes) : [],
      connections: workflow.connections ? JSON.parse(workflow.connections) : [],
    };
    return json({ workflow: result });
  } catch (error) {
    console.error('Error fetching workflow:', error);
    return json({ error: 'Failed to fetch workflow' }, { status: 500 });
  }
};

// PUT /api/workflows/{id} — update a workflow
export const PUT: RequestHandler = async ({ params, request }) => {
  try {
    const body = await request.json();
    const workflow = await prisma.workflow.update({
      where: { id: params.id },
      data: {
        name: body.name,
        description: body.description || null,
        nodes: body.nodes ? JSON.stringify(body.nodes) : undefined,
        connections: body.connections ? JSON.stringify(body.connections) : undefined,
      },
    });
    const result = {
      ...workflow,
      nodes: workflow.nodes ? JSON.parse(workflow.nodes) : [],
      connections: workflow.connections ? JSON.parse(workflow.connections) : [],
    };
    return json({ workflow: result });
  } catch (error) {
    console.error('Error updating workflow:', error);
    return json({ error: 'Failed to update workflow' }, { status: 500 });
  }
};

// DELETE /api/workflows/{id} — delete a workflow
export const DELETE: RequestHandler = async ({ params }) => {
  try {
    await prisma.workflow.delete({
      where: { id: params.id },
    });
    return json({ success: true, message: `Workflow '${params.id}' deleted` });
  } catch (error) {
    console.error('Error deleting workflow:', error);
    return json({ error: 'Failed to delete workflow' }, { status: 500 });
  }
};
