# DNA Platform - Infrastructure, Logs & TODO

**Low-Code/No-Code Bioinformatic Workflow Engine**

![Status](https://img.shields.io/badge/Status-Alpha-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-green)
![Node](https://img.shields.io/badge/Node.js-20+-green)
![Ollama](https://img.shields.io/badge/Ollama-Local_LLMs-orange)

---

## 📋 Table of Contents

- [Infrastructure](#infrastructure)
- [Service Architecture](#service-architecture)
- [API Endpoints](#api-endpoints)
- [Logging Configuration](#logging-configuration)
- [Environment Variables](#environment-variables)
- [TODO / Roadmap](#todo--roadmap)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)

---

## 🏗️ Infrastructure

### Prerequisites

| Component | Version | Purpose |
|-----------|---------|---------|
| .NET SDK | 10.0+ | Backend API & Workflow Engine |
| Node.js | 20+ | Svelte Frontend |
| Ollama | Latest | Local LLM Inference |
| Git | 2.0+ | Version Control |

### Ollama Setup (Local LLM Inference)

```bash
# Install Ollama (Windows)
# Download from: https://ollama.com/download

# Pull recommended models
ollama pull llama3.2        # General purpose (3B)

---

## 🔧 Service Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           DNA Platform                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    Svelte Frontend (DevUI)                       │   │
│  │  - Visual Workflow Designer                                      │   │
│  │  - Node Palette & Canvas                                         │   │
│  │  - Real-time Execution Monitor                                   │   │
│  │  - Agent & Skill Management                                      │   │
│  │  Port: 5173 (dev) / 3000 (prod)                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                              │                                           │
│                              │ HTTP/JSON (REST API)                      │
│                              ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              ASP.NET Core API (DNAPlatform.DevUI.API)            │   │
│  │  - /api/workflows     CRUD + Execute                             │   │
│  │  - /api/executions    Status, Pause, Resume, Cancel              │   │
│  │  - /api/agents        Create, Run, Bind Skills                   │   │
│  │  - /api/skills        List, Execute                              │   │
│  │  - /swagger           API Documentation                          │   │
│  │  - /health            Health Check                               │   │
│  │  Port: 5001 (HTTPS) / 5000 (HTTP)                                │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                              │                                           │
│                              │ In-Process / DI                          │
│                              ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │           DNAPlatform.AgentFramework (Core Engine)               │   │
│  │  - WorkflowOrchestrator (Topological DAG Execution)              │   │
│  │  - NodeExecutor (Individual Node Processing)                     │   │
│  │  - AgentManager (AI Agent Lifecycle)                             │   │
│  │  - SkillRegistry (Plugin System)                                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                              │                                           │
│              ┌───────────────┼───────────────┐                          │
│              ▼               ▼               ▼                          │
│  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐                 │
│  │ OllamaAgent   │ │ DefaultAgent  │ │ CustomAgents  │                 │
│  │ (Local LLM)   │ │ (Simulated)   │ │ (Future)      │                 │
│  │ Port: 11434   │ │               │ │               │                 │
│  └───────────────┘ └───────────────┘ └───────────────┘                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | SvelteKit | 2.x |
| Frontend | TypeScript | 5.x |
| Frontend | Tailwind CSS | 4.x |
| Frontend | Vite | 6.x |
| Backend | ASP.NET Core | 10.0 |
| Backend | C# | 12.0 |
| Backend | Swagger/OpenAPI | 6.9 |
| ORM | In-Memory Store | - |
| AI | Ollama | Latest |
| AI | Microsoft Semantic Kernel | Planned |

---

## 🌐 API Endpoints

### Workflows API (`/api/workflows`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/workflows` | List all workflows |
| GET | `/api/workflows/{id}` | Get workflow by ID |
| POST | `/api/workflows` | Create new workflow |
| PUT | `/api/workflows/{id}` | Update workflow |
| DELETE | `/api/workflows/{id}` | Delete workflow |
| POST | `/api/workflows/{id}/execute` | Execute workflow |
| POST | `/api/workflows/validate` | Validate workflow definition |

### Executions API (`/api/executions`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/executions/{executionId}` | Get execution status |
| POST | `/api/executions/{executionId}/pause` | Pause execution |
| POST | `/api/executions/{executionId}/resume` | Resume execution |
| POST | `/api/executions/{executionId}/cancel` | Cancel execution |

### Agents API (`/api/agents`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/agents` | List all agents |
| GET | `/api/agents/{agentId}` | Get agent details |
| POST | `/api/agents` | Create new agent |
| DELETE | `/api/agents/{agentId}` | Delete agent |
| POST | `/api/agents/{agentId}/run` | Run agent with prompt |
| POST | `/api/agents/{agentId}/bind-skill` | Bind skill to agent |

### Skills API (`/api/skills`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/skills` | List all skills |

---

## 📝 Logging Configuration

### Log Levels

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "DNAPlatform": "Debug",
      "DNAPlatform.AgentFramework": "Trace",
      "DNAPlatform.DevUI.API": "Debug"
    }
  }
}
```

### Log Categories

| Category | Level | Output |
|----------|-------|--------|
| `DNAPlatform.WorkflowOrchestrator` | Debug | Execution flow, node status |
| `DNAPlatform.NodeExecutor` | Debug | Node execution timing |
| `DNAPlatform.AgentManager` | Information | Agent creation/deletion |
| `DNAPlatform.OllamaAgent` | Trace | LLM prompts/responses |
| `Microsoft.AspNetCore` | Warning | HTTP request warnings |

### Console Output Format

```
[12:34:56 INF] DNA Platform - DevUI API Starting
[12:34:56 DBG] WorkflowOrchestrator: Starting execution wf-123
[12:34:57 DBG] NodeExecutor: Node 'analyze' completed in 45ms
[12:34:57 TRC] OllamaAgent: Received response from llama3.2 (150 tokens)
```

---

## 🔐 Environment Variables

### Development

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Environment name |
| `ASPNETCORE_URLS` | https://localhost:5001;http://localhost:5000 | API URLs |
| `OLLAMA_BASE_URL` | http://localhost:11434 | Ollama API URL |
| `OLLAMA_DEFAULT_MODEL` | llama3.2 | Default LLM model |
| `DEVUI_FRONTEND_URL` | http://localhost:5173 | CORS allowed origin |

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "llama3.2",
    "Timeout": 120000
  },
  "DevUI": {
    "FrontendUrl": "http://localhost:5173"

---

## ✅ TODO / Roadmap

### Phase 1: Foundation ✅ Complete
- [x] Core workflow orchestration engine
- [x] Node execution framework
- [x] Agent manager & skill registry
- [x] Console demo with 3 patterns
- [x] ASP.NET Core REST API
- [x] Swagger/OpenAPI documentation
- [x] In-memory workflow store

### Phase 2: DevUI API ✅ Complete
- [x] SvelteKit frontend setup
- [x] Prisma + SQLite persistence layer
- [x] Workflow CRUD endpoints
- [x] Agent CRUD endpoints
- [x] Execution monitoring API
- [x] Request logging (console + web Logs page)

### Phase 3: Web Designer + Custom Skills ✅ Complete
- [x] Custom skills framework (`DNAPlatform.Skills`)
- [x] Built-in + bioinformatics skills
- [x] GUI-triggered skill execution
- [x] Skill “Details” parameter editor
- [x] Custom snippet model + snippet CRUD
- [x] Snippet execute endpoint (hello-world .NET handler)
- [x] “Create Custom Snippet” dialog on `/skills`
- [x] Drag-and-drop designer integrated into `/workflows`
- [x] Designer IO ports from snippet/node metadata
- [x] Click node → inline inspect/edit panel
- [x] Connected services status panel

### Phase 4: Advanced Features 📋 Planned
- [ ] WebSocket real-time updates
- [ ] Workflow templates library
- [ ] Import/Export workflows (JSON)
- [ ] Version history
- [ ] User authentication
- [ ] Multi-user collaboration

### Phase 5: Bioinformatics 📋 Planned
- [ ] Sequence alignment skills
- [ ] FASTA/FASTQ file handling
- [ ] BLAST integration
- [ ] Statistical analysis nodes
- [ ] Visualization components
- [ ] R/Python script execution

### Phase 6: Production 📋 Planned
- [ ] Database persistence (PostgreSQL)
- [ ] Redis caching
- [ ] Docker containers
- [ ] Kubernetes deployment
- [ ] CI/CD pipeline
- [ ] Monitoring & alerting
- [ ] Unit tests
- [ ] Integration tests

  }
}
```


### File Logging (Optional)

```csharp
// Add to Program.cs for file logging
builder.Logging.AddFile("logs/dna-platform-{Date}.log");
```

| GET | `/api/skills/{skillId}` | Get skill details |
| GET | `/api/skills/category/{category}` | List skills by category |
| POST | `/api/skills/{skillId}/execute` | Execute skill directly |

### System Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | API info |
| GET | `/health` | Health check |
| GET | `/swagger` | Swagger UI |

| Runtime | .NET | 10.0 |
| Runtime | Node.js | 20+ |

ollama pull llama3.2:1b     # Lightweight model
ollama pull codellama       # Code-specific tasks
ollama pull mistral         # Alternative general model

# Verify Ollama is running
curl http://localhost:11434/api/tags

# Ollama runs as a Windows service by default
# API endpoint: http://localhost:11434
```

### System Requirements

- **RAM**: 8GB minimum (16GB recommended for LLM inference)
- **Disk**: 10GB free (models are 2-7GB each)
- **OS**: Windows 10/11, Linux, or macOS
- **GPU**: Optional (CUDA support for faster inference)
