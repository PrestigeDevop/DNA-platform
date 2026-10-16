import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';
import { logStore } from '$lib/server/logStore';

// POST /api/snippets/{id}/execute — execute a snippet
export const POST: RequestHandler = async ({ params, request }) => {
  try {
    const snippet = await prisma.customSnippet.findUnique({
      where: { id: params.id }
    });

    if (!snippet) {
      return json({ error: `Snippet '${params.id}' not found` }, { status: 404 });
    }

    const body = await request.json().catch(() => ({}));
    let inputText = 'World';

    for (const [key, val] of Object.entries(body)) {
      if (key !== 'action' && typeof val === 'string' && val.trim().length > 0) {
        inputText = val.trim();
        break;
      }
    }

    logStore.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'Snippet',
      message: `Snippet '${params.id}' executing with input: ${inputText}`,
      source: 'backend'
    });

    return json({
      success: true,
      snippetId: params.id,
      data: {
        result: `Hello ${inputText} from DNA Platform Polyglot Kernel`,
        timestamp: new Date().toISOString()
      },
      executedAt: new Date().toISOString()
    });
  } catch (error) {
    console.error('Error executing snippet:', error);
    return json(
      {
        error: error instanceof Error ? error.message : 'Failed to execute snippet'
      },
      { status: 500 }
    );
  }
};
