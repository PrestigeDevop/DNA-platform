import type { RequestHandler } from './$types';
import { logStore, type LogEntry } from '$lib/server/logStore';

// GET /api/logs/stream — SSE stream for real-time log updates
export const GET: RequestHandler = async ({ url }) => {
  const sinceId = parseInt(url.searchParams.get('sinceId') ?? '0', 10);

  let intervalId: ReturnType<typeof setInterval>;
  let onLogHandler: (entry: LogEntry) => void;

  const stream = new ReadableStream({
    start(controller) {
      // Send historical backlog
      const backlog = logStore.get(isNaN(sinceId) ? 0 : sinceId);
      for (const entry of backlog) {
        controller.enqueue(`event: log\ndata: ${JSON.stringify(entry)}\n\n`);
      }

      // Listen for new logs
      onLogHandler = (entry: LogEntry) => {
        try {
          controller.enqueue(`event: log\ndata: ${JSON.stringify(entry)}\n\n`);
        } catch {
          // Stream closed
        }
      };
      logStore.on('log', onLogHandler);

      // 15s keepalive comment to prevent proxy buffering
      intervalId = setInterval(() => {
        try {
          controller.enqueue(`: keepalive\n\n`);
        } catch {
          clearInterval(intervalId);
        }
      }, 15000);
    },
    cancel() {
      if (intervalId) clearInterval(intervalId);
      if (onLogHandler) logStore.off('log', onLogHandler);
    }
  });

  return new Response(stream, {
    headers: {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      Connection: 'keep-alive'
    }
  });
};
