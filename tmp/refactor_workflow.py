import re

with open('src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/components/WorkflowDesigner.svelte', 'r') as f:
    text = f.read()

# 1. Remove flow templates subbar:
# <div class="designer-subbar">...</div>
subbar_pattern = re.compile(r'\s*<div class="designer-subbar">.*?</div>\s*(?=\s*<div class="designer-body">)', re.DOTALL)
text = subbar_pattern.sub('\n', text)

# 2. Add IntelliSense and Focus Mode state and functions to script
script_addition = '''  // Expression & IntelliSense State for Node Inspector (n8n / Elsa Style)
  let showFocusMode = false;
  let showIntelliSense = false;
  let autocompleteCursor = 0;
  let autocompleteQuery = '';
  let selectedSuggestionIndex = 0;
  let testingExpr = false;
  let testExprOutput = '';

  const EXPRESSION_SYMBOLS = [
    { label: 'Console.log()', insert: 'Console.log()', icon: '📜', desc: 'Log to execution stream' },
    { label: 'Time.now', insert: 'Time.now', icon: '⏱️', desc: 'Current ISO timestamp' },
    { label: 'this.name', insert: 'this.name', icon: '🏷️', desc: 'Name of the current node' },
    { label: 'this.id', insert: 'this.id', icon: '🆔', desc: 'ID of current node' },
    { label: 'this.type', insert: 'this.type', icon: '🧩', desc: 'Node type' },
    { label: '$input', insert: '$input', icon: '📥', desc: 'Incoming payload from upstream' },
    { label: '$json', insert: '$json', icon: '📋', desc: 'Incoming JSON object' },
    { label: '$nodes', insert: '$nodes', icon: '🔀', desc: 'Map of all upstream node outputs' },
    { label: 'DateTime.UtcNow', insert: 'DateTime.UtcNow', icon: '⏰', desc: 'UTC date time' },
    { label: 'DnaSequence', insert: 'new DnaSequence("")', icon: '🧬', desc: 'DNA sequence object' },
    { label: 'NeedlemanWunsch', insert: 'NeedlemanWunsch.Align()', icon: '🔬', desc: 'Pairwise sequence aligner' }
  ];

  let autocompleteSuggestions = [...EXPRESSION_SYMBOLS];

  function openNodeSmartConfig(node: any) {
    if (!node) return;
    selectedId = node.id;
    selectedEdgeId = null;
    testExprOutput = '';
  }

  function attachExpressionToNode(node: any) {
    if (!node) return;
    if (!node.config) node.config = {};
    node.config.hasExpression = true;
    if (!node.config.expression) {
      node.config.expression = `Console.log(Time.now + " " + this.name + " has executed");`;
    }
    if (!node.config.expressionLanguage) {
      node.config.expressionLanguage = 'javascript';
    }
    nodes = [...nodes];
  }

  function detachExpressionFromNode(node: any) {
    if (!node || !node.config) return;
    node.config.hasExpression = false;
    node.config.expression = '';
    testExprOutput = '';
    nodes = [...nodes];
  }

  function onExpressionInput(e: any) {
    const val: string = e.target.value;
    const cursor: number = e.target.selectionStart || 0;
    autocompleteCursor = cursor;

    const textBefore = val.slice(0, cursor);
    const lastWordMatch = textBefore.match(/([a-zA-Z0-9_\$.\>]+)$/);
    if (!lastWordMatch) {
      showIntelliSense = false;
      return;
    }

    const token = lastWordMatch[1];
    autocompleteQuery = token;

    if (token.endsWith('.')) {
      const parent = token.slice(0, -1);
      if (parent.toLowerCase() === 'console') {
        autocompleteSuggestions = [
          { label: 'log()', insert: 'log()', icon: '📜', desc: 'Write string to execution log' },
          { label: 'warn()', insert: 'warn()', icon: '⚠️', desc: 'Write warning to log stream' },
          { label: 'error()', insert: 'error()', icon: '❌', desc: 'Write error to log stream' }
        ];
      } else if (parent.toLowerCase() === 'time') {
        autocompleteSuggestions = [
          { label: 'now', insert: 'now', icon: '⏱️', desc: 'Current ISO timestamp string' },
          { label: 'utc', insert: 'utc', icon: '🌐', desc: 'UTC string' },
          { label: 'unix', insert: 'unix', icon: '🔢', desc: 'Unix milliseconds epoch' }
        ];
      } else if (parent.toLowerCase() === 'this') {
        autocompleteSuggestions = [
          { label: 'name', insert: 'name', icon: '🏷️', desc: 'Node display name' },
          { label: 'id', insert: 'id', icon: '🆔', desc: 'Unique node identifier' },
          { label: 'type', insert: 'type', icon: '🧩', desc: 'Node category type' },
          { label: 'config', insert: 'config', icon: '⚙️', desc: 'Node configuration dictionary' }
        ];
      } else {
        autocompleteSuggestions = EXPRESSION_SYMBOLS;
      }
      showIntelliSense = true;
      selectedSuggestionIndex = 0;
      return;
    }

    const query = token.toLowerCase();
    autocompleteSuggestions = EXPRESSION_SYMBOLS.filter(
      (s) => s.label.toLowerCase().includes(query) || s.insert.toLowerCase().includes(query)
    );

    if (autocompleteSuggestions.length > 0) {
      showIntelliSense = true;
      selectedSuggestionIndex = 0;
    } else {
      showIntelliSense = false;
    }
  }

  function selectSuggestion(item: any) {
    if (!currentNode) return;
    const currentCode = currentNode.config?.expression || '';
    const textBefore = currentCode.slice(0, autocompleteCursor);
    const textAfter = currentCode.slice(autocompleteCursor);

    const lastWordMatch = textBefore.match(/([a-zA-Z0-9_\$.\>]+)$/);
    let newBefore = textBefore;
    if (lastWordMatch) {
      const matchWord = lastWordMatch[1];
      if (matchWord.endsWith('.')) {
        newBefore = textBefore + item.insert;
      } else {
        newBefore = textBefore.slice(0, textBefore.length - matchWord.length) + item.insert;
      }
    } else {
      newBefore = textBefore + item.insert;
    }

    currentNode.config.expression = newBefore + textAfter;
    showIntelliSense = false;
    nodes = [...nodes];
  }

  function onExpressionKeydown(e: KeyboardEvent) {
    if (!showIntelliSense || autocompleteSuggestions.length === 0) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      selectedSuggestionIndex = (selectedSuggestionIndex + 1) % autocompleteSuggestions.length;
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      selectedSuggestionIndex = (selectedSuggestionIndex - 1 + autocompleteSuggestions.length) % autocompleteSuggestions.length;
    } else if (e.key === 'Enter' || e.key === 'Tab') {
      e.preventDefault();
      selectSuggestion(autocompleteSuggestions[selectedSuggestionIndex]);
    } else if (e.key === 'Escape') {
      showIntelliSense = false;
    }
  }

  async function testCurrentNodeExpression() {
    if (!currentNode) return;
    testingExpr = true;
    testExprOutput = '';

    const exprCode = currentNode.config?.expression || '';
    const mockInput = currentNode.config?.value || 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC';

    try {
      const captured: string[] = [];
      const fakeConsole = {
        log: (...args: any[]) => captured.push(args.map(a => typeof a === 'object' ? JSON.stringify(a) : String(a)).join(' ')),
        warn: (...args: any[]) => captured.push('[WARN] ' + args.join(' ')),
        error: (...args: any[]) => captured.push('[ERROR] ' + args.join(' '))
      };

      const ctx = {
        Console: fakeConsole,
        console: fakeConsole,
        Time: {
          now: new Date().toISOString(),
          utc: new Date().toUTCString(),
          unix: Date.now()
        },
        DateTime: {
          UtcNow: new Date().toISOString()
        },
        this: {
          id: currentNode.id,
          name: currentNode.name,
          type: currentNode.type,
          config: currentNode.config || {}
        },
        $input: { value: mockInput, text: mockInput },
        $json: { value: mockInput, text: mockInput },
        $nodes: {}
      };

      const fn = new Function(...Object.keys(ctx), `
        try {
          ${exprCode.includes('return') ? exprCode : `const _res = (${exprCode}); return _res;`}
        } catch (err) {
          Console.log("Runtime error: " + err.message);
          return null;
        }
      `);

      const res = fn(...Object.values(ctx));
      let out = '';
      if (captured.length > 0) {
        out += captured.join('\\n') + '\\n';
      }
      if (res !== undefined && res !== null) {
        out += `=> Return Value: ${typeof res === 'object' ? JSON.stringify(res, null, 2) : res}`;
      }
      testExprOutput = out.trim() || 'Expression executed with no logs or return value.';
    } catch (e: any) {
      testExprOutput = `Syntax / Evaluation Error: ${e.message}`;
    } finally {
      testingExpr = false;
    }
  }
'''

# Replace old openNodeSmartConfig and custom component methods with script_addition
old_open_smart_pattern = re.compile(r'function openNodeSmartConfig\(node: any\)\s*\{.*?function addCustomProperty\(\)\s*\{.*?\}\s*\}', re.DOTALL)
if old_open_smart_pattern.search(text):
    text = old_open_smart_pattern.sub(script_addition, text)
else:
    # Fallback insertion before </script>
    text = text.replace('function onClose() {', script_addition + '\n  function onClose() {')

# 3. Replace the old "Component Entity Properties" block in aside.inspector with clean node fields
old_entity_props_pattern = re.compile(r'<!-- DYNAMIC TYPE CONFIGURATION -->\s*<div class="section">\s*<div class="section-title">Component Entity Properties</div>\s*<!-- INPUT NODE CONFIG WITH STRONGLY TYPED FIELDS -->\s*\{#if currentNode\.type === \'Input\'\}.*?<!-- AGENT NODE CONFIG -->', re.DOTALL)

clean_node_config = '''<!-- DYNAMIC TYPE CONFIGURATION -->
          <div class="section">
            <div class="section-title">Node Parameters</div>

            <!-- INPUT NODE CONFIG -->
            {#if currentNode.type === 'Input'}
              <div class="field">
                <label>Input Data / Nucleotide Sequence</label>
                <textarea
                  rows="4"
                  bind:value={currentNode.config.value}
                  placeholder="Enter raw input sequence, text data, or JSON payload..."
                ></textarea>
              </div>

            <!-- AGENT NODE CONFIG -->'''

text = old_entity_props_pattern.sub(clean_node_config, text)

# 4. In aside.inspector, right before the Delete Node button, add the Attached Expression Section
old_delete_btn = '''          <button
            type="button"
            class="danger"
            on:click={() => {'''

new_expression_section = '''          <!-- ATTACHED EXPRESSION / SCRIPT SECTION (n8n / Elsa Style) -->
          <div class="section expr-section-box">
            {#if !currentNode.config?.hasExpression && !currentNode.config?.expression}
              <button
                type="button"
                class="btn-attach-expression"
                on:click={() => attachExpressionToNode(currentNode)}
              >
                + Attach Expression (Custom Script)
              </button>
            {:else}
              <div class="expr-header-row">
                <span class="expr-section-title">⚡ Attached Expression</span>
                <div class="expr-btn-group">
                  <button
                    type="button"
                    class="btn-expr-focus"
                    on:click={() => (showFocusMode = true)}
                    title="Open Fullscreen Focus Mode Editor"
                  >
                    ⛶ Focus Mode
                  </button>
                  <button
                    type="button"
                    class="btn-expr-detach"
                    on:click={() => detachExpressionFromNode(currentNode)}
                    title="Remove attached expression"
                  >
                    ✕ Detach
                  </button>
                </div>
              </div>

              <div class="expr-runtime-row">
                <label>Runtime:</label>
                <select bind:value={currentNode.config.expressionLanguage} class="expr-lang-select">
                  <option value="javascript">JavaScript (Node)</option>
                  <option value="csharp">C# (.NET 8)</option>
                  <option value="python">Python 3</option>
                </select>
                <span class="expr-auto-run">Runs on execute</span>
              </div>

              <div class="expr-editor-wrap">
                <textarea
                  class="expr-code-editor font-mono"
                  rows="5"
                  bind:value={currentNode.config.expression}
                  on:input={onExpressionInput}
                  on:keydown={onExpressionKeydown}
                  placeholder="Console.log(Time.now + ' ' + this.name + ' has executed');"
                ></textarea>

                <!-- Floating IntelliSense Dropdown -->
                {#if showIntelliSense && autocompleteSuggestions.length > 0}
                  <div class="intelli-dropdown">
                    {#each autocompleteSuggestions as item, idx}
                      <button
                        type="button"
                        class="intelli-item"
                        class:selected={idx === selectedSuggestionIndex}
                        on:mousedown|preventDefault={() => selectSuggestion(item)}
                      >
                        <span class="intelli-icon">{item.icon}</span>
                        <span class="intelli-label font-mono">{item.label}</span>
                        <span class="intelli-desc">{item.desc}</span>
                      </button>
                    {/each}
                  </div>
                {/if}
              </div>

              <div class="expr-test-bar">
                <button
                  type="button"
                  class="btn-test-runner"
                  on:click={testCurrentNodeExpression}
                  disabled={testingExpr}
                >
                  {testingExpr ? 'Testing...' : '🧪 Test Expression'}
                </button>
              </div>

              {#if testExprOutput}
                <div class="expr-test-output font-mono">
                  <div class="output-title">Console Output:</div>
                  <pre>{testExprOutput}</pre>
                </div>
              {/if}
            {/if}
          </div>

          <button
            type="button"
            class="danger"
            on:click={() => {'''

text = text.replace(old_delete_btn, new_expression_section)

# 5. Remove the old drawer modal showNodeDrawer
old_drawer_pattern = re.compile(r'\{#if showNodeDrawer && inspectingNode\}.*?\{/if\}\s*(?=\s*\{#if showContextMenu && contextMenuNode\})', re.DOTALL)
# or if it appears after context menu
if '{#if showNodeDrawer && inspectingNode}' in text:
    old_drawer_pattern2 = re.compile(r'\{#if showNodeDrawer && inspectingNode\}.*?</div>\s*</div>\s*\{/if\}', re.DOTALL)
    text = old_drawer_pattern2.sub('', text)

# 6. Add Focus Mode Modal before closing root div
focus_mode_modal = '''
  <!-- FOCUS MODE FULLSCREEN CODE EDITOR MODAL -->
  {#if showFocusMode && currentNode}
    <div class="focus-modal-overlay" on:click|self={() => (showFocusMode = false)}>
      <div class="focus-modal-card">
        <div class="focus-modal-header">
          <div class="focus-modal-title">
            <span class="focus-title-icon">⚡</span>
            <div>
              <h3>{currentNode.name} — Expression Script (Focus Mode)</h3>
              <span class="focus-subtitle">Full-screen authoring with real-time IntelliSense and Console execution</span>
            </div>
          </div>
          <div class="focus-header-actions">
            <select bind:value={currentNode.config.expressionLanguage} class="expr-lang-select">
              <option value="javascript">JavaScript (Node)</option>
              <option value="csharp">C# (.NET 8)</option>
              <option value="python">Python 3</option>
            </select>
            <button type="button" class="btn-exit-focus" on:click={() => (showFocusMode = false)}>
              ✕ Exit Focus Mode
            </button>
          </div>
        </div>

        <div class="focus-modal-body">
          <div class="focus-editor-wrapper">
            <textarea
              class="focus-textarea font-mono"
              bind:value={currentNode.config.expression}
              on:input={onExpressionInput}
              on:keydown={onExpressionKeydown}
              placeholder="Console.log(Time.now + ' ' + this.name + ' has executed');"
              rows="18"
            ></textarea>

            <!-- Focus Mode IntelliSense Autocomplete -->
            {#if showIntelliSense && autocompleteSuggestions.length > 0}
              <div class="intelli-dropdown focus-intelli">
                {#each autocompleteSuggestions as item, idx}
                  <button
                    type="button"
                    class="intelli-item"
                    class:selected={idx === selectedSuggestionIndex}
                    on:mousedown|preventDefault={() => selectSuggestion(item)}
                  >
                    <span class="intelli-icon">{item.icon}</span>
                    <span class="intelli-label font-mono">{item.label}</span>
                    <span class="intelli-desc">{item.desc}</span>
                  </button>
                {/each}
              </div>
            {/if}
          </div>

          <div class="focus-footer-bar">
            <button
              type="button"
              class="btn-test-runner focus-run-btn"
              on:click={testCurrentNodeExpression}
              disabled={testingExpr}
            >
              {testingExpr ? 'Testing...' : '🧪 Test Expression'}
            </button>
            <span class="focus-hint">Shortcut: Type <code>Console.</code>, <code>Time.</code>, <code>this.</code>, or <code>$input</code></span>
          </div>

          {#if testExprOutput}
            <div class="focus-output-card font-mono">
              <div class="output-title">Console Output:</div>
              <pre>{testExprOutput}</pre>
            </div>
          {/if}
        </div>
      </div>
    </div>
  {/if}
'''

# Insert before </style> or right before last closing tag of markup
text = text.replace('<!-- TAB 2: STATE MACHINE DESIGNER -->', focus_mode_modal + '\n  <!-- TAB 2: STATE MACHINE DESIGNER -->')

# 7. Add CSS styles for Expression Section, IntelliSense dropdown, and Focus Mode
css_styles = '''
  /* EXPRESSION SECTION & FOCUS MODE STYLES */
  .expr-section-box {
    margin-top: 1rem;
    padding: 0.85rem;
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(56, 189, 248, 0.2);
    border-radius: 0.5rem;
  }
  .btn-attach-expression {
    width: 100%;
    padding: 0.6rem;
    background: rgba(14, 165, 233, 0.15);
    border: 1px dashed rgba(56, 189, 248, 0.4);
    color: #38bdf8;
    font-weight: 600;
    font-size: 0.85rem;
    border-radius: 0.375rem;
    cursor: pointer;
    transition: all 0.2s ease;
  }
  .btn-attach-expression:hover {
    background: rgba(14, 165, 233, 0.25);
    border-color: #38bdf8;
  }
  .expr-header-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;
  }
  .expr-section-title {
    font-size: 0.85rem;
    font-weight: 700;
    color: #38bdf8;
  }
  .expr-btn-group {
    display: flex;
    gap: 0.35rem;
  }
  .btn-expr-focus {
    background: rgba(168, 85, 247, 0.15);
    border: 1px solid rgba(168, 85, 247, 0.3);
    color: #c084fc;
    font-size: 0.72rem;
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    cursor: pointer;
  }
  .btn-expr-focus:hover {
    background: rgba(168, 85, 247, 0.25);
  }
  .btn-expr-detach {
    background: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.3);
    color: #fca5a5;
    font-size: 0.72rem;
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    cursor: pointer;
  }
  .expr-runtime-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
    font-size: 0.75rem;
    color: #94a3b8;
  }
  .expr-lang-select {
    background: #0f172a;
    border: 1px solid #334155;
    color: #f8fafc;
    font-size: 0.75rem;
    padding: 0.2rem 0.4rem;
    border-radius: 4px;
  }
  .expr-auto-run {
    font-size: 0.7rem;
    color: #22c55e;
    margin-left: auto;
  }
  .expr-editor-wrap {
    position: relative;
    margin-bottom: 0.5rem;
  }
  .expr-code-editor {
    width: 100%;
    background: #090d16;
    border: 1px solid #334155;
    color: #f8fafc;
    padding: 0.5rem;
    font-size: 0.8rem;
    border-radius: 4px;
    resize: vertical;
    box-sizing: border-box;
  }
  .expr-code-editor:focus {
    outline: none;
    border-color: #38bdf8;
  }
  .intelli-dropdown {
    position: absolute;
    bottom: 100%;
    left: 0;
    width: 100%;
    max-height: 180px;
    overflow-y: auto;
    background: #0f172a;
    border: 1px solid #38bdf8;
    border-radius: 4px;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.7);
    z-index: 1000;
  }
  .intelli-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    padding: 0.35rem 0.6rem;
    background: transparent;
    border: none;
    color: #e2e8f0;
    cursor: pointer;
    text-align: left;
    font-size: 0.75rem;
    border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  }
  .intelli-item:hover,
  .intelli-item.selected {
    background: rgba(14, 165, 233, 0.25);
    color: #38bdf8;
  }
  .intelli-icon {
    font-size: 0.85rem;
  }
  .intelli-label {
    font-weight: 600;
    color: #f1f5f9;
  }
  .intelli-desc {
    font-size: 0.65rem;
    color: #94a3b8;
    margin-left: auto;
  }
  .expr-test-bar {
    display: flex;
    justify-content: flex-end;
    margin-bottom: 0.5rem;
  }
  .btn-test-runner {
    background: rgba(34, 197, 94, 0.15);
    border: 1px solid rgba(34, 197, 94, 0.35);
    color: #4ade80;
    padding: 0.3rem 0.7rem;
    font-size: 0.75rem;
    font-weight: 600;
    border-radius: 4px;
    cursor: pointer;
  }
  .btn-test-runner:hover:not(:disabled) {
    background: rgba(34, 197, 94, 0.25);
  }
  .expr-test-output {
    background: #050811;
    border: 1px solid #1e293b;
    border-radius: 4px;
    padding: 0.5rem;
    font-size: 0.75rem;
    color: #cbd5e1;
    max-height: 120px;
    overflow-y: auto;
  }
  .output-title {
    font-size: 0.7rem;
    color: #94a3b8;
    margin-bottom: 0.25rem;
  }

  /* FOCUS MODE MODAL */
  .focus-modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.85);
    backdrop-filter: blur(12px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    padding: 2rem;
  }
  .focus-modal-card {
    background: #0f172a;
    border: 1px solid rgba(56, 189, 248, 0.3);
    border-radius: 0.75rem;
    width: 100%;
    max-width: 900px;
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.8);
    display: flex;
    flex-direction: column;
    max-height: 90vh;
    overflow: hidden;
  }
  .focus-modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.25rem 1.5rem;
    border-bottom: 1px solid #1e293b;
    background: rgba(30, 41, 59, 0.4);
  }
  .focus-modal-title {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }
  .focus-title-icon {
    font-size: 1.5rem;
  }
  .focus-modal-title h3 {
    margin: 0;
    font-size: 1.1rem;
    color: #f8fafc;
  }
  .focus-subtitle {
    font-size: 0.75rem;
    color: #94a3b8;
  }
  .focus-header-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }
  .btn-exit-focus {
    background: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.35);
    color: #fca5a5;
    padding: 0.4rem 0.85rem;
    font-weight: 600;
    font-size: 0.8rem;
    border-radius: 6px;
    cursor: pointer;
  }
  .btn-exit-focus:hover {
    background: rgba(239, 68, 68, 0.25);
  }
  .focus-modal-body {
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    overflow-y: auto;
  }
  .focus-editor-wrapper {
    position: relative;
    width: 100%;
  }
  .focus-textarea {
    width: 100%;
    background: #080c15;
    border: 1px solid #334155;
    color: #f8fafc;
    padding: 1rem;
    font-size: 0.95rem;
    border-radius: 6px;
    resize: vertical;
    box-sizing: border-box;
    line-height: 1.5;
  }
  .focus-textarea:focus {
    outline: none;
    border-color: #38bdf8;
  }
  .focus-intelli {
    bottom: auto;
    top: 50px;
    max-width: 380px;
  }
  .focus-footer-bar {
    display: flex;
    align-items: center;
    gap: 1rem;
  }
  .focus-run-btn {
    padding: 0.5rem 1.25rem;
    font-size: 0.85rem;
  }
  .focus-hint {
    font-size: 0.8rem;
    color: #94a3b8;
  }
  .focus-output-card {
    background: #050811;
    border: 1px solid #1e293b;
    border-radius: 6px;
    padding: 1rem;
    font-size: 0.85rem;
    color: #cbd5e1;
    max-height: 180px;
    overflow-y: auto;
  }
'''

text = text.replace('</style>', css_styles + '\n</style>')

with open('src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/components/WorkflowDesigner.svelte', 'w') as f:
    f.write(text)

print('WorkflowDesigner.svelte successfully refactored!')
