import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { executeWorkflow } from '$lib/server/workflow/orchestrator';

// POST /api/workflows/execute-with-context
export const POST: RequestHandler = async ({ request }) => {
  const body = await request.json().catch(() => null);
  const workflow = body?.workflow;
  const context = body?.context || {};

  if (!workflow || !workflow.nodes || !workflow.nodes.length) {
    return json({ error: 'Workflow must contain at least one node' }, { status: 400 });
  }

  const result = await executeWorkflow(workflow, context);
  return json(result);
};
