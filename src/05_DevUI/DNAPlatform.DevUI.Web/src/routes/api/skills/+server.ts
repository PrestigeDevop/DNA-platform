import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { skillRegistry } from '$lib/server/skills/registry';

// GET /api/skills — list all skills
export const GET: RequestHandler = async () => {
  const skills = skillRegistry.listSkills();
  return json({
    count: skills.length,
    skills
  });
};
