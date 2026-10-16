/**
 * Unified high-performance log bus for the DevUI.
 *
 * Collects two streams into one list:
 *   1. FRONTEND - safe console messages and explicit log calls.
 *   2. BACKEND  - streamed from SvelteKit API via SSE at GET /api/logs/stream,
 *      with smooth throttled batching to prevent browser UI freezing.
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

export const logRecords = writable<LogRecord[]>([]);
export const logStreaming = writable<boolean>(false);
export const logStreamPaused = writable<boolean>(false);
export const logStreamError = writable<string>('');
export const consoleCaptureEnabled = writable<boolean>(false);

const MAX_RECORDS = 400;
const BATCH_FLUSH_INTERVAL_MS = 150;
const POLL_INTERVAL_MS = 3000;

let frontendSeq = 0;
let backendCursor = 0;
let pollTimer: ReturnType<typeof setInterval> | null = null;
let flushTimer: ReturnType<typeof setTimeout> | null = null;
let consoleCaptured = false;
let eventSource: EventSource | null = null;
let isLogging = false;

// Internal batch queue to prevent store update storms
let pendingQueue: LogRecord[] = [];
const seenKeys = new Set<string>();

const NOISY_PATTERNS = [
  '[vite]',
  '[HMR]',
  'Download the Svelte',
  'svelte-announcer',
  'api/logs',
  '__vite__',
  'chrome-extension',
  'node_modules',
  'ResizeObserver loop',
  ': keepalive'
];

function isNoisy(text: string): boolean {
  if (!text || typeof text !== 'string') return true;
  return NOISY_PATTERNS.some((p) => text.includes(p));
}

function scheduleFlush() {
  if (flushTimer) return;
  flushTimer = setTimeout(() => {
    flushTimer = null;
    flushBatch();
  }, BATCH_FLUSH_INTERVAL_MS);
}

function flushBatch() {
  if (pendingQueue.length === 0) return;
  const toAdd = pendingQueue.splice(0, pendingQueue.length);

  logRecords.update((list) => {
    const combined = [...list, ...toAdd];
    if (combined.length > MAX_RECORDS) {
      const trimmed = combined.slice(combined.length - MAX_RECORDS);
      // Prune seenKeys cache periodically to avoid memory leak
      if (seenKeys.size > MAX_RECORDS * 2) {
        seenKeys.clear();
        for (const r of trimmed) {
          seenKeys.add(`${r.source}:${r.id}`);
        }
      }
      return trimmed;
    }
    return combined;
  });
}

function push(record: LogRecord) {
  if (isNoisy(record.message)) return;
  if (get(logStreamPaused)) return;

  const key = `${record.source}:${record.id}`;
  if (seenKeys.has(key)) return;
  seenKeys.add(key);

  pendingQueue.push(record);
  scheduleFlush();
}

function stringify(value: unknown): string {
  if (value === null || value === undefined) return String(value);
  if (typeof value === 'string') return value;
  if (value instanceof Error) return value.name + ': ' + value.message;
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

export function logFrontend(level: string, message: string, category = 'frontend') {
  if (isLogging) return;
  frontendSeq += 1;
  const record: LogRecord = {
    id: -frontendSeq,
    timestamp: new Date().toISOString(),
    level,
    category,
    message,
    source: 'frontend'
  };
  push(record);
  return record;
}

export const log = {
  debug: (m: string, c = 'frontend') => logFrontend('Debug', m, c),
  info: (m: string, c = 'frontend') => logFrontend('Information', m, c),
  warn: (m: string, c = 'frontend') => logFrontend('Warning', m, c),
  error: (m: string, c = 'frontend') => logFrontend('Error', m, c)
};

/**
 * Mirror browser console output with strict reentrancy protection
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

  function safeLog(level: string, args: unknown[]) {
    if (isLogging || !get(consoleCaptureEnabled)) return;
    isLogging = true;
    try {
      const text = args.map(stringify).join(' ');
      if (!isNoisy(text)) {
        logFrontend(level, text, 'browser-console');
      }
    } finally {
      isLogging = false;
    }
  }

  console.log = (...args: unknown[]) => {
    original.log(...args);
    safeLog('Information', args);
  };
  console.info = (...args: unknown[]) => {
    original.info(...args);
    safeLog('Information', args);
  };
  console.warn = (...args: unknown[]) => {
    original.warn(...args);
    safeLog('Warning', args);
  };
  console.error = (...args: unknown[]) => {
    original.error(...args);
    safeLog('Error', args);
  };

  window.addEventListener('error', (event) => {
    if (!get(consoleCaptureEnabled)) return;
    safeLog('Error', [
      `Uncaught: ${event.message} (${event.filename || 'unknown'}:${event.lineno || 0})`
    ]);
  });

  window.addEventListener('unhandledrejection', (event) => {
    if (!get(consoleCaptureEnabled)) return;
    safeLog('Error', ['Unhandled promise rejection: ' + stringify(event.reason)]);
  });
}

/**
 * Start SSE streaming from the backend log buffer with batching
 */
export function startLogStream() {
  if (typeof window === 'undefined') return;
  if (eventSource) return;

  try {
    eventSource = new EventSource('/api/logs/stream');

    eventSource.onopen = () => {
      logStreaming.set(true);
      logStreamError.set('');
    };

    eventSource.addEventListener('log', (e: MessageEvent) => {
      try {
        const entry = JSON.parse(e.data);
        if (entry && entry.id !== undefined) {
          push({
            id: entry.id,
            timestamp: entry.timestamp || new Date().toISOString(),
            level: entry.level || 'Information',
            category: entry.category || 'Backend',
            message: entry.message || '',
            exception: entry.exception ?? null,
            source: 'backend'
          });
          if (entry.id > backendCursor) backendCursor = entry.id;
        }
      } catch {
        // ignore parse error
      }
    });

    eventSource.onerror = () => {
      if (eventSource) {
        eventSource.close();
        eventSource = null;
      }
      logStreaming.set(false);
      fallbackToPolling();
    };
  } catch {
    fallbackToPolling();
  }
}

function fallbackToPolling() {
  if (typeof window === 'undefined' || pollTimer) return;

  const tick = async () => {
    if (get(logStreamPaused)) return;
    try {
      const res = await fetch(`/api/logs?sinceId=${backendCursor}&limit=80`);
      if (!res.ok) return;
      const payload = await res.json();
      const entries = Array.isArray(payload.logs) ? payload.logs : [];
      for (const entry of entries) {
        push({
          id: entry.id,
          timestamp: entry.timestamp,
          level: entry.level || 'Information',
          category: entry.category || 'Backend',
          message: entry.message || '',
          exception: entry.exception ?? null,
          source: 'backend'
        });
        if (entry.id > backendCursor) backendCursor = entry.id;
      }
      logStreaming.set(true);
      logStreamError.set('');
    } catch {
      logStreaming.set(false);
    }
  };

  pollTimer = setInterval(tick, POLL_INTERVAL_MS);
  void tick();
}

export function stopLogStream() {
  if (eventSource) {
    eventSource.close();
    eventSource = null;
  }
  if (pollTimer) {
    clearInterval(pollTimer);
    pollTimer = null;
  }
  logStreaming.set(false);
}

export function togglePauseStream() {
  logStreamPaused.update((p) => !p);
}

export async function clearLogs() {
  pendingQueue = [];
  seenKeys.clear();
  logRecords.set([]);
  backendCursor = 0;
  frontendSeq = 0;
  try {
    await fetch('/api/logs', { method: 'DELETE' });
  } catch {
    // ignore
  }
}

export function currentLogs(): LogRecord[] {
  return get(logRecords);
}
