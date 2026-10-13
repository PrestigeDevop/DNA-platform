> **Header note (added 2026-10-05; the spec text below is unchanged):**
>
> This file is the user-supplied spec/prompt, preserved as an input artifact.
> **It is aspirational intent, not a description of current reality.** Nothing
> below should be read as "already built". The requirement-by-requirement
> assessment lives in [`GAP_ANALYSIS.md`](./GAP_ANALYSIS.md); the working task
> list is [`TODO.md`](./TODO.md); the authoritative repo guidance is
> [`../AGENTS.md`](../AGENTS.md).

### Settled decisions — do not relitigate

These were decided against this spec and are recorded here so a later agent does
not re-derive them:

| Spec claim | Reality | Decision |
|---|---|---|
| §1, §6 "Existing CLI" | No CLI exists. The only `Exe` is the ConsoleApp demo, whose `Main` never reads `args`. | **Greenfield.** `src/07_CLI/DNAPlatform.Cli`, built on `System.CommandLine`. It is a thin HTTP client over the existing API — never a second execution engine (§15). |
| §8 "use the project's existing realtime transport" | None exists. `/logs` polls at 1500 ms via `setInterval`. | **Build the transport.** SSE is the starting point; a message broker is not introduced. `/logs` polling remains a tracked gap, not a reuse. |
| §13/§14, §17 "PostgreSQL is the primary datastore" | `schema.prisma` is SQLite; the bootstrap path has no Docker. | **Deferred.** SQLite stays. §13 is unmet and tracked. §14's modelling *intent* is honoured incrementally (see `WorkflowVersion` in `TODO.md`). |
| §7 ACP | The only official ACP SDK is TypeScript (`@agentclientprotocol/sdk`). | **Thin TS adapter** in `src/08_ACP/`, mapping to HTTP calls against `:5254`. Zero DAG logic in TypeScript. |
| §16 priority #9, and any agent framework | Microsoft Agent Framework is priority #7, does not implement ACP, and its `Workflow` type collides with this repo's. | **Deferred.** Build the `IAgent` seam; add MAF later as one provider. `Microsoft.Agents.AI.Workflows` is never adopted — see [`AGENT_FRAMEWORK_INTEGRATION.md`](./AGENT_FRAMEWORK_INTEGRATION.md). |

Sections **§5, §9, §11, §12, §15 and §17** are sound and bind. §4's Test step is
structural validation only — no speculative validation engine.

---

You are working on an existing open-source local-first bioinformatics low-code/no-code workflow platform.

The goal is to improve and extend the CURRENT platform without replacing its working architecture.

# 1. NON-NEGOTIABLE STACK

Keep the existing technology stack:

* Frontend: SvelteKit + TypeScript
* Backend: .NET 8
* Node.js: existing Node LTS setup
* Existing Workflow Designer
* Existing workflow execution engine
* Existing CLI
* PostgreSQL as the primary persistent datastore

Do NOT migrate to React, Blazor, Next.js, Vue, FastAPI, Elsa, Pipelit, Mage, or another framework.

Do NOT rewrite working subsystems just to resemble a reference project.

Reference projects are UX/architecture references only.

# 2. REFERENCE PROJECTS

Study these projects before implementing changes.

## Pipelit — primary visual reference

https://github.com/theuselessai/Pipelit

Use it as reference for:

* visual workflow canvas
* node palette
* node configuration panel
* typed connections
* live execution state
* real-time execution updates
* workflow-oriented interaction
* CLI-oriented operation
* agent configuration concepts

Pipelit currently uses a React Flow editor and pushes execution updates in real time. Reproduce the useful interaction patterns, not its implementation.

## Elsa Workflow — workflow lifecycle reference

https://github.com/elsa-workflows/elsa-studio
https://docs.elsaworkflows.io/

Use it as reference for:

* workflow/activity inspector
* configuration/property editing
* workflow definition vs execution instance
* testing
* diagnostics
* workflow lifecycle
* execution monitoring
* larger-workflow usability

Do not introduce Elsa itself.

## Mage — code/data workflow reference

https://github.com/mage-ai/mage-ai
https://www.mage.ai/solutions/outcomes/automate

Study especially:

* modular code blocks
* notebook-style workflow development
* Python / SQL / R block model
* data-flow-oriented pipeline organization
* logs and live previews
* step-by-step debugging
* validation
* retry/replay/recovery concepts
* workflow checks
* reusable workflow context

Mage's current product explicitly combines sources, code blocks, transformations, checks, schedules, monitoring, validation, recovery, and reusable workflow context.

We may use these ideas later for our own Bioinformatics Code Block system, but DO NOT implement the full code-block system yet unless explicitly required below.

# 3. PRODUCT DIRECTION

The platform should feel like:

Pipelit-level visual workflow UX
+
Elsa-level workflow lifecycle clarity
+
Mage-style modular scientific/data workflow capabilities

while remaining specifically a bioinformatics platform.

The workflow is the primary unit of execution.

A node is an executable component inside a workflow.

An agent is an optional intelligent component, not the definition of the whole platform.

# 4. WORKFLOW PAGE

The Workflow page should have a clear workflow-level lifecycle.

Do NOT add a mandatory "Test" button to every node.

Instead provide a workflow-level:

* Configure
* Test
* Run

The intended flow is:

Configure → Test workflow → Run workflow

## Configure

When the user clicks Configure, open an environment/runtime configuration view.

The environment should represent the local polyglot execution environment.

At minimum it should communicate:

* Local environment
* Available runtimes
* Mise configuration
* Dependency/runtime status

Support:

* Import mise configuration
* Export mise configuration
* Check mise dependency status
* Verify the environment before execution

The UI must make it obvious which environment the workflow will execute in.

## Mise status

The dependency status should have three conceptual states:

GREEN:
The expected mise configuration/dependency exists and is available.

YELLOW:
The requested dependency/configuration is missing, but the platform can reasonably fall back to the platform-provided embedded/default runtime.

RED:
The dependency is unavailable, incompatible, or cannot be resolved.

For platform-provided defaults, initially account for:

* .NET 8
* Node.js LTS

Do not invent additional embedded runtimes unless they already exist in the project.

The status system must remain extensible because future bioinformatics workflows may require Python, R, Java, Bash, Conda/Bioconda tools, containers, etc.

## Test

"Test" is a workflow-level operation.

Its purpose is to verify that the COMPLETE workflow is ready to execute.

Test should eventually be able to detect things such as:

* invalid workflow structure
* missing inputs
* invalid connections
* incompatible input/output types
* unavailable tools
* missing mise dependencies
* runtime incompatibility
* missing configuration
* unavailable plugins
* other execution blockers

Do not create a full speculative validation engine if the project does not currently support it.

Build the UI/API contract so additional validation checks can be added later.

Test must NOT execute the real production workflow unless explicitly defined by the existing execution semantics.

# 5. WORKFLOW CANVAS

Preserve the existing canvas implementation if it is already functional.

Improve it rather than replacing it.

Desired UX:

* infinite canvas
* zoom/pan
* node palette
* readable nodes
* typed connections
* selected-node state
* clean node configuration
* useful execution state
* useful validation state
* error visibility
* optional minimap/controls where appropriate

A node should communicate at a glance:

* tool/activity name
* node type
* inputs/outputs
* validation state
* runtime/dependency state when relevant
* execution state when a workflow is running

Execution state should be real, not simulated:

Idle
→ Queued
→ Running
→ Success / Failed / Skipped

Where useful, use visual state on the node itself.

# 6. WORKFLOW EXECUTION + CLI

CLI is a first-class interface of the platform.

Preserve the existing CLI and inspect how it currently works before modifying it.

The CLI should eventually support the same underlying execution model as the UI.

Do not implement separate execution logic for:

* web UI
* CLI
* agent
* ACP

Instead use the same backend execution/application services underneath.

The CLI should expose clean commands for things such as:

* workflow execution
* workflow validation/test
* workflow status
* logs
* cancellation
* environment/runtime checks

Use the actual existing command structure where possible rather than inventing a completely new CLI design.

# 7. ACP — MUST BE SUPPORTED

Implement support for the Agent Client Protocol (ACP).

ACP must be treated as a real protocol boundary, not a custom imitation.

Use the official ACP specification/SDK appropriate for the project's implementation.

ACP communication should support:

* initialization
* session lifecycle
* prompts/requests where appropriate
* structured updates
* streaming progress
* cancellation
* errors
* capabilities where applicable

For local process execution, support ACP over stdio.

Important transport behavior:

* Client → Agent requests/notifications through stdin
* Agent → Client responses/notifications through stdout
* stdout must remain valid ACP protocol output
* diagnostic/non-protocol process logs should use stderr

Do not mix arbitrary console logs into ACP stdout.

Create an ACP adapter/service around the existing execution/agent infrastructure instead of rewriting the execution engine.

The project should be able to expose an ACP-compatible interface through the CLI.

# 8. REAL-TIME LOGGING — IMPORTANT

The current Logs page can hang/block because execution output is being handled in a buffered or blocking way.

Fix this properly.

DO NOT use a blocking "read everything, then display it" model.

Use streaming process/event handling.

For ACP-backed processes:

* consume ACP responses and `session/update`-style notifications incrementally
* process stdout incrementally
* process stderr independently
* do not wait for process termination before updating the UI
* do not buffer the entire execution output in memory

The backend must persist run events/logs while simultaneously streaming them to connected UI clients.

The frontend Logs page must receive updates in real time.

Recommended architecture:

CLI/process
→ ACP/stdin/stdout or execution event stream
→ .NET execution service
→ persist event/log in PostgreSQL
→ push live event to SvelteKit client

Use the project's existing realtime transport if one already exists.

If a realtime transport does not exist, prefer a simple .NET-native streaming mechanism such as SignalR or SSE rather than introducing another message broker solely for logs.

The important property is:

RUNNING WORKFLOW
→ backend receives event
→ database records event
→ UI receives event immediately

Do not poll the CLI output from the browser.

# 9. LOG PAGE

The Logs/Run page should support:

* live streaming logs
* timestamps
* workflow run ID
* node/activity association where available
* severity/type
* stdout/stderr distinction where applicable
* execution status
* errors
* structured execution events
* historical run retrieval

The page should continue receiving events while the process is running.

After completion, the same run should remain available from PostgreSQL.

A refresh must not destroy the run history.

# 10. SKILLS PAGE

Keep the Skills page intentionally simple for now.

Add two UI actions:

* Import MCP Plugins
* Add New Plugin

For this phase:

* build the UI/interaction shell
* create the appropriate data/API shape if necessary
* do NOT implement the full plugin marketplace/installation system yet
* do NOT add speculative plugin runtime complexity

The Skills architecture should remain extensible.

# 11. FUTURE CODE BLOCKS

Do not fully implement the Code Block system yet.

However, design the architecture so Skills and future workflow nodes can eventually support reusable code blocks inspired by Mage.

Future code blocks may represent:

* Python
* R
* SQL
* JavaScript/Node
* Bash
* other supported scientific languages

The future model should be compatible with:

Skill
→ Code Block
→ Workflow Node

Do not force this abstraction into the current implementation unless the repository already has an appropriate abstraction.

# 12. AGENT PAGE

The Agent page can take interaction/conceptual inspiration from:

* Pipelit agents
* Microsoft Copilot-style agent configuration

Each Agent should eventually have structured configuration areas for things such as:

* system prompt
* model configuration
* tools
* MCP integrations
* memory/context
* RICE framework configuration
* vector-backed knowledge/context
* permissions
* runtime/environment
* future workflow/skill access

Do not turn the whole application into an agent orchestration platform.

Agents are components that can operate within the broader bioinformatics workflow platform.

# 13. POSTGRESQL-FIRST ARCHITECTURE

Make PostgreSQL the primary persistent datastore for the platform.

The objective is to reduce unnecessary separate infrastructure/services.

Persist in PostgreSQL where appropriate:

* users
* workflows
* workflow versions
* nodes
* edges
* skills
* plugins
* agents
* agent configuration
* execution records
* execution events
* logs
* environment/mise metadata
* runtime metadata
* validation results
* credentials/secure references as appropriate
* future code blocks
* vector embeddings

For vector search/agent memory, prefer PostgreSQL + pgvector instead of introducing a separate vector database.

The system should be designed so normal relational data and vector data can live together.

Do NOT introduce a separate vector database unless there is a demonstrated technical requirement that PostgreSQL + pgvector cannot reasonably satisfy.

Do not introduce Redis/Kafka/etc. solely because a reference project uses it.

Use PostgreSQL as the source of truth.

# 14. DATABASE DESIGN PRINCIPLE

Avoid making the database schema overly generic or abstract.

Model the domain explicitly:

Workflow
WorkflowVersion
WorkflowNode
WorkflowEdge
WorkflowRun
WorkflowEvent
Skill
Plugin
Agent
AgentConfiguration
RuntimeEnvironment
MiseConfiguration
ValidationResult
Embedding/Memory

Use JSON/JSONB where schema flexibility is genuinely useful, but keep important relationships and searchable fields relational.

# 15. ARCHITECTURAL PRINCIPLE

Do not create independent implementations of the same concept.

There should ideally be one underlying concept for:

workflow definition
workflow validation
workflow execution
workflow run
workflow events
workflow logs

and multiple interfaces over it:

* Web UI
* CLI
* ACP
* future API integrations

The UI should not become a second execution engine.

The CLI should not become a second execution engine.

ACP should not become a third execution engine.

They should all use the same underlying services.

# 16. IMPLEMENTATION PROCESS

Before changing code:

1. Inspect the existing repository.
2. Identify the current implementation of:

   * Workflow Designer
   * workflow persistence
   * execution engine
   * CLI
   * logs
   * Skills
   * Agents
   * configuration
   * database
3. Identify what already works.
4. Identify what is currently broken.
5. Identify what can be extended rather than rewritten.
6. Produce a concise gap analysis.

Then implement incrementally.

Priority order:

1. Fix real-time execution/log streaming.
2. Introduce/finish ACP + CLI integration.
3. Add workflow-level Configure/Test flow.
4. Add mise/runtime status UI.
5. Improve Workflow Designer UX using Pipelit + Elsa references.
6. Add Skills UI placeholders.
7. Improve Agent configuration model.
8. Consolidate persistence around PostgreSQL/pgvector.
9. Prepare architecture for future Code Blocks.

Do not implement future features prematurely.

# 17. HARD CONSTRAINTS

* Keep SvelteKit.
* Keep .NET 8.
* Keep Node LTS.
* Keep the existing execution architecture where it already works.
* PostgreSQL is the primary datastore.
* Prefer pgvector for vectors.
* ACP is a required integration.
* CLI is a first-class interface.
* Real-time logs are required.
* Do not use blocking output collection for live logs.
* Do not fake execution status.
* Do not create duplicate execution engines.
* Do not copy React/Blazor implementations into SvelteKit.
* Do not blindly copy Mage/Pipelit/Elsa architecture.
* Do not add infrastructure just because a reference project uses it.
* Do not over-engineer future plugin/code-block functionality.
* Do not rewrite working code without a concrete reason.

# FINAL OBJECTIVE

The end result should feel like a serious scientific workflow platform:

Visual and intuitive like Pipelit
+
Workflow lifecycle/inspection clarity like Elsa
+
Modular code/data workflow thinking like Mage
+
ACP/CLI interoperability
+
Real-time execution visibility
+
Mise-aware reproducible local environments
+
PostgreSQL + pgvector as the unified persistence layer

while remaining:

SvelteKit + .NET 8
local-first
bioinformatics-focused
extensible
and based on the project's existing working architecture.
