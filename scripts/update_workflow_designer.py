import os

p = 'src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/components/WorkflowDesigner.svelte'
with open(p, 'r', encoding='utf-8') as f:
    c = f.read()

# 1. Inject methods
target_fn = '  function onNodeMouseDown(e: MouseEvent, node: any) {'
methods = '''  // Double-Click Smart Component configuration modal & inline expression evaluator
  let configModalNode: any = null;
  let configExpression = '';
  let configExpressionResult = '';
  let configProps: Array<{ name: string; type: string; value: any; min?: number; max?: number; step?: number }> = [];

  function openNodeSmartConfig(node: any) {
    configModalNode = node;
    configExpression = node.config?.expression || '{{ $input.gc > 0.50 ? "High GC" : "Low GC" }}';
    configExpressionResult = '';

    const defaultProps = [
      { name: 'GcThreshold', type: 'number', value: node.config?.gcThreshold ?? 0.55, min: 0, max: 1, step: 0.05 },
      { name: 'StrictValidation', type: 'boolean', value: node.config?.strictValidation ?? true },
      { name: 'RetryCount', type: 'number', value: node.config?.retryCount ?? 3, min: 1, max: 10, step: 1 }
    ];

    configProps = node.config?.smartProps
      ? JSON.parse(JSON.stringify(node.config.smartProps))
      : defaultProps;
  }

  function testNodeExpression() {
    try {
      const mockInput = {
        sequence: 'ATGGTGCACCTGACTCCT',
        length: 18,
        gc: 0.555,
        status: 'Valid'
      };

      let cleanExpr = configExpression.trim();
      if (cleanExpr.startsWith('{{') && cleanExpr.endsWith('}}')) {
        cleanExpr = cleanExpr.slice(2, -2).trim();
      }

      const fn = new Function('$input', 'properties', 'return (' + cleanExpr + ');');
      const propMap: Record<string, any> = {};
      configProps.forEach((p) => (propMap[p.name] = p.value));

      const result = fn(mockInput, propMap);
      configExpressionResult = `Result: ${typeof result === 'object' ? JSON.stringify(result) : String(result)}`;
    } catch (e: any) {
      configExpressionResult = `Evaluation Error: ${e.message}`;
    }
  }

  function saveNodeSmartConfig() {
    if (!configModalNode) return;
    nodes = nodes.map((n) => {
      if (n.id === configModalNode.id) {
        return {
          ...n,
          config: {
            ...n.config,
            expression: configExpression,
            smartProps: configProps
          }
        };
      }
      return n;
    });
    configModalNode = null;
  }

  function closeNodeSmartConfig() {
    configModalNode = null;
  }

'''

assert target_fn in c, "target_fn not found"
c = c.replace(target_fn, methods + target_fn)

# 2. Add button in inspector
target_inspector = '<div class="inspector-title">Node Inspector & Properties</div>'
inspector_btn = '''<div class="inspector-title">Node Inspector & Properties</div>
        {#if currentNode}
          <button type="button" class="btn-smart-config" on:click={() => openNodeSmartConfig(currentNode)}>
            ⚡ Customize Smart Component (Double-Click)
          </button>
        {/if}'''

assert target_inspector in c, "target_inspector not found"
c = c.replace(target_inspector, inspector_btn)

# 3. Add modal markup before TAB 2
target_modal_place = '  <!-- TAB 2: UNITY STYLE STATE MACHINE DESIGNER -->'
modal_markup = '''  {#if configModalNode}
    <div class="smart-config-modal-overlay" on:click|self={closeNodeSmartConfig}>
      <div class="smart-config-modal">
        <div class="scm-header">
          <div class="scm-title-box">
            <span class="scm-tag">Smart Component Node</span>
            <h3>⚡ {configModalNode.name || configModalNode.type}</h3>
          </div>
          <button type="button" class="scm-close" on:click={closeNodeSmartConfig}>✕</button>
        </div>

        <div class="scm-body">
          <div class="scm-section">
            <div class="scm-label">Unity-Style Reflected Public Properties</div>
            <div class="scm-props-grid">
              {#each configProps as prop}
                <div class="scm-prop-item">
                  <span class="scm-prop-name">{prop.name}</span>
                  {#if prop.type === 'boolean'}
                    <label class="switch">
                      <input type="checkbox" bind:checked={prop.value} />
                      <span class="slider"></span>
                    </label>
                  {:else if prop.type === 'number'}
                    <div class="scm-num-box">
                      <input type="range" min={prop.min ?? 0} max={prop.max ?? 100} step={prop.step ?? 1} bind:value={prop.value} />
                      <input type="number" class="scm-num-input font-mono" bind:value={prop.value} />
                    </div>
                  {:else}
                    <input type="text" class="scm-text-input" bind:value={prop.value} />
                  {/if}
                </div>
              {/each}
            </div>
          </div>

          <div class="scm-section">
            <div class="scm-expr-header">
              <span class="scm-label">n8n-Style Node Expression (Evaluated with $input and properties)</span>
              <a href="/kernel" class="scm-kernel-link" target="_blank">Open in Kernel Studio ↗</a>
            </div>
            <input
              type="text"
              class="scm-expr-input font-mono"
              bind:value={configExpression}
              placeholder="e.g. {{ $input.gc > 0.50 ? 'Pass' : 'Reject' }}"
            />
            <div class="scm-expr-actions">
              <button type="button" class="btn-test-expr" on:click={testNodeExpression}>
                ▶ Test Expression with Mock Sequence
              </button>
              {#if configExpressionResult}
                <span class="scm-expr-res font-mono">{configExpressionResult}</span>
              {/if}
            </div>
          </div>
        </div>

        <div class="scm-footer">
          <button type="button" class="btn-cancel" on:click={closeNodeSmartConfig}>Cancel</button>
          <button type="button" class="btn-save-scm" on:click={saveNodeSmartConfig}>💾 Save to Node</button>
        </div>
      </div>
    </div>
  {/if}

  <!-- TAB 2: AG-UI STATE MACHINE PROTOCOL DESIGNER -->'''

assert target_modal_place in c, "target_modal_place not found"
c = c.replace(target_modal_place, modal_markup)

# 4. Add CSS styles before the closing </style>
css_insertion = '''
  .btn-smart-config {
    background: #0284c7;
    color: #ffffff;
    border: none;
    padding: 0.45rem 0.75rem;
    border-radius: 6px;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
    margin-bottom: 0.5rem;
    text-align: center;
  }
  .btn-smart-config:hover { background: #0369a1; }

  .smart-config-modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.75);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    backdrop-filter: blur(4px);
  }
  .smart-config-modal {
    background: #0f172a;
    border: 1px solid #0284c7;
    border-radius: 10px;
    width: 620px;
    max-width: 95vw;
    box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);
    overflow: hidden;
  }
  .scm-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.25rem;
    background: #1e293b;
    border-bottom: 1px solid #334155;
  }
  .scm-tag { font-size: 0.7rem; color: #38bdf8; text-transform: uppercase; font-weight: 700; }
  .scm-header h3 { margin: 0.2rem 0 0 0; font-size: 1.15rem; color: #f8fafc; }
  .scm-close { background: transparent; border: none; color: #94a3b8; font-size: 1.1rem; cursor: pointer; }
  .scm-close:hover { color: #f8fafc; }
  .scm-body { padding: 1.25rem; display: flex; flex-direction: column; gap: 1.25rem; }
  .scm-label { font-size: 0.8rem; font-weight: 600; color: #94a3b8; margin-bottom: 0.5rem; display: block; }
  .scm-props-grid { display: flex; flex-direction: column; gap: 0.5rem; }
  .scm-prop-item { display: flex; justify-content: space-between; align-items: center; background: #1e293b; padding: 0.5rem 0.75rem; border-radius: 6px; }
  .scm-prop-name { font-size: 0.825rem; font-weight: 600; color: #f8fafc; }
  .scm-num-box { display: flex; align-items: center; gap: 0.5rem; }
  .scm-num-input { width: 55px; background: #090d16; border: 1px solid #334155; color: #38bdf8; padding: 0.25rem; border-radius: 4px; text-align: right; }
  .scm-text-input { background: #090d16; border: 1px solid #334155; color: #f8fafc; padding: 0.35rem 0.5rem; border-radius: 4px; font-size: 0.8rem; }
  .scm-expr-header { display: flex; justify-content: space-between; align-items: center; }
  .scm-kernel-link { font-size: 0.75rem; color: #38bdf8; text-decoration: none; }
  .scm-kernel-link:hover { text-decoration: underline; }
  .scm-expr-input { width: 100%; background: #090d16; border: 1px solid #334155; color: #10b981; padding: 0.5rem; border-radius: 6px; font-size: 0.85rem; box-sizing: border-box; }
  .scm-expr-actions { display: flex; align-items: center; gap: 0.75rem; margin-top: 0.5rem; flex-wrap: wrap; }
  .btn-test-expr { background: #1e293b; border: 1px solid #334155; color: #38bdf8; padding: 0.35rem 0.75rem; border-radius: 4px; font-size: 0.75rem; font-weight: 600; cursor: pointer; }
  .btn-test-expr:hover { background: #334155; }
  .scm-expr-res { font-size: 0.75rem; color: #a7f3d0; }
  .scm-footer { display: flex; justify-content: flex-end; gap: 0.75rem; padding: 0.75rem 1.25rem; background: #1e293b; border-top: 1px solid #334155; }
  .btn-cancel { background: transparent; border: 1px solid #475569; color: #cbd5e1; padding: 0.45rem 1rem; border-radius: 6px; cursor: pointer; }
  .btn-save-scm { background: #059669; border: none; color: #ffffff; padding: 0.45rem 1rem; border-radius: 6px; font-weight: 600; cursor: pointer; }
  .btn-save-scm:hover { background: #047857; }
'''

idx_style_close = c.rfind('</style>')
assert idx_style_close != -1
c = c[:idx_style_close] + css_insertion + c[idx_style_close:]

with open(p, 'w', encoding='utf-8') as f:
    f.write(c)

print('Updated WorkflowDesigner.svelte successfully!')
