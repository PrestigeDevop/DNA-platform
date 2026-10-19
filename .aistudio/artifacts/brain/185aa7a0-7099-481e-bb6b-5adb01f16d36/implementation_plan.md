# Logic Component Studio, Middleware Architecture, Dynamic ECS Properties & Interactive Agent Console

Build an end-to-end authoring and execution architecture for custom Logic Components and Microsoft Agent Framework middleware, with automatic `mise` runtime requirement detection, code-reflected ECS properties in the Workflow Designer, and an interactive multi-turn chat test console with live tool calling in the Agents page.

---

## User Review & Critical Decisions

> [!IMPORTANT]
> The following architectural decisions were confirmed in Phase 1 and are locked for implementation:

- **Tool Annotations & Runtime Packaging**: Use native language decorators and attributes (`[Tool]` in C#, `@tool` in Python, `@tool` / JSDoc in JS/TS). The authoring studio will parse these annotations to extract AG-UI / MCP tool schemas and generate `mise` runtime requirements (`dotnet@8.0`, `python@3.11`, `node@20`, plus pip, nuget, and npm dependencies) automatically when saving or publishing components.
- **Custom ECS Node Property Reflection**: Implement a **Hybrid** architecture: automatically reflect public properties and attributes from component code into typed schemas (e.g., `enum` dropdowns, Vector DB endpoints, numeric sliders, booleans), while providing an interactive property manager in the Workflow Node Inspector so users can customize widgets directly.
- **Agent Test Console**: Transform the single-shot test drawer on `/agents` into an **Interactive Multi-Turn Chat Stream** featuring conversational message bubbles, expandable tool call cards with real arguments and outputs, duration metrics, and session history.

---

## 1. Overview & Core Concept

### What It Delivers
1. **Logic Component & Middleware Authoring Studio (`/authoring`)**:
   - Author custom components, pipeline tools, and agent middleware using Microsoft Agent Framework conventions (`IAgentMiddleware` / `AgentMiddleware`).
   - Multi-language support: **C# (.NET 8)**, **Python 3 (CSnakes Polyglot Bridge)**, and **JavaScript (Node.js)**.
   - Code reflection engine that automatically scans source code for `[Tool]`, `@tool`, `@mise_runtime`, `[Category]`, and typed properties.
   - Auto-generated `mise` environment configuration (`mise.toml` / runtime tags) and AG-UI MCP tool descriptors.
   - Publish to local component marketplace or export directly into the Workflow Designer palette.

2. **Custom ECS Node Property Inspector (`/workflows`)**:
   - Nodes bound to custom Logic Components automatically render tailored UI controls based on reflected or user-defined ECS properties:
     - **Enum / Choice**: Renders select dropdowns with discrete allowed values.
     - **Vector DB / Store**: Renders vector database connection selector (e.g. Chroma, Qdrant, pgvector) with collection name and embedding dimension inputs.
     - **Float / Int Thresholds**: Renders precision sliders alongside numeric inputs.
     - **Boolean Gates**: Renders reactive toggle switches.
     - **Secret / Key**: Renders masked inputs.

3. **Interactive Multi-Turn Agent Chat Console (`/agents`)**:
   - Real-time conversational interface for Microsoft Copilot Bioinformatician and Data Analysis Specialist.
   - Live tool calling integration showing real-time execution cards:
     - Tool name, icon, and AG-UI role (`worker`, `evaluator`, `system`).
     - Expandable arguments payload.
     - Real-time output display (sequence alignments, reverse complements, GC stats, workspace file operations, C# DateTime).
     - Execution latency in milliseconds.
   - Multi-turn conversation history with session reset and quick prompt suggestions.

---

## 2. User Experience & Visual Design

### Visual Identity & Theme
- **Color Discipline (60-30-10)**:
  - 60% neutral canvas: Deep obsidian slate (`#07090e` / `#0a0f1d`).
  - 30% structural surfaces: Crisp hairline bordered panels (`#1e293b` borders, `#0f172a` card surfaces).
  - 10% precision telemetry accents: Cyan (`#0284c7`), Emerald (`#10b981`), Amber (`#f59e0b`), Purple (`#a855f7`).
- **Typography**: Clean display font paired with `font-mono tabular-nums` for code, coordinates, timestamps, and tool payload JSON.
- **Zero-Pill Restraint**: Metadata rendered as unboxed typography with typographic separators (`·` and `/`); buttons styled as tactile segmented controls.

### Key Workflows
1. **Authoring & Publishing Middleware**:
   - User selects a starter template (e.g., C# Agent Middleware, CSnakes BioPython Tool, Vector DB Retriever, or JS Stream Normalizer).
   - User types code; the **Annotation Parser** live-detects tools, parameters, and required mise runtimes in an interactive sidebar summary.
   - User clicks **"Validate & Publish to Marketplace"** or **"Add to Workflow Palette"**.
2. **Configuring ECS Node Properties in Flow Designer**:
   - User adds or selects a Logic Component node on the Flow Canvas.
   - The Node Inspector reflects the component's properties: an Enum becomes a dropdown, a Vector DB property displays endpoint and collection fields, and threshold numbers render sliders.
   - Values synchronize immediately with `node.config.properties` and evaluate inside expressions (`this.properties`).
3. **Interactive Agent Testing**:
   - User opens the **Test Chat Console** on `/agents`.
   - User enters a prompt (e.g. `"Align sample sequence ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC against reference and report current UTC timestamp"`).
   - The agent streams its response, rendering expandable cards for each invoked tool (`csharp-runtime/get_current_time`, `sequence-aligner`).
   - User can follow up in the same thread with subsequent prompts.

---

## 3. Key Product Decisions & Trade-Offs

### Decision 1: Language-Native Tool Annotations vs. Separate Manifest
- **Chosen Approach**: Use native language annotations (`[Tool]` in C#, `@tool` in Python, `@tool` JSDoc/decorator in JS) combined with an automated parser that derives the tool manifest on the fly.
- **Why**: Eliminates synchronization drift between code and metadata. Developers write standard idiom-first code, and the platform extracts schemas, parameter types, and `mise` runtime requirements automatically.
- **Alternative Considered**: Requiring a separate `manifest.json` file—rejected because it creates duplicate boilerplate and maintenance overhead.

### Decision 2: Hybrid ECS Property Architecture
- **Chosen Approach**: Auto-reflect public properties and attributes from component code as the baseline schema, while giving users an interactive property manager to add, edit, or adjust UI control types (Enum, VectorDB, Slider, Text).
- **Why**: Provides instant out-of-the-box convenience for written code while giving workflow designers full visual flexibility to adapt properties for domain workflows.

### Decision 3: Multi-Turn In-Memory Chat Session for Agent Testing
- **Chosen Approach**: Maintain active conversation history in the Svelte store and backend agent session context, passing message history into Copilot ONNX and tool reasoners.
- **Why**: Allows true multi-turn iterative reasoning where the agent remembers prior tool outputs and user refinements.

---

## 4. Technical Architecture & Data Strategy

```
┌───────────────────────────────────────────────────────────────────────────┐
│                     DNA Platform Full-Stack Topology                      │
└───────────────────────────────────────────────────────────────────────────┘
                                     │
           ┌─────────────────────────┴─────────────────────────┐
           ▼                                                   ▼
┌──────────────────────────────────────┐   ┌────────────────────────────────┐
│   Authoring & Component Studio       │   │  Interactive Agent Console     │
│   (/authoring)                       │   │  (/agents)                     │
│  • Monaco Code Editor (C#, Py, JS)   │   │ • Multi-turn Chat Thread       │
│  • Annotation & [Tool] Parser        │   │ • Expandable Tool Call Cards   │
│  • Mise Runtime Detector (.mise.toml)│   │ • Live Copilot ONNX Reasoning  │
│  • Middleware Pipeline Descriptors   │   │ • Real Execution Timing (ms)   │
└──────────────────┬───────────────────┘   └───────────────┬────────────────┘
                   │                                       │
                   ▼                                       ▼
┌───────────────────────────────────────────────────────────────────────────┐
│              SvelteKit API Gateway & Server Services                     │
│  • /api/authoring/reflect   : Parses code, extracts tools, mise runtimes  │
│  • /api/authoring/publish   : Registers custom snippet to DB & palette   │
│  • /api/agents/[id]/chat    : Multi-turn streaming chat & tool reasoner   │
│  • /api/mcp/toggle          : Interactive runtime tool & module toggles   │
└──────────────────┬────────────────────────────────────────────────────────┘
                   │
                   ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                 Workflow Canvas & Dynamic ECS Inspector                   │
│                 (/workflows)                                              │
│  • Reflected ECS Custom Property Widgets:                                 │
│    - Enum Dropdowns (`select`)                                            │
│    - Vector DB Selector (`ChromaDB`, `Qdrant`, `pgvector`)               │
│    - Numeric Sliders & Min/Max Bounds                                     │
│    - Boolean Switches & Secret Fields                                     │
│  • Single Execution Engine (`orchestrator.ts`)                            │
│  • In-Process CSnakes Python & Roslyn C# Runtimes                         │
└───────────────────────────────────────────────────────────────────────────┘
```

### Component Implementation Mapping
1. `src/lib/server/authoring/parser.ts`:
   - Scans code for tool annotations (`[Tool]`, `@tool`, `@mise_runtime`, `[Category]`, `[Description]`, properties).
   - Generates AG-UI / MCP tool definitions and `mise` runtime package tags.
2. `src/routes/api/authoring/reflect/+server.ts`:
   - Endpoint receiving code and language, returning parsed tools, ECS property definitions, and mise dependencies.
3. `src/routes/authoring/+page.svelte`:
   - Enhanced UI with Starter Middleware templates, Code Editor, Real-Time Reflection Panel, and "Publish to Palette" action.
4. `src/lib/components/WorkflowDesigner.svelte`:
   - Custom ECS property renderer supporting Enum, VectorDB, Range/Slider, Boolean, and Text input types.
5. `src/routes/api/agents/[id]/chat/+server.ts`:
   - Multi-turn conversation and tool execution endpoint.
6. `src/routes/agents/+page.svelte`:
   - Interactive chat drawer with real-time message bubbles, expandable tool call cards, and quick suggestion chips.
