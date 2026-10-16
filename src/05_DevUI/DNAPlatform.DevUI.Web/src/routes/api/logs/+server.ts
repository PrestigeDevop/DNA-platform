import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { logStore } from '$lib/server/logStore';

// GET /api/logs — returns in-memory logs
export const GET: RequestHandler = async ({ url }) => {
  const sinceId = parseInt(url.searchParams.get('sinceId') ?? '0', 10);
  const limit = parseInt(url.searchParams.get('limit') ?? '500', 10);

  const entries = logStore.get(isNaN(sinceId) ? 0 : sinceId, isNaN(limit) ? 500 : limit);

  return json({
    count: entries.length,
    totalBuffered: logStore.count,
    lastId: entries.length > 0 ? entries[entries.length - 1].id : sinceId,
    logs: entries
  });
};

// POST /api/logs — frontend log forwarding
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    if (!body || !body.message) {
      return json({ success: false, error: "A non-empty 'message' is required." }, { status: 400 });
    }

    const entry = logStore.add({
      timestamp: body.timestamp || new Date().toISOString(),
      level: body.level || 'Information',
      category: body.category || 'Frontend',
      message: body.message,
      exception: body.exception || null,
      source: 'frontend'
    });

    return json({ success: true, id: entry.id });
  } catch (error) {
    return json({ success: false, error: 'Failed to record log entry' }, { status: 500 });
  }
};

// DELETE /api/logs — clear logs
export const DELETE: RequestHandler = async () => {
  const removed = logStore.clear();
  return json({ success: true, removed });
};
