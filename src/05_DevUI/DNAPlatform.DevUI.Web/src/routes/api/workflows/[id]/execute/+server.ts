import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

const BACKEND_URL = process.env.BACKEND_URL || 'http://127.0.0.1:5254';

// POST /api/workflows/{id}/execute — execute a stored workflow
export const POST: RequestHandler = async ({ params, request }) => {
  try {
    // 1. Fetch the workflow from Prisma
    const workflow = await prisma.workflow.findUnique({
      where: { id: params.id },
    });

    if (!workflow) {
      return json({ error: `Workflow '${params.id}' not found` }, { status: 404 });
    }

    // Parse nodes/connections from JSON
    const nodes = workflow.nodes ? JSON.parse(workflow.nodes) : [];
    const connections = workflow.connections ? JSON.parse(workflow.connections) : [];

    // Validate
    if (!nodes || !nodes.length) {
      return json({ error: 'Workflow must contain at least one node' }, { status: 400 });
    }

    // 2. Call the .NET backend to execute
    const workflowPayload = {
      id: workflow.id,
      name: workflow.name,
      nodes: nodes,
      connections: connections,
    };

    const res = await fetch(`${BACKEND_URL}/api/workflows/execute`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(workflowPayload),
    });

    if (!res.ok) {
      const error = await res.json().catch(() => ({ error: res.statusText }));
      return json({ error: error.error || 'Workflow execution failed' }, { status: 502 });
    }

    const result = await res.json();

    // 3. Store execution result in Prisma
    const status = result.status || 'Completed';
    const isCompleted = status === 'Completed' || status === 'completed';

    // .NET serializes TimeSpan as string like "00:00:00.2246541" — parse to ms
    function parseDurationMs(d: unknown): number | null {
      if (typeof d === 'number') return d;
      if (typeof d === 'string') {
        const m = /^(?:(\d+)\.)?(\d+):(\d+):(\d+)(?:\.(\d+))?$/.exec(d);
        if (m) {
          const [, days, h, min, s, frac] = m;
          const ms = (Number(days || 0) * 86400 + Number(h) * 3600 + Number(min) * 60 + Number(s)) * 1000
            + (frac ? Math.round(Number('0.' + frac) * 1000) : 0);
          return ms;
        }
      }
      return null;
    }
    const durationMs = parseDurationMs(result.duration)
      ?? parseDurationMs(result.durationMs)
      ?? parseDurationMs(result.fullExecution?.duration);

    await prisma.execution.create({
      data: {
        workflowId: workflow.id,
        workflowName: workflow.name,
        status: status,
        startTime: new Date(result.fullExecution?.startedAt || Date.now()),
        endTime: isCompleted ? new Date(result.fullExecution?.completedAt || Date.now()) : null,
        duration: durationMs,
        outputs: result.outputs ? JSON.stringify(result.outputs) : null,
        errors: result.errors ? JSON.stringify(result.errors) : null,
      },
    });

    // 4. Return result
    return json(result);
  } catch (error) {
    console.error('Error executing workflow:', error);
    return json({ 
      error: error instanceof Error ? error.message : 'Failed to execute workflow' 
    }, { status: 500 });
  }
};
