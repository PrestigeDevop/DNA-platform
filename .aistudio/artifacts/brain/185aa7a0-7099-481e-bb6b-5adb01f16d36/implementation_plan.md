# Dedicated Polyglot Kernel, Smart Component Studio & Agent Interaction Hub

Comprehensive implementation plan to migrate the Polyglot Kernel and Component Authoring from the general Skills page to a dedicated `/kernel` studio with three integrated tabs, real C# backend execution with Unity-style public property reflection, visual node-compatible component authoring, and real-time Agent testing with AG-UI state management.

---

## User Review & Critical Decisions

> [!IMPORTANT]
> The following user preferences were confirmed during Phase 1:
> - **Navigation Route**: Dedicated `/kernel` page featuring 3 sub-tabs: **Polyglot Kernel**, **Smart Component Authoring**, and **Real-Time Agent Interaction**.
> - **C# Execution Engine**: Real backend code evaluation with automatic extraction of public fields/properties and `[Tool]` / `[Description]` attributes to populate smart component inspector interfaces (similar to Unity Inspector serialization).
> - **Workflow Canvas Integration**: Double-click node inspector on the workflow canvas enabling live customization and inline JavaScript/Python expression evaluation directly in DAG nodes, coupled with AG-UI state management.

---

## 1. Overview & Core Concept

### What It Delivers
1. **Dedicated `/kernel` Studio**:
   - Replaces the cluttered sections previously embedded in the Skills catalog with a cohesive, full-width development environment.
   - **Tab 1: Polyglot Kernel**: Live interactive multi-language cells (C#, Python via CSnakes, JavaScript, Shell/Bash) with real backend evaluation, syntax autocompletion, execution timers, and formatted stdout/stderr.
   - **Tab 2: Smart Component Authoring**: Visual component builder compatible with the node ecosystem. Categories include `{ State Machine, Shell Tools, Modules, MCP Server, Agent as MCP }`. Auto-detects public properties (e.g., `public double Threshold { get; set; } = 0.85;`) and exposes them as interactive sliders, toggles, and inputs.
   - **Tab 3: Real-Time Agent Lab**: Interactive agent testing environment aligned with the Microsoft Agent Framework Provider Support Matrix (hosting, function tools, code act, and local MCP). Features live AG-UI streaming, cognitive reasoning traces (`THINKING_DELTA`), and Human-in-the-Loop (`APPROVAL_REQUESTED`) approval cards.
2. **Workflow Canvas Double-Click & Inline Expressions**:
   - Double-clicking any workflow node opens the smart component configuration drawer.
   - Renames "Unity State Machine" to a unified, standard "State Machine" node type, synchronized with AG-UI JSON Patch state management (`STATE_DELTA`).
   - Enables n8n-style inline expressions (e.g., `{{ $input.sequence.length > 50 ? 'Long' : 'Short' }}`) evaluated in real time.

---

## 2. User Experience & Visual Design

### Key User Flows

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ TOP NAVIGATION:  [ Dashboard ]  [ Workflows ]  [ Kernel & Studio ★ ]  [ Skills ] ...  │
└────────────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ /kernel  ─── Tabs: [ 💻 Polyglot Kernel ]  [ ✍️ Component Studio ]  [ 🤖 Agent Lab ]  │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ TAB 1: Polyglot Kernel                                                                 │
│ ┌────────────────────────────────────────────────────────────────────────────────────┐ │
│ │ Language Selector: [ C# (.NET 8) ▼ ]  [ Run Cell ▶ ]  [ + Add Cell ]  [ Clear ]    │ │
│ │ ┌──────────────────────────────────────────────────┐ ┌───────────────────────────┐ │ │
│ │ │ // Real C# Script with Preloaded DNA Types       │ │ Output (Real Backend / 32ms)│ │ │
│ │ │ var seq = new DnaSequence("ATGCGATCGATCG");     │ │ Length: 13                  │ │ │
│ │ │ Console.WriteLine($"GC: {seq.CalculateGc():P2}");│ │ GC: 53.85%                 │ │ │
│ │ └──────────────────────────────────────────────────┘ └───────────────────────────┘ │ │
│ └────────────────────────────────────────────────────────────────────────────────────┘ │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ TAB 2: Smart Component Authoring (Node Compatible)                                     │
│ ┌──────────────────────────────────────────────────┬─────────────────────────────────┐ │
│ │ Category: [ State Machine ▼ ]                    │ Live Inspector Preview (Unity)  │ │
│ │ Code Editor: [ C# / Python / JS ]                │ ┌─────────────────────────────┐ │ │
│ │ public class DnaFilterComponent : SmartComponent │ │ Public Properties:          │ │ │
│ │ {                                                │ │ • Threshold: [ 0.75 ─────● ]│ │ │
│ │    [Description("Max GC fraction allowed")]      │ │ • MaxMutations: [ 3 ]       │ │ │
│ │    public double GcThreshold { get; set; } = 0.75│ │ • StrictValidation: [✔ ON]  │ │ │
│ │ }                                                │ └─────────────────────────────┘ │ │
│ └──────────────────────────────────────────────────┴─────────────────────────────────┘ │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ TAB 3: Agent Lab (Microsoft Agent Framework & AG-UI)                                   │
│ ┌──────────────────────────────────────────────────┬─────────────────────────────────┐ │
│ │ Agent Pipeline: Copilot + Function Tools         │ Live AG-UI Event Stream (SSE)   │ │
│ │ Input: "Perform sequence mutation on HBB at pos 6│ 💬 Text Delta                   │ │
│ │ [ Submit Task ]                                  │ ⚠️ APPROVAL_REQUESTED Card      │ │
│ │                                                  │    [ Authorize ]  [ Deny ]      │ │
│ └──────────────────────────────────────────────────┴─────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### Aesthetic & Visual Rules (Science & Biotech Theme)
- **Palette**: Dominant clean lab dark surface (`#0B0E14`), slate borders (`#1E293B`), and cyan telemetry accents (`#06B6D4` / `#0EA5E9`).
- **Typography**: Interface in clean geometric sans (`Plus Jakarta Sans`), code and telemetry in `font-mono tabular-nums` (`JetBrains Mono`).
- **Unity-Style Inspector Cards**: Clean field rows with label, input widget (stepper, range scrubber, toggle, or text), and type indicator.
- **Zero-Pill Discipline**: Metadata displayed as unboxed text separated by typographic middots (`·`).

---

## 3. Key Product Decisions & Trade-Offs

### Decision 1: C# Real Execution Strategy
- **Approach**: Multi-tiered C# evaluation pipeline in `DNAPlatform.DevUI.API` (`POST /api/polyglot/execute`):
  1. *Reflection & Parsing*: Extracts public properties, default values, and `[Tool]` / `[Description]` attributes to generate the schema dynamically.
  2. *Live Compilation*: Executes Roslyn / C# Interactive compilation with standard references pre-loaded (`System.Text.Json`, `System.Numerics`, `DNAPlatform.Core.BioinformaticsTypes`).
  3. *CSnakes Fallback Bridge*: For Python cells, dispatches directly through `CSnakes` in `DNAPlatform.Core`.
- **Why**: Eliminates mock / fake execution messages and guarantees exact mathematical and algorithmic results across both C# and Python.

### Decision 2: Smart Component Architecture & Node Compatibility
- **Approach**: All authored components export a standard manifest:
  ```json
  {
    "id": "comp-dna-filter",
    "name": "DNA Filter",
    "category": "State Machine",
    "properties": [
      { "name": "GcThreshold", "type": "number", "default": 0.75, "min": 0, "max": 1 },
      { "name": "StrictValidation", "type": "boolean", "default": true }
    ],
    "expression": "input.gc < properties.GcThreshold"
  }
  ```
- **Why**: Allows instant dragging and dropping onto the Workflow canvas as a native node, editable via double-click.

### Decision 3: AG-UI State Management Protocol
- **Approach**: Integrates `@ag-ui/client` subscriber patterns (`docs.ag-ui.com/sdk/js/client/subscriber#state-management`) for real-time `STATE_SNAPSHOT` and JSON Patch `STATE_DELTA` updates during agent execution and human-in-the-loop interventions.

---

## 4. Technical Architecture & Component Hierarchy

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND (SvelteKit DevUI)                                │
├───────────────────────────────┬───────────────────────────────┬────────────────────────┤
│       /workflows Route        │         /kernel Route         │      /skills Route     │
│   • Canvas DAG Editor         │   • Polyglot Notebook Tab     │   • Catalog & Registry │
│   • Node Double-Click Drawer  │   • Component Authoring Tab   │   • Standard MCP Tools │
│   • Inline Expression Eval    │   • Real-Time Agent Lab Tab   │   • Read-only Browser  │
└───────────────┬───────────────┴───────────────┬───────────────┴────────────────────────┘
                │                               │
                ▼                               ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                            SVELTEKIT API & PROXY LAYER                                 │
│   • /api/polyglot/execute (Real C# / Python / JS dispatcher)                           │
│   • /api/components (CRUD for custom authored smart components)                        │
│   • /api/agui (AG-UI SSE stream + Human-in-the-Loop interrupts)                        │
└───────────────────────────────────────┬────────────────────────────────────────────────┘
                                        │
                                        ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                         BACKEND RUNTIME (.NET 8 & CSnakes)                             │
│   • DNAPlatform.DevUI.API (:5254) — C# Roslyn Script Engine + Attribute Inspector       │
│   • DNAPlatform.Core — CSnakes Python-C# zero-overhead interop bridge                  │
│   • DNAPlatform.AgentFramework — Single unified orchestrator & Microsoft Agent pipeline│
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Implementation Sequence

1. **Backend C# Real Evaluation Engine (`src/05_DevUI/DNAPlatform.DevUI.API`)**:
   - Enhance `/api/polyglot/execute` to compile and execute genuine C# code, capturing stdout and returning reflected public properties/attributes.
2. **Dedicated `/kernel` Route & Layout**:
   - Create `src/routes/kernel/+page.svelte` featuring the 3 sub-tabs.
   - Update navigation in `src/routes/+layout.svelte` to include `/kernel` ("Kernel & Studio").
   - Clean up `/skills/+page.svelte` so it focuses strictly on the skills catalog.
3. **Smart Component Authoring & Node Categories**:
   - Implement categories: `State Machine`, `Shell Tools`, `Modules`, `MCP Server`, `Agent as MCP`.
   - Build the Unity-style property inspector that auto-populates inputs from public fields.
   - Store custom authored components in SQLite via Prisma.
4. **Workflow Canvas Integration**:
   - Add double-click handler on workflow canvas nodes to open the smart component drawer.
   - Support n8n-style inline expressions (`{{ ... }}`).
   - Update state machine node to sync with AG-UI `STATE_DELTA` subscriber.
5. **Real-Time Agent Testing Lab**:
   - Embed interactive test console with SSE connection to `/api/agui`, thinking traces, and HITL authorization cards.
6. **Verification & Testing**:
   - Verify with `compile_applet` and `npm test` across all 10 platform suites.
