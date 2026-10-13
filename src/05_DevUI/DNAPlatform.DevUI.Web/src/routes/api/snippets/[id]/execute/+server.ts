import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

const BACKEND_URL = process.env.BACKEND_URL || 'http://127.0.0.1:5254';

// POST /api/snippets/{id}/execute — execute a stored snippet via the .NET runtime
export const POST: RequestHandler = async ({ params, request }) => {
  try {
    const snippet = await prisma.customSnippet.findUnique({
      where: { id: params.id },
    });

    if (!snippet) {
      return json({ error: `Snippet '${params.id}' not found` }, { status: 404 });
    }

    const body = await request.json().catch(() => ({}));
    const action = snippet.action ? JSON.parse(snippet.action) : {};

    // The runtime handler lives in the .NET backend; forward the snippet action
    // alongside the caller-supplied inputs so the handler can dispatch on kind.
    const res = await fetch(`${BACKEND_URL}/api/snippets/${params.id}/execute`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...body, action }),
    });

    if (!res.ok) {
      const error = await res.json().catch(() => ({ error: res.statusText }));
      return json(error, { status: 502 });
    }

    return json(await res.json());
  } catch (error) {
    console.error('Error executing snippet:', error);
    return json({ error: 'Failed to execute snippet' }, { status: 500 });
  }
};
