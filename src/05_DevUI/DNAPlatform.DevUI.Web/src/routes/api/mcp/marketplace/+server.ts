import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { marketPlaceService } from '$lib/server/mcp/marketplace';

// GET /api/mcp/marketplace - list catalog items
export const GET: RequestHandler = async () => {
  const plugins = marketPlaceService.getPlugins();
  return json({
    count: plugins.length,
    plugins
  });
};

// POST /api/mcp/marketplace - toggle install
export const POST: RequestHandler = async ({ request }) => {
  const body = await request.json().catch(() => ({}));
  const { id, action } = body;
  if (!id) {
    return json({ error: 'Plugin id is required' }, { status: 400 });
  }

  if (action === 'uninstall') {
    marketPlaceService.uninstall(id);
  } else {
    marketPlaceService.install(id);
  }

  return json({ success: true, id, installed: action !== 'uninstall' });
};
