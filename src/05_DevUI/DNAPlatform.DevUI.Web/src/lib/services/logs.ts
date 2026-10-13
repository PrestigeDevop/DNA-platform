/**
 * Unified log bus for the DevUI.
 *
 * Collects two streams into one list so the web Logs page shows everything:
 *   1. FRONTEND - console.log/warn/error, window errors, and explicit log calls
 *      issued by the app (api.ts, page components, ...).
 *   2. BACKEND  - streamed from the .NET API via SSE at GET /api/logs/stream, with
 *      a fallback to polling GET /api/logs if EventSource fails.
 *
 * The backend writes through ILogger, and LogStoreLoggerProvider mirrors every
 * record into the in-memory buffer, so if a line appears in the CLI it also
 * appears here (and vice versa).
 */
import { writable, get } from 'svelte/store';

export type LogSource = "frontend" | "backend";

export interface LogRecord {
  id: number;
  timestamp: string;
  level: string;
  category: string;
  message: string;
  source: LogSource;
  exception?: string | null;
}

/** Live list of merged log records (oldest first). */
export const logRecords = writable<LogRecord[]>([]);
/** True while backend streaming or polling is active. */
export const logStreaming = writable<boolean>(false);
/** Last streaming/polling error, if the backend is unreachable. */
export const logStreamError = writable<string>("");

const MAX_RECORDS = 500;
const POLL_INTERVAL_MS = 1500;
const RECONNECT_DELAY_MS = 5000;

// Frontend ids count DOWN from -1 so they never collide with backend ids (positive).
let frontendSeq = 0;
let backendCursor = 0;
let pollTimer: ReturnType<typeof setInterval> | null = null;
let consoleCaptured = false;
let eventSource: EventSource | null = null;
let reconnectTimer: ReturnType<typeof setTimeout> | null = null;

const NOISY_PATTERNS = ["[vite]", "[HMR]", "Download the Svelte"];

function isNoisy(text: string): boolean {
  return NOISY_PATTERNS.some((p) => text.includes(p));
}

function push(record: LogRecord) {
  if (isNoisy(record.message)) return;
  logRecords.update((list) => {
    const next = [...list, record];
    return next.length > MAX_RECORDS ? next.slice(next.length - MAX_RECORDS) : next;
  });
}

/** Normalise anything (Error, object, string) into a readable message. */
function stringify(value: unknown): string {
  if (value === null || value === undefined) return String(value);
  if (typeof value === "string") return value;
  if (value instanceof Error) return value.name + ": " + value.message;
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

/** Write a frontend-originated record into the log bus. */
export function logFrontend(level: string, message: string, category = "frontend") {
  frontendSeq += 1;
  const record: LogRecord = {
    id: -frontendSeq,
    timestamp: new Date().toISOString(),
    level,
    category,
    message,
    source: "frontend"
  };
  push(record);
  return record;
}

export const log = {
  debug: (m: string, c = "frontend") => logFrontend("Debug", m, c),
  info: (m: string, c = "frontend") => logFrontend("Information", m, c),
  warn: (m: string, c = "frontend") => logFrontend("Warning", m, c),
  error: (m: string, c = "frontend") => logFrontend("Error", m, c)
};

/**
 * Mirror browser console output + uncaught errors into the log bus.
 * Idempotent - safe to call from every page load.
 */
export function captureConsole() {
  if (consoleCaptured || typeof window === "undefined") return;
  consoleCaptured = true;

  const original = {
    log: console.log.bind(console),
    info: console.info.bind(console),
    warn: console.warn.bind(console),
    error: console.error.bind(console)
  };

  console.log = (...args: unknown[]) => {
    original.log(...args);
    logFrontend("Information", args.map(stringify).join(" "));
  };
  console.info = (...args: unknown[]) => {
    original.info(...args);
    logFrontend("Information", args.map(stringify).join(" "));
  };
  console.warn = (...args: unknown[]) => {
    original.warn(...args);
    logFrontend("Warning", args.map(stringify).join(" "));
  };
  console.error = (...args: unknown[]) => {
    original.error(...args);
    logFrontend("Error", args.map(stringify).join(" "));
  };

  window.addEventListener("error", (event) => {
    logFrontend("Error", "Uncaught: " + event.message + " (" + event.filename + ":" + event.lineno + ")", "window");
  });

  window.addEventListener("unhandledrejection", (event) => {
    logFrontend("Error", "Unhandled promise rejection: " + stringify(event.reason), "window");
  });

  logFrontend("Information", "Frontend log capture started", "logs");
}

/**
 * Start SSE streaming from the backend log buffer.
 * Pushes all backend entries into the shared logRecords store.
 * Falls back to polling if the server sends an error or disconnects.
 */
export function startLogStream() {
  if (typeof window === "undefined") return;
  if (eventSource) return; // already connected

  eventSource = new EventSource("/api/logs/stream");

  eventSource.addEventListener("open", () => {
    logStreaming.set(true);
    logStreamError.set("");
  });

  // The backend sends named SSE events: "event: log\ndata: {json}\n\n"
  eventSource.addEventListener("log", (e: MessageEvent) => {
    try {
      const entry = JSON.parse(e.data);
      push({
        id: entry.id,
        timestamp: entry.timestamp,
        level: entry.level,
        category: entry.category,
        message: entry.message,
        exception: entry.exception ?? null,
        source: "backend"
      } as LogRecord);
      if (entry.id > backendCursor) backendCursor = entry.id;
    } catch {
      // ignore malformed frames
    }
  });

  eventSource.addEventListener("error", () => {
    if (eventSource) {
      eventSource.close();
      eventSource = null;
    }
    logStreaming.set(false);

    // Fall back to polling after a brief delay
    reconnectTimer = setTimeout(() => {
      fallbackToPolling();
    }, RECONNECT_DELAY_MS);
  });
}

/**
 * Polling fallback for when SSE is unavailable (e.g., behind a proxy that
 * buffers responses). Fetches /api/logs?sinceId=... and merges new entries
 * into the shared logRecords store.
 */
function fallbackToPolling() {
  if (typeof window === "undefined" || pollTimer) return;

  logFrontend("Warning", "SSE unavailable, falling back to polling", "logs");

  const tick = async () => {
    try {
      const res = await fetch("/api/logs?sinceId=" + backendCursor + "&limit=200");
      if (!res.ok) {
        logStreamError.set("Backend returned " + res.status);
        return;
      }
      const payload = await res.json();
      const entries = Array.isArray(payload.logs) ? payload.logs : [];

      for (const entry of entries) {
        push({
          id: entry.id,
          timestamp: entry.timestamp,
          level: entry.level,
          category: entry.category,
          message: entry.message,
          exception: entry.exception ?? null,
          source: "backend"
        } as LogRecord);
        if (entry.id > backendCursor) backendCursor = entry.id;
      }

      logStreaming.set(true);
      logStreamError.set("");
    } catch (e) {
      logStreaming.set(false);
      logStreamError.set(stringify(e));
    }
  };

  pollTimer = setInterval(tick, POLL_INTERVAL_MS);
  void tick();
  logStreaming.set(true);
}

/** Stop SSE streaming and/or polling. */
export function stopLogStream() {
  if (eventSource) {
    eventSource.close();
    eventSource = null;
  }
  if (reconnectTimer) {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;
  }
  if (pollTimer) {
    clearInterval(pollTimer);
    pollTimer = null;
  }
  logStreaming.set(false);
}

/** Clear both the local list and the backend buffer. */
export async function clearLogs() {
  logRecords.set([]);
  backendCursor = 0;
  frontendSeq = 0;

  try {
    const res = await fetch("/api/logs", { method: "DELETE" });
    if (!res.ok) logStreamError.set("Clear failed: " + res.status);
  } catch (e) {
    logStreamError.set(stringify(e));
  }

  logFrontend("Information", "Log buffer cleared", "logs");
}

/** Current snapshot without subscribing (handy inside components). */
export function currentLogs(): LogRecord[] {
  return get(logRecords);
}



