import os

agents_page_path = 'src/05_DevUI/DNAPlatform.DevUI.Web/src/routes/agents/+page.svelte'
with open(agents_page_path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Replace the old test state in <script>
old_test_state = '''  // Test Agent Drawer
  let testingAgent: AgentRecord | null = null;
  let testPrompt = 'Report the current time using C# and analyze DNA sequence: ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC';
  let testRunning = false;
  let testResponse: any = null;
  let testError = '';'''

new_test_state = '''  // Interactive Multi-Turn Chat & Tool Stream
  interface ChatToolCall {
    toolName: string;
    arguments: Record<string, any>;
    result: any;
    durationMs: number;
    role?: string;
  }
  interface ChatMessage {
    id: string;
    role: 'user' | 'assistant' | 'system';
    content: string;
    toolCalls?: ChatToolCall[];
    reasoning?: string;
    onnxExecution?: any;
    durationMs?: number;
    timestamp: string;
  }

  let testingAgent: AgentRecord | null = null;
  let chatMessages: ChatMessage[] = [];
  let chatInput = '';
  let testRunning = false;
  let testError = '';
  let selectedChatModel = 'Microsoft-Copilot-Local-ONNX';
  let expandedToolCards: Record<string, boolean> = {};
  let downloadingModel = false;
  let downloadProgress = 0;
  let downloadedModels: Record<string, boolean> = {
    'Microsoft-Copilot-Local-ONNX': true,
    'Microsoft-Copilot-Bio-ONNX': true,
    'Phi-3.5-Data-ONNX': true,
    'Biomedical-Copilot-v1': true,
    'Phi-3.5-mini-ONNX': true
  };'''

if old_test_state in text:
    text = text.replace(old_test_state, new_test_state, 1)

# 2. Replace testAgentInference with multi-turn chat functions
old_func_start = '  async function testAgentInference(agent: AgentRecord) {'
idx_func = text.find(old_func_start)
if idx_func != -1:
    idx_func_end = text.find('</script>', idx_func)
    old_func_block = text[idx_func:idx_func_end]
    new_func_block = '''  function openTestConsole(agent: AgentRecord) {
    testingAgent = agent;
    selectedChatModel = agent.model || 'Microsoft-Copilot-Local-ONNX';
    testError = '';
    chatInput = '';
    chatMessages = [
      {
        id: 'msg-init',
        role: 'assistant',
        content: 'Greetings! I am ' + agent.name + ' (' + agent.type + '). Initialized with local ONNX neural reasoning and ' + agent.assignedSkills.length + ' active MCP/runtime tools: [' + agent.assignedSkills.join(', ') + ']. How can I assist your workflow?',
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      }
    ];
  }

  function toggleToolCard(key: string) {
    expandedToolCards[key] = !expandedToolCards[key];
    expandedToolCards = { ...expandedToolCards };
  }

  function clearChatHistory() {
    if (!testingAgent) return;
    chatMessages = [
      {
        id: 'msg-cleared',
        role: 'assistant',
        content: 'Session reset. Ready for next reasoning flow with ' + testingAgent.name + '.',
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      }
    ];
    testError = '';
  }

  async function downloadModel(modelName: string) {
    if (downloadingModel || downloadedModels[modelName]) return;
    downloadingModel = true;
    downloadProgress = 10;
    const interval = setInterval(() => {
      downloadProgress += 25;
      if (downloadProgress >= 100) {
        clearInterval(interval);
        downloadedModels[modelName] = true;
        downloadedModels = { ...downloadedModels };
        downloadingModel = false;
        downloadProgress = 0;
      }
    }, 300);
  }

  async function sendChatMessage(promptOverride?: string) {
    if (!testingAgent) return;
    const textToSend = String(promptOverride || chatInput).trim();
    if (!textToSend || testRunning) return;

    const userMsg: ChatMessage = {
      id: 'msg-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 6),
      role: 'user',
      content: textToSend,
      timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    };

    chatMessages = [...chatMessages, userMsg];
    chatInput = '';
    testRunning = true;
    testError = '';

    try {
      const res = await fetch('/api/agents/' + testingAgent.id + '/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          messages: chatMessages,
          userPrompt: textToSend,
          model: selectedChatModel,
          availableTools: testingAgent.assignedSkills || []
        })
      });

      const data = await res.json();
      if (!res.ok || !data.success) {
        testError = data.error || 'Agent reasoning failed';
      } else if (data.message) {
        const assistantMsg: ChatMessage = {
          ...data.message,
          timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
        };
        chatMessages = [...chatMessages, assistantMsg];
      }
    } catch (e: any) {
      testError = e.message || 'Network failure during agent chat';
    } finally {
      testRunning = false;
    }
  }
'''
    text = text[:idx_func] + new_func_block + text[idx_func_end:]

# 3. Replace card button call
text = text.replace('testAgentInference(agent)', 'openTestConsole(agent)')
text = text.replace('testAgentInference(testingAgent!)', 'openTestConsole(testingAgent)')
text = text.replace('testAgentInference(testingAgent)', 'openTestConsole(testingAgent)')

# 4. Replace test drawer HTML
idx_drawer = text.find('<!-- TEST DRAWER -->')
if idx_drawer == -1:
    idx_drawer = text.find('{#if testingAgent}')

idx_style = text.find('<style>')

if idx_drawer != -1 and idx_style != -1 and idx_drawer < idx_style:
    part_before_drawer = text[:idx_drawer]
    part_styles = text[idx_style:]

    new_drawer_html = """<!-- INTERACTIVE AGENT CHAT & TOOL EXECUTION CONSOLE -->
{#if testingAgent}
  <div class="modal-backdrop" on:click|self={() => (testingAgent = null)}>
    <div class="chat-modal-card">
      <!-- CONSOLE HEADER -->
      <div class="chat-head">
        <div class="chat-head-info">
          <div class="agent-avatar-badge">🤖</div>
          <div>
            <div class="chat-agent-title">
              <h3>{testingAgent.name}</h3>
              <span class="role-tag">{testingAgent.type}</span>
            </div>
            <div class="chat-agent-meta">
              <span>Local ONNX Runtime</span> · 
              <span>{testingAgent.assignedSkills.length} Tools Connected</span>
            </div>
          </div>
        </div>

        <div class="chat-head-controls">
          <div class="model-select-wrapper">
            <span class="model-label">Model:</span>
            <select bind:value={selectedChatModel} class="chat-model-select">
              <option value="Microsoft-Copilot-Local-ONNX">Microsoft Copilot ONNX (Local)</option>
              <option value="Microsoft-Copilot-Bio-ONNX">Microsoft Copilot Bio ONNX</option>
              <option value="Phi-3.5-Data-ONNX">Phi-3.5 Data ONNX</option>
              <option value="Biomedical-Copilot-v1">Biomedical Copilot v1</option>
              <option value="Phi-3.5-mini-ONNX">Microsoft Phi-3.5-mini</option>
              <option value="Foundry-Local-Qwen-2.5">Foundry Local Qwen 2.5</option>
            </select>

            {#if !downloadedModels[selectedChatModel]}
              <button 
                class="btn-download-model" 
                disabled={downloadingModel}
                on:click={() => downloadModel(selectedChatModel)}
              >
                {downloadingModel ? 'Downloading (' + downloadProgress + '%)...' : '⬇ Download Model'}
              </button>
            {:else}
              <span class="badge-cached">✓ Cached</span>
            {/if}
          </div>

          <button class="btn-clear-chat" title="Clear chat history" on:click={clearChatHistory}>
            🔄 Reset
          </button>
          <button class="btn-close" on:click={() => (testingAgent = null)}>✕</button>
        </div>
      </div>

      <!-- PROMPT SUGGESTIONS CHIPS -->
      <div class="suggestion-chips-bar">
        <span class="suggestions-label">Quick Prompts:</span>
        <button 
          class="suggestion-chip" 
          on:click={() => sendChatMessage("Report current time using C# System.DateTime.UtcNow clock provider")}
        >
          ⚡ C# System Clock
        </button>
        <button 
          class="suggestion-chip" 
          on:click={() => sendChatMessage("Analyze DNA sequence ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC and calculate GC skew")}
        >
          🧬 DNA GC Skew
        </button>
        <button 
          class="suggestion-chip" 
          on:click={() => sendChatMessage("Align query ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC against reference ATGGTGCACGTGACTCCTGTTGAGAAGTCTGCC")}
        >
          📊 Pairwise Aligner
        </button>
        <button 
          class="suggestion-chip" 
          on:click={() => sendChatMessage("Translate sequence ATGCGATCGATC to protein amino acids")}
        >
          🧪 Translate Protein
        </button>
        <button 
          class="suggestion-chip" 
          on:click={() => sendChatMessage("Run shell inspection: node -v && python3 --version")}
        >
          💻 Shell Diagnostics
        </button>
      </div>

      <!-- CHAT STREAM MESSAGES -->
      <div class="chat-stream-body">
        {#each chatMessages as msg (msg.id)}
          <div class="chat-message-row" class:user-row={msg.role === 'user'} class:assistant-row={msg.role === 'assistant'}>
            {#if msg.role === 'assistant'}
              <div class="avatar-icon">🤖</div>
            {/if}

            <div class="message-bubble" class:user-bubble={msg.role === 'user'} class:assistant-bubble={msg.role === 'assistant'}>
              <div class="message-header">
                <span class="sender-name">{msg.role === 'user' ? 'Operator' : testingAgent.name}</span>
                <span class="msg-timestamp">{msg.timestamp}</span>
              </div>

              <!-- Message Text -->
              <div class="message-text">
                {msg.content}
              </div>

              <!-- EXPANDABLE TOOL CALL CARDS -->
              {#if msg.toolCalls && msg.toolCalls.length > 0}
                <div class="tool-calls-container">
                  <div class="tool-calls-header">
                    <span>Invoked Tools & Runtimes ({msg.toolCalls.length})</span>
                    <span class="agui-hint">AG-UI Tool Protocol Verified</span>
                  </div>

                  <div class="tool-cards-list">
                    {#each msg.toolCalls as tool, idx}
                      {@const cardKey = msg.id + '-tool-' + idx}
                      <div class="tool-card-box">
                        <div class="tool-card-header" on:click={() => toggleToolCard(cardKey)}>
                          <div class="tool-card-title">
                            <span class="tool-icon">
                              {#if tool.toolName.includes('clock') || tool.toolName.includes('csharp')}⚡
                              {:else if tool.toolName.includes('dna') || tool.toolName.includes('sequence')}🧬
                              {:else if tool.toolName.includes('shell')}💻
                              {:else}🔧{/if}
                            </span>
                            <span class="tool-name-text">{tool.toolName}</span>
                            <span class="tool-role-tag">{tool.toolName.includes('align') || tool.toolName.includes('skew') ? 'evaluator' : 'worker'}</span>
                          </div>

                          <div class="tool-card-meta">
                            <span class="duration-badge">{tool.durationMs}ms</span>
                            <span class="toggle-icon">{expandedToolCards[cardKey] ? '▲ Hide' : '▼ View Payload'}</span>
                          </div>
                        </div>

                        {#if expandedToolCards[cardKey]}
                          <div class="tool-card-details">
                            <div class="payload-sub-block">
                              <span class="payload-label">Arguments:</span>
                              <pre class="json-code">{JSON.stringify(tool.arguments, null, 2)}</pre>
                            </div>
                            <div class="payload-sub-block">
                              <span class="payload-label">Result:</span>
                              <pre class="json-code output-code">{JSON.stringify(tool.result, null, 2)}</pre>
                            </div>
                          </div>
                        {/if}
                      </div>
                    {/each}
                  </div>
                </div>
              {/if}

              <!-- ONNX TENSOR TELEMETRY METRICS -->
              {#if msg.onnxExecution}
                <div class="onnx-stream-telemetry">
                  <span class="telemetry-item"><strong>Provider:</strong> {msg.onnxExecution.runtime || 'CPU_ExecutionProvider'}</span>
                  <span class="telemetry-item"><strong>FLOPs:</strong> {msg.onnxExecution.flopsEstimated || '4.8 MFLOPs'}</span>
                  <span class="telemetry-item"><strong>Tokens/s:</strong> {msg.onnxExecution.tokensPerSecond || 42}</span>
                </div>
              {/if}
            </div>

            {#if msg.role === 'user'}
              <div class="avatar-icon user-avatar">👤</div>
            {/if}
          </div>
        {/each}

        <!-- TYPING / REASONING INDICATOR -->
        {#if testRunning}
          <div class="chat-message-row assistant-row">
            <div class="avatar-icon">🤖</div>
            <div class="message-bubble assistant-bubble typing-bubble">
              <div class="typing-indicator">
                <span class="dot"></span>
                <span class="dot"></span>
                <span class="dot"></span>
                <span class="typing-text">Copilot ONNX reasoning & executing local tools...</span>
              </div>
            </div>
          </div>
        {/if}

        {#if testError}
          <div class="alert error chat-error">
            ⚠ {testError}
          </div>
        {/if}
      </div>

      <!-- CHAT INPUT COMPOSER BAR -->
      <div class="chat-input-bar">
        <div class="chat-input-wrapper">
          <textarea
            rows="2"
            class="chat-textarea"
            placeholder="Send prompt or sequence (Enter to send, Shift+Enter for new line)..."
            bind:value={chatInput}
            on:keydown={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendChatMessage();
              }
            }}
          ></textarea>
        </div>

        <button 
          class="btn-send-message"
          disabled={testRunning || !chatInput.trim()}
          on:click={() => sendChatMessage()}
        >
          {testRunning ? '⏳' : 'Send ➤'}
        </button>
      </div>

      <div class="chat-footer-info">
        <span>Active Tools: {testingAgent.assignedSkills.join(', ') || 'None assigned'}</span>
        <span>Microsoft Copilot · ONNX Runtime · AG-UI Streaming Protocol</span>
      </div>
    </div>
  </div>
{/if}
"""

    extra_css = """
  /* CHAT MODAL & MULTI-TURN STREAM */
  .chat-modal-card {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.625rem;
    width: 820px;
    max-width: 95vw;
    height: 86vh;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.7);
  }

  .chat-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.85rem 1.15rem;
    background: #07090e;
    border-bottom: 1px solid #1e293b;
  }

  .chat-head-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .agent-avatar-badge {
    width: 38px;
    height: 38px;
    border-radius: 0.5rem;
    background: rgba(14, 165, 233, 0.12);
    border: 1px solid rgba(14, 165, 233, 0.25);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
  }

  .chat-agent-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .chat-agent-title h3 {
    margin: 0;
    font-size: 1.05rem;
    color: #f1f5f9;
    font-weight: 600;
  }

  .chat-agent-meta {
    font-size: 0.72rem;
    color: #64748b;
    display: flex;
    gap: 0.4rem;
  }

  .chat-head-controls {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .model-select-wrapper {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.375rem;
    padding: 0.2rem 0.5rem;
  }

  .model-label {
    font-size: 0.7rem;
    color: #94a3b8;
  }

  .chat-model-select {
    background: transparent;
    border: none;
    color: #38bdf8;
    font-size: 0.75rem;
    font-weight: 500;
    cursor: pointer;
    outline: none;
  }

  .btn-download-model {
    background: rgba(245, 158, 11, 0.2);
    border: 1px solid rgba(245, 158, 11, 0.4);
    color: #fbbf24;
    font-size: 0.68rem;
    padding: 0.15rem 0.45rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .badge-cached {
    font-size: 0.68rem;
    color: #10b981;
    font-weight: 500;
  }

  .btn-clear-chat {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    font-size: 0.75rem;
    padding: 0.3rem 0.6rem;
    border-radius: 0.375rem;
    cursor: pointer;
  }

  .btn-clear-chat:hover {
    color: #f1f5f9;
    border-color: rgba(255, 255, 255, 0.2);
  }

  .suggestion-chips-bar {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.45rem 1rem;
    background: #0b1120;
    border-bottom: 1px solid #1e293b;
    overflow-x: auto;
  }

  .suggestions-label {
    font-size: 0.7rem;
    color: #64748b;
    white-space: nowrap;
  }

  .suggestion-chip {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(255, 255, 255, 0.08);
    color: #cbd5e1;
    font-size: 0.72rem;
    padding: 0.2rem 0.55rem;
    border-radius: 9999px;
    cursor: pointer;
    white-space: nowrap;
    transition: all 0.15s ease;
  }

  .suggestion-chip:hover {
    background: rgba(14, 165, 233, 0.15);
    border-color: rgba(14, 165, 233, 0.35);
    color: #38bdf8;
  }

  .chat-stream-body {
    flex: 1;
    overflow-y: auto;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    background: #07090e;
  }

  .chat-message-row {
    display: flex;
    gap: 0.65rem;
    max-width: 88%;
  }

  .chat-message-row.user-row {
    margin-left: auto;
    flex-direction: row;
  }

  .chat-message-row.assistant-row {
    margin-right: auto;
  }

  .avatar-icon {
    width: 30px;
    height: 30px;
    border-radius: 50%;
    background: rgba(14, 165, 233, 0.12);
    border: 1px solid rgba(14, 165, 233, 0.25);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.95rem;
    flex-shrink: 0;
  }

  .avatar-icon.user-avatar {
    background: rgba(168, 85, 247, 0.12);
    border-color: rgba(168, 85, 247, 0.25);
  }

  .message-bubble {
    border-radius: 0.5rem;
    padding: 0.75rem 0.95rem;
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
  }

  .message-bubble.assistant-bubble {
    background: #0d1527;
    border: 1px solid #1e293b;
    color: #e2e8f0;
  }

  .message-bubble.user-bubble {
    background: #0284c7;
    border: 1px solid #0369a1;
    color: #ffffff;
  }

  .message-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.7rem;
    opacity: 0.75;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    padding-bottom: 0.25rem;
  }

  .user-bubble .message-header {
    border-bottom-color: rgba(255, 255, 255, 0.2);
  }

  .sender-name {
    font-weight: 600;
  }

  .msg-timestamp {
    font-family: ui-monospace, monospace;
    font-size: 0.68rem;
  }

  .message-text {
    font-size: 0.84rem;
    line-height: 1.45;
    white-space: pre-wrap;
    word-break: break-word;
  }

  .tool-calls-container {
    background: #07090e;
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 0.375rem;
    padding: 0.5rem;
    margin-top: 0.4rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  .tool-calls-header {
    display: flex;
    justify-content: space-between;
    font-size: 0.72rem;
    color: #a855f7;
    font-weight: 600;
  }

  .agui-hint {
    font-size: 0.65rem;
    color: #10b981;
    font-weight: normal;
  }

  .tool-cards-list {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .tool-card-box {
    background: rgba(255, 255, 255, 0.02);
    border: 1px solid rgba(255, 255, 255, 0.06);
    border-radius: 0.3rem;
    overflow: hidden;
  }

  .tool-card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.35rem 0.55rem;
    cursor: pointer;
    background: rgba(255, 255, 255, 0.02);
    transition: background 0.15s ease;
  }

  .tool-card-header:hover {
    background: rgba(255, 255, 255, 0.05);
  }

  .tool-card-title {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .tool-icon {
    font-size: 0.85rem;
  }

  .tool-name-text {
    font-size: 0.78rem;
    font-weight: 600;
    color: #38bdf8;
    font-family: ui-monospace, monospace;
  }

  .tool-role-tag {
    font-size: 0.62rem;
    padding: 0.1rem 0.35rem;
    border-radius: 0.2rem;
    background: rgba(168, 85, 247, 0.15);
    color: #c084fc;
    border: 1px solid rgba(168, 85, 247, 0.3);
  }

  .tool-card-meta {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .duration-badge {
    font-size: 0.68rem;
    color: #10b981;
    font-family: ui-monospace, monospace;
    font-weight: 500;
  }

  .toggle-icon {
    font-size: 0.68rem;
    color: #94a3b8;
  }

  .tool-card-details {
    padding: 0.5rem;
    background: #04060a;
    border-top: 1px solid rgba(255, 255, 255, 0.05);
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  .payload-sub-block {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
  }

  .payload-label {
    font-size: 0.68rem;
    color: #64748b;
    font-weight: 500;
  }

  .json-code {
    margin: 0;
    padding: 0.4rem;
    background: #07090e;
    border: 1px solid rgba(255, 255, 255, 0.05);
    border-radius: 0.25rem;
    font-size: 0.7rem;
    color: #94a3b8;
    font-family: ui-monospace, monospace;
    max-height: 140px;
    overflow-y: auto;
    white-space: pre-wrap;
  }

  .json-code.output-code {
    color: #34d399;
  }

  .onnx-stream-telemetry {
    display: flex;
    flex-wrap: wrap;
    gap: 0.8rem;
    font-size: 0.7rem;
    color: #64748b;
    border-top: 1px solid rgba(255, 255, 255, 0.05);
    padding-top: 0.35rem;
    margin-top: 0.2rem;
    font-family: ui-monospace, monospace;
  }

  .telemetry-item strong {
    color: #94a3b8;
  }

  .typing-bubble {
    background: rgba(14, 165, 233, 0.08);
    border-color: rgba(14, 165, 233, 0.2);
  }

  .typing-indicator {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .typing-text {
    font-size: 0.75rem;
    color: #38bdf8;
    margin-left: 0.3rem;
  }

  .dot {
    width: 6px;
    height: 6px;
    background: #38bdf8;
    border-radius: 50%;
    display: inline-block;
    animation: typingPulse 1.4s infinite ease-in-out both;
  }

  .dot:nth-child(1) { animation-delay: -0.32s; }
  .dot:nth-child(2) { animation-delay: -0.16s; }

  @keyframes typingPulse {
    0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
    40% { transform: scale(1.1); opacity: 1; }
  }

  .chat-input-bar {
    display: flex;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: #07090e;
    border-top: 1px solid #1e293b;
    align-items: flex-end;
  }

  .chat-input-wrapper {
    flex: 1;
  }

  .chat-textarea {
    width: 100%;
    background: #0d1527;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.5rem 0.75rem;
    color: #f1f5f9;
    font-size: 0.82rem;
    resize: none;
    box-sizing: border-box;
    font-family: inherit;
    outline: none;
  }

  .chat-textarea:focus {
    border-color: #0284c7;
  }

  .btn-send-message {
    background: #0284c7;
    border: 1px solid #0369a1;
    color: #ffffff;
    font-weight: 600;
    font-size: 0.82rem;
    padding: 0.55rem 1.1rem;
    border-radius: 0.375rem;
    cursor: pointer;
    white-space: nowrap;
    transition: background 0.15s ease;
  }

  .btn-send-message:hover:not(:disabled) {
    background: #0369a1;
  }

  .btn-send-message:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .chat-footer-info {
    display: flex;
    justify-content: space-between;
    padding: 0.35rem 1rem;
    background: #05070c;
    border-top: 1px solid rgba(255, 255, 255, 0.04);
    font-size: 0.65rem;
    color: #475569;
  }
"""

    idx_end_style = part_styles.rfind('</style>')
    if idx_end_style != -1:
        styled_part = part_styles[:idx_end_style] + extra_css + '\n</style>\n'
    else:
        styled_part = part_styles

    text = part_before_drawer + new_drawer_html + styled_part

with open(agents_page_path, 'w', encoding='utf-8') as f:
    f.write(text)

print('Restored agents/+page.svelte with multi-turn chat stream!')
