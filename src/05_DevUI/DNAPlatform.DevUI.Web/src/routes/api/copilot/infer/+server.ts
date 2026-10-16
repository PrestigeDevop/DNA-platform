import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { copilotEngine } from '$lib/server/ai/copilotOnnx';

// POST /api/copilot/infer - run local Copilot ONNX inference
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const result = await copilotEngine.generate(body);
    return json({ success: true, ...result });
  } catch (error: any) {
    return json({ success: false, error: error.message || 'Inference failed' }, { status: 500 });
  }
};
