import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

const PlatformVersion = '0.3.0-alpha';

export const GET: RequestHandler = async () => {
  return json({
    status: 'Healthy',
    timestamp: new Date().toISOString(),
    version: PlatformVersion
  });
};
