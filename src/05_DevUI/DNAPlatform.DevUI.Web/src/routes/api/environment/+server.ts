import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async () => {
  return json({
    name: 'DNA Platform',
    projectRoot: '/app/applet',
    miseInstalled: false,
    miseVersion: null,
    experimentalEnabled: false,
    overallStatus: 'green',
    runtimes: [
      {
        id: 'node',
        name: 'Node.js',
        requestedVersion: '22',
        resolvedVersion: process.version,
        installed: true,
        active: true,
        source: 'platform-default',
        status: 'green',
        message: `Node.js runtime active (${process.version})`
      }
    ],
    tasks: [
      { name: 'dev', description: 'Start Vite development server' },
      { name: 'build', description: 'Build production bundle' }
    ],
    checkedAt: new Date().toISOString()
  });
};
