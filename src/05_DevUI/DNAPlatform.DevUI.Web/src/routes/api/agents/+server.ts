import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// GET /api/agents — list all agents
export const GET: RequestHandler = async () => {
  try {
    const agents = await prisma.agent.findMany({
      orderBy: { createdAt: 'desc' },
    });
    return json({ agents: agents || [] });
  } catch (error) {
    console.error('Error fetching agents:', error);
    return json({ agents: [] });
  }
};

// POST /api/agents — create a new agent
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const agent = await prisma.agent.create({
      data: {
        id: body.id || undefined,
        name: body.name || 'Unnamed Agent',
        type: body.type || 'StandardAgent',
        prompt: body.prompt || null,
        model: body.model || 'gpt-4',
      },
    });
    return json({ agent });
  } catch (error) {
    console.error('Error creating agent:', error);
    return json({ error: 'Failed to create agent' }, { status: 500 });
  }
};
