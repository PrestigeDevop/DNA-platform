import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { skillRegistry } from '$lib/server/skills/registry';

// GET /api/skills/[id] — get skill metadata
export const GET: RequestHandler = async ({ params }) => {
  const skill = skillRegistry.getSkill(params.id);
  if (!skill) {
    return json({ message: `Skill '${params.id}' not found` }, { status: 404 });
  }

  return json(skill.getMetadata());
};
