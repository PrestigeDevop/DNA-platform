import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { parseComponentCode } from '$lib/server/authoring/parser';

// POST /api/authoring/reflect — Live reflection & validation of component code
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const code = String(body.code || '');
    const language = String(body.language || 'csharp');
    const result = parseComponentCode(code, language);
    return json({
      success: true,
      result
    });
  } catch (err: any) {
    return json({
      success: false,
      error: err.message || 'Reflection failed'
    }, { status: 400 });
  }
};
