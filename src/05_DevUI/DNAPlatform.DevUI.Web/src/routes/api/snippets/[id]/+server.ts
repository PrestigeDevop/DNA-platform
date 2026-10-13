import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// PUT /api/snippets/{id} -- update a snippet
export const PUT: RequestHandler = async ({ params, request }) => {
  try {
    const body = await request.json();
    const snippet = await prisma.customSnippet.update({
      where: { id: params.id },
      data: {
        name: body.name,
        runtime: body.runtime || 'PolyglotKernel',
        description: body.description || null,
        inputs: body.inputs ? JSON.stringify(body.inputs) : '[]',
        outputs: body.outputs ? JSON.stringify(body.outputs) : '[]',
        action: body.action ? JSON.stringify(body.action) : '{}',
      },
    });
    const result = {
      ...snippet,
      inputs: snippet.inputs ? JSON.parse(snippet.inputs) : [],
      outputs: snippet.outputs ? JSON.parse(snippet.outputs) : [],
      action: snippet.action ? JSON.parse(snippet.action) : null,
    };
    return json({ snippet: result });
  } catch (error) {
    console.error('Error updating snippet:', error);
    return json({ error: 'Failed to update snippet' }, { status: 500 });
  }
};

// DELETE /api/snippets/{id} -- delete a snippet
export const DELETE: RequestHandler = async ({ params }) => {
  try {
    await prisma.customSnippet.delete({
      where: { id: params.id },
    });
    return json({ success: true });
  } catch (error) {
    console.error('Error deleting snippet:', error);
    return json({ error: 'Failed to delete snippet' }, { status: 500 });
  }
};