/**
 * Unified log bus for the DevUI.
 *
 * Collects two streams into one list so the in-app console shows everything:
 *   1. FRONTEND - console.log/warn/error, window errors, and explicit log calls
 *      issued by the app (api.ts, page components, ...).
 *   2. BACKEND  - polled from the .NET API at GET /api/logs, which mirrors the
 *      exact same records the backend prints to its CLI console.
 *
 * The backend writes through ILogger, and LogStoreLoggerProvider mirrors every
 * record into the in-memory buffer, so if a line appears in the CLI it also
 * appears here (and vice versa).
 */
import { writable, get } from 'svelte/store';

export type LogSource = 'frontend' | 'backend';

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
/** True while backend polling is active. */
export const logPolling = writable<boolean>(false);
/** Last polling error, if the backend is unreachable. */
export const logPollError = writable<string>('');

const MAX_RECORDS = 500;
const POLL_INTERVAL_MS = 1500;

// Frontend ids count DOWN from -1 so they never collide with backend ids (positive).
let frontendSeq = 0;
let backendCursor = 0;
let pollTimer: ReturnType<typeof setInterval> | null = null;
let consoleCaptured = false;

const NOISY_PATTERNS = ['[vite]', '[HMR]', 'Download the Svelte'];

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
  if (typeof value === 'string') return value;
  if (value instanceof Error) return `${value.name}: ${value.message}`;
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

/** Write a frontend-originated record into the console. */
export function logFrontend(level: string, message: string, category = 'frontend') {
  frontendSeq += 1;
  push({
    id: -frontendSeq,
    timestamp: new Date().toISOString(),
    level,
    category,
    message,
    source: 'frontend'
  });
}

export const log = {
  debug: (m: string, c = 'frontend') => logFrontend('Debug', m, c),
  info: (m: string, c = 'frontend') => logFrontend('Information', m, c),
  warn: (m: string, c = 'frontend') => logFrontend('Warning', m, c),
  error: (m: string, c = 'frontend') => logFrontend('Error', m, c)
};
/**
 * Mirror browser console output + uncaught errors into the log bus.
 * Idempotent - safe to call from every page load.
 */
export function captureConsole() {
  if (consoleCaptured || typeof window === 'undefined') return;
  consoleCaptured = true;

  const original = {
    log: console.log.bind(console),
    info: console.info.bind(console),
    warn: console.warn.bind(console),
    error: console.error.bind(console)
  };

  console.log = (...args: unknown[]) => {
    original(...args);
    logFrontend('Information', args.map(stringify).join(' '));
  };
  console.info = (...args: unknown[]) => {
    original(...args);
    logFrontend('Information', args.map(stringify).join(' '));
  };
  console.warn = (...args: unknown[]) => {
    original(...args);
    logFrontend('Warning', args.map(stringify).join(' '));
  };
  console.error = (...args: unknown[]) => {
    original(...args);
    logFrontend('Error', args.map(stringify).join(' '));
  };

  window.addEventListener('error', (event) => {
    logFrontend('Error', `Uncaught: ${event.message} (${event.filename}:${event.lineno})`, 'window');
  });

  window.addEventListener('unhandledrejection', (event) => {
    logFrontend('Error', `Unhandled promise rejection: ${stringify(event.reason)}`, 'window');
  });

  logFrontend('Information', 'Frontend log capture started', 'logs');
}

/**
 * Start polling the backend log buffer.
 * Uses an id cursor so only NEW records are transferred on each tick.
 */
export function startBackendPolling() {
  if (typeof window === 'undefined' || pollTimer) return;

  const tick = async () => {
    try {
      const res = await fetch(`/api/logs?sinceId=${backendCursor}&limit=200`);
      if (!res.ok) {
        logPollError.set(`Backend returned ${res.status}`);
        return;
      }
      const payload = await res.json();
      const entries: LogRecord[] = Array.isArray(payload.logs) ? payload.logs : [];

      for (const entry of entries) {
        push({
          id: entry.id,
          timestamp: entry.timestamp,
          level: entry.level,
          category: entry.category,
          message: entry.message,
          exception: entry.exception ?? null,
          source: 'backend'
        });
        if (entry.id > backendCursor) backendCursor = entry.id;
      }

      logPolling.set(true);
      logPollError.set('');
    } catch (e) {
      logPolling.set(false);
      logPollError.set(stringify(e));
    }
  };

  pollTimer = setInterval(tick, POLL_INTERVAL_MS);
  void tick(); // immediate first fetch
  logPolling.set(true);
}

export function stopBackendPolling() {
  if (pollTimer) {
    clearInterval(pollTimer);
    pollTimer = null;
  }
  logPolling.set(false);
}

/** Clear both the local list and the backend buffer. */
export async function clearLogs() {
  logRecords.set([]);
  backendCursor = 0;
  frontendSeq = 0;

  try {
    const res = await fetch('/api/logs', { method: 'DELETE' });
    if (!res.ok) logPollError.set(`Clear failed: ${res.status}`);
  } catch (e) {
    logPollError.set(stringify(e));
  }

  logFrontend('Information', 'Log buffer cleared', 'logs');
}

/** Current snapshot without subscribing (handy inside components). */
export function currentLogs(): LogRecord[] {
  return get(logRecords);
}
