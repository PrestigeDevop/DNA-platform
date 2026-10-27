import os

# 1. Restore src/routes/api/agents/[id]/chat/+server.ts
chat_route_code = '''import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';
import { copilotEngine } from '$lib/server/ai/copilotOnnx';

const BUILTIN_FALLBACK_AGENTS: Record<string, { name: string; type: string; prompt: string; tools: string[]; model: string }> = {
  'bioinformatic-scientist': {
    name: 'Bioinformatics Scientist',
    type: 'ScientificReasoner',
    prompt: 'You are an elite Bioinformatic Scientist Copilot running local ONNX neural inference. You analyze DNA/RNA sequences, compute GC metrics, align sequences, and evaluate genetic variants with rigorous scientific precision.',
    tools: [
      'dna-bioinformatics/reverse_complement',
      'dna-bioinformatics/gc_skew_analysis',
      'dna-bioinformatics/dna_to_protein',
      'sequence-aligner',
      'csharp-runtime/get_current_time'
    ],
    model: 'Microsoft-Copilot-Bio-ONNX'
  },
  'data-analysis-specialist': {
    name: 'Data Analysis Specialist',
    type: 'DataAnalyst',
    prompt: 'You are a Data Analysis Copilot specialized in FASTA/FASTQ artifacts, pipeline metrics synthesis, and C# runtime clock synchronization.',
    tools: [
      'csharp-runtime/get_current_time',
      'fasta-loader',
      'shell-tools/execute'
    ],
    model: 'Phi-3.5-Data-ONNX'
  }
};

export const POST: RequestHandler = async ({ params, request }) => {
  try {
    const agentId = params.id;
    const body = await request.json();
    const { messages = [], userPrompt, model: overrideModel, availableTools: customTools } = body;

    const currentPrompt = String(userPrompt || (messages.length > 0 ? messages[messages.length - 1].content : '')).trim();
    if (!currentPrompt) {
      return json({ success: false, error: 'User prompt is required' }, { status: 400 });
    }

    let agentName = 'Copilot Agent';
    let systemPrompt = 'You are Microsoft Copilot, an expert bioinformatic reasoning and workflow agent.';
    let agentModel = 'Microsoft-Copilot-Local-ONNX';
    let agentTools: string[] = ['csharp-runtime/get_current_time', 'dna-bioinformatics/gc_skew_analysis'];

    try {
      const dbAgent = await prisma.agent.findUnique({
        where: { id: agentId },
        include: { skills: true }
      });
      if (dbAgent) {
        agentName = dbAgent.name;
        if (dbAgent.prompt) systemPrompt = dbAgent.prompt;
        if (dbAgent.model) agentModel = dbAgent.model;
        if (dbAgent.skills && dbAgent.skills.length > 0) {
          agentTools = dbAgent.skills.map(s => s.skillId);
        }
      } else if (BUILTIN_FALLBACK_AGENTS[agentId]) {
        const builtin = BUILTIN_FALLBACK_AGENTS[agentId];
        agentName = builtin.name;
        systemPrompt = builtin.prompt;
        agentModel = builtin.model;
        agentTools = builtin.tools;
      }
    } catch (e) {
      if (BUILTIN_FALLBACK_AGENTS[agentId]) {
        const builtin = BUILTIN_FALLBACK_AGENTS[agentId];
        agentName = builtin.name;
        systemPrompt = builtin.prompt;
        agentModel = builtin.model;
        agentTools = builtin.tools;
      }
    }

    const effectiveModel = overrideModel || agentModel;
    const toolsToUse = Array.isArray(customTools) && customTools.length > 0 ? customTools : agentTools;

    const conversationContext = messages
      .slice(-6)
      .map((m: any) => (m.role === 'user' ? 'User: ' : 'Copilot: ') + m.content)
      .join('\\n');

    const inferenceResult = await copilotEngine.generate({
      model: effectiveModel,
      systemPrompt: systemPrompt + '\\n\\nRecent Conversation History:\\n' + conversationContext,
      userPrompt: currentPrompt,
      availableTools: toolsToUse,
      context: {
        agentId,
        agentName,
        historyLength: messages.length
      }
    });

    const assistantMessage = {
      id: 'msg-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 6),
      role: 'assistant',
      content: inferenceResult.response,
      toolCalls: inferenceResult.toolCalls,
      reasoning: inferenceResult.reasoning,
      model: inferenceResult.model,
      timestamp: new Date().toISOString()
    };

    return json({
      success: true,
      agentId,
      agentName,
      model: inferenceResult.model,
      message: assistantMessage,
      toolCalls: inferenceResult.toolCalls,
      onnxExecution: inferenceResult.onnxExecution,
      durationMs: inferenceResult.durationMs,
      tokensGenerated: inferenceResult.tokensGenerated,
      totalTokens: inferenceResult.totalTokens
    });
  } catch (err: any) {
    console.error('Error in agent chat endpoint:', err);
    return json({
      success: false,
      error: err.message || 'Agent chat processing failed'
    }, { status: 500 });
  }
};
'''

chat_dest = 'src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/agents/[id]/chat/+server.ts'
os.makedirs(os.path.dirname(chat_dest), exist_ok=True)
with open(chat_dest, 'w', encoding='utf-8') as f:
    f.write(chat_route_code)
print('Restored agent chat endpoint!')

# 2. Restore src/routes/api/authoring/reflect/+server.ts
reflect_code = '''import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { parseComponentCode } from '$lib/server/authoring/parser';

// POST /api/authoring/reflect — Live reflection & validation of component code
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const code = String(body.code || '');
    const language = String(body.language || 'csharp');
    const result = parseComponentCode(code, language);
    return json({
      success: true,
      result
    });
  } catch (err: any) {
    return json({
      success: false,
      error: err.message || 'Reflection failed'
    }, { status: 400 });
  }
};
'''

reflect_dest = 'src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/authoring/reflect/+server.ts'
os.makedirs(os.path.dirname(reflect_dest), exist_ok=True)
with open(reflect_dest, 'w', encoding='utf-8') as f:
    f.write(reflect_code)
print('Restored authoring reflect endpoint!')

# 3. Restore src/routes/api/authoring/publish/+server.ts
publish_code = '''import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { marketPlaceService } from '$lib/server/mcp/marketplace';
import { logStore } from '$lib/server/logStore';
import fs from 'fs';
import path from 'path';

export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const name = String(body.name || 'Custom Extension').trim();
    const category = body.category || 'Extensions';
    const language = body.language || 'csharp';
    const description = body.description || 'Custom authored logic component & tool suite';
    const code = body.code || '';
    const tools = Array.isArray(body.tools) ? body.tools : [];
    const properties = Array.isArray(body.properties) ? body.properties : [];
    const mise = body.mise || {};
    const isMiddleware = Boolean(body.isMiddleware);

    const pluginId = 'ext-' + name.toLowerCase().replace(/[^a-z0-9]/g, '-') + '-' + Date.now().toString(36).slice(-4);
    const plugin = {
      id: pluginId,
      name,
      author: 'Local Developer',
      version: '1.0.0',
      description,
      category: category as any,
      icon: isMiddleware ? '🛡️' : language === 'python' ? '🐍' : language === 'csharp' ? '⚡' : '🟨',
      installed: true,
      featured: false,
      tags: [language, isMiddleware ? 'middleware' : 'tool', ...tools.map((t: any) => t.name)],
      toolsCount: tools.length
    };

    marketPlaceService.publishPlugin(plugin, tools);

    try {
      const workspaceExtDir = path.resolve(process.cwd(), 'workspace', 'extensions');
      if (!fs.existsSync(workspaceExtDir)) {
        fs.mkdirSync(workspaceExtDir, { recursive: true });
      }
      const extFile = path.resolve(workspaceExtDir, `${pluginId}.json`);
      fs.writeFileSync(extFile, JSON.stringify({
        ...plugin,
        code,
        tools,
        properties,
        mise,
        publishedAt: new Date().toISOString()
      }, null, 2), 'utf-8');

      if (mise.miseToml) {
        const miseFile = path.resolve(workspaceExtDir, `${pluginId}.mise.toml`);
        fs.writeFileSync(miseFile, mise.miseToml, 'utf-8');
      }
    } catch (fsErr) {
      console.warn('Could not persist extension to workspace disk:', fsErr);
    }

    logStore.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'Marketplace',
      message: `[Local Marketplace] Published component/middleware "${name}" (${pluginId}) with ${tools.length} tool(s)`,
      source: 'backend'
    });

    return json({
      success: true,
      pluginId,
      name,
      toolsCount: tools.length,
      isMiddleware,
      miseToml: mise.miseToml || '',
      status: 'Published to local server marketplace & registered with runtime'
    });
  } catch (err: any) {
    return json({
      success: false,
      error: err.message || 'Failed to publish component'
    }, { status: 500 });
  }
};
'''

publish_dest = 'src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/api/authoring/publish/+server.ts'
os.makedirs(os.path.dirname(publish_dest), exist_ok=True)
with open(publish_dest, 'w', encoding='utf-8') as f:
    f.write(publish_code)
print('Restored authoring publish endpoint!')
