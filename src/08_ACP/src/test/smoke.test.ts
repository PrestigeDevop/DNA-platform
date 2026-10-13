import { spawn, ChildProcess } from "node:child_process";
import { test } from "node:test";
import assert from "node:assert";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = join(__filename, "..");
const IS_WINDOWS = process.platform === "win32";
const IS_COMPILED = __filename.endsWith(".js");

const ADAPTER_PATH = IS_COMPILED
  ? join(__dirname, "..", "index.js")
  : join(__dirname, "..", "index.ts");
const TSX_PATH = join(process.cwd(), "node_modules", ".bin", "tsx.cmd");

interface JsonRpcRequest {
  jsonrpc: "2.0";
  id: string | number;
  method: string;
  params?: unknown;
}

interface JsonRpcNotification {
  jsonrpc: "2.0";
  method: string;
  params?: unknown;
}

interface JsonRpcResponse {
  jsonrpc: "2.0";
  id: string | number | null;
  result?: unknown;
  error?: { code: number; message: string; data?: unknown };
}

function sendRequest(proc: ChildProcess, request: JsonRpcRequest): void {
  proc.stdin!.write(`${JSON.stringify(request)}\n`);
}

function sendNotification(proc: ChildProcess, notification: JsonRpcNotification): void {
  proc.stdin!.write(`${JSON.stringify(notification)}\n`);
}

function readResponse(proc: ChildProcess): Promise<JsonRpcResponse> {
  return new Promise((resolve, reject) => {
    function onData(chunk: Buffer) {
      const lines = chunk.toString().trim().split("\n");
      for (const line of lines) {
        if (line.trim()) {
          proc.stdout!.removeListener("data", onData);
          proc.stdout!.removeListener("error", onError);
          proc.stdout!.removeListener("end", onEnd);
          try {
            resolve(JSON.parse(line) as JsonRpcResponse);
          } catch (e) {
            reject(e);
          }
          return;
        }
      }
    }
    function onError(err: Error) {
      proc.stdout!.removeListener("data", onData);
      proc.stdout!.removeListener("error", onError);
      proc.stdout!.removeListener("end", onEnd);
      reject(err);
    }
    function onEnd() {
      proc.stdout!.removeListener("data", onData);
      proc.stdout!.removeListener("error", onError);
      proc.stdout!.removeListener("end", onEnd);
      reject(new Error("Process stdout ended unexpectedly"));
    }
    proc.stdout!.on("data", onData);
    proc.stdout!.on("error", onError);
    proc.stdout!.on("end", onEnd);
  });
}

function isValidJsonRpcResponse(response: unknown): response is JsonRpcResponse {
  if (typeof response !== "object" || response === null) return false;
  const r = response as Record<string, unknown>;
  return r.jsonrpc === "2.0" &&
    (typeof r.id === "string" || typeof r.id === "number" || r.id === null) &&
    ("result" in r || "error" in r);
}

function createAdapterProcess(): ChildProcess {
  if (IS_COMPILED) {
    return spawn("node", [ADAPTER_PATH], {
      stdio: ["pipe", "pipe", "pipe"],
      env: { ...process.env, DNA_PLATFORM_API_URL: "http://127.0.0.1:5254" },
    });
  } else {
    const args = IS_WINDOWS ? ["/c", TSX_PATH, ADAPTER_PATH] : ["tsx", ADAPTER_PATH];
    const command = IS_WINDOWS ? "cmd" : "tsx";
    return spawn(command, args, {
      stdio: ["pipe", "pipe", "pipe"],
      env: { ...process.env, DNA_PLATFORM_API_URL: "http://127.0.0.1:5254" },
    });
  }
}

test("ACP adapter smoke test - full session", async () => {
  const proc = createAdapterProcess();
  
  proc.stderr!.on("data", (data) => {
    process.stderr.write(`[SMOKE] ${data}`);
  });

  await new Promise<void>((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error("Startup timeout")), 10000);
    proc.stderr!.once("data", () => {
      clearTimeout(timeout);
      setTimeout(resolve, 100);
    });
    proc.once("error", (err) => {
      clearTimeout(timeout);
      reject(err);
    });
    proc.once("exit", (code) => {
      clearTimeout(timeout);
      reject(new Error(`Process exited with code ${code}`));
    });
  });

  try {
    // Test 1: initialize
    sendRequest(proc, {
      jsonrpc: "2.0",
      id: 1,
      method: "initialize",
      params: { protocolVersion: 1 },
    });
    let response = await readResponse(proc);
    assert.ok(isValidJsonRpcResponse(response));
    assert.strictEqual(response.id, 1);
    assert.ok(response.result);
    assert.ok((response.result as { protocolVersion: number }).protocolVersion);
    assert.ok((response.result as { agentCapabilities: unknown }).agentCapabilities);

    // Test 2: newSession
    sendRequest(proc, {
      jsonrpc: "2.0",
      id: 2,
      method: "session/new",
      params: { cwd: process.cwd(), mcpServers: [] },
    });
    response = await readResponse(proc);
    assert.ok(isValidJsonRpcResponse(response));
    assert.strictEqual(response.id, 2);
    if (response.result) {
      assert.ok((response.result as { sessionId: string }).sessionId);
    } else {
      assert.ok(response.error);
      assert.strictEqual(response.error!.code, -32603);
    }

    // Test 3: prompt with valid workflow
    const workflow = {
      nodes: [
        { id: "1", type: "Input", position: { x: 0, y: 0 }, data: { label: "Input" } },
        { id: "2", type: "Output", position: { x: 200, y: 0 }, data: { label: "Output" } },
      ],
      connections: [{ source: "1", target: "2" }],
    };
    sendRequest(proc, {
      jsonrpc: "2.0",
      id: 3,
      method: "session/prompt",
      params: {
        sessionId: "test-session-1",
        prompt: [{ type: "text", text: JSON.stringify(workflow) }],
      },
    });
    response = await readResponse(proc);
    assert.ok(isValidJsonRpcResponse(response));
    assert.strictEqual(response.id, 3);
    if (response.result) {
      assert.ok(["end_turn", "refusal", "cancelled", "max_tokens", "max_turn_requests"].includes(
        (response.result as { stopReason: string }).stopReason
      ));
    } else {
      assert.ok(response.error);
    }

    // Test 4: prompt with invalid JSON
    sendRequest(proc, {
      jsonrpc: "2.0",
      id: 4,
      method: "session/prompt",
      params: {
        sessionId: "test-session-1",
        prompt: [{ type: "text", text: "not valid json" }],
      },
    });
    response = await readResponse(proc);
    assert.ok(isValidJsonRpcResponse(response));
    assert.strictEqual(response.id, 4);
    assert.ok(response.result);
    assert.strictEqual((response.result as { stopReason: string }).stopReason, "refusal");

    // Test 5: cancel notification
    sendNotification(proc, {
      jsonrpc: "2.0",
      method: "session/cancel",
      params: { sessionId: "test-session-1" },
    });
    sendRequest(proc, {
      jsonrpc: "2.0",
      id: 5,
      method: "initialize",
      params: { protocolVersion: 1 },
    });
    response = await readResponse(proc);
    assert.ok(isValidJsonRpcResponse(response));
    assert.strictEqual(response.id, 5);

    // Test 6: stdout contains only valid JSON-RPC frames
    sendRequest(proc, {
      jsonrpc: "2.0",
      id: 6,
      method: "initialize",
      params: { protocolVersion: 1 },
    });
    response = await readResponse(proc);
    assert.ok(isValidJsonRpcResponse(response));
    assert.strictEqual(response.jsonrpc, "2.0");
    assert.ok(typeof response.id === "number" || typeof response.id === "string");
    assert.ok("result" in response || "error" in response);

  } finally {
    proc.kill();
    await new Promise<void>((resolve) => proc.once("exit", () => resolve()));
  }
});
