import { agent, ndJsonStream, AGENT_METHODS } from "@agentclientprotocol/sdk";
const BACKEND_URL = process.env.DNA_PLATFORM_API_URL || "http://127.0.0.1:5254";
const HTTP_TIMEOUT_MS = 5000;
function logToStderr(message) {
    process.stderr.write(`[ACP] ${message}\n`);
}
async function fetchWithTimeout(url, options = {}) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), HTTP_TIMEOUT_MS);
    try {
        return await fetch(url, { ...options, signal: controller.signal });
    }
    finally {
        clearTimeout(timeoutId);
    }
}
async function httpPost(path, body) {
    const response = await fetchWithTimeout(`${BACKEND_URL}${path}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
    });
    if (!response.ok) {
        const text = await response.text();
        throw new Error(`HTTP ${response.status}: ${text}`);
    }
    return response.json();
}
async function httpGet(path) {
    const response = await fetchWithTimeout(`${BACKEND_URL}${path}`);
    if (!response.ok) {
        const text = await response.text();
        throw new Error(`HTTP ${response.status}: ${text}`);
    }
    return response.json();
}
const capabilities = {
    promptCapabilities: {
        image: false,
        audio: false,
        embeddedContext: false,
    },
    sessionCapabilities: {
        list: null,
        delete: null,
        additionalDirectories: null,
        fork: null,
        resume: null,
        close: null,
    },
};
const app = agent({ name: "dna-platform-acp-adapter" });
app.onRequest(AGENT_METHODS.initialize, async (ctx) => {
    try {
        const params = ctx.params;
        logToStderr(`initialize: protocolVersion=${params.protocolVersion}`);
        const response = {
            protocolVersion: 1,
            agentCapabilities: capabilities,
            agentInfo: { name: "dna-platform-acp-adapter", version: "0.1.0" },
        };
        return response;
    }
    catch (error) {
        logToStderr(`initialize error: ${error}`);
        throw error;
    }
});
app.onRequest(AGENT_METHODS.session_new, async (ctx) => {
    try {
        const params = ctx.params;
        logToStderr(`newSession: cwd=${params.cwd}`);
        try {
            await httpGet("/api/info");
            logToStderr("Backend health check passed");
        }
        catch (error) {
            logToStderr(`Backend health check failed: ${error}`);
            throw new Error("Backend unavailable");
        }
        const response = {
            sessionId: crypto.randomUUID(),
        };
        return response;
    }
    catch (error) {
        logToStderr(`newSession error: ${error}`);
        throw error;
    }
});
app.onRequest(AGENT_METHODS.session_prompt, async (ctx) => {
    try {
        const params = ctx.params;
        logToStderr(`prompt: sessionId=${params.sessionId}, blocks=${params.prompt.length}`);
        try {
            let workflowDefinition;
            const textBlock = params.prompt.find((b) => b.type === "text");
            if (!textBlock) {
                throw new Error("Prompt must contain a text content block with workflow JSON");
            }
            try {
                workflowDefinition = JSON.parse(textBlock.text);
            }
            catch {
                throw new Error("Prompt text must be a valid JSON workflow definition");
            }
            const result = await httpPost("/api/workflows/execute", workflowDefinition);
            const response = {
                stopReason: result.success ? "end_turn" : "refusal",
            };
            return response;
        }
        catch (error) {
            logToStderr(`Prompt execution failed: ${error}`);
            const response = {
                stopReason: "refusal",
            };
            return response;
        }
    }
    catch (error) {
        logToStderr(`session_prompt error: ${error}`);
        throw error;
    }
});
app.onNotification(AGENT_METHODS.session_cancel, async (ctx) => {
    try {
        const params = ctx.params;
        logToStderr(`cancel: sessionId=${params.sessionId}`);
    }
    catch (error) {
        logToStderr(`session_cancel error: ${error}`);
    }
});
const stdout = new WritableStream({
    write(chunk) {
        try {
            process.stdout.write(chunk);
        }
        catch {
            // Ignore write errors (e.g., EPIPE)
        }
    },
});
const stdin = new ReadableStream({
    start(controller) {
        process.stdin.on("data", (chunk) => controller.enqueue(chunk));
        process.stdin.on("end", () => controller.close());
        process.stdin.on("error", (error) => controller.error(error));
    },
});
const stream = ndJsonStream(stdout, stdin);
app.connect(stream);
logToStderr("ACP adapter started, listening on stdin");
process.on("uncaughtException", (error) => {
    logToStderr(`Uncaught exception: ${error}`);
    // Don't exit, let the process continue
});
process.on("unhandledRejection", (reason) => {
    logToStderr(`Unhandled rejection: ${reason}`);
    // Don't exit, let the process continue
});
//# sourceMappingURL=index.js.map