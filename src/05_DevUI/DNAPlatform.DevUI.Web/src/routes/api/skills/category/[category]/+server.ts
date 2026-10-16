import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { skillRegistry } from '$lib/server/skills/registry';

// GET /api/skills/category/[category] — list skills by category
export const GET: RequestHandler = async ({ params }) => {
  const skills = skillRegistry.listSkillsByCategory(params.category);
  return json({
    count: skills.length,
    category: params.category,
    skills
  });
};
