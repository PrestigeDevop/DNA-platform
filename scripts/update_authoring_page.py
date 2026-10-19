import os

authoring_path = 'src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/authoring/+page.svelte'
with open(authoring_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Let's see what needs replacing in authoring/+page.svelte:
# We will enhance:
# 1. STARTER_TEMPLATES
# 2. State variables (parsedTools, parsedProps, parsedMise, isMiddleware, reflectTab)
# 3. testAndReflectComponent, publishComponent, addToWorkflowPalette
# 4. Markup: Add Reflection & Mise Panel under/beside the editor
# 5. Styles: Add styles for reflection panels, tool cards, mise badges

new_page_code = """<script lang="ts">
  import { onMount } from 'svelte';
  import MonacoEditor from '$lib/components/MonacoEditor.svelte';

  interface PortDef {
    name: string;
    type: string;
  }

  interface StarterTemplate {
    name: string;
    category: string;
    language: 'csharp' | 'python' | 'javascript' | 'bash';
    moduleDependency: string;
    description: string;
    inputs: PortDef[];
    outputs: PortDef[];
    code: string;
  }

  const STARTER_TEMPLATES: StarterTemplate[] = [
    {
      name: 'Microsoft Agent Framework C# Middleware',
      category: 'Agent Middleware & Guardrails',
      language: 'csharp',
      moduleDependency: 'microsoft-agent-middleware',
      description: 'C# IAgentMiddleware implementing Microsoft Agent Framework pipeline hooks, [Tool] attributes, LogLevel enum, and pgvector semantic storage.',
      inputs: [
        { name: 'inputPrompt', type: 'String' },
        { name: 'contextPayload', type: 'JSON' }
      ],
      outputs: [
        { name: 'isValid', type: 'Boolean' },
        { name: 'sanitizedOutput', type: 'String' },
        { name: 'telemetry', type: 'JSON' }
      ],
      code: `using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Agents.Core;

#r "nuget: Microsoft.Agents.Core, 1.0.0"
#r "nuget: Microsoft.Agents.Protocols, 1.0.0"
#r "nuget: Pgvector, 0.3.0"

[Category("Agent Middleware & Guardrails")]
[Description("Microsoft Agent Framework Middleware for request inspection, tool filtering, and vector logging")]
public class AgentInspectionMiddleware : IAgentMiddleware
{
    public enum PolicyLevel { Permissive, Standard, Strict }

    [Description("Enforcement level for agent guardrails")]
    public PolicyLevel Policy { get; set; } = PolicyLevel.Standard;

    [Description("Vector Database connection for semantic guardrails and memory")]
    public string VectorStore { get; set; } = "pgvector_default";

    [Description("Minimum safety confidence threshold")]
    public double MinConfidence { get; set; } = 0.88;

    [Tool("validate_input_payload", "Validates input prompt or DNA sequence against compliance policies")]
    public bool ValidateInput(string inputPrompt)
    {
        Console.WriteLine($"[Middleware] Validating prompt with policy {Policy}");
        return !string.IsNullOrWhiteSpace(inputPrompt) && inputPrompt.Length < 10000;
    }

    [Tool("mask_sensitive_biomarkers", "Scans text and masks proprietary pathogen markers")]
    public string MaskBiomarkers(string responseText)
    {
        return responseText.Replace("FLAGGED_PATHOGEN", "[RESTRICTED]");
    }

    public async Task InvokeAsync(AgentContext context, Func<AgentContext, Task> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[AgentMiddleware] Before Agent Execution: Policy={Policy}, VectorStore={VectorStore}");
        await next(context);
        Console.WriteLine($"[AgentMiddleware] After Agent Execution completed");
    }
}`
    },
    {
      name: 'CSnakes Python Bio-Guardrail Smart Component',
      category: 'Genomics & Bioinformatics',
      language: 'python',
      moduleDependency: 'csnakes-biopython',
      description: 'Python smart component with @tool decorators, Enum choices, pgvector embeddings, and in-process BioPython execution.',
      inputs: [
        { name: 'querySequence', type: 'DNA_Sequence' },
        { name: 'referenceSequence', type: 'DNA_Sequence' }
      ],
      outputs: [
        { name: 'alignmentScore', type: 'Float' },
        { name: 'passed', type: 'Boolean' }
      ],
      code: `# CSnakes Polyglot Module & Smart Component
# Annotated with @tool and typed ECS properties

from typing import Dict, Any
from enum import Enum

class GuardrailMode(str, Enum):
    PERMISSIVE = "permissive"
    STRICT = "strict"
    QUARANTINE = "quarantine"

class GenomicGuardrailComponent:
    \"\"\"CSnakes BioPython Smart Component with vector memory and tool annotations.\"\"\"
    
    guardrail_mode: GuardrailMode = GuardrailMode.STRICT
    min_confidence: float = 0.92
    vector_store: str = "pgvector_default"

    @tool(name="screen_dna_pathogen", description="Screens sequence against biosecurity pathogen database using CSnakes")
    def screen_dna_pathogen(self, sequence: str) -> Dict[str, Any]:
        \"\"\"Screens genetic sequence using BioPython in-process.\"\"\"
        g_count = sequence.count("G") + sequence.count("g")
        c_count = sequence.count("C") + sequence.count("c")
        gc_skew = (g_count - c_count) / max(g_count + c_count, 1)
        return {
            "passed": True,
            "gc_skew": round(gc_skew, 4),
            "guardrail_mode": self.guardrail_mode,
            "vector_store": self.vector_store
        }

    @tool(name="log_compliance_audit", description="Writes security audit entry to relational database")
    def log_compliance_audit(self, sequence_id: str, status: str) -> bool:
        return True`
    },
    {
      name: 'JavaScript Agent Stream Filter Middleware',
      category: 'Custom Extensions',
      language: 'javascript',
      moduleDependency: 'node-stream-runtime',
      description: 'Node.js middleware with @mise_runtime tags, JSDoc @tool annotations, stream sanitation, and pgvector memory lookup.',
      inputs: [
        { name: 'streamChunk', type: 'String' },
        { name: 'metadata', type: 'JSON' }
      ],
      outputs: [
        { name: 'filteredChunk', type: 'String' },
        { name: 'modified', type: 'Boolean' }
      ],
      code: `/**
 * @mise_runtime node@20
 * @package npm:@microsoft/agents-bot-hosting
 * @package npm:pgvector
 */

// Microsoft Agent Framework JavaScript Middleware & Smart Component
export class AgentStreamFilterMiddleware {
  constructor() {
    this.inspectionLevel = "standard"; // enum: minimal, standard, strict
    this.vectorStore = "pgvector_default";
    this.maskConfidenceThreshold = 0.85;
    this.enableAuditLog = true;
  }

  /**
   * @tool filter_stream_chunk
   * @description Masks API keys and sensitive tokens from agent output stream
   * @param {string} chunk - Stream chunk text
   * @returns {string} Sanitized chunk
   */
  filterStreamChunk(chunk) {
    return chunk.replace(/[a-f0-9]{32}/gi, "[REDACTED_TOKEN]");
  }

  /**
   * @tool query_pgvector_memory
   * @description Retrieves nearest semantic memories from pgvector database
   * @param {string} query - Search prompt
   * @returns {object} Matching memories
   */
  async queryPgvectorMemory(query) {
    return {
      store: this.vectorStore,
      matches: 2,
      topSimilarity: 0.94
    };
  }
}`
    },
    {
      name: 'CSnakes BioPython GC & Alignment',
      category: 'Genomics & Bioinformatics',
      language: 'csharp',
      moduleDependency: 'csnakes-biopython',
      description: 'C# class utilizing CSnakes in-process bridge to invoke BioPython Needleman-Wunsch & GC analysis with zero IPC overhead.',
      inputs: [
        { name: 'querySequence', type: 'DNA_Sequence' },
        { name: 'referenceSequence', type: 'DNA_Sequence' }
      ],
      outputs: [
        { name: 'alignmentScore', type: 'Float' },
        { name: 'alignmentMatrix', type: 'JSON' }
      ],
      code: `using System;
using System.ComponentModel;

[Category("Genomics & Bioinformatics")]
[Description("CSnakes BioPython In-Process Alignment & GC Engine")]
public class CSnakesBioAligner
{
    [Description("Minimum alignment score cutoff")]
    public double MatchCutoff { get; set; } = 0.85;

    [Description("Gap open penalty")]
    public int GapPenalty { get; set; } = -2;

    [Tool("needleman_wunsch_align", "Executes in-process pairwise alignment via BioPython")]
    public object Execute(string querySequence, string referenceSequence)
    {
        Console.WriteLine($"[CSnakes] Invoking in-process BioPython aligner: {querySequence.Length} bp vs {referenceSequence.Length} bp");
        return new {
            status = "Aligned",
            score = 94.5,
            length = querySequence.Length
        };
    }
}`
    },
    {
      name: 'StateMachineBehaviour FSM Controller',
      category: 'State Machine & FSM',
      language: 'csharp',
      moduleDependency: 'statemachine-core',
      description: 'Controls the workflow serialized FSM. Evaluates transition conditions and triggers state transitions programmatically.',
      inputs: [
        { name: 'incomingPayload', type: 'JSON' }
      ],
      outputs: [
        { name: 'fsmTransitionEvent', type: 'String' },
        { name: 'allowFlow', type: 'Boolean' }
      ],
      code: `using System;
using System.ComponentModel;

[Category("State Machine & FSM")]
[Description("AG-UI Serialized State Machine Behaviour Controller")]
public class StateMachineController
{
    [Description("Target state to transition towards")]
    public string TargetState { get; set; } = "Processing";

    [Tool("evaluate_fsm_transition", "Evaluates payload conditions and yields NextState transition event")]
    public object EvaluateState(string incomingPayload)
    {
        return new {
            fsmTransitionEvent = "NEXT_STAGE",
            allowFlow = true
        };
    }
}`
    },
    {
      name: 'Workspace File System Artifact Processor',
      category: 'System & File Operations',
      language: 'csharp',
      moduleDependency: 'filesystem-workspace',
      description: 'Process and persist workflow artifacts directly to local disk with checksum and size metrics.',
      inputs: [
        { name: 'reportData', type: 'JSON' },
        { name: 'targetFile', type: 'String' }
      ],
      outputs: [
        { name: 'savedPath', type: 'String' },
        { name: 'fileSize', type: 'Int32' },
        { name: 'success', type: 'Boolean' }
      ],
      code: `using System;
using System.IO;
using System.ComponentModel;

[Category("System & File Operations")]
[Description("Persists genomic artifacts to local workspace storage")]
public class WorkspaceArtifactSaver
{
    [Description("Relative subfolder inside workspace")]
    public string DestinationFolder { get; set; } = "artifacts";

    [Tool("save_workspace_artifact", "Writes formatted string or JSON to workspace filesystem")]
    public object SaveArtifact(string reportData, string targetFile)
    {
        return new {
            savedPath = $"workspace/{DestinationFolder}/{targetFile}",
            fileSize = reportData.Length,
            success = true
        };
    }
}`
    }
  ];

  const INSTALLED_MODULES = [
    { id: 'microsoft-agent-middleware', name: 'Microsoft Agent Framework Runtime (.NET 8)', env: 'Microsoft.Agents.Core / Protocols' },
    { id: 'csnakes-biopython', name: 'CSnakes BioPython Polyglot Bridge (v1.2.0)', env: 'CPython 3.10+ / C# .NET 8' },
    { id: 'statemachine-core', name: 'AG-UI Serialized State Machine FSM Engine', env: 'Native C# .NET 8 Core' },
    { id: 'filesystem-workspace', name: 'Workspace File System Module', env: 'Local File System / Node.js' },
    { id: 'node-stream-runtime', name: 'Node.js High-Throughput Stream Module', env: 'Node.js 20 LTS' }
  ];

  let compName = STARTER_TEMPLATES[0].name;
  let compCategory = STARTER_TEMPLATES[0].category;
  let compLanguage: 'csharp' | 'python' | 'javascript' | 'bash' = STARTER_TEMPLATES[0].language;
  let compModuleDep = STARTER_TEMPLATES[0].moduleDependency;
  let compDescription = STARTER_TEMPLATES[0].description;
  let compInputs: PortDef[] = JSON.parse(JSON.stringify(STARTER_TEMPLATES[0].inputs));
  let compOutputs: PortDef[] = JSON.parse(JSON.stringify(STARTER_TEMPLATES[0].outputs));
  let compCode = STARTER_TEMPLATES[0].code;

  let testingComponent = false;
  let testOutput = '';
  let saving = false;
  let saveSuccessMessage = '';

  // Reflection & Mise State
  let reflectionResult: any = null;
  let reflectionLoading = false;
  let activeReflectTab: 'tools' | 'properties' | 'mise' | 'diagnostics' = 'tools';

  onMount(async () => {
    // Initial reflection on template load
    await reflectCode();
  });

  function loadTemplate(tmpl: StarterTemplate) {
    compName = tmpl.name;
    compCategory = tmpl.category;
    compLanguage = tmpl.language;
    compModuleDep = tmpl.moduleDependency;
    compDescription = tmpl.description;
    compInputs = JSON.parse(JSON.stringify(tmpl.inputs));
    compOutputs = JSON.parse(JSON.stringify(tmpl.outputs));
    compCode = tmpl.code;
    testOutput = '';
    saveSuccessMessage = '';
    reflectCode();
  }

  function addInputPort() {
    compInputs = [...compInputs, { name: `in_${compInputs.length + 1}`, type: 'String' }];
  }

  function removeInputPort(idx: number) {
    compInputs = compInputs.filter((_, i) => i !== idx);
  }

  function addOutputPort() {
    compOutputs = [...compOutputs, { name: `out_${compOutputs.length + 1}`, type: 'JSON' }];
  }

  function removeOutputPort(idx: number) {
    compOutputs = compOutputs.filter((_, i) => i !== idx);
  }

  async function reflectCode() {
    reflectionLoading = true;
    try {
      const res = await fetch('/api/authoring/reflect', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          code: compCode,
          language: compLanguage
        })
      });
      const data = await res.json();
      if (data.success && data.result) {
        reflectionResult = data.result;
        if (data.result.category && data.result.category !== 'Logic Component') {
          compCategory = data.result.category;
        }
      }
    } catch (e: any) {
      console.warn('Code reflection error:', e);
    } finally {
      reflectionLoading = false;
    }
  }

  async function testAndReflectComponent() {
    testingComponent = true;
    testOutput = '';
    saveSuccessMessage = '';

    try {
      // 1. Run live code reflection
      await reflectCode();

      // 2. Validate via polyglot runner
      const res = await fetch('/api/polyglot/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: compLanguage,
          code: compCode
        })
      });

      const data = await res.json();
      if (data.success) {
        let msg = `✔ ${compLanguage.toUpperCase()} Verification & Code Parse Passed (${data.durationMs}ms)\n\n`;
        if (reflectionResult) {
          msg += `── Detected Tools: ${reflectionResult.tools.length} ──\n`;
          for (const t of reflectionResult.tools) {
            msg += `  • [${t.role.toUpperCase()}] ${t.name}: ${t.description} (${t.parameters.length} params)\n`;
          }
          msg += `\n── Reflected ECS Properties: ${reflectionResult.properties.length} ──\n`;
          for (const p of reflectionResult.properties) {
            msg += `  • ${p.name} (${p.type}): default = ${JSON.stringify(p.defaultValue)}\n`;
          }
          if (reflectionResult.mise) {
            msg += `\n── Mise Runtime Requirements ──\n`;
            for (const [r, v] of Object.entries(reflectionResult.mise.runtimes || {})) {
              msg += `  • runtime: ${r}@${v}\n`;
            }
          }
        }
        msg += `\n── Execution Output ──\n` + (data.output || '(Diagnostics check passed)');
        testOutput = msg;
        activeReflectTab = 'diagnostics';
      } else {
        testOutput = `⚠ Verification Error:\n${data.error || data.output || 'Execution failed'}`;
        activeReflectTab = 'diagnostics';
      }
    } catch (e: any) {
      testOutput = `Network / Execution Error: ${e.message}`;
    } finally {
      testingComponent = false;
    }
  }

  async function publishComponent() {
    saving = true;
    saveSuccessMessage = '';

    // First ensure latest reflection is available
    if (!reflectionResult) {
      await reflectCode();
    }

    const payload = {
      name: compName,
      category: compCategory,
      language: compLanguage,
      moduleDependency: compModuleDep,
      description: compDescription,
      inputs: compInputs,
      outputs: compOutputs,
      code: compCode,
      tools: reflectionResult?.tools || [],
      properties: reflectionResult?.properties || [],
      mise: reflectionResult?.mise || {},
      isMiddleware: Boolean(reflectionResult?.isMiddleware)
    };

    try {
      const res = await fetch('/api/authoring/publish', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      const data = await res.json();
      if (res.ok && data.success) {
        saveSuccessMessage = `✔ Successfully Published "${compName}" to Local Server Marketplace! Registered ${data.toolsCount} AG-UI tool(s) and generated companion ${data.pluginId}.mise.toml in workspace.`;
      } else {
        saveSuccessMessage = `⚠ Publication error: ${data.error || 'Server error'}`;
      }
    } catch (err: any) {
      saveSuccessMessage = `Network error during publish: ${err.message}`;
    } finally {
      saving = false;
    }
  }

  function addToWorkflowPalette() {
    try {
      const customComp = {
        id: 'custom-' + compName.toLowerCase().replace(/[^a-z0-9]/g, '-') + '-' + Date.now().toString(36).slice(-4),
        name: compName,
        category: compCategory,
        language: compLanguage,
        inputs: compInputs,
        outputs: compOutputs,
        code: compCode,
        properties: reflectionResult?.properties || [],
        isMiddleware: reflectionResult?.isMiddleware || false
      };

      const existing = JSON.parse(localStorage.getItem('dna_custom_components') || '[]');
      existing.push(customComp);
      localStorage.setItem('dna_custom_components', JSON.stringify(existing));

      saveSuccessMessage = `✔ Added "${compName}" to Workflow Palette! When you open the Workflow Designer, this component appears in the palette ready to drag-and-drop.`;
    } catch (e: any) {
      saveSuccessMessage = `Added to local workflow session: ${e.message}`;
    }
  }
</script>

<div class="authoring-page">
  <header class="authoring-header">
    <div class="header-left">
      <h2>🧩 Logic Component & Extension Authoring Studio</h2>
      <span class="header-sub">Author Microsoft Agent Framework Middleware, CSnakes Polyglot Tools & Smart Components</span>
    </div>
    <div class="header-actions">
      <button
        type="button"
        class="btn-test"
        disabled={testingComponent}
        on:click={testAndReflectComponent}
      >
        {testingComponent ? '⏳ Analyzing...' : '⚡ Test & Live Reflect'}
      </button>
      <button
        type="button"
        class="btn-publish"
        disabled={saving}
        on:click={publishComponent}
      >
        🚀 {saving ? 'Publishing...' : 'Publish to Local Server'}
      </button>
      <button
        type="button"
        class="btn-palette"
        on:click={addToWorkflowPalette}
      >
        📋 Add to Workflow Palette
      </button>
    </div>
  </header>

  {#if saveSuccessMessage}
    <div class="alert-success">{saveSuccessMessage}</div>
  {/if}

  <div class="authoring-layout">
    <!-- Left Sidebar: Templates & Module Dependency -->
    <aside class="authoring-sidebar">
      <div class="sidebar-section">
        <h3>Starter Templates</h3>
        <p class="section-desc">Select a baseline template with verified decorators & runtime bindings:</p>
        <div class="template-list">
          {#each STARTER_TEMPLATES as tmpl}
            <button
              type="button"
              class="template-card"
              class:active={compName === tmpl.name}
              on:click={() => loadTemplate(tmpl)}
            >
              <div class="tmpl-top">
                <span class="tmpl-name">{tmpl.name}</span>
                <span class="tmpl-lang {tmpl.language}">{tmpl.language.toUpperCase()}</span>
              </div>
              <p class="tmpl-desc">{tmpl.description}</p>
            </button>
          {/each}
        </div>
      </div>

      <div class="sidebar-section">
        <h3>Platform Module Linkage</h3>
        <p class="section-desc">Declare platform module dependencies:</p>
        <div class="module-dep-box">
          <label for="module-select">Required Platform Module:</label>
          <select id="module-select" bind:value={compModuleDep} class="module-dropdown">
            {#each INSTALLED_MODULES as mod}
              <option value={mod.id}>{mod.name}</option>
            {/each}
          </select>
          <div class="env-info">
            <span>Runtime Environment:</span>
            <code>{INSTALLED_MODULES.find((m) => m.id === compModuleDep)?.env || 'Standard Environment'}</code>
          </div>
        </div>
      </div>
    </aside>

    <!-- Main Studio: Configuration, Monaco Editor & Live Reflection -->
    <main class="studio-main">
      <div class="config-bar">
        <div class="config-row main-props">
          <div class="form-group flex-2">
            <label for="comp-name">Component / Middleware Name:</label>
            <input
              id="comp-name"
              type="text"
              bind:value={compName}
              placeholder="e.g. AgentInspectionMiddleware"
              class="input-text"
            />
          </div>

          <div class="form-group flex-1">
            <label for="comp-cat">Category:</label>
            <select id="comp-cat" bind:value={compCategory} class="input-select">
              <option value="Agent Middleware & Guardrails">Agent Middleware & Guardrails</option>
              <option value="Genomics & Bioinformatics">Genomics & Bioinformatics</option>
              <option value="State Machine & FSM">State Machine & FSM</option>
              <option value="System & File Operations">System & File Operations</option>
              <option value="Custom Extensions">Custom Extensions</option>
            </select>
          </div>

          <div class="form-group flex-1">
            <label for="comp-lang">Target Language:</label>
            <select
              id="comp-lang"
              bind:value={compLanguage}
              on:change={reflectCode}
              class="input-select"
            >
              <option value="csharp">C# (.NET 8 Core)</option>
              <option value="python">Python 3 (CSnakes Polyglot)</option>
              <option value="javascript">JavaScript (Node.js 20)</option>
              <option value="bash">POSIX Bash Shell</option>
            </select>
          </div>
        </div>

        <!-- Programmable Multi-I/O Ports -->
        <div class="ports-config-container">
          <div class="ports-col">
            <div class="ports-header">
              <span class="ports-title">📥 Multi-Input Ports ({compInputs.length})</span>
              <button type="button" class="btn-port-add" on:click={addInputPort}>+ Add Input</button>
            </div>
            <div class="ports-list">
              {#each compInputs as inp, i}
                <div class="port-row">
                  <input type="text" bind:value={inp.name} placeholder="Port name" class="port-name-input" />
                  <select bind:value={inp.type} class="port-type-select">
                    <option value="String">String</option>
                    <option value="DNA_Sequence">DNA_Sequence</option>
                    <option value="JSON">JSON</option>
                    <option value="Float">Float</option>
                    <option value="Int32">Int32</option>
                    <option value="Boolean">Boolean</option>
                  </select>
                  <button type="button" class="btn-port-del" on:click={() => removeInputPort(i)}>✕</button>
                </div>
              {/each}
            </div>
          </div>

          <div class="ports-col">
            <div class="ports-header">
              <span class="ports-title">📤 Multi-Output Ports ({compOutputs.length})</span>
              <button type="button" class="btn-port-add" on:click={addOutputPort}>+ Add Output</button>
            </div>
            <div class="ports-list">
              {#each compOutputs as out, i}
                <div class="port-row">
                  <input type="text" bind:value={out.name} placeholder="Port name" class="port-name-input" />
                  <select bind:value={out.type} class="port-type-select">
                    <option value="JSON">JSON</option>
                    <option value="DNA_Sequence">DNA_Sequence</option>
                    <option value="String">String</option>
                    <option value="Float">Float</option>
                    <option value="Int32">Int32</option>
                    <option value="Boolean">Boolean</option>
                  </select>
                  <button type="button" class="btn-port-del" on:click={() => removeOutputPort(i)}>✕</button>
                </div>
              {/each}
            </div>
          </div>
        </div>
      </div>

      <!-- Monaco Editor for Component Implementation -->
      <div class="editor-section">
        <div class="editor-header">
          <div class="editor-title">
            <span>Implementation ({compLanguage.toUpperCase()})</span>
            {#if reflectionResult?.isMiddleware}
              <span class="badge-middleware">🛡️ Microsoft Agent Framework Middleware Detected</span>
            {/if}
          </div>
          <div class="editor-tools">
            <button type="button" class="btn-sub-reflect" on:click={reflectCode}>
              🔄 Refresh Reflection
            </button>
            <span class="syntax-badge">Monaco Interactive</span>
          </div>
        </div>
        <MonacoEditor
          height="320px"
          language={compLanguage}
          bind:value={compCode}
        />
      </div>

      <!-- LIVE CODE REFLECTION & MISE REQUIREMENTS PANEL -->
      <div class="reflection-panel">
        <div class="reflection-nav">
          <button
            type="button"
            class="reflect-tab-btn"
            class:active={activeReflectTab === 'tools'}
            on:click={() => (activeReflectTab = 'tools')}
          >
            🛠️ Annotated Tools ({reflectionResult?.tools?.length || 0})
          </button>
          <button
            type="button"
            class="reflect-tab-btn"
            class:active={activeReflectTab === 'properties'}
            on:click={() => (activeReflectTab = 'properties')}
          >
            ⚡ Reflected ECS Properties ({reflectionResult?.properties?.length || 0})
          </button>
          <button
            type="button"
            class="reflect-tab-btn"
            class:active={activeReflectTab === 'mise'}
            on:click={() => (activeReflectTab = 'mise')}
          >
            📦 Mise Runtime & Packages
          </button>
          {#if testOutput}
            <button
              type="button"
              class="reflect-tab-btn"
              class:active={activeReflectTab === 'diagnostics'}
              on:click={() => (activeReflectTab = 'diagnostics')}
            >
              📊 Compiler Diagnostics
            </button>
          {/if}
        </div>

        <div class="reflection-tab-content">
          <!-- TAB 1: ANNOTATED TOOLS -->
          {#if activeReflectTab === 'tools'}
            <div class="tools-tab-pane">
              {#if reflectionResult?.tools && reflectionResult.tools.length > 0}
                <div class="tools-grid">
                  {#each reflectionResult.tools as tool}
                    <div class="tool-reflect-card">
                      <div class="tool-reflect-head">
                        <span class="tool-name-tag">
                          <span class="role-icon">{tool.role === 'evaluator' ? '⚖️' : '🔧'}</span>
                          <strong>{tool.name}</strong>
                        </span>
                        <span class="badge-role">{tool.role}</span>
                      </div>
                      <p class="tool-desc-text">{tool.description || 'No description provided'}</p>

                      <div class="params-block">
                        <span class="params-label">Parameters ({tool.parameters.length}):</span>
                        {#if tool.parameters.length > 0}
                          <div class="params-tags">
                            {#each tool.parameters as param}
                              <span class="param-tag">
                                <code>{param.name}</code>: <span class="param-type">{param.type}</span>
                              </span>
                            {/each}
                          </div>
                        {:else}
                          <span class="empty-param-text">No parameters (invokes directly)</span>
                        {/if}
                      </div>
                    </div>
                  {/each}
                </div>
              {:else}
                <div class="empty-reflection">
                  <span>No annotated tools detected. Use <code>[Tool("name", "desc")]</code> in C#, <code>@tool(name="...")</code> in Python, or <code>@tool</code> JSDoc in JS.</span>
                </div>
              {/if}
            </div>

          <!-- TAB 2: REFLECTED ECS PROPERTIES -->
          {:else if activeReflectTab === 'properties'}
            <div class="properties-tab-pane">
              <div class="prop-intro-hint">
                These properties are auto-reflected from class properties and rendered as tailored widgets in the Workflow Designer Inspector:
              </div>
              {#if reflectionResult?.properties && reflectionResult.properties.length > 0}
                <div class="properties-grid">
                  {#each reflectionResult.properties as prop}
                    <div class="prop-reflect-card">
                      <div class="prop-card-header">
                        <span class="prop-name"><strong>{prop.name}</strong></span>
                        <span class="prop-type-badge {prop.type}">{prop.type.toUpperCase()}</span>
                      </div>
                      {#if prop.description}
                        <div class="prop-desc">{prop.description}</div>
                      {/if}

                      <!-- PREVIEW INTERACTIVE CONTROL -->
                      <div class="prop-control-preview">
                        {#if prop.type === 'enum'}
                          <label class="control-label">Enum Select Dropdown:</label>
                          <select class="preview-select" disabled>
                            {#each prop.options || [] as opt}
                              <option selected={opt === prop.defaultValue}>{opt}</option>
                            {/each}
                          </select>
                        {:else if prop.type === 'vectordb'}
                          <label class="control-label">Vector DB Store Selector (pgvector default):</label>
                          <div class="preview-vectordb-box">
                            <div class="vdb-row">
                              <span class="vdb-label">Provider:</span>
                              <span class="vdb-val">pgvector (Local PostgreSQL / Prisma)</span>
                            </div>
                            <div class="vdb-row">
                              <span class="vdb-label">Collection:</span>
                              <span class="vdb-val">genomic_embeddings (1536 dim)</span>
                            </div>
                          </div>
                        {:else if prop.type === 'number'}
                          <label class="control-label">Precision Threshold Slider:</label>
                          <div class="preview-slider-row">
                            <input type="range" min="0" max="1" step="0.05" value={prop.defaultValue || 0.85} disabled class="preview-slider" />
                            <span class="slider-num">{prop.defaultValue || 0.85}</span>
                          </div>
                        {:else if prop.type === 'boolean'}
                          <label class="control-label">Toggle Switch:</label>
                          <span class="preview-toggle">{prop.defaultValue ? '✔ Enabled [ON]' : '✖ Disabled [OFF]'}</span>
                        {:else}
                          <label class="control-label">Text Input:</label>
                          <input type="text" value={prop.defaultValue || ''} disabled class="preview-input" />
                        {/if}
                      </div>
                    </div>
                  {/each}
                </div>
              {:else}
                <div class="empty-reflection">
                  <span>No ECS properties declared yet. Declare public properties (e.g., <code>public PolicyLevel Policy &#123; get; set; &#125;</code>) to reflect.</span>
                </div>
              {/if}
            </div>

          <!-- TAB 3: MISE RUNTIME & PACKAGES -->
          {:else if activeReflectTab === 'mise'}
            <div class="mise-tab-pane">
              <div class="mise-header-info">
                <span>The system labels required runtime versions and package dependencies for <code>mise</code>:</span>
              </div>

              <div class="mise-overview-cards">
                <!-- Runtimes -->
                <div class="mise-card">
                  <span class="mise-card-title">⚙️ Mise Runtimes</span>
                  <div class="mise-tag-list">
                    {#each Object.entries(reflectionResult?.mise?.runtimes || { [compLanguage === 'csharp' ? 'dotnet' : compLanguage === 'python' ? 'python' : 'node']: 'default' }) as [rt, ver]}
                      <span class="mise-tag runtime">{rt}@{ver}</span>
                    {/each}
                  </div>
                </div>

                <!-- Dependencies -->
                <div class="mise-card">
                  <span class="mise-card-title">📦 Package Dependencies</span>
                  <div class="mise-tag-list">
                    {#if reflectionResult?.mise?.dependencies?.nuget?.length > 0}
                      {#each reflectionResult.mise.dependencies.nuget as nuget}
                        <span class="mise-tag nuget">nuget:{nuget}</span>
                      {/each}
                    {/if}
                    {#if reflectionResult?.mise?.dependencies?.pip?.length > 0}
                      {#each reflectionResult.mise.dependencies.pip as pip}
                        <span class="mise-tag pip">pip:{pip}</span>
                      {/each}
                    {/if}
                    {#if reflectionResult?.mise?.dependencies?.npm?.length > 0}
                      {#each reflectionResult.mise.dependencies.npm as npm}
                        <span class="mise-tag npm">npm:{npm}</span>
                      {/each}
                    {/if}
                    {#if !reflectionResult?.mise?.dependencies?.nuget?.length && !reflectionResult?.mise?.dependencies?.pip?.length && !reflectionResult?.mise?.dependencies?.npm?.length}
                      <span class="empty-tag">Standard core runtime library</span>
                    {/if}
                  </div>
                </div>
              </div>

              <!-- Mise.toml Preview -->
              <div class="mise-toml-preview">
                <div class="toml-header">
                  <span>Auto-Generated .mise.toml Descriptor</span>
                  <button
                    type="button"
                    class="btn-copy-toml"
                    on:click={() => {
                      if (reflectionResult?.mise?.miseToml) {
                        navigator.clipboard.writeText(reflectionResult.mise.miseToml);
                        alert('Copied .mise.toml configuration to clipboard!');
                      }
                    }}
                  >
                    📋 Copy .mise.toml
                  </button>
                </div>
                <pre class="toml-code">{reflectionResult?.mise?.miseToml || '# .mise.toml generated upon code reflection'}</pre>
              </div>
            </div>

          <!-- TAB 4: COMPILER DIAGNOSTICS -->
          {:else if activeReflectTab === 'diagnostics'}
            <div class="diagnostics-tab-pane">
              <pre class="results-pre font-mono">{testOutput}</pre>
            </div>
          {/if}
        </div>
      </div>
    </main>
  </div>
</div>

<style>
  .authoring-page {
    max-width: 1500px;
    margin: 0 auto;
    color: #e2e8f0;
  }

  .authoring-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid #1e293b;
    padding-bottom: 1rem;
    margin-bottom: 1.25rem;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .header-left h2 {
    margin: 0 0 0.25rem 0;
    font-size: 1.4rem;
    color: #f8fafc;
  }

  .header-sub {
    font-size: 0.8rem;
    color: #94a3b8;
  }

  .header-actions {
    display: flex;
    gap: 0.6rem;
  }

  .btn-test {
    background: #0284c7;
    color: white;
    border: none;
    border-radius: 0.375rem;
    padding: 0.5rem 0.85rem;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.15s ease;
  }

  .btn-test:hover:not(:disabled) { background: #0369a1; }

  .btn-publish {
    background: #10b981;
    color: #042f2e;
    border: none;
    border-radius: 0.375rem;
    padding: 0.5rem 0.85rem;
    font-size: 0.8rem;
    font-weight: 700;
    cursor: pointer;
    transition: background 0.15s ease;
  }

  .btn-publish:hover:not(:disabled) { background: #059669; color: white; }

  .btn-palette {
    background: #8b5cf6;
    color: white;
    border: none;
    border-radius: 0.375rem;
    padding: 0.5rem 0.85rem;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-palette:hover { background: #7c3aed; }

  .alert-success {
    background: rgba(16, 185, 129, 0.15);
    border: 1px solid rgba(16, 185, 129, 0.3);
    color: #34d399;
    padding: 0.75rem 1rem;
    border-radius: 0.375rem;
    font-size: 0.85rem;
    margin-bottom: 1rem;
  }

  .authoring-layout {
    display: grid;
    grid-template-columns: 310px 1fr;
    gap: 1.25rem;
    align-items: start;
  }

  /* Left Sidebar */
  .authoring-sidebar {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
  }

  .sidebar-section {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    padding: 1rem;
  }

  .sidebar-section h3 {
    margin: 0 0 0.35rem 0;
    font-size: 0.95rem;
    color: #f1f5f9;
  }

  .section-desc {
    margin: 0 0 0.75rem 0;
    font-size: 0.75rem;
    color: #64748b;
  }

  .template-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    max-height: 480px;
    overflow-y: auto;
  }

  .template-card {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.65rem;
    text-align: left;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .template-card:hover {
    border-color: #0284c7;
    background: #131e35;
  }

  .template-card.active {
    border-color: #38bdf8;
    background: rgba(14, 165, 233, 0.12);
  }

  .tmpl-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.3rem;
  }

  .tmpl-name {
    font-size: 0.82rem;
    font-weight: 600;
    color: #f1f5f9;
  }

  .tmpl-lang {
    font-size: 0.65rem;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
    font-weight: 700;
  }

  .tmpl-lang.csharp { background: #68217a; color: #f3e8ff; }
  .tmpl-lang.python { background: #1e3a8a; color: #93c5fd; }
  .tmpl-lang.javascript { background: #854d0e; color: #fef08a; }
  .tmpl-lang.bash { background: #374151; color: #f3f4f6; }

  .tmpl-desc {
    margin: 0;
    font-size: 0.72rem;
    color: #94a3b8;
    line-height: 1.35;
  }

  .module-dep-box {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .module-dep-box label {
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .module-dropdown {
    background: #090e17;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    padding: 0.45rem;
    border-radius: 0.375rem;
    font-size: 0.78rem;
  }

  .env-info {
    font-size: 0.7rem;
    color: #64748b;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
  }

  .env-info code {
    background: #0f172a;
    padding: 0.2rem 0.4rem;
    border-radius: 0.25rem;
    color: #38bdf8;
    font-size: 0.7rem;
  }

  /* Main Studio */
  .studio-main {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .config-bar {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
  }

  .config-row.main-props {
    display: flex;
    gap: 0.75rem;
  }

  .form-group {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .form-group.flex-1 { flex: 1; }
  .form-group.flex-2 { flex: 2; }

  .form-group label {
    font-size: 0.75rem;
    color: #94a3b8;
    font-weight: 500;
  }

  .input-text, .input-select {
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    border-radius: 0.375rem;
    padding: 0.45rem 0.65rem;
    font-size: 0.82rem;
  }

  .ports-config-container {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
    border-top: 1px solid #1e293b;
    padding-top: 0.75rem;
  }

  .ports-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.4rem;
  }

  .ports-title {
    font-size: 0.75rem;
    font-weight: 600;
    color: #cbd5e1;
  }

  .btn-port-add {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #38bdf8;
    font-size: 0.7rem;
    padding: 0.15rem 0.45rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .ports-list {
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .port-row {
    display: flex;
    gap: 0.4rem;
    align-items: center;
  }

  .port-name-input {
    flex: 2;
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    border-radius: 0.25rem;
    padding: 0.25rem 0.45rem;
    font-size: 0.75rem;
  }

  .port-type-select {
    flex: 1.5;
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #38bdf8;
    border-radius: 0.25rem;
    padding: 0.25rem;
    font-size: 0.75rem;
  }

  .btn-port-del {
    background: transparent;
    border: none;
    color: #ef4444;
    cursor: pointer;
    font-size: 0.75rem;
    padding: 0 0.3rem;
  }

  /* Monaco Section */
  .editor-section {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    overflow: hidden;
  }

  .editor-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem 0.85rem;
    background: #0b1120;
    border-bottom: 1px solid #1e293b;
    font-size: 0.78rem;
  }

  .editor-title {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .badge-middleware {
    background: rgba(16, 185, 129, 0.15);
    border: 1px solid rgba(16, 185, 129, 0.3);
    color: #34d399;
    font-size: 0.68rem;
    padding: 0.15rem 0.45rem;
    border-radius: 0.25rem;
    font-weight: 600;
  }

  .editor-tools {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .btn-sub-reflect {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #38bdf8;
    font-size: 0.72rem;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .syntax-badge {
    color: #64748b;
    font-size: 0.7rem;
  }

  /* Live Reflection & Mise Panel */
  .reflection-panel {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.5rem;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  .reflection-nav {
    display: flex;
    background: #07090e;
    border-bottom: 1px solid #1e293b;
    overflow-x: auto;
  }

  .reflect-tab-btn {
    background: transparent;
    border: none;
    border-bottom: 2px solid transparent;
    color: #94a3b8;
    font-size: 0.78rem;
    font-weight: 600;
    padding: 0.65rem 1rem;
    cursor: pointer;
    white-space: nowrap;
    transition: all 0.15s ease;
  }

  .reflect-tab-btn:hover {
    color: #f1f5f9;
  }

  .reflect-tab-btn.active {
    color: #38bdf8;
    border-bottom-color: #38bdf8;
    background: rgba(14, 165, 233, 0.06);
  }

  .reflection-tab-content {
    padding: 1rem;
    background: #090e17;
    min-height: 180px;
  }

  /* Tools Tab */
  .tools-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 0.75rem;
  }

  .tool-reflect-card {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  .tool-reflect-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .tool-name-tag {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    font-size: 0.82rem;
    color: #38bdf8;
    font-family: ui-monospace, monospace;
  }

  .badge-role {
    font-size: 0.65rem;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
    background: rgba(168, 85, 247, 0.15);
    color: #c084fc;
    border: 1px solid rgba(168, 85, 247, 0.3);
  }

  .tool-desc-text {
    margin: 0;
    font-size: 0.72rem;
    color: #94a3b8;
    line-height: 1.35;
  }

  .params-block {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    border-top: 1px solid rgba(255, 255, 255, 0.05);
    padding-top: 0.35rem;
  }

  .params-label {
    font-size: 0.68rem;
    color: #64748b;
  }

  .params-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 0.3rem;
  }

  .param-tag {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.2rem;
    padding: 0.1rem 0.35rem;
    font-size: 0.68rem;
    color: #cbd5e1;
  }

  .param-type {
    color: #a855f7;
  }

  /* Properties Tab */
  .prop-intro-hint {
    font-size: 0.75rem;
    color: #94a3b8;
    margin-bottom: 0.75rem;
  }

  .properties-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 0.75rem;
  }

  .prop-reflect-card {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
  }

  .prop-card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .prop-name {
    font-size: 0.82rem;
    color: #f1f5f9;
    font-family: ui-monospace, monospace;
  }

  .prop-type-badge {
    font-size: 0.65rem;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
    font-weight: 600;
  }

  .prop-type-badge.enum { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }
  .prop-type-badge.vectordb { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
  .prop-type-badge.number { background: rgba(14, 165, 233, 0.15); color: #38bdf8; border: 1px solid rgba(14, 165, 233, 0.3); }
  .prop-type-badge.boolean { background: rgba(168, 85, 247, 0.15); color: #c084fc; border: 1px solid rgba(168, 85, 247, 0.3); }
  .prop-type-badge.string { background: rgba(255, 255, 255, 0.05); color: #cbd5e1; border: 1px solid rgba(255, 255, 255, 0.1); }

  .prop-desc {
    font-size: 0.7rem;
    color: #64748b;
  }

  .prop-control-preview {
    background: #090e17;
    border: 1px solid rgba(255, 255, 255, 0.06);
    border-radius: 0.25rem;
    padding: 0.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .control-label {
    font-size: 0.68rem;
    color: #94a3b8;
  }

  .preview-select, .preview-input {
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    padding: 0.25rem 0.45rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
  }

  .preview-vectordb-box {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    font-size: 0.7rem;
    color: #cbd5e1;
  }

  .vdb-label { color: #64748b; margin-right: 0.3rem; }
  .vdb-val { color: #34d399; font-family: ui-monospace, monospace; }

  .preview-slider-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .preview-slider {
    flex: 1;
  }

  .slider-num {
    font-size: 0.75rem;
    color: #38bdf8;
    font-family: ui-monospace, monospace;
  }

  .preview-toggle {
    font-size: 0.72rem;
    color: #a855f7;
    font-weight: 600;
  }

  /* Mise Tab */
  .mise-tab-pane {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .mise-header-info {
    font-size: 0.78rem;
    color: #94a3b8;
  }

  .mise-overview-cards {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
  }

  .mise-card {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .mise-card-title {
    font-size: 0.8rem;
    font-weight: 600;
    color: #f1f5f9;
  }

  .mise-tag-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
  }

  .mise-tag {
    font-size: 0.72rem;
    padding: 0.15rem 0.45rem;
    border-radius: 0.25rem;
    font-family: ui-monospace, monospace;
  }

  .mise-tag.runtime {
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(14, 165, 233, 0.3);
    color: #38bdf8;
  }

  .mise-tag.nuget {
    background: rgba(104, 33, 122, 0.25);
    border: 1px solid rgba(104, 33, 122, 0.5);
    color: #f3e8ff;
  }

  .mise-tag.pip {
    background: rgba(30, 58, 138, 0.25);
    border: 1px solid rgba(30, 58, 138, 0.5);
    color: #93c5fd;
  }

  .mise-tag.npm {
    background: rgba(234, 88, 12, 0.2);
    border: 1px solid rgba(234, 88, 12, 0.4);
    color: #fed7aa;
  }

  .empty-tag, .empty-param-text {
    font-size: 0.7rem;
    color: #64748b;
  }

  .mise-toml-preview {
    background: #0f172a;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    overflow: hidden;
  }

  .toml-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.45rem 0.75rem;
    background: #07090e;
    border-bottom: 1px solid #1e293b;
    font-size: 0.75rem;
    color: #cbd5e1;
  }

  .btn-copy-toml {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #38bdf8;
    font-size: 0.7rem;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .toml-code {
    margin: 0;
    padding: 0.75rem;
    font-size: 0.75rem;
    color: #34d399;
    font-family: ui-monospace, monospace;
    white-space: pre-wrap;
    max-height: 200px;
    overflow-y: auto;
  }

  .empty-reflection {
    padding: 1.5rem;
    text-align: center;
    color: #64748b;
    font-size: 0.8rem;
  }

  .empty-reflection code {
    color: #38bdf8;
    background: #0f172a;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
  }

  .results-pre {
    margin: 0;
    padding: 0.75rem;
    background: #07090e;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    font-size: 0.75rem;
    color: #38bdf8;
    max-height: 250px;
    overflow-y: auto;
    white-space: pre-wrap;
  }
</style>
"""

with open(authoring_path, 'w', encoding='utf-8') as f:
    f.write(new_page_code)
print('Successfully rewritten authoring/+page.svelte with templates, reflection, and mise tabs!')
