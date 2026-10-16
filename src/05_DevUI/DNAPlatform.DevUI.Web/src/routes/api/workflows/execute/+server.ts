import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { executeWorkflow } from '$lib/server/workflow/orchestrator';

// POST /api/workflows/execute — ad-hoc workflow execution
export const POST: RequestHandler = async ({ request }) => {
  const workflow = await request.json().catch(() => null);
  if (!workflow || !workflow.nodes || !workflow.nodes.length) {
    return json({ error: 'Workflow must contain at least one node' }, { status: 400 });
  }

  const result = await executeWorkflow(workflow);
  return json(result);
};
