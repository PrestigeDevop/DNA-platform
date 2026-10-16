import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import fs from 'fs';
import path from 'path';

export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json().catch(() => null);
    if (!body || !body.content || typeof body.content !== 'string' || !body.content.trim()) {
      return json({ success: false, error: "A non-empty 'content' field is required." }, { status: 400 });
    }

    const misePath = path.resolve(process.cwd(), 'mise.toml');
    fs.writeFileSync(misePath, body.content, 'utf-8');

    return json({
      success: true,
      path: misePath
    });
  } catch (error) {
    return json({ success: false, error: 'Failed to write mise.toml' }, { status: 500 });
  }
};
