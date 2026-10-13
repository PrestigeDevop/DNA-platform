import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

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
