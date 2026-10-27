# Implementation Plan: .NET Core Workflow Platform Architecture, Mise MCP Toolchain, and CLI Tooling SDK

Architect and document the complete system architecture for the plug-and-drop WYSIWYG bioinformatic workflow platform. Update `docs/architecture.md` with complete schemas, class structures, protocols, and developer recipes; enhance the cross-platform CLI Tooling SDK with animated interactive menus and dynamic Python execution; and execute end-to-end smoke and integration testing.

---

## User Review Required

> [!IMPORTANT]
> The architectural requirements and user choices are confirmed:
> 1. **CLI Tooling Interface**: Interactive animated terminal menu with dynamic Python execution sub-menu.
> 2. **Monaco Editor LSP Integration**: WebSocket JSON-RPC bridge connected to the backend compiler.
> 3. **Marketplace Dependency Resolution**: Fully automated `mise mcp` resolution and runtime isolation.

---

## Proposed Changes

### 1. Architecture Documentation (`docs/architecture.md`)

Update `docs/architecture.md` to provide the definitive, production-grade architectural specification:

- **Section 1: Unified System Architecture**:
  - Full ASCII topology diagram illustrating the flow between SvelteKit UI (:3000), .NET Core Backend (:5254), Embedded Polyglot Kernel (:8888), AG-UI Hosting Server (:5000), Real MCP Server (`mcp-servers/mise-mcp`), and WebSocket JSON-RPC LSP bridge.
- **Section 2: Backend .NET Core Workflow Engine & Native MCP Server**:
  - DAG execution engine, topological sorting, state delta management, and inline expression evaluation.
  - Native MCP Server specification: Tool registration, prompt exposition, and resource streaming adhering to the Model Context Protocol 2024-11-05 spec.
  - C# Developer SDK attributes:
    ```csharp
    [McpTool("dna_alignment", "Performs Needleman-Wunsch pairwise alignment")]
    public record AlignmentRequest(
        [McpParameter("First DNA sequence")] string SequenceA,
        [McpParameter("Second DNA sequence")] string SequenceB
    );
    ```
- **Section 3: Embedded Polyglot Kernel Server & Language Runtime Sandbox**:
  - Embedded .NET Interactive Polyglot Kernel orchestration across C#, F#, Python (via CSnakes), JavaScript/Node.js, and PowerShell.
  - Process isolation, memory bounds, and zero-overhead biological marshaling.
- **Section 4: SvelteKit Frontend, Monaco Editor & WebSocket JSON-RPC LSP**:
  - WYSIWYG node-based workflow canvas integration.
  - Monaco Editor hooked into backend LSP server via WebSocket JSON-RPC for syntax highlighting, diagnostics, and contextual variable autocompletion from upstream node outputs.
- **Section 5: Package Management & 'mise mcp' Dependency Pipeline**:
  - Automated toolchain provisioning with `mise mcp`.
  - JSON Marketplace manifest standard (`dna-module.json`) detailing metadata, UI inspector bindings, backend requirements, and multi-language package managers (`nuget`, `npm`, `pip`).
  - Automated unpack, security scan, and runtime tool registration pipeline.
- **Section 6: Agentic Tool Exposition & The `/studio` Route**:
  - Dynamic discovery into the AI Agent Tool Registry.
  - Granular user permissions and context injection for LLM reasoning loops.
  - `/studio` playground architecture: manual payload testing, polyglot step execution, LLM argument verification, and LSP/runtime diagnostics.
- **Section 7: Cross-Platform CLI Tooling SDK**:
  - Terminal architecture, animation frames, service process management, and dynamic Python scripting engine.

---

### 2. Cross-Platform CLI Tooling SDK Enhancement (`scripts/dna.js`)

Enhance `scripts/dna.js` to serve as the unified, animated developer entry point:

- **Animated Startup Banner**:
  - Implement an animated multi-frame ASCII DNA double-helix banner with color spectrum transitions on launch.
- **Interactive Menu Enhancements**:
  - Add option `[9] 🐍 Execute Dynamic Python Code & CSnakes Annotations`:
    - Sub-menu with instant bioinformatics presets (Reverse Complement, GC Skew, PAM motif scanner, Amino Acid translation).
    - Interactive REPL prompt to execute arbitrary inline Python statements with instant stdout, execution time, and return value capture.
    - CLI tool annotation helper to convert Python functions into MCP tool schemas.
- **CLI Command Dispatcher**:
  - Add `dna py "<code>"` and `dna python` flags for non-interactive and scripted execution directly from bash/terminal.
  - Dedicated service launchers for Web DevUI, Polyglot Interactive Kernel, and mise MCP server.

---

### 3. Developer Tooling & Reference Updates (`docs/tools/mcp-mise-interactive-tooling.md`)

- Document the JSON module packaging specification (`dna-module.json`).
- Document the WebSocket JSON-RPC LSP protocol frame format.
- Update CLI commands table and dynamic Python scripting instructions.

---

## Verification Plan

### Automated Verification
1. **Compilation & Build**:
   - Run `npm run build` to verify SvelteKit and Vite compilation succeed with 0 errors.
2. **Smoke Test Suite**:
   - Run `npm test` (`node scripts/2-test-dev.mjs`) to verify all 10 agentic, API, and protocol tests pass.
3. **CLI Tooling Tests**:
   - Run `node scripts/dna.js env` to verify polyglot runtime diagnostics.
   - Run `node scripts/dna.js py "import sys; print(f'Python {sys.version.split()[0]} online')"` to verify dynamic Python execution.
4. **Endpoint Health Checks**:
   - Verify HTTP 200 on all primary routes: `/`, `/workflows`, `/studio`, `/kernel`, `/agents`.
