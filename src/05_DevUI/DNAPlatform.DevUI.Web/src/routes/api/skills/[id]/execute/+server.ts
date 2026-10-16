import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { skillRegistry } from '$lib/server/skills/registry';

// POST /api/skills/[id]/execute — execute skill
export const POST: RequestHandler = async ({ params, request }) => {
  const inputs = await request.json().catch(() => ({}));
  const result = await skillRegistry.executeSkill(params.id, inputs);

  if (!result.success) {
    return json({ success: false, error: result.errorMessage }, { status: 400 });
  }

  return json({
    success: true,
    skillId: params.id,
    data: result.data,
    executedAt: result.executedAt,
    durationMs: result.durationMs
  });
};
