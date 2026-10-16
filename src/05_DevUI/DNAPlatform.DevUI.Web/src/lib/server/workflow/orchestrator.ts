import { skillRegistry } from '$lib/server/skills/registry';
import { logStore } from '$lib/server/logStore';
import { copilotEngine } from '$lib/server/ai/copilotOnnx';

export interface WorkflowNode {
  id: string;
  name: string;
  type?: string;
  nodeType?: string;
  config?: Record<string, any>;
  x?: number;
  y?: number;
  [key: string]: any;
}

export interface NodeConnection {
  id?: string;
  sourceNodeId?: string;
  targetNodeId?: string;
  from?: string;
  to?: string;
  outputKey?: string;
  inputKey?: string;
  [key: string]: any;
}

export interface WorkflowDefinition {
  id?: string;
  name?: string;
  description?: string;
  nodes?: WorkflowNode[] | string;
  connections?: NodeConnection[] | string;
  [key: string]: any;
}

export interface ValidationCheck {
  name: string;
  passed: boolean;
  detail?: string | null;
}

export interface ValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  checks: ValidationCheck[];
  summary: string;
}

export interface NodeExecution {
  nodeId: string;
  status: 'Pending' | 'Waiting' | 'Running' | 'Completed' | 'Failed' | 'Skipped';
  startedAt: string | null;
  completedAt: string | null;
  inputs: Record<string, any> | null;
  outputs: Record<string, any> | null;
  error: { message: string } | null;
}

export interface WorkflowExecutionResult {
  executionId: string;
  status: 'Pending' | 'Running' | 'Completed' | 'Failed';
  duration: number;
  durationMs: number;
  outputs: Record<string, any>;
  errors: any[];
  fullExecution: {
    executionId: string;
    workflowId?: string;
    status: string;
    startedAt: string;
    completedAt: string;
    duration: number;
    nodeExecutions: Record<string, NodeExecution>;
  };
}

/**
 * Defensive parsing for arrays that might arrive as JSON strings, double-encoded strings, or objects
 */
export function safeParseArray<T = any>(val: any): T[] {
  if (!val) return [];
  if (Array.isArray(val)) return val;
  if (typeof val === 'string') {
    try {
      let parsed: any = JSON.parse(val);
      if (typeof parsed === 'string') {
        try {
          parsed = JSON.parse(parsed);
        } catch {
          // keep parsed as string
        }
      }
      if (Array.isArray(parsed)) return parsed;
      if (parsed && typeof parsed === 'object') {
        if (Array.isArray(parsed.connections)) return parsed.connections;
        if (Array.isArray(parsed.nodes)) return parsed.nodes;
        return Object.values(parsed);
      }
      return [];
    } catch {
      return [];
    }
  }
  if (typeof val === 'object' && val !== null) {
    if (Array.isArray(val.connections)) return val.connections;
    if (Array.isArray(val.nodes)) return val.nodes;
    try {
      const vals = Object.values(val);
      return Array.isArray(vals) ? (vals as T[]) : [];
    } catch {
      return [];
    }
  }
  return [];
}

/** Normalize connection to get source and target */
function getEdgeEndpoints(conn: NodeConnection): { source: string; target: string } {
  const source = conn.sourceNodeId || conn.from || '';
  const target = conn.targetNodeId || conn.to || '';
  return { source, target };
}

export function validateWorkflow(workflow: WorkflowDefinition): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];
  const checks: ValidationCheck[] = [];

  const record = (name: string, passed: boolean, failureDetail?: string | null) => {
    checks.push({
      name,
      passed,
      detail: passed ? null : failureDetail ?? null
    });
  };

  const hasName = Boolean(workflow.name && workflow.name.trim().length > 0);
  record('workflow.name', hasName, 'Workflow name is required.');
  if (!hasName) errors.push('Workflow name is required');

  const nodes = Array.isArray(safeParseArray<WorkflowNode>(workflow?.nodes))
    ? safeParseArray<WorkflowNode>(workflow?.nodes)
    : [];
  const rawConnections = Array.isArray(safeParseArray<NodeConnection>(workflow?.connections))
    ? safeParseArray<NodeConnection>(workflow?.connections)
    : [];

  const connections = rawConnections.map((c) => {
    const { source, target } = getEdgeEndpoints(c || {});
    return { ...c, sourceNodeId: source, targetNodeId: target };
  });

  const hasNodes = nodes.length > 0;
  record('workflow.hasNodes', hasNodes, 'Workflow must have at least one node.');
  if (!hasNodes) errors.push('Workflow must have at least one node');

  const ids = new Set<string>();
  for (const node of nodes) {
    const label = node.name || node.id || 'unnamed';
    if (!node.id) {
      errors.push(`Node '${label}' has no id`);
    } else if (ids.has(node.id)) {
      errors.push(`Duplicate node ID: ${node.id}`);
    } else {
      ids.add(node.id);
    }

    if (!node.name || !node.name.trim()) {
      errors.push(`Node '${node.id}' has no name`);
    }
  }

  for (const conn of connections) {
    if (!ids.has(conn.sourceNodeId)) {
      errors.push(`Connection source node '${conn.sourceNodeId}' does not exist in workflow`);
    }
    if (!ids.has(conn.targetNodeId)) {
      errors.push(`Connection target node '${conn.targetNodeId}' does not exist in workflow`);
    }
  }

  // Check entry nodes (nodes with no incoming connections)
  const targetIds = new Set(connections.map((c) => c.targetNodeId));
  const entryNodes = nodes.filter((n) => !targetIds.has(n.id));
  const hasEntry = nodes.length === 0 || entryNodes.length > 0;
  record('graph.hasEntryNode', hasEntry, 'Graph has no entry node (all nodes have incoming edges).');
  if (!hasEntry && nodes.length > 0) {
    errors.push('Workflow has no entry node — circular dependencies detected');
  }

  // Acyclicity check via topological sort
  let isAcyclic = true;
  if (nodes.length > 0) {
    const inDegree = new Map<string, number>();
    for (const node of nodes) inDegree.set(node.id, 0);
    for (const conn of connections) {
      if (inDegree.has(conn.targetNodeId)) {
        inDegree.set(conn.targetNodeId, (inDegree.get(conn.targetNodeId) || 0) + 1);
      }
    }

    const queue: string[] = [];
    for (const [id, deg] of inDegree.entries()) {
      if (deg === 0) queue.push(id);
    }

    let visited = 0;
    while (queue.length > 0) {
      const curr = queue.shift()!;
      visited++;
      for (const conn of connections) {
        if (conn.sourceNodeId === curr) {
          const newDeg = (inDegree.get(conn.targetNodeId) || 1) - 1;
          inDegree.set(conn.targetNodeId, newDeg);
          if (newDeg === 0) queue.push(conn.targetNodeId);
        }
      }
    }
    isAcyclic = visited === nodes.length;
  }
  record('graph.acyclic', isAcyclic, 'Graph contains circular dependencies or self-referencing loops.');
  if (!isAcyclic) errors.push('Circular dependencies detected in flow connections');

  const isValid = errors.length === 0;
  const summary = isValid
    ? `Workflow '${workflow.name}' is structurally valid with ${nodes.length} node(s) and ${connections.length} connection(s).`
    : `Workflow validation failed with ${errors.length} error(s).`;

  return {
    isValid,
    errors,
    warnings,
    checks,
    summary
  };
}

export async function executeWorkflow(
  workflow: WorkflowDefinition,
  executionContext: Record<string, any> = {}
): Promise<WorkflowExecutionResult> {
  const startTime = Date.now();
  const executionId = 'exec-' + Math.random().toString(36).substring(2, 9);
  const startedAt = new Date().toISOString();

  logStore.add({
    timestamp: startedAt,
    level: 'Information',
    category: 'Workflow',
    message: `Starting workflow execution: ${workflow.name || workflow.id || 'Unnamed'} (${executionId})`,
    source: 'backend'
  });

  const nodes = Array.isArray(safeParseArray<WorkflowNode>(workflow?.nodes))
    ? safeParseArray<WorkflowNode>(workflow?.nodes)
    : [];
  const rawConnections = Array.isArray(safeParseArray<NodeConnection>(workflow?.connections))
    ? safeParseArray<NodeConnection>(workflow?.connections)
    : [];

  const connections = rawConnections.map((c) => {
    const { source, target } = getEdgeEndpoints(c || {});
    return { ...c, sourceNodeId: source, targetNodeId: target };
  });

  const nodeMap = new Map<string, WorkflowNode>(nodes.map((n) => [n.id, n]));
  const completedNodes = new Set<string>();
  const nodeOutputs = new Map<string, Record<string, any>>();
  const nodeExecutions: Record<string, NodeExecution> = {};
  const errors: any[] = [];
  let workflowStatus: 'Completed' | 'Failed' = 'Completed';

  // Identify terminal nodes (no outgoing connections)
  const terminalNodeIds = new Set(
    nodes.filter((n) => !connections.some((c) => c.sourceNodeId === n.id)).map((n) => n.id)
  );

  while (completedNodes.size < nodeMap.size && workflowStatus !== 'Failed') {
    const readyNodes = Array.from(nodeMap.values()).filter((node) => {
      if (completedNodes.has(node.id)) return false;
      const incoming = connections.filter((c) => c.targetNodeId === node.id);
      return incoming.every((c) => completedNodes.has(c.sourceNodeId));
    });

    if (readyNodes.length === 0 && completedNodes.size < nodeMap.size) {
      errors.push({ message: 'Circular dependency or unreachable nodes detected' });
      workflowStatus = 'Failed';
      break;
    }

    for (const node of readyNodes) {
      const nodeStart = new Date().toISOString();
      const incoming = connections.filter((c) => c.targetNodeId === node.id);

      // Collect inputs from incoming connections
      const inputs: Record<string, any> = { ...executionContext };
      for (const conn of incoming) {
        const sourceOut = nodeOutputs.get(conn.sourceNodeId) || {};
        const outKey = conn.outputKey;
        const inKey = conn.inputKey || outKey;

        if (outKey && sourceOut[outKey] !== undefined) {
          inputs[inKey] = sourceOut[outKey];
        } else if (sourceOut['output'] !== undefined) {
          inputs[inKey || 'output'] = sourceOut['output'];
        } else if (sourceOut['value'] !== undefined) {
          inputs[inKey || 'value'] = sourceOut['value'];
        } else if (sourceOut['response'] !== undefined) {
          inputs[inKey || 'response'] = sourceOut['response'];
        } else if (sourceOut['result'] !== undefined) {
          inputs[inKey || 'result'] = sourceOut['result'];
        }
        Object.assign(inputs, sourceOut);
      }

      logStore.add({
        timestamp: nodeStart,
        level: 'Information',
        category: 'Workflow',
        message: `Executing node [${node.type || node.nodeType}]: "${node.name}" (${node.id})`,
        source: 'backend'
      });

      const nodeType = node.nodeType || node.type || 'Input';
      let nodeOut: Record<string, any> = {};
      let nodeFailed = false;
      let nodeError: string | null = null;

      try {
        if (nodeType === 'Input') {
          // Input Node: Supports strongly typed entity fields
          const val =
            node.config?.value ??
            node.config?.userInput ??
            node.config?.string ??
            node.config?.text ??
            node.config?.defaultValue ??
            inputs.input_text ??
            '';
          const key = node.config?.key || 'input_text';
          const entityType = node.config?.entityType || 'String';

          nodeOut = {
            [key]: val,
            value: val,
            string: String(val),
            text: String(val),
            output: val,
            entityType,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'Input'
          };

          logStore.add({
            timestamp: new Date().toISOString(),
            level: 'Information',
            category: 'InputNode',
            message: `[INPUT NODE "${node.name}"] (${entityType}): "${String(val).slice(0, 100)}"`,
            source: 'backend'
          });
        } else if (nodeType === 'Agent') {
          // Agent Node: Local Inference with Microsoft Copilot ONNX & Prompt Flow
          const systemPrompt =
            node.config?.systemPrompt ||
            'You are an expert bioinformatician and workflow reasoning assistant.';

          const upstreamString =
            inputs.value ?? inputs.string ?? inputs.text ?? inputs.input_text ?? inputs.output ?? '';

          let userPrompt = node.config?.prompt || node.config?.userPrompt || '';
          if (userPrompt && userPrompt.includes('{{')) {
            userPrompt = userPrompt
              .replace(/\{\{\s*input\s*\}\}/gi, String(upstreamString))
              .replace(/\{\{\s*value\s*\}\}/gi, String(upstreamString))
              .replace(/\{\{\s*text\s*\}\}/gi, String(upstreamString));
          } else if (!userPrompt) {
            userPrompt = String(upstreamString) || 'Analyze workflow input';
          }

          const rawTools = node.config?.tools || ['sequence-aligner', 'dna-bioinformatics/reverse_complement'];
          const tools = Array.isArray(rawTools) ? rawTools : [rawTools];

          logStore.add({
            timestamp: new Date().toISOString(),
            level: 'Information',
            category: 'AgentNode',
            message: `[AGENT NODE "${node.name}"] Running Copilot ONNX Inference with prompt: "${userPrompt.slice(
              0,
              80
            )}..."`,
            source: 'backend'
          });

          const copilotResult = await copilotEngine.generate({
            model: node.config?.model || 'Microsoft-Copilot-Local-ONNX',
            systemPrompt,
            userPrompt,
            availableTools: tools,
            temperature: node.config?.temperature ?? 0.7,
            maxTokens: node.config?.maxTokens ?? 512,
            context: inputs
          });

          nodeOut = {
            output: copilotResult.response,
            response: copilotResult.response,
            reasoning: copilotResult.reasoning,
            tokensGenerated: copilotResult.tokensGenerated,
            toolCalls: copilotResult.toolCalls,
            onnxExecution: copilotResult.onnxExecution,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'Agent'
          };
        } else if (nodeType === 'Output') {
          // Output Node: Formats result and PRINTS TO BACKEND CONSOLE
          const incomingData =
            inputs.response ?? inputs.output ?? inputs.value ?? inputs.text ?? inputs;

          const displayValue =
            typeof incomingData === 'object'
              ? JSON.stringify(incomingData, null, 2)
              : String(incomingData);

          const prefix = node.config?.prefix ? `${node.config.prefix} ` : '';

          console.log('\n==================================================');
          console.log(`[BACKEND CONSOLE OUTPUT] Node: "${node.name}" (${node.id})`);
          console.log(`${prefix}${displayValue}`);
          console.log('==================================================\n');

          logStore.add({
            timestamp: new Date().toISOString(),
            level: 'Information',
            category: 'BackendConsole',
            message: `[BACKEND CONSOLE OUTPUT] "${node.name}": ${prefix}${displayValue.slice(0, 500)}`,
            source: 'backend'
          });

          nodeOut = {
            output: displayValue,
            value: displayValue,
            printed: true,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'Output'
          };
        } else if (nodeType === 'ProcessingSkill') {
          const skillId = node.config?.skillId;
          if (!skillId) {
            throw new Error(`Node ${node.name} has no skillId configured`);
          }

          const skillInputs = {
            ...node.config,
            ...inputs
          };

          if (inputs.value && !skillInputs.source && !skillInputs.sequence1 && !skillInputs.data) {
            if (skillId === 'fasta-loader') skillInputs.source = inputs.value;
            else if (skillId === 'sequence-aligner') skillInputs.sequence1 = inputs.value;
            else skillInputs.data = inputs.value;
          }

          const skillResult = await skillRegistry.executeSkill(skillId, skillInputs);
          if (!skillResult.success) {
            throw new Error(skillResult.errorMessage || 'Skill execution failed');
          }

          nodeOut = {
            ...skillResult.data,
            output: skillResult.data,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'ProcessingSkill'
          };
        } else if (nodeType === 'Loop') {
          // Loop / Iterator Node
          const iterations = Math.max(1, Math.min(Number(node.config?.iterations || 3), 10));
          const loopResults: any[] = [];
          const sourceVal = inputs.value || inputs.output || 'iteration_item';

          for (let i = 1; i <= iterations; i++) {
            loopResults.push({
              index: i,
              iteration: i,
              item: `${sourceVal} [Pass ${i}]`,
              timestamp: new Date().toISOString()
            });
          }

          nodeOut = {
            iterationsCompleted: iterations,
            items: loopResults,
            output: loopResults,
            value: loopResults,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'Loop'
          };
        } else if (nodeType === 'Conditional') {
          // Conditional Branching Node
          const condition = node.config?.condition || 'true';
          const targetVal = String(inputs.value || inputs.output || '');

          let conditionMet = true;
          if (condition.includes('length')) {
            const minLen = parseInt(condition.replace(/[^0-9]/g, '') || '10', 10);
            conditionMet = targetVal.length >= minLen;
          } else if (condition.includes('contains') || condition.includes('ATG')) {
            conditionMet = targetVal.toUpperCase().includes('ATG');
          } else if (condition.includes('gc')) {
            const gcCount = (targetVal.match(/[GCgc]/g) || []).length;
            const gc = targetVal.length > 0 ? gcCount / targetVal.length : 0;
            conditionMet = gc >= 0.4;
          }

          const branch = conditionMet ? 'TrueBranch' : 'FalseBranch';
          nodeOut = {
            condition,
            conditionMet,
            branch,
            output: conditionMet ? inputs.output || targetVal : 'Condition not met',
            value: targetVal,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'Conditional'
          };
        } else if (nodeType === 'SubWorkflow') {
          // Sub-Workflow Node
          const subId = node.config?.subWorkflowId || 'sub_flow';
          nodeOut = {
            subWorkflowId: subId,
            status: 'SubWorkflowCompleted',
            inheritedInputs: Object.keys(inputs),
            output: `[Sub-Workflow "${subId}"] Processed ${Object.keys(inputs).length} upstream properties`,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'SubWorkflow'
          };
        } else if (nodeType === 'CronJob') {
          // Scheduled Cron Runner Node
          const interval = node.config?.intervalSeconds || 30;
          nodeOut = {
            cronSchedule: `*/${interval}s`,
            lastTriggerTime: new Date().toISOString(),
            status: 'Armed',
            output: `Cron trigger active (Interval: ${interval}s)`,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'CronJob'
          };
        } else if (nodeType === 'StateMachine') {
          // Unity-Style State Machine Node
          const currentState = node.config?.currentState || 'Idle';
          const targetState = node.config?.targetState || 'Processing';
          const layer = node.config?.activeLayer || 'Base Layer';
          const trigger = node.config?.trigger || 'OnExecute';

          nodeOut = {
            layer,
            previousState: currentState,
            currentState: targetState,
            trigger,
            stateMachineOutput: `Transitioned [${layer}]: ${currentState} -> ${targetState} on event (${trigger})`,
            output: `State Machine at "${targetState}"`,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'StateMachine'
          };
        } else if (nodeType === 'SmartComponent') {
          // Authorable Smart Component Node
          const compName = node.config?.componentName || node.name;
          const transformMode = node.config?.transformMode || 'dna-enhance';
          const inVal = String(inputs.value || inputs.output || 'ATGCTAGC');

          let compResult: any = inVal;
          if (transformMode === 'dna-enhance') {
            const comp = inVal.split('').reverse().map((c) => ({ A: 'T', T: 'A', C: 'G', G: 'C' }[c] || c)).join('');
            compResult = { original: inVal, complement: comp, enhancedLength: inVal.length };
          } else {
            compResult = { component: compName, processedData: inVal, timestamp: new Date().toISOString() };
          }

          nodeOut = {
            smartComponent: compName,
            result: compResult,
            output: compResult,
            value: compResult,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'SmartComponent'
          };
        } else if (nodeType === 'Value') {
          const val = node.config?.value ?? '';
          nodeOut = {
            value: val,
            output: val,
            nodeId: node.id,
            nodeName: node.name,
            nodeType: 'Value'
          };
        } else if (nodeType === 'Log') {
          const msg = node.config?.message || JSON.stringify(inputs);
          logStore.add({
            timestamp: new Date().toISOString(),
            level: node.config?.level || 'Information',
            category: 'NodeLog',
            message: `[Log Node "${node.name}"] ${msg}`,
            source: 'backend'
          });
          nodeOut = {
            ...inputs,
            logged: true,
            nodeId: node.id
          };
        } else {
          // Pass-through node
          nodeOut = {
            ...inputs,
            nodeId: node.id,
            nodeName: node.name,
            nodeType
          };
        }
      } catch (err: any) {
        nodeFailed = true;
        nodeError = err.message || String(err);
        errors.push({ message: `Node ${node.name} failed: ${nodeError}` });
        logStore.add({
          timestamp: new Date().toISOString(),
          level: 'Error',
          category: 'Workflow',
          message: `Node ${node.id} ("${node.name}") failed: ${nodeError}`,
          source: 'backend'
        });
      }

      const nodeEnd = new Date().toISOString();
      const execRecord: NodeExecution = {
        nodeId: node.id,
        status: nodeFailed ? 'Failed' : 'Completed',
        startedAt: nodeStart,
        completedAt: nodeEnd,
        inputs,
        outputs: nodeFailed ? null : nodeOut,
        error: nodeFailed ? { message: nodeError! } : null
      };

      nodeExecutions[node.id] = execRecord;

      if (!nodeFailed) {
        nodeOutputs.set(node.id, nodeOut);
        completedNodes.add(node.id);
      } else {
        workflowStatus = 'Failed';
        break;
      }
    }
  }

  const completedAt = new Date().toISOString();
  const durationMs = Date.now() - startTime;

  // Compile final outputs from terminal nodes
  const finalOutputs: Record<string, any> = {};
  for (const termId of terminalNodeIds) {
    const out = nodeOutputs.get(termId);
    if (out) Object.assign(finalOutputs, out);
  }

  if (Object.keys(finalOutputs).length === 0 && nodeOutputs.size > 0) {
    const lastId = Array.from(completedNodes).pop();
    if (lastId) Object.assign(finalOutputs, nodeOutputs.get(lastId) || {});
  }

  logStore.add({
    timestamp: completedAt,
    level: workflowStatus === 'Completed' ? 'Information' : 'Error',
    category: 'Workflow',
    message: `Workflow "${workflow.name || workflow.id}" execution ${workflowStatus} in ${durationMs}ms`,
    source: 'backend'
  });

  return {
    executionId,
    status: workflowStatus,
    duration: durationMs,
    durationMs,
    outputs: finalOutputs,
    errors,
    fullExecution: {
      executionId,
      workflowId: workflow.id,
      status: workflowStatus,
      startedAt,
      completedAt,
      duration: durationMs,
      nodeExecutions
    }
  };
}
