import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';
import { executeWorkflow } from '$lib/server/workflow/orchestrator';

function safeParseJson(val: any): any[] {
  if (!val) return [];
  if (Array.isArray(val)) return val;
  if (typeof val === 'string') {
    try {
      let parsed = JSON.parse(val);
      if (typeof parsed === 'string') {
        try {
          parsed = JSON.parse(parsed);
        } catch {
          // ignore
        }
      }
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
  return [];
}

// POST /api/workflows/{id}/execute — execute a stored workflow
export const POST: RequestHandler = async ({ params }) => {
  try {
    const workflow = await prisma.workflow.findUnique({
      where: { id: params.id },
    });

    if (!workflow) {
      return json({ error: `Workflow '${params.id}' not found` }, { status: 404 });
    }

    const nodes = safeParseJson(workflow.nodes);
    const connections = safeParseJson(workflow.connections);

    if (!nodes || !nodes.length) {
      return json({ error: 'Workflow must contain at least one node' }, { status: 400 });
    }

    const workflowPayload = {
      id: workflow.id,
      name: workflow.name,
      nodes,
      connections,
    };

    const result = await executeWorkflow(workflowPayload);
    const status = result.status || 'Completed';
    const isCompleted = status.toLowerCase() === 'completed';
    const reported = result.fullExecution?.nodeExecutions ?? {};

    const nodeResults = nodes.map((node: { id: string }) => {
      const execution = reported[node.id];
      if (!execution) {
        return {
          nodeId: node.id,
          status: 'Skipped',
          startedAt: null,
          completedAt: null,
          inputs: null,
          outputs: null,
          error: null
        };
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

    try {
      await prisma.execution.create({
        data: {
          workflowId: workflow.id,
          workflowName: workflow.name,
          status,
          startTime: new Date(result.fullExecution?.startedAt || Date.now()),
          endTime: isCompleted ? new Date(result.fullExecution?.completedAt || Date.now()) : null,
          duration: result.duration,
          outputs: result.outputs ? JSON.stringify(result.outputs) : null,
          errors: result.errors?.length ? JSON.stringify(result.errors) : null,
          nodeResults: JSON.stringify(nodeResults),
          version: workflow.version,
        },
      });
    } catch (dbErr) {
      console.warn('Could not record execution in DB:', dbErr);
    }

    return json({
      ...result,
      nodeResults,
      workflowId: workflow.id,
      version: workflow.version
    });
  } catch (error) {
    console.error('Error executing workflow:', error);
    return json(
      {
        error: error instanceof Error ? error.message : 'Failed to execute workflow'
      },
      { status: 500 }
    );
  }
};
