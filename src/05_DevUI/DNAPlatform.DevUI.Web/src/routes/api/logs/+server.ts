import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

const BACKEND_URL = process.env.BACKEND_URL || 'http://127.0.0.1:5254';

// GET /api/logs — proxy the backend in-memory log buffer.
export const GET: RequestHandler = async ({ url }) => {
  const sinceId = url.searchParams.get('sinceId') ?? '0';
  const limit = url.searchParams.get('limit') ?? '200';
  try {
    const res = await fetch(`${BACKEND_URL}/api/logs?sinceId=${sinceId}&limit=${limit}`);
    const data = await res.json();
    return json(data, { status: res.status });
  } catch (error) {
    console.error('Error fetching logs:', error);
    return json({ error: 'Failed to fetch logs' }, { status: 502 });
  }
};

// DELETE /api/logs — clear the backend log buffer.
export const DELETE: RequestHandler = async () => {
  try {
    const res = await fetch(`${BACKEND_URL}/api/logs`, { method: 'DELETE' });
    const data = await res.json();
    return json(data, { status: res.status });
  } catch (error) {
    console.error('Error clearing logs:', error);
    return json({ error: 'Failed to clear logs' }, { status: 502 });
  }
};
