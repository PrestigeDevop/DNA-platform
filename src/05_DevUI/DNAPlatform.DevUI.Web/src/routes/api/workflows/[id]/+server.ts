import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';

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

// GET /api/workflows/{id} — get a single workflow
export const GET: RequestHandler = async ({ params }) => {
  try {
    const workflow = await prisma.workflow.findUnique({
      where: { id: params.id },
    });

    if (!workflow) {
      return json({ error: `Workflow '${params.id}' not found` }, { status: 404 });
    }

    const result = {
      ...workflow,
      nodes: safeParseJson(workflow.nodes),
      connections: safeParseJson(workflow.connections),
    };

    return json({ workflow: result });
  } catch (error) {
    console.error('Error fetching workflow:', error);
    return json({ error: 'Failed to fetch workflow' }, { status: 500 });
  }
};

// PUT /api/workflows/{id} — update a workflow
export const PUT: RequestHandler = async ({ params, request }) => {
  try {
    const body = await request.json();
    const existing = await prisma.workflow.findUnique({ where: { id: params.id } });

    if (!existing) {
      return json({ error: `Workflow '${params.id}' not found` }, { status: 404 });
    }

    const nextStatus = body.status === 'Published' || body.status === 'Draft'
      ? body.status
      : existing.status;

    const isPublishing = nextStatus === 'Published' && existing.status !== 'Published';
    const nextVersion = isPublishing ? existing.version + 1 : existing.version;

    // Normalize nodes & connections to clean JSON strings without double-encoding
    const normalizedNodes = safeParseJson(body.nodes);
    const normalizedConnections = safeParseJson(body.connections);

    const workflow = await prisma.workflow.update({
      where: { id: params.id },
      data: {
        name: body.name,
        description: body.description || null,
        nodes: JSON.stringify(normalizedNodes),
        connections: JSON.stringify(normalizedConnections),
        status: nextStatus,
        version: nextVersion,
      },
    });

    if (isPublishing) {
      await prisma.workflowVersion.create({
        data: {
          workflowId: workflow.id,
          version: nextVersion,
          status: 'Published',
          changeNote: body.changeNote ?? null,
          definition: JSON.stringify({
            name: workflow.name,
            description: workflow.description,
            nodes: normalizedNodes,
            connections: normalizedConnections,
          }),
        },
      });
    }

    const result = {
      ...workflow,
      nodes: normalizedNodes,
      connections: normalizedConnections,
    };

    return json({ workflow: result });
  } catch (error) {
    console.error('Error updating workflow:', error);
    return json({ error: 'Failed to update workflow' }, { status: 500 });
  }
};

// DELETE /api/workflows/{id} — delete a workflow
export const DELETE: RequestHandler = async ({ params }) => {
  try {
    await prisma.workflow.delete({
      where: { id: params.id },
    });

    return json({ success: true, message: `Workflow '${params.id}' deleted` });
  } catch (error) {
    console.error('Error deleting workflow:', error);
    return json({ error: 'Failed to delete workflow' }, { status: 500 });
  }
};
