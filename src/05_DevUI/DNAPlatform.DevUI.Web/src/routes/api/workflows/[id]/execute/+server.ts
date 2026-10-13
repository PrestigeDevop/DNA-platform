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

    // 3b. Persist per-node execution state.
    //
    // The engine returns fullExecution.nodeExecutions keyed by node id. That map is
    // the only source of truth for what each node actually did, so it is stored
    // rather than synthesised: the designer renders these chips and the log page
    // renders the journal from the same rows. Nodes the engine never reported (an
    // unreachable branch) are recorded as Skipped so the designer shows a real
    // state instead of no badge at all.
    const reported = result.fullExecution?.nodeExecutions ?? {};
    const nodeResults = nodes.map((node: { id: string }) => {
      const execution = reported[node.id];
      if (!execution) {
        return { nodeId: node.id, status: 'Skipped', startedAt: null, completedAt: null, inputs: null, outputs: null, error: null };
      }
      return {
        nodeId: node.id,
        status: execution.status ?? 'Pending',
        startedAt: execution.startedAt ?? null,
        completedAt: execution.completedAt ?? null,
        inputs: execution.inputs ?? null,
        outputs: execution.outputs ?? null,
        error: execution.error ?? null,
      };
    });

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
        nodeResults: JSON.stringify(nodeResults),
        version: workflow.version,
      },
    });

    // 4. Return result. nodeResults is added so the caller can paint chips without
    // a second round trip, and it is the same array that was persisted.
    return json({ ...result, nodeResults, workflowId: workflow.id, version: workflow.version });
  } catch (error) {
    console.error('Error executing workflow:', error);
    return json({ 
      error: error instanceof Error ? error.message : 'Failed to execute workflow' 
    }, { status: 500 });
  }
};
