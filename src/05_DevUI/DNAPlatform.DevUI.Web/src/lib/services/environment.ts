/**
 * Typed client for the local execution environment (mise) endpoints.
 *
 * These are served by `EnvironmentController` on the .NET backend (:5254) and
 * proxied by vite (`/api/environment` -> http://127.0.0.1:5254). There is no
 * SvelteKit route for them, so calls must use relative paths.
 *
 * Backs the workflow-level Configure view (docs/refactor.md section 4).
 * Mirrors $lib/services/api.ts: same `request<T>()` helper, same empty BASE_URL,
 * same "throw Error(message)" failure style, one method per endpoint.
 *
 * NOTE: ASP.NET Core serializes with the default camelCase naming policy, so
 * every property below is camelCase on the wire.
 */

/** GREEN / YELLOW / RED states from docs/refactor.md section 4, lines 141-161. */
export type EnvironmentStatusLevel = "green" | "yellow" | "red";

/**
 * Where a runtime version comes from. "mise" means mise.toml provides it,
 * "platform-default" means the platform-provided fallback is used instead.
 */
export type RuntimeSource = "mise" | "platform-default" | "none";

/** One runtime (mise tool) requested for this project. */
export interface RuntimeStatus {
  /** mise tool id, e.g. "dotnet" or "node". */
  id: string;
  /** Human readable name, e.g. ".NET". */
  name: string;
  /** Version request from mise.toml, or the platform default request. */
  requestedVersion: string | null;
  /** Concrete version mise resolved; null when nothing was resolved. */
  resolvedVersion: string | null;
  installed: boolean;
  /** True when mise reports this version as the active one for the project. */
  active: boolean;
  source: RuntimeSource;
  status: EnvironmentStatusLevel;
  /** Backend explanation of the state, rendered verbatim in the UI. */
  message: string | null;
}

/** A task declared by mise (from mise.toml or an inherited config). */
export interface MiseTask {
  name: string;
  description: string | null;
}

/** Body of GET /api/environment and POST /api/environment/verify. */
export interface EnvironmentStatus {
  name: string;
  /** Directory holding mise.toml, or "" when none was found. */
  projectRoot: string | null;
  miseInstalled: boolean;
  /** mise CLI version, or null when mise is unavailable. */
  miseVersion: string | null;
  /** True when MISE_EXPERIMENTAL is on (required by the mise MCP server). */
  experimentalEnabled: boolean;
  /** Worst of the per-runtime states: red > yellow > green. */
  overallStatus: EnvironmentStatusLevel;
  runtimes: RuntimeStatus[];
  tasks: MiseTask[];
  /** ISO timestamp of the last check. */
  checkedAt: string | null;
}

/** Body of a successful POST /api/environment/mise/import. */
export interface MiseImportResult {
  success: boolean;
  /** Absolute path written; empty on failure. */
  path: string | null;
  message?: string | null;
}

/**
 * Result of POST /api/environment/mise/export. A project without mise.toml is a
 * normal state (the backend answers 404), so it is modelled as a value instead
 * of a thrown error.
 */
export type MiseExportResult =
  | { found: true; content: string }
  | { found: false; message: string | null };

/** Per-call options; `signal` lets the caller abort in-flight requests. */
export interface EnvironmentCallOptions {
  signal?: AbortSignal;
}

const BASE_URL = "";

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(BASE_URL + url, options);
  if (!res.ok) {
    const error = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(error.error || error.message || res.statusText);
  }
  return res.json() as Promise<T>;
}

export const environmentApi = {
  /** Snapshot of the environment. Always 200, mise or no mise. */
  async getEnvironment(options: EnvironmentCallOptions = {}): Promise<EnvironmentStatus> {
    return request<EnvironmentStatus>("/api/environment", { signal: options.signal });
  },

  /** Re-runs every check and returns a fresh snapshot. Same payload as get. */
  async verifyEnvironment(options: EnvironmentCallOptions = {}): Promise<EnvironmentStatus> {
    return request<EnvironmentStatus>("/api/environment/verify", {
      method: "POST",
      signal: options.signal
    });
  },

  /**
   * POST /api/environment/mise/export
   * A 404 means "this project has no mise.toml yet", which the caller renders
   * as an empty state, so it resolves instead of throwing.
   */
  async exportMise(options: EnvironmentCallOptions = {}): Promise<MiseExportResult> {
    const res = await fetch(BASE_URL + "/api/environment/mise/export", {
      method: "POST",
      signal: options.signal
    });

    if (res.status === 404) {
      const body = await res.json().catch(() => null);
      const message = body && typeof body.message === "string" ? body.message : null;
      return { found: false, message };
    }

    if (!res.ok) {
      const body = await res.json().catch(() => ({ error: res.statusText }));
      throw new Error(body.error || body.message || res.statusText);
    }

    const data = await res.json();
    const content = typeof data?.content === "string" ? data.content : "";
    if (!content.trim()) {
      return { found: false, message: null };
    }
    return { found: true, content };
  },

  /**
   * POST /api/environment/mise/import
   * Overwrites the project mise.toml. A rejected payload (empty, oversized, or
   * containing null bytes) throws with the backend `error` text so the caller
   * can show it inline.
   */
  async importMise(
    content: string,
    options: EnvironmentCallOptions = {}
  ): Promise<MiseImportResult> {
    const data = await request<MiseImportResult>("/api/environment/mise/import", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ content }),
      signal: options.signal
    });

    if (!data?.success) {
      throw new Error(data?.message || "The backend rejected this mise configuration.");
    }
    return data;
  }
};
