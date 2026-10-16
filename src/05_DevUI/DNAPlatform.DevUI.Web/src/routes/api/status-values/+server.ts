import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async () => {
  return json({
    workflowStatus: ['Pending', 'Running', 'Paused', 'Completed', 'Failed', 'Cancelled'],
    nodeStatus: ['Pending', 'Waiting', 'Running', 'Completed', 'Failed', 'Skipped']
  });
};
