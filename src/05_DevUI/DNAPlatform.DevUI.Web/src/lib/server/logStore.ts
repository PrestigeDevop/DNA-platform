import { EventEmitter } from 'events';
import fs from 'fs';
import path from 'path';

export interface LogEntry {
  id: number;
  timestamp: string;
  level: string;
  category: string;
  message: string;
  exception?: string | null;
  source: string;
}

class LogStore extends EventEmitter {
  private entries: LogEntry[] = [];
  private nextId = 1;
  private capacity = 2000;
  private logFilePath: string;

  constructor() {
    super();
    // Resolve project root logs directory
    const candidates = [
      path.resolve(process.cwd(), 'logs'),
      path.resolve(process.cwd(), '../../../../logs'),
      path.resolve(process.cwd(), '../logs')
    ];
    let resolvedDir = candidates[0];
    for (const c of candidates) {
      if (fs.existsSync(c)) {
        resolvedDir = c;
        break;
      }
    }
    try {
      if (!fs.existsSync(resolvedDir)) {
        fs.mkdirSync(resolvedDir, { recursive: true });
      }
    } catch {}
    this.logFilePath = path.join(resolvedDir, 'platform_errors.log');

    this.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'System',
      message: 'DNA Platform - Node.js Workflow Engine Initialized',
      source: 'backend'
    });
  }

  get count(): number {
    return this.entries.length;
  }

  add(entry: Omit<LogEntry, 'id'>): LogEntry {
    const fullEntry: LogEntry = {
      ...entry,
      id: this.nextId++
    };

    this.entries.push(fullEntry);
    if (this.entries.length > this.capacity) {
      this.entries.shift();
    }

    // Persist Error and Warning logs to disk for diagnostics
    if (fullEntry.level === 'Error' || fullEntry.level === 'Warning' || fullEntry.exception) {
      try {
        const line = `[${fullEntry.timestamp}] [${fullEntry.level}] [${fullEntry.category}] ${fullEntry.message}${fullEntry.exception ? `\nException: ${fullEntry.exception}` : ''}\n`;
        fs.appendFileSync(this.logFilePath, line, 'utf8');
      } catch {}
    }

    this.emit('log', fullEntry);
    return fullEntry;
  }

  get(sinceId = 0, limit = 500): LogEntry[] {
    const filtered = this.entries.filter((e) => e.id > sinceId);
    return filtered.slice(0, limit);
  }

  clear(): number {
    const count = this.entries.length;
    this.entries = [];
    return count;
  }
}

const globalForLogs = globalThis as unknown as {
  logStore: LogStore | undefined;
};

export const logStore = globalForLogs.logStore ?? new LogStore();

if (process.env.NODE_ENV !== 'production') {
  globalForLogs.logStore = logStore;
}
