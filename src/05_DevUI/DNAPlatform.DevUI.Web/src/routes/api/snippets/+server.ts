import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

// GET /api/snippets — list all snippets
export const GET: RequestHandler = async () => {
  try {
    const snippets = await prisma.customSnippet.findMany({
      orderBy: { updatedAt: 'desc' },
    });
    const result = snippets.map((s) => ({
      ...s,
      inputs: s.inputs ? JSON.parse(s.inputs) : [],
      outputs: s.outputs ? JSON.parse(s.outputs) : [],
      action: s.action ? JSON.parse(s.action) : null,
    }));
    return json({ snippets: result });
  } catch (error) {
    console.error('Error fetching snippets:', error);
    return json({ error: 'Failed to fetch snippets' }, { status: 500 });
  }
};

// POST /api/snippets — create a new snippet
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const snippet = await prisma.customSnippet.create({
      data: {
        id: body.id || undefined,
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
    return json({ snippet: result }, { status: 201 });
  } catch (error) {
    console.error('Error creating snippet:', error);
    return json({ error: 'Failed to create snippet' }, { status: 500 });
  }
};

// PUT /api/snippets/{id} — update a snippet
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

// DELETE /api/snippets/{id} — delete a snippet
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

