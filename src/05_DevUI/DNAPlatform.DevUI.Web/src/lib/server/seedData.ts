import { DEMO_WORKFLOWS, type DemoWorkflow } from '$lib/data/demoWorkflows';

export { DEMO_WORKFLOWS, type DemoWorkflow };

export async function ensureWorkflowsSeeded(prisma: any) {
  try {
    const count = await prisma.workflow.count();
    if (count === 0) {
      console.log('[Seed] Seeding initial demonstration workflows...');
      for (const wf of DEMO_WORKFLOWS) {
        try {
          await prisma.workflow.create({
            data: {
              id: wf.id,
              name: wf.name,
              description: wf.description,
              nodes: JSON.stringify(wf.nodes),
              connections: JSON.stringify(wf.connections),
              status: wf.status,
              version: wf.version,
            }
          });
        } catch (err) {
          console.warn(`[Seed] Warning inserting workflow ${wf.id}:`, err);
        }
      }
      console.log('[Seed] Demonstration workflows seeded successfully.');
    }
  } catch (e) {
    console.warn('[Seed] Note during seed check:', e);
  }
}
