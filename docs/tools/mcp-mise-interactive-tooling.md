# MCP, Mise & Microsoft Interactive Tooling Guide

## Overview
This document logs the architecture, real integrations, gotchas, and error logs for the DNA Platform's polyglot runtime, MCP (Model Context Protocol) ecosystem, and Microsoft .NET Interactive integration.

## Architecture

### 1. Genuine `mise` MCP Server (`mcp-servers/mise-mcp/`)
- **Protocol**: Model Context Protocol (MCP) JSON-RPC 2.0 over `stdio`.
- **Engine**: Genuine wrapper around `/usr/local/bin/mise`.
- **Exposed Tools**:
  - `mise_list`: Queries installed language runtimes (`mise ls --json`).
  - `mise_current`: Queries currently active environment runtimes (`mise current --json`).
  - `mise_install`: Installs toolchain plugins/versions (`mise install [tool]`).
  - `mise_plugins`: Lists registered plugins and repository sources.
  - `mise_doctor`: Validates environment health and shim consistency.
  - `mise_run`: Executes tasks defined in `mise.toml`.

### 2. Microsoft .NET Interactive REPL Engine
- **Global Tool**: `Microsoft.dotnet-interactive` (v1.0.456201) backed by .NET 8 SDK and ASP.NET Core 7 runtime.
- **CLI Commands**:
  - `npm run interactive` or `node scripts/dna.js interactive`
  - Launches `dotnet-interactive jupyter --default-kernel csharp --http-port 8888`
- **Fallback / Dual-Engine Execution**:
  - High-speed in-process Roslyn compiler and CSnakes Python bridge available at `/api/interactive`.

### 3. Smart Component & Extension Authoring (`/studio`)
- **Unity-Style Inspector Reflection**:
  - Parses C# classes, records, and Python modules.
  - Extracts `[Tool("name")]`, `[Description("text")]`, `[Range(min, max)]`, and public serialized properties.
  - Renders interactive UI controls:
    - Enum: `<select>` dropdown with all enum values.
    - VectorDB: Vector DB store selector with `pgvector (PostgreSQL)` default, Pinecone, Qdrant, Milvus.
    - Range/Number: Precision threshold slider (`min`, `max`, `step`).
    - Boolean: Toggle switch (`[ON] / [OFF]`).
    - String/Expression: Evaluatable code / template input.
- **Automated Marketplace Installation & Mise Sync**:
  - Marketplace packages define required runtimes in `.mise.toml`.
  - When installed, components are dynamically registered into the Workflow Designer palette.

### 4. Autonomous De Novo Workflow Demonstration Hub (`/workflows`)
- **Flagship Demonstration Pipeline**:
  - `wf-crispr-synthesis`: De Novo CRISPR-Cas9 Target Optimization & Synthesis Assembly
  - Multi-stage pipeline:
    1. Input Target Gene Sequence (Homo sapiens EMX1 locus)
    2. Microsoft Copilot Local ONNX Reasoning for 20-nt spacer selection
    3. Thermodynamic Stability & GC/Tm Analysis
    4. CSnakes Python-C# zero-overhead off-target risk scoring against GRCh38
    5. AG-UI Human-in-the-Loop (HITL) interrupt gate (`APPROVAL_REQUESTED`)
    6. Golden Gate assembly oligonucleotides generation with Type IIS BsaI overhangs (`5'-CACC...` and `5'-AAAC...`)
    7. Workspace FASTA/GenBank exporter (`workspace/crispr_emx1_oligos.fasta`)
    8. LIMS production synthesis order specification
- **Interactive Workbench & Protocol Stream**:
  - Live sequence thermodynamic metrics calculator (GC%, Nearest-Neighbor Tm, MW in kDa).
  - PAM motif (NGG) scanner with exact start positions.
  - Interactive AG-UI SSE stream terminal displaying `THINKING_DELTA`, `APPROVAL_REQUESTED`, and `STATE_DELTA` RFC 6902 JSON patch updates.
  - 1-click "Open in Canvas" linking directly to `WorkflowDesigner.svelte`.

## Gotchas & Error Logs

1. **Svelte 4 Template Non-Null Assertion**:
   - *Error*: `Unexpected token` on `testingAgent!` or `activeKernel as any` in Svelte template markup.
   - *Cause*: Svelte 4 template expressions `{...}` parse standard ECMAScript only, not TypeScript type assertions.
   - *Fix*: Type variables inside `<script lang="ts">` and avoid `!` or `as` in template markup.

2. **SvelteKit Server-Only Module Protection**:
   - *Error*: `Cannot import $lib/server/seedData.ts into code that runs in the browser, as this could leak sensitive information.`
   - *Cause*: SvelteKit restricts any code or types located in `$lib/server/` from being imported into client components.
   - *Fix*: Place client-safe workflow definitions in `$lib/data/demoWorkflows.ts` and import from there.

3. **.NET Interactive Runtime Dependencies**:
   - *Gotcha*: `Microsoft.dotnet-interactive` requires `Microsoft.AspNetCore.App 7.0` even when installed under .NET 8 SDK.
   - *Fix*: Run `dotnet-install.sh --channel 7.0 --runtime aspnetcore` into `/usr/share/dotnet`.

4. **URL Encoding of SvelteKit Route Paths in File Tools**:
   - *Gotcha*: File paths containing `+` (e.g. `+page.svelte`) can be translated to spaces by HTTP decoders if not carefully handled.
   - *Fix*: Use URL encoded `%2B` in API tools or run script-based modifications.

