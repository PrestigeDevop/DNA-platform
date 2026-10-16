import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async () => {
  return json({
    nodeTypes: [
      'Input',
      'Value',
      'Output',
      'Agent',
      'ProcessingSkill',
      'Log',
      'Conditional',
      'Loop',
      'SubWorkflow'
    ]
  });
};
