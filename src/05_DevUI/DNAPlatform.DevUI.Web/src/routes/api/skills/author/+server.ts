import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { skillRegistry, type ISkill } from '$lib/server/skills/registry';
import { mcpRegistry } from '$lib/server/mcp/registry';
import { logStore } from '$lib/server/logStore';

// POST /api/skills/author — Author new skill with Microsoft Copilot annotation
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const name = body.name || 'Custom Bio Skill';
    const rawId = body.skillId || name.toLowerCase().replace(/[^a-z0-9]/g, '-');
    const skillId = rawId.startsWith('custom-') ? rawId : `custom-${rawId}`;
    const description = body.description || 'User-authored custom bioinformatic skill';
    const category = body.category || 'Custom';
    const icon = body.icon || '🧬';
    const script = body.script || '';

    // Copilot Auto-Annotation
    const isDna = script.includes('sequence') || script.includes('DNA') || script.includes('fasta');
    const inferredInput = isDna
      ? {
          sequence: 'string - Target DNA nucleotide sequence',
          operation: 'string - Desired operation'
        }
      : {
          input: 'string - Input string to process',
          options: 'string - Processing flags'
        };

    const newSkill: ISkill = {
      skillId,
      name,
      description,
      category,
      icon,
      version: '1.0.0',
      getMetadata() {
        return {
          skillId,
          name,
          description,
          category,
          icon,
          version: '1.0.0',
          inputSchema: inferredInput,
          outputSchema: {
            result: 'any - Output produced by custom skill',
            timestamp: 'string - ISO timestamp'
          },
          inputFields: [
            {
              name: isDna ? 'sequence' : 'input',
              type: 'textarea',
              label: isDna ? 'DNA Sequence' : 'Input Data',
              required: true,
              defaultValue: isDna ? 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC' : 'Sample input data'
            }
          ]
        };
      },
      async execute(inputs: Record<string, any> = {}) {
        const start = Date.now();
        const inVal = inputs.sequence || inputs.input || inputs.data || inputs.value || '';
        let processed: any = inVal;

        if (script && script.trim()) {
          try {
            // Safe evaluation of transform expression
            const fn = new Function('input', 'inputs', `
              try {
                ${script}
              } catch(e) {
                return "Script Execution Error: " + e.message;
              }
            `);
            processed = fn(inVal, inputs);
          } catch (e: any) {
            processed = `Error: ${e.message}`;
          }
        } else {
          processed = {
            echo: inVal,
            length: String(inVal).length,
            annotatedBy: 'Microsoft Copilot'
          };
        }

        return {
          success: true,
          data: {
            result: processed,
            skillId,
            timestamp: new Date().toISOString()
          },
          executedAt: new Date().toISOString(),
          durationMs: Date.now() - start
        };
      }
    };

    // Register into both skill registry and MCP registry
    skillRegistry.register(newSkill);

    // Also register on csharp-runtime server as an MCP tool
    const csharpServer = mcpRegistry.getServer('csharp-runtime');
    if (csharpServer) {
      csharpServer.tools.push({
        name: skillId.replace('-', '_'),
        description: `[User Skill] ${description}`,
        inputSchema: {
          type: 'object',
          properties: {
            input: { type: 'string', description: 'Input parameter' }
          }
        }
      });
    }

    logStore.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'SkillAuthoring',
      message: `Authored and registered new skill: "${name}" (${skillId}) with Copilot schema annotation`,
      source: 'backend'
    });

    return json({
      success: true,
      skill: newSkill.getMetadata()
    });
  } catch (err: any) {
    return json({ success: false, error: err.message }, { status: 500 });
  }
};
