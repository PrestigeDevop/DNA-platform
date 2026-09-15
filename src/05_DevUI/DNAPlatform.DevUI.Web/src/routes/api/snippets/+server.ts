import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';
import { BACKEND_URL } from '$env/static/private';

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

// POST /api/snippets/{id}/execute — proxy snippet execution to the .NET backend
export const POST_execute: RequestHandler = async ({ params, request }) => {
  try {
    const backend = BACKEND_URL || 'http://127.0.0.1:5254';
    const inputs = await request.json().catch(() => ({}));
    const res = await fetch(`${backend}/api/snippets/${params.id}/execute`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(inputs),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ error: res.statusText }));
      return json(err, { status: res.status });
    }
    return json(await res.json());
  } catch (error) {
    console.error('Error executing snippet:', error);
    return json({ error: 'Failed to execute snippet' }, { status: 500 });
  }
};