import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import fs from 'fs';
import path from 'path';

export const POST: RequestHandler = async () => {
  try {
    const misePath = path.resolve(process.cwd(), 'mise.toml');
    if (fs.existsSync(misePath)) {
      const content = fs.readFileSync(misePath, 'utf-8');
      return json({ content });
    }
    return json({ message: 'No mise.toml found for this project.' }, { status: 404 });
  } catch (error) {
    return json({ message: 'Failed to read mise.toml' }, { status: 500 });
  }
};
