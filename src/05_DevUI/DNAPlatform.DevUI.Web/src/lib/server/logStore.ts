import { EventEmitter } from 'events';

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

  constructor() {
    super();
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
