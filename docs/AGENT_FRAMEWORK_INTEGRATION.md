# Microsoft Agent Framework (MAF) — Integration Assessment

**Status:** research only. No code was applied. No `.csproj` or NuGet reference was touched.
**Scope:** evaluate `microsoft/agent-framework` as an adoptable provider behind the existing `IAgent` seam.
**Date of research:** 2026-10-05.

---

## 1. What MAF is

Microsoft Agent Framework (MAF) is an open, multi-language framework for building "production-grade AI agents and multi-agent workflows" in .NET, Python and Go ([repo README](https://github.com/microsoft/agent-framework)). It supersedes both AutoGen and Semantic Kernel as Microsoft's forward-looking agent SDK: it was "developed by the core AutoGen and Semantic Kernel teams at Microsoft, and is designed to be a new foundation for building AI applications going forward" ([AutoGen migration guide](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-autogen)). The core abstraction is `AIAgent` — an "LLM wrapper" that processes input, calls tools, and returns responses — with `ChatClientAgent` as the standard implementation built over any `Microsoft.Extensions.AI.IChatClient` ([hosting guide](https://learn.microsoft.com/en-us/agent-framework/get-started/hosting)). It is MIT-licensed, and 1.0 was announced as production-ready with "stable APIs, and a commitment to long-term support" ([1.0 blog](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/)).

MAF is **orchestration-first**. Its headline features are graph-based multi-agent workflows (sequential, concurrent, handoff, group chat), checkpointing, time-travel, human-in-the-loop, middleware pipelines and OpenTelemetry ([README](https://github.com/microsoft/agent-framework)).

---

## 2. Verified facts

Every row carries a source. Anything not directly confirmed is marked `UNVERIFIED` rather than guessed.

| Fact | Value | Confidence | Source |
|---|---|---|---|
| Licence | MIT ("Copyright (c) Microsoft Corporation") | **Verified** — full text fetched | [LICENSE](https://raw.githubusercontent.com/microsoft/agent-framework/main/LICENSE), [repo sidebar "MIT license"](https://github.com/microsoft/agent-framework) |
| Core .NET package | `Microsoft.Agents.AI` | **Verified** | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI/), [README install section](https://github.com/microsoft/agent-framework) |
| Current stable version | **1.23.0**, published 2026-09-29 | **Verified** (per-package page; the profile page showed a stale 1.16.0) | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI/) |
| Cadence | ~weekly minors (1.13.0 Jul 3 → 1.23.0 Sep 29) | **Verified** from version table | [NuGet versions](https://www.nuget.org/packages/Microsoft.Agents.AI/) |
| Target frameworks | `net10.0;net9.0;net8.0` core; `+netstandard2.0;net472` | **Verified** — `TargetFrameworksCore` in build props | [dotnet/Directory.Build.props](https://github.com/microsoft/agent-framework/blob/main/dotnet/Directory.Build.props) |
| `net8.0` usable | Yes — NuGet marks `net8.0` "compatible"; repo targets it | **Verified** | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI/), [Transparency FAQ](https://github.com/microsoft/agent-framework/blob/main/TRANSPARENCY_FAQ.md) |
| Transitive dependency weight | 1.23.0 pulls `Microsoft.Extensions.AI` >=10.10.0, `.Abstractions` >=10.10.1, `.Evaluation`, `Microsoft.Extensions.Compliance.Abstractions`, `Microsoft.Extensions.FileSystemGlobbing`, `Microsoft.ML.Tokenizers` >=2.0.0, `System.Text.Json` >=10.0.12, `System.Threading.Channels` >=10.0.12 | **Verified** (dependency list) | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI/) |
| Agent abstraction | `AIAgent` (abstract base) / `ChatClientAgent` over `IChatClient` | **Verified** | [AIAgent API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent), [ChatClientAgent.cs](https://github.com/microsoft/agent-framework/blob/main/dotnet/src/Microsoft.Agents.AI/ChatClient/ChatClientAgent.cs) |
| Invoke (non-streaming) | `Task<AgentResponse> RunAsync(string / ChatMessage / IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken)` | **Verified** | [AIAgent.RunAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent.runasync) |
| Invoke (streaming) | `IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(..., CancellationToken)` | **Verified** | [AIAgent.RunStreamingAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent.runstreamingasync), [running-agents](https://learn.microsoft.com/en-us/agent-framework/concepts/agents/running-agents) |
| Cancellation | `CancellationToken` on every `RunAsync`/`RunStreamingAsync` overload | **Verified** | same as above |
| Session/state | `AgentSession` + `CreateSessionAsync`, `Serialize/DeserializeSessionAsync` | **Verified** | [AIAgent API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent) |
| Session state is caller-owned | `.NET` `AIAgent` exposes session serialize/deserialize rather than owning storage | **Verified** | [AIAgent API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent) |
| DI hosting | `Microsoft.Agents.AI.Hosting`; `builder.AddAIAgent(name, instructions, chatClientServiceKey)` -> `IHostedAgentBuilder`, `.WithAITool()`, `.WithInMemorySessionStore()` | **Verified** | [hosting guide](https://learn.microsoft.com/en-us/agent-framework/get-started/hosting) |
| Middleware | `.AsBuilder().Use(...)`, run + streaming middleware | **Verified** | [middleware doc](https://learn.microsoft.com/en-us/agent-framework/concepts/agents/middleware/) |
| Observability | Built-in OpenTelemetry | **Verified** | [README](https://github.com/microsoft/agent-framework) |
| Tool model | `AIFunction` via `AIFunctionFactory`; `AIAgent.AsAIFunction()` to use an agent as a tool | **Verified** | [AgentExtensions.cs](https://github.com/microsoft/agent-framework/blob/main/dotnet/src/Microsoft.Agents.AI/AgentExtensions.cs) |
| **MCP support** | **Yes.** `Microsoft.Agents.AI.Mcp` + `ModelContextProtocol` SDK; `Microsoft.Agents.AI.Workflows.Declarative.Mcp` (stable). MCP *skills* via `UseMcpSkills()` | **Verified** | [Mcp NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Mcp/1.22.0-alpha.260918.1), [local MCP tools doc](https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools), [MCP-skills blog](https://devblogs.microsoft.com/agent-framework/discover-agent-skills-from-mcp-servers-in-net/), [PR #6108](https://github.com/microsoft/agent-framework/pull/6108) |
| MCP maturity | `Microsoft.Agents.AI.Mcp` is **alpha/prerelease** (`1.22.0-alpha.*`) | **Verified** | [Mcp NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Mcp/1.22.0-alpha.260918.1) |
| **ACP (Agent Client Protocol)** | **No.** MAF exposes A2A, AG-UI, OpenAI-compatible endpoints, MCP, Telegram — ACP is absent from the protocol table, the hosting list, and the `dotnet/src` package list | **Verified absent** (docs + package listing; not verified by source grep) | [hosting](https://learn.microsoft.com/en-us/agent-framework/get-started/hosting), [self-hosting](https://learn.microsoft.com/en-us/agent-framework/hosting/self-hosting/), [dotnet/src](https://github.com/microsoft/agent-framework/tree/main/dotnet/src) |
| Semantic Kernel dependency | **No dependency on SK.** Base layer is `Microsoft.Extensions.AI`. SK depends on `Extensions.AI`, not the reverse | **Verified** (dependency direction via NuGet reverse-deps) | [Microsoft.Extensions.AI NuGet](https://www.nuget.org/packages/Microsoft.Extensions.AI/10.10.0), [from-semantic-kernel](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel) |
| AutoGen superseded | Yes — dedicated migration guide; MAF replaces it | **Verified** | [from-autogen](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-autogen) |
| AutoGen guide coverage | Migration guide is **Python**-focused; .NET parity not stated | **Partially verified** | [from-autogen](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-autogen) |
| **Ollama support (.NET)** | **Yes.** `OllamaSharp` -> `.AsIChatClient()` -> `.AsAIAgent(instructions, name)` | **Verified** | [Ollama provider doc](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/model-providers/ollama) |
| OpenAI-compatible endpoint (.NET) | `OpenAIChatClient`/`OpenAIChatCompletionClient` with `base_url` is documented for **Python**; the .NET code samples on the same page show only `OllamaSharp` | **UNVERIFIED for .NET** — treat as likely-but-unconfirmed | [Ollama provider doc](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/model-providers/ollama), [openai-endpoints](https://learn.microsoft.com/en-us/agent-framework/hosting/self-hosting/openai-endpoints) |
| AOT / trimming | `IsAotCompatible` set for net7.0+ TFMs | **Verified** | [Directory.Build.props](https://github.com/microsoft/agent-framework/blob/main/dotnet/Directory.Build.props) |
| Name collision to watch | MAF ships `Microsoft.Agents.AI.DevUI`, unrelated to this repo's "DevUI" | **Verified** | [dotnet/src](https://github.com/microsoft/agent-framework/tree/main/dotnet/src) |
| Commercial surface | MIT code; but Foundry/Azure components pull Azure services under Microsoft Product Terms, and third-party/non-Azure models are called out as "Non-Microsoft Products under the Microsoft Product Terms" | **Verified** | [README "Important Notes"](https://github.com/microsoft/agent-framework), [Transparency FAQ](https://github.com/microsoft/agent-framework/blob/main/TRANSPARENCY_FAQ.md) |
| Local-first data posture | Framework is provider-neutral; no hosted dependency in the core path | **Verified** (Ollama path is fully local) | [Ollama provider doc](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/model-providers/ollama) |
| .NET 8 caveats | Packages resolve on net8.0 but drag `Microsoft.Extensions.*` **10.x** into a .NET 8 app; exact runtime behaviour on `net8.0` + ASP.NET Core 8 not validated here | **UNVERIFIED — must be spike-tested** | [NuGet dependency list](https://www.nuget.org/packages/Microsoft.Agents.AI/) |
| Release stability | 1.0+ GA, "long-term support" commitment, but weekly minor cadence | **Verified** | [1.0 blog](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/) |

---

## 3. Honest fit assessment

### The recommendation

**Defer adoption. Build the seam now; add MAF behind it later, as one provider among several.**

MAF is a competent, MIT-licensed, `net8.0`-capable framework whose MCP support maps unusually well onto this platform's section 10 Skills page, and its Ollama path means a local-first platform can stay local. It is also explicitly orchestration-first, which is the part that does not fit. None of these facts are disqualifying; the problem is sequencing and scope.

### Does adopting MAF violate `refactor.md`?

**section 1 (keep the stack) — not violated.** MAF is a NuGet library, not a replacement framework. SvelteKit, .NET 8, Node LTS and the existing designer all stay. Adding `Microsoft.Agents.AI` does not make SvelteKit a second thing or the orchestrator obsolete.

**section 3 / section 12 (agents are optional components) — violated in spirit, satisfiable in practice.** section 12 says plainly: "Do not turn the whole application into an agent orchestration platform." MAF's centre of gravity is the opposite: `Microsoft.Agents.AI.Workflows`, graph executors, edges, checkpointing, handoff and group-chat orchestration. If MAF's workflow engine is ever allowed to become *the* execution engine, the platform becomes exactly what section 12 forbids. This is avoidable but only by a deliberate decision: **MAF must never own workflow execution.** `IWorkflowOrchestrator` stays the single engine (section 15); MAF is used only for the `Agent` node's inner step.

**section 15 (no duplicate implementations) — the real risk, and the one to design against.** `refactor.md` section 15 permits one underlying concept per concern with multiple interfaces over it. MAF ships its own `Workflow` graph abstraction with overlapping vocabulary (`Workflow`, executors, edges, checkpointing) that maps almost 1:1 onto this repo's `Workflow` / `WorkflowNode` / `NodeConnection` / `IWorkflowOrchestrator` in `AgentFrameworkStubs.cs`. Two of those coexisting is the textbook section 15 violation. A MAF `Workflow` and a platform `Workflow` in the same process will also collide on the type name.

**section 17 (no over-engineering) — violated if adopted early.** The repo has **no LLM wired at all**. `InMemoryAgent.Run` is a deterministic echo (`InMemoryAgentFramework.cs:180-188`). Adopting a large framework before any provider exists means carrying ~14 transitive packages to reach a code path that cannot yet execute a real model.

### The four considerations, weighed

| Consideration | Weight | Assessment |
|---|---|---|
| (a) Large framework, zero LLMs wired | **Heavy, against** | ~14 transitive deps for an agent layer that still returns `"[StandardAgent] echo: ..."`. Classic section 17 violation. |
| (b) `ISkillRegistry` already is a plugin seam | **Light, neutral** | `ISkillRegistry` + `SkillRegistryAdapter` + `DNAPlatform.Skills` already cover the extension point. MAF adds MCP and provider breadth but **duplicates** the seam rather than replacing it. Two registries would need a merge rule. |
| (c) MAF is orchestration-first; sections 3/12 say agents are components | **Heavy, against** | MAF's value is multi-agent orchestration — precisely the capability section 12 says must not define the platform. Adopting it means paying for the wrong emphasis. |
| (d) Priority #7 of 9 | **Decisive, against** | `refactor.md` section 16 orders: realtime logs -> ACP/CLI -> Configure/Test -> mise status -> designer UX -> skills shell -> **agent config (#7)** -> PostgreSQL -> code blocks. Real-time logs and ACP/CLI come first and are both currently incomplete. |

### Where MAF genuinely wins

- **MCP.** Verified first-class support, and it aligns with section 10's "Import MCP Plugins" button better than anything else in the ecosystem. The caveat: the .NET MCP package is **alpha**.
- **Ollama + local-first.** Verified .NET path via `OllamaSharp` + `AsIChatClient` + `AsAIAgent`. Directly serves the "local-first, data stays on-premises" stance.
- **Stream + cancel.** `RunStreamingAsync` with `CancellationToken` is a clean match for section 8 realtime streaming and section 7 ACP cancellation — which the current `IAgent.Run` does **not** support at all (see section 4).
- **Sessions are caller-owned.** `AgentSession` serialize/deserialize leaves history persistence to the platform, which fits section 13's PostgreSQL-first, one-source-of-truth rule instead of hiding storage inside a framework.

### Bottom line

MAF is a reasonable *eventual* provider for the `Agent` node and a poor *platform* framework. Adopt it **behind the existing `IAgent` seam**, after priority #1-#6 land, and only ever as one implementation of `IAgent`. Do not adopt `Microsoft.Agents.AI.Workflows` at all — it is the section 15 collision and it is not needed.
---

## 4. Proposed integration seam (proposal only — not applied)

### The gap this must solve first

`IAgent` in `src/06_Skills/AgentFrameworkStubs.cs:65-71` is:

```csharp
public interface IAgent
{
    string AgentId { get; }
    IEnumerable<string> GetAvailableSkills();
    Task<AgentResponse> Run(string prompt, Dictionary<string, object>? context = null);
    Task BindSkill(string skillId, ISkill skill);
}
```

`Run` has **no `CancellationToken` and no streaming**. section 7 (ACP cancellation) and section 8 (realtime logs) both need both. So a naive "MAF implements `IAgent`" adapter would throw away MAF's two most relevant capabilities — streaming and cancellation.

**The proposal is therefore additive, not a change:** leave `IAgent` untouched (so `InMemoryAgentManager` keeps working and nothing else breaks) and introduce a small optional interface that a richer provider can also implement. Matched to the existing file's conventions — block-scoped namespace `DNAPlatform.AgentFramework`, plain classes with `{ get; set; }`, nullable annotations, no XML docs in the stub file.

```csharp
// PROPOSAL ONLY — illustrative. Would live in a NEW file, e.g.
// src/06_Skills/AgentStreaming.cs, leaving AgentFrameworkStubs.cs untouched.

namespace DNAPlatform.AgentFramework
{
    // Optional capability interface. An agent that can stream its response and
    // honour cancellation implements this alongside IAgent. Kept separate so the
    // existing IAgent contract (and every current implementation) stays valid.
    public interface IStreamingAgent
    {
        IAsyncEnumerable<AgentResponseChunk> RunStreaming(
            string prompt,
            Dictionary<string, object>? context = null,
            CancellationToken cancellationToken = default);
    }

    public class AgentResponseChunk
    {
        public string Delta { get; set; } = string.Empty;

        // True on the terminal chunk so the caller can close the stream cleanly.
        public bool IsFinal { get; set; }

        public int? TokensUsed { get; set; }
    }

    // What kind of provider backs an agent. Lets one IAgentManager serve both
    // implementations and lets the API report which is active.
    public enum AgentProviderKind
    {
        InMemory = 0,
        MicrosoftAgentFramework = 1
    }

    // Factory seam. MAF lives entirely behind this; nothing above it should
    // reference Microsoft.Agents.AI types.
    public interface IAgentProviderFactory
    {
        AgentProviderKind Kind { get; }

        // Returns true when the provider is usable (package present, model
        // endpoint reachable). Keeps /api/agents working when it is not.
        Task<bool> IsAvailable();

        Task<IAgent> CreateAgent(string agentId, AgentConfig config);
    }
}
```

The MAF implementation would adapt `AIAgent` -> `IAgent` + `IStreamingAgent`, translating `RunAsync` into the existing `AgentResponse` (`Response`, `TokensUsed`) and `RunStreamingAsync` into `AgentResponseChunk`. `AgentConfig` already carries `SystemPrompt`, `Model`, `Temperature`, `MaxTokens` and `CustomConfig`, which is enough to seed an MAF `ChatClientAgent` without schema changes.

Note the deliberate constraint: **`Microsoft.Agents.AI` types must not appear in any interface this platform owns.** They stay inside the adapter. That is what keeps the seam honest and makes MAF removable.

### DI registration change (illustrative)

Current state, `src/05_DevUI/DNAPlatform.DevUI.API/Program.cs:77`:

```csharp
builder.Services.AddSingleton<IAgentManager, InMemoryAgentManager>();
```

Proposed shape — note the provider is resolved from configuration, and `InMemoryAgentManager` stays the default so a missing MAF package is a degraded response, never a startup crash:

```csharp
// PROPOSAL ONLY. Existing registration above stays.
builder.Services.AddSingleton<IAgentProviderFactory, InMemoryAgentProviderFactory>();
builder.Services.AddSingleton<AgentProviderOptions>(...);   // "InMemory" | "MicrosoftAgentFramework"

// MicrosoftAgentFrameworkAgentProviderFactory lives behind a conditional
// registration so the assembly only loads when the feature is switched on.
builder.Services.AddSingleton<IAgentProviderFactory, MicrosoftAgentFrameworkAgentProviderFactory>();

// Manager picks a provider per agent based on Agent.type / config, and falls
// back to in-memory when the requested provider reports IsAvailable() == false.
builder.Services.AddSingleton<IAgentManager, InMemoryAgentManager>();
```

Because `InMemoryAgentManager` holds a `ConcurrentDictionary<string, IAgent>` today, selecting a provider per agent is a small change to `CreateAgent` — dispatch to the factory named by `AgentConfig.AgentType` instead of always constructing `InMemoryAgent`.
---

## 5. Agent configuration model (proposal only)

### Constraint

`Agent` in `src/05_DevUI/DNAPlatform.DevUI.Web/prisma/schema.prisma:24-32` is 7 flat columns and is read/written by the existing SvelteKit `/api/agents` routes. **It must stay backward compatible** — same column names, same envelope (`{ agents }`, `{ agent }`).

### Second constraint: SQLite today, PostgreSQL later

The datasource is currently `provider = "sqlite"`, and SQLite has no `Json` column type, so every blob column is `String` and `JSON.parse`d in the route handler. **JSONB is therefore not available yet** — it arrives with `refactor.md` section 16 priority #8. The sketch below targets the PostgreSQL schema; the interim form is noted.

### `Agent` unchanged

```prisma
model Agent {
  id          String   @id @default(cuid())
  name        String
  type        String   @default("StandardAgent")
  prompt      String?
  model       String?  @default("gpt-4")
  createdAt   DateTime @default(now())
  updatedAt   DateTime @updatedAt

  // Backward-compatible addition: nullable 1:1 detail row.
  // Existing rows and existing routes keep working unchanged.
  configuration AgentConfiguration?
}
```

### `AgentConfiguration` — relational where it matters, JSONB where it genuinely helps

section 14 says "Use JSON/JSONB where schema flexibility is genuinely useful, but keep important relationships and searchable fields relational." Splitting on that line:

| Goes **relational** | Why |
|---|---|
| `agentId`, `providerKind`, `enabled` | Filterable, indexed, drives provider selection |
| `modelProvider`, `modelName` | Searchable, comparable across agents |
| `temperature`, `maxTokens` | Scalar, typed, defaults at the column |
| `runtimeEnvironmentId` | Real FK into the `RuntimeEnvironment` model section 14 names |
| `memoryStrategy` | Enum-like, queried by the memory subsystem |
| `embeddingModel` | Tied to the pgvector table section 13 mandates |

| Goes **JSONB** | Why |
|---|---|
| `tools` | Heterogeneous per-tool config; shape differs per tool. Querying individual tools is not a real use case |
| `mcpServers` | Transport-specific blobs (stdio command+args, HTTP url+headers). Changes shape with every MCP feature |
| `permissions` | Nested allow/deny sets per tool and per path. Genuinely tree-shaped |
| `riceConfig` | section 12's RICE block is a document, not a relation |
| `vectorKnowledge` | Pointers/ids to pgvector rows plus retrieval params; the vectors themselves live in pgvector per section 13 |
| `modelExtras` | Escape hatch for provider-specific knobs (top_p, frequency_penalty, reasoning effort) — avoids a column per vendor |

```prisma
// PROPOSAL ONLY. Targets PostgreSQL (section 13 / section 16 priority #8).
// On SQLite keep provider = "sqlite" and declare these five as String,
// JSON.parse/JSON.stringify at the route boundary — same as Workflow.nodes today.

model AgentConfiguration {
  agentId            String   @id
  agent              Agent    @relation(fields: [agentId], references: [id], onDelete: Cascade)

  // ---- relational: filtered, compared, indexed ----
  providerKind       String   @default("InMemory")   // InMemory | MicrosoftAgentFramework
  enabled            Boolean  @default(true)
  modelProvider      String?                         // Ollama | OpenAI | AzureOpenAI | Foundry
  modelName          String?
  temperature        Float?
  maxTokens          Int?
  memoryStrategy     String   @default("None")       // None | Session | PgVector
  embeddingModel     String?
  runtimeEnvironmentId String?

  // ---- JSONB: genuinely flexible, not queried field-by-field ----
  tools              Json?     // [{ skillId, name, config }]
  mcpServers         Json?     // [{ name, transport: stdio|http, command?, args?, url?, env? }]
  permissions        Json?     // { toolAllow: [], toolDeny: [], pathAllow: [], pathDeny: [] }
  riceConfig         Json?     // section 12 RICE block
  vectorKnowledge    Json?     // { collection, topK, minScore, sourceIds: [] }
  modelExtras        Json?     // provider-specific escape hatch

  createdAt          DateTime @default(now())
  updatedAt          DateTime @updatedAt

  @@index([providerKind])
  @@index([modelProvider, modelName])
}
```

Per section 13, embeddings live in PostgreSQL + pgvector, **not** a separate vector database — `vectorKnowledge` above only holds pointers and retrieval parameters.

---

## 6. Phased plan (respects `refactor.md` section 16)

**Phase 0 is not MAF. Phase 0 is `refactor.md` section 16 items 1-6.** Nothing below should start until the realtime-logging and ACP/CLI work lands, because both change the shape of what an agent has to plug into.

| Phase | Content | MAF involved? |
|---|---|---|
| **0** | section 16 **1-6**: realtime execution/log streaming; ACP + CLI; workflow Configure/Test; mise/runtime status UI; designer UX; Skills UI placeholders | **No** |
| **1** | **Spike, throwaway.** A scratch `net8.0` console project (not in `DNAPlatform.sln`) that references `Microsoft.Agents.AI` + `OllamaSharp`, runs one agent against local Ollama, and — critically — exercises `RunStreamingAsync` + `CancellationToken` on **net8.0**. Answers the `UNVERIFIED` rows in section 2. Delete or keep out-of-tree. | Yes, isolated |
| **2** | Land the **seam** from section 4 independent of MAF: `IStreamingAgent`, `AgentProviderKind`, `IAgentProviderFactory`, `IAgent.RunStreaming`, provider dispatch in `IAgentManager`. No new NuGet package in the solution yet. | No |
| **3** | Agent **configuration model** (section 5 above) as a data-only change: `AgentConfiguration` + backward-compatible `Agent` relation, plus the Agent-page UI for section 12's configuration areas. Still provider-agnostic. | No |
| **4** | **Add MAF as a provider** behind `IAgentProviderFactory`, gated by `providerKind` and `IsAvailable()`. Reuse the `AgentConfiguration` rows already created in Phase 3. Default remains `InMemory`. This is `refactor.md` section 16 **priority #7**. | Yes |
| **5** | MCP: wire `Microsoft.Agents.AI.Mcp` to the Skills-page "Import MCP Plugins" action from section 16 **priority #6** — but only after Phase 4 is stable, and accepting that the .NET MCP package is **alpha** | Yes |
| **6** | PostgreSQL + pgvector consolidation (section 16 **priority #8**): flip the datasource, convert the JSON columns to real `Json`, add vector storage for `vectorKnowledge`. Also fixes the SQLite `String`-blob workaround repo-wide. | Indirect |

Hard rule for Phase 4 onward: **`Microsoft.Agents.AI.Workflows` is not adopted.** `IWorkflowOrchestrator` remains the single execution engine (section 15). If a future requirement genuinely needs MAF graph orchestration, that is a separate decision requiring its own refactor.md amendment.
---

## 7. Risks and open questions

### Risks

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| 1 | **Two agent abstractions.** The repo's `IAgent` vs MAF's `AIAgent`. Types leak, adapters multiply, and `refactor.md` section 15 forbids parallel implementations of one concept | **High** | Keep MAF types strictly inside `MicrosoftAgentFrameworkAgentProviderFactory`. Contract-test every provider against `IAgent` + `IStreamingAgent` so behaviour cannot drift. If MAF has to reach outside its adapter to make something work, that is the signal to stop |
| 2 | **Two `Workflow` abstractions.** MAF's graph `Workflow` collides by name and by concept with this repo's `Workflow`/`IWorkflowOrchestrator` — the clearest section 15 violation available | **High** | Do not reference `Microsoft.Agents.AI.Workflows`. Phase 4 scope is `Microsoft.Agents.AI` only. Enforce with a review rule: no `using Microsoft.Agents.AI.Workflows` outside a spike |
| 3 | **.NET 8 dependency drift.** MAF 1.23.0 pulls `Microsoft.Extensions.*` **10.x** packages into an ASP.NET Core 8 app | Medium-High | Phase 1 spike exists solely to test this. Pin exact versions if adopted; keep the transitive graph small |
| 4 | **Weekly release cadence.** 1.13 -> 1.23 in under three months; a minor bump can move dependencies under you | Medium | Pin the exact version. Upgrade deliberately, not on a floating range |
| 5 | **Licence / commercial-surface ambiguity.** The code is MIT (verified), but Foundry/Azure packages pull Microsoft Product Terms, and the README explicitly says third-party and non-Azure-Direct models are "Non-Microsoft Products" governed by their own terms | Medium | Adopt only the core + Ollama path, which has no Microsoft service dependency. Legal review before any Foundry/Azure component. Keep a note that "MIT repo" does not mean "all Azure-adjacent components are MIT" |
| 6 | **Dependency weight.** ~14 transitive packages for a layer that currently echoes a string | Medium | It is why this is Phase 4, not Phase 0 |
| 7 | **Alpha MCP package.** `Microsoft.Agents.AI.Mcp` is prerelease | Medium | Phase 5 only, isolated, behind the Skills-page shell. Re-evaluate at 1.x |
| 8 | **`ISkillRegistry` duplication.** MAF has its own tool/MCP model alongside `ISkillRegistry` | Medium | Decide once: `DNAPlatform.Skills` stays the platform's skill contract; MAF tools are adapted *into* `ISkill`, never the reverse |
| 9 | **Naming collision.** MAF's `Microsoft.Agents.AI.DevUI` is unrelated to this repo's DevUI | Low | Document it; nothing shared |
| 10 | **Fast-moving 1.x API.** `AIAgent` is 1.x and still evolving | Low | The adapter is the only file that needs to change when it does |

### Open questions

1. **Does `net8.0` + ASP.NET Core 8 actually behave** with `Microsoft.Extensions.*` 10.x in the graph? Unverified; Phase 1 answers it.
2. **.NET OpenAI-compatible endpoint support** (pointing `OpenAIChatClient` at `http://localhost:11434/v1/`) is documented for Python; the .NET samples show only `OllamaSharp`. Confirm before designing around it.
3. Is `Microsoft.Agents.AI.Mcp` reaching 1.x, and on what timeline? Affects whether Phase 5 is worth starting.
4. Is there any intent at Microsoft to add ACP to MAF? Today it is absent. This matters because `refactor.md` section 7 mandates ACP — and **MAF cannot satisfy it**, so ACP must be built against the execution services independently (section 15: "Create an ACP adapter/service around the existing execution/agent infrastructure").
5. How does the `AgentSession` serialize/deserialize cycle map onto a `WorkflowEvent`/`Execution` row for durable replay (section 13)?
6. Should `agent-framework-durable-extension` ever be considered? It targets Azure Functions / durable execution — likely out of scope for a local-first platform, but not formally ruled out.

---

## 8. Sources

| URL | What it supports |
|---|---|
| https://github.com/microsoft/agent-framework | What MAF is; MIT licence badge; provider + orchestration feature list; `dotnet add package Microsoft.Agents.AI`; .NET quickstart; "Important Notes" third-party commercial-surface language |
| https://raw.githubusercontent.com/microsoft/agent-framework/main/LICENSE | MIT licence, full text, "Copyright (c) Microsoft Corporation" |
| https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/ | 1.0 GA for .NET and Python; stable APIs; long-term support commitment; NuGet ID |
| https://www.nuget.org/packages/Microsoft.Agents.AI/ | Current stable 1.23.0 (2026-09-29); `net8.0` compatibility; full transitive dependency list incl. `Microsoft.Extensions.AI` 10.x and `Microsoft.ML.Tokenizers` |
| https://github.com/microsoft/agent-framework/blob/main/dotnet/Directory.Build.props | `TargetFrameworksCore = net10.0;net9.0;net8.0`; netstandard2.0/net472; `IsAotCompatible` |
| https://github.com/microsoft/agent-framework/tree/main/dotnet/src | .NET package inventory — `Microsoft.Agents.AI.Mcp`, `.A2A`, `.AGUI`, `.DevUI`, `.Workflows`, `.Hosting*`. Establishes **ACP absence** |
| https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent | `AIAgent` surface: `RunAsync`/`RunStreamingAsync`/`CreateSessionAsync`/`SerializeSessionAsync`; session ownership |
| https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent.runasync | `RunAsync` overloads and `CancellationToken` |
| https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.aiagent.runstreamingasync | `RunStreamingAsync` returning `IAsyncEnumerable<AgentResponseUpdate>`; `AgentSession`/`AgentRunOptions` params |
| https://learn.microsoft.com/en-us/agent-framework/concepts/agents/running-agents/ | Streaming vs non-streaming invocation; `ChatClientAgentRunOptions` merging into `ChatOptions` |
| https://learn.microsoft.com/en-us/agent-framework/concepts/agents/middleware/ | `.AsBuilder().Use(...)` middleware pipeline, run + streaming |
| https://learn.microsoft.com/en-us/agent-framework/get-started/hosting | DI: `AddAIAgent`, `IHostedAgentBuilder`, `.WithAITool()`, `.WithInMemorySessionStore()`, `AddWorkflow`; **hosting protocol table (A2A / OpenAI-compatible / Durable / AG-UI — no ACP)** |
| https://learn.microsoft.com/en-us/agent-framework/hosting/self-hosting/ | Self-hosting protocol integration list (OpenAI, A2A, AG-UI) — corroborates no ACP |
| https://learn.microsoft.com/en-us/agent-framework/hosting/self-hosting/openai-endpoints/ | OpenAI-compatible endpoint hosting and `base_url` client support |
| https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/model-providers/ollama | **Ollama .NET path** via `OllamaSharp` -> `AsIChatClient` -> `AsAIAgent`; local-first claim; Python `base_url` variant |
| https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools/ | MCP tool integration in .NET; `ModelContextProtocol` package |
| https://www.nuget.org/packages/Microsoft.Agents.AI.Mcp/1.22.0-alpha.260918.1 | `Microsoft.Agents.AI.Mcp` version + **alpha** status; `ModelContextProtocol` >= 2.2.0; net8.0 support |
| https://devblogs.microsoft.com/agent-framework/discover-agent-skills-from-mcp-servers-in-net/ | MCP-based agent skills; `UseMcpSkills()` on `AgentSkillsProviderBuilder` |
| https://github.com/microsoft/agent-framework/pull/6108 | MCP skills implementation (PR source of truth for new types) |
| https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-autogen | AutoGen superseded; MAF built by the core AutoGen + SK teams; Workflow vs GraphFlow; streaming; MCP parity |
| https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel | Semantic Kernel migration path |
| https://www.nuget.org/packages/Microsoft.Extensions.AI/10.10.0 | `IChatClient` abstraction; **dependency direction** — SK depends on Extensions.AI, not the reverse; OllamaSharp depends on it |
| https://github.com/microsoft/agent-framework/blob/main/dotnet/src/Microsoft.Agents.AI/ChatClient/ChatClientAgent.cs | `ChatClientAgent` constructor: `IChatClient`, `instructions`, `name`, `description`, `tools` |
| https://github.com/microsoft/agent-framework/blob/main/dotnet/src/Microsoft.Agents.AI/AgentExtensions.cs | `AsAIFunction()`, `AsBuilder()` — agents-as-tools and composition |
| https://github.com/microsoft/agent-framework/blob/main/TRANSPARENCY_FAQ.md | Supported platform versions incl. .NET 8.0; provider-neutrality; external-service data boundary and licensing caveats |

### Local sources

| Path | Used for |
|---|---|
| `docs/refactor.md` | sections 1, 3, 7, 8, 10, 12, 13, 14, 15, 16, 17 constraints and priority order |
| `src/06_Skills/AgentFrameworkStubs.cs` | `IAgent` shape (lines 65-71), `IAgentManager`, `AgentConfig`, `ISkillRegistry`, stub code style |
| `src/05_DevUI/DNAPlatform.DevUI.API/Services/InMemoryAgentFramework.cs` | `InMemoryAgentManager`, `InMemoryAgent.Run` echo behaviour (lines 180-188) |
| `src/05_DevUI/DNAPlatform.DevUI.API/Program.cs` | Current DI registrations (lines 62-83) |
| `src/05_DevUI/DNAPlatform.DevUI.API/DNAPlatform.DevUI.API.csproj` | `net8.0`, ASP.NET Core 8 packages |
| `src/05_DevUI/DNAPlatform.DevUI.Web/prisma/schema.prisma` | `Agent` 7-column model (lines 24-32); `provider = "sqlite"` constraint |
