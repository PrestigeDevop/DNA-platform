import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { validateWorkflow } from '$lib/server/workflow/orchestrator';

// POST /api/workflows/validate — structural validation
export const POST: RequestHandler = async ({ request }) => {
  const body = await request.json().catch(() => ({}));
  const result = validateWorkflow(body);
  return json(result);
};
