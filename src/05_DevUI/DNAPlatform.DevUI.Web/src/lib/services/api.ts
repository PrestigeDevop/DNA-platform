const BASE_URL = "";

interface ApiResponse<T> {
  [key: string]: T;
}

/** One structural check reported by `POST /api/workflows/validate`. */
export interface ValidationCheck {
  name: string;
  passed: boolean;
  detail?: string | null;
}

/**
 * Response shape of `POST /api/workflows/validate` (Configure -> Test -> Run).
 * `checks` is echoed so the UI can state what was actually verified instead of
 * implying the definition was fully checked.
 */
export interface ValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  checks: ValidationCheck[];
  summary: string;
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, options);
  if (!res.ok) {
    const error = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(error.error || error.message || res.statusText);
  }
  return res.json() as Promise<T>;
}

export const api = {
  // --- Health & Info ---
  async health() {
    return request<any>("/health");
  },

  async getInfo() {
    return request<any>("/api/info");
  },

  // --- Skills (proxied to .NET backend) ---
  async getSkills() {
    const data = await request<ApiResponse<any[]>>("/api/skills");
    return data.skills || [];
  },

  async getSkill(skillId: string) {
    return request<any>(`/api/skills/${skillId}`);
  },

  async executeSkill(skillId: string, inputs?: Record<string, any>) {
    return request<any>(`/api/skills/${skillId}/execute`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(inputs || {})
    });
  },

  // --- Snippets (SvelteKit server-backed CRUD + backend execute) ---
  async getSnippets() {
    const data = await request<ApiResponse<any[]>>("/api/snippets");
    return data.snippets || [];
  },

  async createSnippet(snippet: any) {
    return request<any>("/api/snippets", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(snippet)
    });
  },

  async updateSnippet(id: string, snippet: any) {
    return request<any>(`/api/snippets/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(snippet)
    });
  },

  async deleteSnippet(id: string) {
    return request<any>(`/api/snippets/${id}`, { method: "DELETE" });
  },

  async executeSnippet(id: string, inputs?: Record<string, any>) {
    return request<any>(`/api/snippets/${id}/execute`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(inputs || {})
    });
  },

  // --- Node Types & Status Values (proxied to .NET backend) ---
  async getNodeTypes() {
    return request<any>("/api/node-types");
  },

  async getStatusValues() {
    return request<any>("/api/status-values");
  },

  // --- Workflows (SvelteKit server endpoints with Prisma) ---
  async getWorkflows() {
    const data = await request<ApiResponse<any[]>>("/api/workflows");
    return data.workflows || [];
  },

  async createWorkflow(workflow: any) {
    return request<any>("/api/workflows", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(workflow)
    });
  },

  async updateWorkflow(id: string, workflow: any) {
    return request<any>(`/api/workflows/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(workflow)
    });
  },

  async deleteWorkflow(id: string) {
    return request<any>(`/api/workflows/${id}`, { method: "DELETE" });
  },

  async executeWorkflowById(id: string) {
    const data = await request<any>(`/api/workflows/${id}/execute`, { method: "POST" });
    return data;
  },

  // --- Workflow Execution (directly to .NET backend, no persistence) ---
  async executeWorkflow(workflow: any) {
    return request<any>("/api/workflows/execute", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(workflow)
    });
  },

  async executeWorkflowWithContext(workflow: any, context?: Record<string, any>) {
    return request<any>("/api/workflows/execute-with-context", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ workflow, context: context || {} })
    });
  },

  // --- Agents (SvelteKit server endpoints with Prisma) ---
  async getAgents() {
    const data = await request<ApiResponse<any[]>>("/api/agents");
    return data.agents || [];
  },

  async createAgent(agent: any) {
    return request<any>("/api/agents", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(agent)
    });
  },

  async deleteAgent(id: string) {
    return request<any>(`/api/agents/${id}`, { method: "DELETE" });
  },

  // --- Workflow Test (Configure -> Test -> Run, docs/refactor.md section 4) ---
  // STRUCTURAL validation only, served by .NET `POST /api/workflows/validate`
  // through the Vite proxy. Always 200 when the body deserializes. Fields are
  // read case-tolerantly: ASP.NET Core camelCases, but a PascalCase payload must
  // not render as "invalid".
  async validateWorkflow(definition: {
    name?: string;
    nodes?: unknown[];
    connections?: unknown[];
  }): Promise<ValidationResult> {
    const raw = await request<Record<string, any>>("/api/workflows/validate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(definition)
    });

    return {
      isValid: raw.isValid ?? raw.IsValid ?? false,
      errors: raw.errors ?? raw.Errors ?? [],
      warnings: raw.warnings ?? raw.Warnings ?? [],
      checks: raw.checks ?? raw.Checks ?? [],
      summary: raw.summary ?? raw.Summary ?? ""
    };
  },

  // --- Executions (SvelteKit server endpoints with Prisma) ---
  async getExecutions() {
    const data = await request<ApiResponse<any[]>>("/api/executions");
    return data.executions || [];
  },

  async getExecution(id: string) {
    return request<any>(`/api/executions/${id}`);
  }
};
