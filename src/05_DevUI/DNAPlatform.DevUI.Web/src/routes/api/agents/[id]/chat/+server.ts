import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db';
import { copilotEngine } from '$lib/server/ai/copilotOnnx';

const BUILTIN_FALLBACK_AGENTS: Record<string, { name: string; type: string; prompt: string; tools: string[]; model: string }> = {
  'bioinformatic-scientist': {
    name: 'Bioinformatic Scientist',
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

// POST /api/agents/[id]/chat — Multi-turn conversation stream with live tool calling
export const POST: RequestHandler = async ({ params, request }) => {
  try {
    const agentId = params.id;
    const body = await request.json();
    const { messages = [], userPrompt, model: overrideModel, availableTools: customTools } = body;

    const currentPrompt = String(userPrompt || (messages.length > 0 ? messages[messages.length - 1].content : '')).trim();
    if (!currentPrompt) {
      return json({ success: false, error: 'User prompt is required' }, { status: 400 });
    }

    // 1. Resolve Agent
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

    // 2. Synthesize conversation history into context
    const conversationContext = messages
      .slice(-6)
      .map((m: any) => `${m.role === 'user' ? 'User' : 'Copilot'}: ${m.content}`)
      .join('\n');

    // 3. Execute Copilot ONNX Reasoning Engine with Live Tool Calling
    const inferenceResult = await copilotEngine.generate({
      model: effectiveModel,
      systemPrompt: `${systemPrompt}\n\nRecent Conversation History:\n${conversationContext}`,
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
