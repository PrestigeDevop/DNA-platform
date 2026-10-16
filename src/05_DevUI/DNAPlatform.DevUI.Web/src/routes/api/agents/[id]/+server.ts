import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// GET /api/agents/{id} — get an agent
export const GET: RequestHandler = async ({ params }) => {
  try {
    const agent = await prisma.agent.findUnique({
      where: { id: params.id },
    });
    if (!agent) {
      return json({ error: `Agent '${params.id}' not found` }, { status: 404 });
    }
    return json({ agent });
  } catch (error) {
    return json({ error: 'Failed to fetch agent' }, { status: 500 });
  }
};

// PUT /api/agents/{id} — update an agent
export const PUT: RequestHandler = async ({ params, request }) => {
  try {
    const body = await request.json();
    const agent = await prisma.agent.update({
      where: { id: params.id },
      data: {
        name: body.name,
        type: body.type,
        prompt: body.prompt || null,
        model: body.model || 'Microsoft-Copilot-Local-ONNX',
      },
    });
    return json({ agent });
  } catch (error: any) {
    console.error('Error updating agent:', error);
    return json({ error: error.message || 'Failed to update agent' }, { status: 500 });
  }
};

// DELETE /api/agents/{id} — delete an agent
export const DELETE: RequestHandler = async ({ params }) => {
  try {
    await prisma.agent.delete({
      where: { id: params.id },
    });
    return json({ success: true, message: `Agent '${params.id}' deleted` });
  } catch (error) {
    console.error('Error deleting agent:', error);
    return json({ error: 'Failed to delete agent' }, { status: 500 });
  }
};
