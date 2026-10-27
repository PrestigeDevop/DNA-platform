# DeNovoAutomata — Architecture & System Requirements Guide

> **Platform Classification**: Enterprise low-code/no-code bioinformatic platform with .NET Core workflow engine, SvelteKit web studio, embedded Polyglot Kernel, genuine Model Context Protocol (MCP) server integrated with `mise`, agentic tool exposition, and cross-platform CLI SDK.

---

## 1. Executive Overview & System Topology

**DeNovoAutomata** unites high-performance compiled .NET 8/9 execution with the scientific computing ecosystem of Python and modern web visualization. It enables bioinformaticians, clinical researchers, and software engineers to:
- Visually compose Directed Acyclic Graph (DAG) computational biology pipelines on a drag-and-drop WYSIWYG canvas.
- Act as a **native Model Context Protocol (MCP) Server** integrated with `mise` (`mise mcp`) for dynamic dependency isolation (NuGet, npm, pip).
- Execute multi-language code cells in-process through an **embedded .NET Interactive Polyglot Kernel** (C#, F#, Python via CSnakes, JavaScript, PowerShell).
- Edit code nodes with syntax highlighting, diagnostics, and upstream variable autocompletion via a **WebSocket JSON-RPC Language Server Protocol (LSP)** bridge.
- Deploy autonomous AI agents governed by the **AG-UI Protocol** with real-time reasoning traces (`THINKING_DELTA`) and Human-in-the-Loop (`APPROVAL_REQUESTED`) interrupts.
- Package, publish, and install modules through a **JSON Marketplace** (`dna-module.json`) that unpacks UI inspectors directly into the workflow canvas.
- Drive development from a cross-platform animated **CLI Tooling SDK** (`scripts/dna.js`) with an interactive menu and dynamic Python execution engine.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                 DeNovoAutomata Frontend                                  │
│  SvelteKit 2 Web Studio (:3000)                                                         │
│  ├── WYSIWYG Workflow Canvas & Node Graph       ├── /studio Playground (3-Subtab Testbed)│
│  ├── Monaco Editor + WebSocket JSON-RPC LSP     ├── AG-UI Human-in-the-Loop Modal      │
│  └── JSON Marketplace & Component Inspector     └── De Novo Workflow Demonstration Hub  │
└───────────────────────────┬─────────────────────────────────┬───────────────────────────┘
                            │ HTTP / SSE / REST               │ WebSocket (JSON-RPC)
                            ▼                                 ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                           Service Gateway & Runtime Protocols                           │
│                                                                                         │
│  ASP.NET Core 8/9 Web API (:5254)             Language Server Protocol (LSP) Bridge     │
│  ├── Minimal APIs & Route Dispatchers         ├── Roslyn C# Diagnostics & Autocomplete  │
│  ├── Skill Registry & Module Manager          ├── Monaco JSON-RPC over WebSockets       │
│  └── Runtime Environment Introspection        └── Upstream Variable Schema Injection    │
│                                                                                         │
│  AG-UI Protocol Server (:5000)                Genuine mise MCP Server (stdio / :3000)   │
│  ├── SSE Streaming (Cognitive Traces)         ├── Model Context Protocol JSON-RPC 2.0   │
│  ├── HITL Interrupts & Resumption             ├── Dynamic Language Toolchain Sandbox    │
│  └── JSON Patch (RFC 6902) State Deltas       └── mise install / isolate (pip/npm/nuget)│
└───────────────────────────┬─────────────────────────────────┬───────────────────────────┘
                            │                                 │
                            └────────────────┬────────────────┘
                                             ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                           Unified .NET Core Execution Engine                            │
│                 src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework                  │
│                                                                                         │
│   WorkflowOrchestrator   │   Embedded Polyglot Kernel   │   Agent & Tool Registry       │
│   ├── Topological Sort   │   ├── .NET Interactive       │   ├── Copilot Local ONNX      │
│   ├── Cycle Detection    │   ├── C# / F# In-Process     │   ├── [McpTool] Discovery     │
│   ├── State Delta Engine │   ├── CSnakes Python C-API   │   ├── Granular Permissions    │
│   └── Node Execution     │   └── JS / PowerShell cells  │   └── Agent Context Injection │
└───────────────────────────┬─────────────────────────────────┬───────────────────────────┘
                            │                                 │
                            ▼                                 ▼
┌───────────────────────────────────────────┐ ┌───────────────────────────────────────────┐
│            Core Polyglot Bridge           │ │            Skill & Tool Modules           │
│        src/01_Core/DNAPlatform.Core       │ │         src/06_Skills & Marketplace       │
│                                           │ │                                           │
│   - CSnakes Python-C# Interop Bridge      │ │   - Needleman-Wunsch Aligner              │
│   - Zero-Copy Biological Marshaling       │ │   - C# High-Precision Timestamp Provider  │
│   - DNA/RNA/Protein Sequence Metrics      │ │   - IUPAC Genetic Sequence Validator      │
│   - GC Skew & Reading Frame Analysis      │ │   - Installed JSON Marketplace Packages   │
└───────────────────────────────────────────┘ └───────────────────────────────────────────┘
```

---

## 2. Core Architectural Components & Requirements

### 2.1 Backend: .NET Core Workflow Engine & Native MCP Server

#### Workflow & Graph Core
The workflow engine evaluates Directed Acyclic Graphs (DAGs) serialized as JSON schemas:
- **Topological Sorting**: Kahn's algorithm resolves execution sequences and validates that the graph is strictly acyclic.
- **Node Execution Isolation**: Each node executes inside a sandboxed scope receiving upstream outputs mapped through explicit connections (`sourceNodeId`, `targetNodeId`, `outputKey`, `inputKey`).
- **State Delta Engine**: Tracks incremental state mutations via RFC 6902 JSON Patch (`STATE_DELTA`) across workflow steps.

#### Real MCP Server Implementation
The platform implements the **Model Context Protocol (MCP)** specification (2024-11-05):
- **Transport**: Supports both `stdio` (for CLI/Copilot desktop integration) and HTTP/SSE (`/api/mcp/sse`) for web clients.
- **Protocol Capabilities**:
  - `tools/list`: Enumerates all active native tools and marketplace modules.
  - `tools/call`: Executes registered C#, Python, or shell tools with strict schema validation.
  - `prompts/list` & `prompts/get`: Provides parameterized bioinformatic reasoning templates.
  - `resources/list` & `resources/read`: Streams workspace artifacts (e.g., FASTA files, alignment reports, LIMS specs).

#### Developer API & SDK Attributes
Developers annotate C# classes and methods with platform attributes for automatic MCP and UI exposure:

```csharp
using System.ComponentModel;
using DNAPlatform.Core.Attributes;

namespace DNAPlatform.Modules.Bioinformatics;

[McpServer("dna-genomics", Version = "1.0.0")]
[Description("Genomics analysis and sequence manipulation toolset.")]
public class GenomicsModule
{
    [McpTool("calculate_gc_skew", "Calculates GC content and GC skew (G - C)/(G + C) for a DNA sequence.")]
    public static GcSkewResult CalculateGcSkew(
        [McpParameter("Nucleotide sequence (A, C, G, T, N)", Required = true)] string sequence,
        [McpParameter("Sliding window size in base pairs", Required = false)] int windowSize = 100)
    {
        int g = 0, c = 0;
        foreach (char b in sequence.ToUpperInvariant())
        {
            if (b == 'G') g++;
            else if (b == 'C') c++;
        }
        double skew = (g + c) == 0 ? 0.0 : (double)(g - c) / (g + c);
        double gcPercent = sequence.Length == 0 ? 0.0 : ((double)(g + c) / sequence.Length) * 100.0;

        return new GcSkewResult(sequence.Length, g, c, gcPercent, Math.Round(skew, 4));
    }
}

public record GcSkewResult(int Length, int GCount, int CCount, double GcPercentage, double GcSkew);
```

#### Embedded Polyglot Kernel Server (.NET Interactive & CSnakes)
- **Engine**: In-process `.NET Interactive` composite kernel hosting C# (`csharp`), F# (`fsharp`), and PowerShell.
- **Python Runtime**: Powered by **CSnakes**, embedding Python 3.12 directly into the .NET process with zero-overhead memory pointers and no inter-process serialization latency.
- **Sandboxing**: Restricts file system write locations to the `workspace/` directory and sets timeout caps per cell execution (default: 30,000ms).

---

### 2.2 SvelteKit Frontend & Web Editor Environment

#### WYSIWYG Workflow Canvas
- **Visual Graph**: SVG-based dynamic connection lines with cubic Bézier curves, port snapping, drag-and-drop node positioning, and mini-map navigation.
- **Per-Node State Reflection**: Live visual state chips (`Pending`, `Running`, `Completed`, `Failed`, `Approval Requested`) with animated data-flow pulses.
- **Inspector Panel**: Unity-inspired component inspector automatically rendering controls based on node metadata:
  - Enums $\rightarrow$ `<select>` dropdowns.
  - Number ranges $\rightarrow$ Sliders with step increments.
  - Boolean flags $\rightarrow$ Toggle switches.
  - Code expressions $\rightarrow$ Monaco editor with LSP autocomplete.

#### Monaco Editor with WebSocket JSON-RPC LSP Bridge
- Code editor nodes hook into an LSP (Language Server Protocol) server over WebSockets (`ws://127.0.0.1:3000/api/lsp` or backend `:5254`).
- **Capabilities**:
  - Live syntax diagnostics and squiggly error markers for C# (Roslyn) and Python (Pyright).
  - Contextual autocompletion injecting upstream node data schemas into the global scope:
    ```javascript
    // Autocompletion provides live upstream properties:
    inputs.sequence         // [DNA_Sequence] Upstream FASTA
    inputs.gcPercentage     // [Float] Calculated GC score
    inputs.selectedSpacer   // [String] 20-nt CRISPR guide RNA
    ```

---

### 2.3 Package Management & 'mise mcp' Dependency Resolution

#### Mise MCP Integration
The platform delegates language runtime provisioning to `mise` (`https://mise.jdx.dev`):
- The backend communicates with `mcp-servers/mise-mcp/index.js` over stdio JSON-RPC.
- When a workflow node or marketplace package requires a specific runtime (e.g., Python 3.12, .NET 8, Node.js 20, Rust), `mise` dynamically resolves, installs, and isolates the toolchain in `.mise.toml` virtual environments.

#### JSON Marketplace Module Packaging Standard
Modules are published as `.tar.gz` packages containing a `dna-module.json` manifest:

```json
{
  "$schema": "https://dnaplatform.org/schemas/module-v1.json",
  "name": "crispr-cas9-optimizer",
  "version": "1.2.0",
  "displayName": "CRISPR-Cas9 Off-Target & Oligo Synthesis",
  "description": "Scans genomic sequences for PAM motifs, evaluates off-target penalties via CSnakes, and synthesizes Golden Gate oligos.",
  "author": "DNA Platform Core Team",
  "category": "Genomics",
  "icon": "🧬",
  "runtimes": {
    "dotnet": ">=8.0.0",
    "python": ">=3.11.0"
  },
  "dependencies": {
    "nuget": [
      { "package": "Bio.Core", "version": "3.0.0" }
    ],
    "pip": [
      { "package": "biopython", "version": ">=1.83" },
      { "package": "numpy", "version": ">=1.26.0" }
    ],
    "npm": []
  },
  "uiComponent": {
    "nodeType": "CrisprCas9Node",
    "inspectorFields": [
      { "name": "pamMotif", "type": "enum", "label": "PAM Motif", "options": ["NGG", "NAG", "NNGRRT"], "default": "NGG" },
      { "name": "maxOffTargetRisk", "type": "float", "label": "Max Off-Target Risk", "min": 0.01, "max": 0.1, "default": 0.05 }
    ]
  },
  "tools": [
    {
      "name": "crispr_scan_spacers",
      "description": "Extracts 20-nt guide RNA spacers matching specified PAM motif.",
      "entryPoint": "CrisprModule.ScanSpacers",
      "language": "csharp"
    }
  ]
}
```

#### Automated Installation Pipeline
1. **Download & Verification**: Downloads package archive, verifies SHA-256 checksum, and performs AST security scans.
2. **Mise Toolchain Provisioning**: Automatically runs `mise install` for requested language runtimes.
3. **Dependency Restoration**: Invokes package managers (`nuget restore`, `pip install -r requirements.txt`, `npm install --production`).
4. **Palette & Tool Registry Injection**: Registers the new node type into the SvelteKit Workflow Designer palette and the Agent MCP Tool Registry without restarting the application server.

---

### 2.4 Agentic Tool Exposition & The `/studio` Route

#### AI Agent Integration & User Grant Access
- Any installed module annotated with `[McpTool]` is registered into the **Agent Tool Registry**.
- When an administrator or user grants an AI Agent access to a module:
  - The tool's JSON schema (name, description, input parameters) is injected into the Agent's system context.
  - LLMs (Local ONNX Copilot, Phi-3.5, or remote models) can invoke backend methods via the standard MCP `tools/call` JSON-RPC protocol.
  - If the module requires sensitive operations, the **AG-UI Protocol** halts execution with an `APPROVAL_REQUESTED` interrupt for operator verification.

#### The Studio Route (`/studio`)
The dedicated playground and developer testing ground provides 3 sub-tabs:
1. **Polyglot Kernel**: Live multi-language scratchpad executing C#, Python (CSnakes), and Node.js cells with instant stdout capture.
2. **Smart Component Authoring**: Real-time code compiler extracting `[Tool]` attributes and rendering live Unity-style inspectors.
3. **Real-Time Agent Interaction**: Interactive agent chat panel testing tool call generation, argument filling, MCP invocation, and AG-UI HITL interrupts.

---

### 2.5 Cross-Platform CLI Tooling SDK (`scripts/dna.js`)

The CLI serves as the unified entry point for developers and automation pipelines:
- **Animated Startup**: Features an animated ASCII DNA double-helix banner with color spectrum transitions.
- **Interactive Menu**:
  ```
  ╔════════════════════════════════════════════════════════════════════════════╗
  ║                     🧬 DNA PLATFORM UNIFIED CLI & TUI                     ║
  ║           Polyglot Bioinformatics, Agent Framework & AG-UI Hosting         ║
  ╚════════════════════════════════════════════════════════════════════════════╝

  Main Menu:
    [1] 🚀 Start All Servers (DevUI + API + AG-UI) — run-all
    [2] 🧪 Run Automated SmokeTest
    [3] ⚙️ Bootstrap Toolchain & SQLite Database
    [4] 🧹 Clean Workspace Artifacts
    [5] 📡 Probe Polyglot Environment (CSnakes & .NET)
    [6] ⚡ Test AG-UI Protocol Stream
    [7] 📓 Launch Microsoft .NET Interactive REPL (port 8888)
    [8] 🧰 Run Mise Toolchain MCP Server / Doctor
    [9] 🐍 Execute Dynamic Python Code & CSnakes Annotations
    [0] 🚪 Exit
  ```
- **Dynamic Python Execution Engine (`dna python` / `dna py`)**:
  - Provides quick bioinformatics calculations (reverse complement, GC skew, PAM extraction, codon translation).
  - Evaluates arbitrary inline Python statements with instant timing and output capture.
  - Generates MCP tool annotations for Python classes and functions.

---

## 3. Repository & Folder Structure

```
.
├── src/
│   ├── 01_Core/
│   │   └── DNAPlatform.Core/              # CSnakes Polyglot Python-C# bridge & biological sequence types
│   ├── 03_AgentWorkflowPatterns/
│   │   └── DNAPlatform.AgentFramework/    # The SINGLE workflow & agent engine (Orchestrator, NodeExecutor)
│   ├── 05_DevUI/
│   │   ├── DNAPlatform.DevUI.API/         # ASP.NET Core 8 Web API (Port 5254)
│   │   ├── DNAPlatform.DevUI.AGUI/        # AG-UI protocol hosting server (Port 5000)
│   │   └── DNAPlatform.DevUI.Web/         # SvelteKit 2 frontend & Prisma SQLite persistence (Port 3000)
│   │       ├── src/lib/components/        # WorkflowDesigner, WorkflowDemonstration, MonacoEditor
│   │       ├── src/lib/data/              # Client-safe demo workflows & schemas
│   │       ├── src/lib/server/            # Prisma client, seedData, and workflow orchestrator
│   │       └── src/routes/
│   │           ├── studio/                # /studio Route (Polyglot Kernel, Authoring, Agent Chat)
│   │           ├── workflows/             # /workflows Route (Interactive Demonstration Hub & Library)
│   │           ├── kernel/                # /kernel Route (.NET Interactive & CSnakes Playground)
│   │           └── agents/                # /agents Route (Agent Registry & Copilot ONNX)
│   ├── 06_Skills/
│   │   ├── DNAPlatform.Skills/            # Skill catalog (Needleman-Wunsch, C# Clock, IUPAC Validator)
│   │   └── DNAPlatform.Skills.Core/       # Core skill contracts & base interfaces
│   ├── 07_CLI/
│   │   └── DNAPlatform.Cli/               # Native C# Terminal.Gui TUI & HTTP CLI client
│   └── 09_Tests/
│       └── DNAPlatform.Tests/             # xUnit test suite for the workflow orchestrator
├── mcp-servers/
│   └── mise-mcp/                          # Genuine mise MCP Server (stdio JSON-RPC)
├── scripts/
│   ├── 1-bootstrap.mjs                    # Script 1: First-time toolchain install & Prisma database sync
│   ├── 2-test-dev.mjs                     # Script 2: Agentic E2E test suite with pre-push artifact clean
│   ├── 3-denovo-automata.mjs              # Script 3: DeNovoAutomata lightweight animated runner with TUI
│   └── dna.js                             # Unified Cross-Platform CLI Tooling SDK with dynamic Python menu
├── docs/
│   ├── architecture.md                    # This document: architecture, system requirements, schemas
│   ├── development.md                     # Consolidated development logs, task lists, and roadmap
│   └── tools/
│       └── mcp-mise-interactive-tooling.md# MCP server guide, mise toolchain, and troubleshooting
├── metadata.json                          # Applet metadata and capabilities
├── package.json                           # Root workspace configuration and npm scripts
└── README.md                              # User-facing platform introduction
```

---

## 4. Operational Guidelines & Gotchas

1. **Svelte 4 Template Non-Null & Type Assertions**:
   - Svelte 4 template expressions `{...}` parse standard ECMAScript only. TypeScript assertions (`as any`, `testingAgent!`) cause fatal build errors.
   - **Rule**: Perform all type checking and null checks inside `<script lang="ts">`.
2. **SvelteKit Module Boundaries (`$lib/server/*`)**:
   - Files under `src/lib/server/` cannot be imported into client components. Shared definitions must reside under `src/lib/data/` or `src/lib/types/`.
3. **Single Execution Engine Mandate**:
   - `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework` is the **single** orchestrator across the repository. Never create secondary execution double engines.
4. **Port Allocations**:
   - `3000`: SvelteKit Web UI Dev Server.
   - `5254`: ASP.NET Core 8 Web API.
   - `5000`: AG-UI SSE Protocol Server.
   - `8888`: Microsoft .NET Interactive Jupyter Server.
