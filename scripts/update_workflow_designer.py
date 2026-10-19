import os

wf_path = 'src/05_DevUI/DNAPlatform.DevUI.Web/src/lib/components/WorkflowDesigner.svelte'
with open(wf_path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Add Agent model download state & Custom ECS state to script
agent_state_insert = """
  // Agent Model Download & Cache Management
  let downloadedAgentModels: Record<string, boolean> = {
    'Microsoft-Copilot-Local-ONNX': true,
    'Microsoft-Copilot-Bio-ONNX': true,
    'Phi-3.5-mini-ONNX': true,
    'Biomedical-Copilot-v1': true
  };
  let agentModelDownloading = false;
  let agentModelProgress = 0;

  function downloadAgentModel(modelName: string) {
    if (agentModelDownloading || downloadedAgentModels[modelName]) return;
    agentModelDownloading = true;
    agentModelProgress = 20;
    const interval = setInterval(() => {
      agentModelProgress += 25;
      if (agentModelProgress >= 100) {
        clearInterval(interval);
        downloadedAgentModels[modelName] = true;
        downloadedAgentModels = { ...downloadedAgentModels };
        agentModelDownloading = false;
        agentModelProgress = 0;
      }
    }, 250);
  }

  // Custom ECS Property Schema Creator State
  let showAddPropertyForm = false;
  let newPropName = '';
  let newPropType: 'enum' | 'vectordb' | 'number' | 'boolean' | 'string' = 'enum';
  let newPropOptions = 'Permissive, Standard, Strict';
  let newPropDefault = 'Standard';
  let newPropDesc = '';

  function addCustomEcsProperty(node: any) {
    if (!node || !newPropName.trim()) return;
    if (!node.config) node.config = {};
    if (!node.config.properties) node.config.properties = {};
    if (!node.config.propertySchemas) node.config.propertySchemas = [];

    const pName = newPropName.trim();
    let schema: any = {
      name: pName,
      type: newPropType,
      description: newPropDesc || `${newPropType} property for ${pName}`,
      defaultValue: newPropDefault
    };

    if (newPropType === 'enum') {
      const opts = newPropOptions.split(',').map(s => s.trim()).filter(Boolean);
      schema.options = opts.length > 0 ? opts : ['Default'];
      schema.defaultValue = opts[0] || 'Default';
      node.config.properties[pName] = schema.defaultValue;
    } else if (newPropType === 'vectordb') {
      schema.defaultValue = 'pgvector_default';
      schema.vectorDbConfig = {
        provider: 'pgvector',
        collection: 'genomic_embeddings',
        dimension: 1536,
        connectionUri: 'postgresql://postgres:postgres@localhost:5432/dnaplatform'
      };
      node.config.properties[pName] = 'pgvector_default';
    } else if (newPropType === 'number') {
      const num = parseFloat(newPropDefault) || 0.85;
      schema.min = 0;
      schema.max = 1;
      schema.step = 0.05;
      schema.defaultValue = num;
      node.config.properties[pName] = num;
    } else if (newPropType === 'boolean') {
      const b = newPropDefault === 'true' || newPropDefault === true;
      schema.defaultValue = b;
      node.config.properties[pName] = b;
    } else {
      schema.defaultValue = newPropDefault;
      node.config.properties[pName] = newPropDefault;
    }

    node.config.propertySchemas = [...node.config.propertySchemas, schema];
    nodes = [...nodes];

    // Reset creator
    newPropName = '';
    showAddPropertyForm = false;
  }

  function removeCustomEcsProperty(node: any, pName: string) {
    if (!node || !node.config) return;
    if (node.config.propertySchemas) {
      node.config.propertySchemas = node.config.propertySchemas.filter((s: any) => s.name !== pName);
    }
    if (node.config.properties) {
      delete node.config.properties[pName];
    }
    nodes = [...nodes];
  }

  async function autoReflectNodeSchemas(node: any) {
    if (!node) return;
    const code = node.config?.script || node.config?.expression || '';
    const lang = node.config?.runtime || node.config?.language || 'csharp';
    try {
      const res = await fetch('/api/authoring/reflect', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code, language: lang })
      });
      const data = await res.json();
      if (data.success && data.result) {
        if (!node.config) node.config = {};
        if (!node.config.properties) node.config.properties = {};
        node.config.propertySchemas = data.result.properties || [];
        for (const p of node.config.propertySchemas) {
          if (node.config.properties[p.name] === undefined) {
            node.config.properties[p.name] = p.defaultValue;
          }
        }
        nodes = [...nodes];
        message = `✔ Auto-reflected ${node.config.propertySchemas.length} ECS properties from code!`;
        setTimeout(() => (message = ''), 3000);
      }
    } catch (e: any) {
      console.warn('Reflection error:', e);
    }
  }

  // Load custom components from authoring studio
  let customStudioComponents: any[] = [];
  function loadCustomStudioComponents() {
    try {
      const stored = localStorage.getItem('dna_custom_components');
      if (stored) {
        customStudioComponents = JSON.parse(stored);
      }
    } catch (e) {}
  }
"""

# Inject state before onMount
idx_mount = text.find('onMount(() => {')
assert idx_mount != -1, 'onMount not found'
text = text[:idx_mount] + agent_state_insert + '\n  ' + text[idx_mount:]

# In onMount, add loadCustomStudioComponents()
text = text.replace('onMount(async () => {', 'onMount(async () => {\n    loadCustomStudioComponents();')

# Fix getNodePreview for Agent
old_preview = """    if (node.type === 'Agent') {
      const m = node.config?.model || 'Copilot-ONNX';
      return `${m.split('-')[0]} · ${node.config?.prompt || '{{input}}'}`;
    }"""

new_preview = """    if (node.type === 'Agent') {
      const m = node.config?.model || 'Microsoft-Copilot-Local-ONNX';
      const prompt = node.config?.prompt || '{{input}}';
      const shortModel = m.replace('Microsoft-', '').replace('-ONNX', '');
      return `[${shortModel}] ⚡ ${prompt}`;
    }"""

if old_preview in text:
    text = text.replace(old_preview, new_preview)

# Fix Agent node visual icon in canvas
old_node_icon = '<span class="node-icon">{meta.icon}</span>'
new_node_icon = """<span class="node-icon">
  {#if node.type === 'Agent'}
    {#if (node.config?.model || '').includes('Bio')}🧬
    {:else if (node.config?.model || '').includes('Phi')}⚡
    {:else if (node.config?.model || '').includes('Biomedical')}🔬
    {:else if (node.config?.model || '').includes('Foundry') || (node.config?.model || '').includes('Qwen')}🌐
    {:else}🤖{/if}
  {:else}
    {meta.icon}
  {/if}
</span>"""

if old_node_icon in text:
    text = text.replace(old_node_icon, new_node_icon, 1)

# Fix Agent inspector with download & change model
old_agent_inspector = """            {:else if currentNode.type === 'Agent'}
              <div class="field">
                <label>Inference Engine & Model</label>
                <select bind:value={currentNode.config.model}>
                  <option value="Microsoft-Copilot-Local-ONNX">Microsoft Copilot ONNX (Local CPU)</option>
                  <option value="Phi-3.5-mini-ONNX">Microsoft Phi-3.5-mini ONNX</option>
                  <option value="Biomedical-Copilot-v1">Biomedical Copilot Reasoning Engine</option>
                </select>
              </div>"""

new_agent_inspector = """            {:else if currentNode.type === 'Agent'}
              <div class="field">
                <div class="field-label-row">
                  <label>Inference Engine & Model</label>
                  {#if downloadedAgentModels[currentNode.config.model || 'Microsoft-Copilot-Local-ONNX']}
                    <span class="badge-model-ready">✓ Local ONNX Engine Ready</span>
                  {:else}
                    <button type="button" class="btn-download-model-sm" disabled={agentModelDownloading} on:click={() => downloadAgentModel(currentNode.config.model)}>
                      {agentModelDownloading ? `Downloading (${agentModelProgress}%)...` : '⬇ Download Model'}
                    </button>
                  {/if}
                </div>
                <select bind:value={currentNode.config.model} class="agent-model-select">
                  <option value="Microsoft-Copilot-Local-ONNX">🤖 Microsoft Copilot ONNX (Local CPU) [Default]</option>
                  <option value="Microsoft-Copilot-Bio-ONNX">🧬 Microsoft Copilot Bio ONNX [Genomics / Aligner]</option>
                  <option value="Phi-3.5-mini-ONNX">⚡ Microsoft Phi-3.5-mini ONNX [High-Speed Low-Latency]</option>
                  <option value="Biomedical-Copilot-v1">🔬 Biomedical Copilot Reasoning Engine</option>
                  <option value="Foundry-Local-Qwen-2.5">🌐 Foundry Local Qwen 2.5 (Local Weights)</option>
                </select>
              </div>"""

if old_agent_inspector in text:
    text = text.replace(old_agent_inspector, new_agent_inspector, 1)

# Fix testNodeExpression return handling
old_expr_fn = """      const exprFn = new Function('$input', 'input', 'properties', 'props', 'Console', 'console', 'Time', 'DateTime', `
        "use strict";
        return (${rawExpr});
      `);"""

new_expr_fn = """      let cleanExpr = rawExpr.trim();
      if (!cleanExpr.includes('return ') && !cleanExpr.includes(';')) {
        cleanExpr = 'return (' + cleanExpr + ');';
      }
      const exprFn = new Function('$input', 'input', 'properties', 'props', 'Console', 'console', 'Time', 'DateTime', `
        "use strict";
        ${cleanExpr}
      `);"""

if old_expr_fn in text:
    text = text.replace(old_expr_fn, new_expr_fn, 1)

# Replace ECS properties section in Inspector (lines 2117-2162)
old_ecs_section = """          <!-- SCRIPTABLE ECS PROPERTIES -->
          <div class="section ecs-properties-section">
            <div class="ecs-prop-header">
              <span class="section-title">⚡ Scriptable ECS Properties</span>
              <button type="button" class="btn-add-ecs-prop" on:click={() => addNodeEcsProperty(currentNode)}>
                + Add ECS Property
              </button>
            </div>
            {#if currentNode.config?.properties && Object.keys(currentNode.config.properties).length > 0}
              <div class="ecs-props-list">
                {#each Object.entries(currentNode.config.properties) as [propKey, propVal]}
                  <div class="ecs-prop-row">
                    <input
                      type="text"
                      class="prop-key-input"
                      value={propKey}
                      on:change={(e) => {
                        const newKey = e.currentTarget.value.trim();
                        if (newKey && newKey !== propKey) {
                          currentNode.config.properties[newKey] = propVal;
                          delete currentNode.config.properties[propKey];
                          nodes = [...nodes];
                        }
                      }}
                    />
                    <span class="prop-sep">:</span>
                    <input
                      type="text"
                      class="prop-val-input"
                      value={String(propVal)}
                      on:change={(e) => {
                        let parsedVal = e.currentTarget.value;
                        if (parsedVal === 'true') parsedVal = true;
                        else if (parsedVal === 'false') parsedVal = false;
                        else if (!isNaN(Number(parsedVal)) && parsedVal !== '') parsedVal = Number(parsedVal);
                        currentNode.config.properties[propKey] = parsedVal;
                        nodes = [...nodes];
                      }}
                    />
                    <button type="button" class="btn-del-prop" on:click={() => removeNodeEcsProperty(currentNode, propKey)}>✕</button>
                  </div>
                {/each}
              </div>
            {:else}
              <span class="empty-hint">No ECS properties declared. Accessible in expressions as <code>this.properties</code>.</span>
            {/if}
          </div>"""

new_ecs_section = """          <!-- HYBRID CUSTOM ECS PROPERTIES INSPECTOR -->
          <div class="section ecs-properties-section">
            <div class="ecs-prop-header">
              <div class="ecs-title-block">
                <span class="section-title">⚡ Custom ECS Node Properties</span>
                <span class="ecs-sub">Enum, Vector DB (pgvector), Sliders, Switches</span>
              </div>
              <div class="ecs-header-btns">
                <button type="button" class="btn-reflect-schema-sm" on:click={() => autoReflectNodeSchemas(currentNode)} title="Auto-reflect schemas from code">
                  🔄 Auto-Reflect
                </button>
                <button type="button" class="btn-add-ecs-prop" on:click={() => (showAddPropertyForm = !showAddPropertyForm)}>
                  {showAddPropertyForm ? '✕ Cancel' : '+ Add Property'}
                </button>
              </div>
            </div>

            <!-- INLINE ADD CUSTOM PROPERTY FORM -->
            {#if showAddPropertyForm}
              <div class="add-prop-inline-card">
                <div class="add-prop-title">Define Custom ECS Property</div>
                <div class="add-prop-grid">
                  <div class="add-prop-field">
                    <label>Property Name:</label>
                    <input type="text" bind:value={newPropName} placeholder="e.g. policyLevel" />
                  </div>
                  <div class="add-prop-field">
                    <label>Control Type:</label>
                    <select bind:value={newPropType}>
                      <option value="enum">Enum Dropdown</option>
                      <option value="vectordb">Vector DB Store (pgvector)</option>
                      <option value="number">Numeric Range Slider</option>
                      <option value="boolean">Boolean Switch</option>
                      <option value="string">Text String</option>
                    </select>
                  </div>
                </div>

                {#if newPropType === 'enum'}
                  <div class="add-prop-field full">
                    <label>Allowed Enum Choices (comma-separated):</label>
                    <input type="text" bind:value={newPropOptions} placeholder="Permissive, Standard, Strict" />
                  </div>
                {/if}

                <div class="add-prop-actions">
                  <button type="button" class="btn-save-custom-prop" on:click={() => addCustomEcsProperty(currentNode)}>
                    ✓ Save Property to Node
                  </button>
                </div>
              </div>
            {/if}

            <!-- RENDERED CUSTOM ECS PROPERTIES -->
            <div class="ecs-rendered-list">
              <!-- 1. Reflected / Defined Schemas -->
              {#if currentNode.config?.propertySchemas && currentNode.config.propertySchemas.length > 0}
                {#each currentNode.config.propertySchemas as schema}
                  <div class="ecs-prop-card">
                    <div class="ecs-prop-card-head">
                      <div class="prop-title-row">
                        <strong class="prop-k">{schema.name}</strong>
                        <span class="prop-t-badge {schema.type}">{schema.type.toUpperCase()}</span>
                      </div>
                      <button type="button" class="btn-del-prop" on:click={() => removeCustomEcsProperty(currentNode, schema.name)}>✕</button>
                    </div>
                    {#if schema.description}
                      <span class="prop-d-text">{schema.description}</span>
                    {/if}

                    <!-- INTERACTIVE WIDGET BASED ON PROPERTY TYPE -->
                    <div class="ecs-widget-wrapper">
                      {#if schema.type === 'enum'}
                        <select 
                          class="ecs-widget-select"
                          value={currentNode.config.properties?.[schema.name] || schema.defaultValue || schema.options?.[0]}
                          on:change={(e) => {
                            if (!currentNode.config.properties) currentNode.config.properties = {};
                            currentNode.config.properties[schema.name] = e.currentTarget.value;
                            nodes = [...nodes];
                          }}
                        >
                          {#each schema.options || ['Standard'] as opt}
                            <option value={opt}>{opt}</option>
                          {/each}
                        </select>

                      {:else if schema.type === 'vectordb'}
                        <div class="ecs-vectordb-widget">
                          <div class="vdb-ctrl-row">
                            <span class="vdb-label">Store:</span>
                            <select 
                              class="vdb-provider-select"
                              value={currentNode.config.properties?.[schema.name] || 'pgvector_default'}
                              on:change={(e) => {
                                if (!currentNode.config.properties) currentNode.config.properties = {};
                                currentNode.config.properties[schema.name] = e.currentTarget.value;
                                nodes = [...nodes];
                              }}
                            >
                              <option value="pgvector_default">pgvector (Local PostgreSQL/Prisma Default)</option>
                              <option value="chroma_genomic">ChromaDB Store</option>
                              <option value="qdrant_cloud">Qdrant Vector Engine</option>
                            </select>
                          </div>
                          <div class="vdb-meta-row">
                            <span>Collection: <code>genomic_embeddings</code></span> · 
                            <span>Dimension: <code>1536</code></span>
                          </div>
                        </div>

                      {:else if schema.type === 'number'}
                        <div class="ecs-slider-widget">
                          <input 
                            type="range" 
                            min={schema.min || 0} 
                            max={schema.max || 1} 
                            step={schema.step || 0.05}
                            value={currentNode.config.properties?.[schema.name] ?? schema.defaultValue ?? 0.85}
                            on:input={(e) => {
                              if (!currentNode.config.properties) currentNode.config.properties = {};
                              currentNode.config.properties[schema.name] = parseFloat(e.currentTarget.value);
                              nodes = [...nodes];
                            }}
                            class="ecs-slider-bar"
                          />
                          <span class="ecs-slider-val">{currentNode.config.properties?.[schema.name] ?? schema.defaultValue ?? 0.85}</span>
                        </div>

                      {:else if schema.type === 'boolean'}
                        <div class="ecs-toggle-widget">
                          <button 
                            type="button" 
                            class="btn-toggle-switch"
                            class:active={currentNode.config.properties?.[schema.name] ?? schema.defaultValue}
                            on:click={() => {
                              if (!currentNode.config.properties) currentNode.config.properties = {};
                              const curr = currentNode.config.properties?.[schema.name] ?? schema.defaultValue;
                              currentNode.config.properties[schema.name] = !curr;
                              nodes = [...nodes];
                            }}
                          >
                            {(currentNode.config.properties?.[schema.name] ?? schema.defaultValue) ? '✔ Enabled [TRUE]' : '✖ Disabled [FALSE]'}
                          </button>
                        </div>

                      {:else}
                        <input 
                          type="text" 
                          class="ecs-text-widget"
                          value={currentNode.config.properties?.[schema.name] ?? schema.defaultValue ?? ''}
                          on:input={(e) => {
                            if (!currentNode.config.properties) currentNode.config.properties = {};
                            currentNode.config.properties[schema.name] = e.currentTarget.value;
                            nodes = [...nodes];
                          }}
                        />
                      {/if}
                    </div>
                  </div>
                {/each}
              {/if}

              <!-- 2. Fallback Generic Properties for keys not in schemas -->
              {#if currentNode.config?.properties}
                {#each Object.entries(currentNode.config.properties) as [propKey, propVal]}
                  {#if !(currentNode.config?.propertySchemas || []).some((s: any) => s.name === propKey)}
                    <div class="ecs-prop-row generic">
                      <span class="prop-key-tag">{propKey}</span>
                      <span class="prop-sep">:</span>
                      <input
                        type="text"
                        class="prop-val-input"
                        value={String(propVal)}
                        on:change={(e) => {
                          let parsedVal = e.currentTarget.value;
                          if (parsedVal === 'true') parsedVal = true;
                          else if (parsedVal === 'false') parsedVal = false;
                          else if (!isNaN(Number(parsedVal)) && parsedVal !== '') parsedVal = Number(parsedVal);
                          currentNode.config.properties[propKey] = parsedVal;
                          nodes = [...nodes];
                        }}
                      />
                      <button type="button" class="btn-del-prop" on:click={() => removeNodeEcsProperty(currentNode, propKey)}>✕</button>
                    </div>
                  {/if}
                {/each}
              {/if}

              {#if (!currentNode.config?.propertySchemas || currentNode.config.propertySchemas.length === 0) && (!currentNode.config?.properties || Object.keys(currentNode.config.properties).length === 0)}
                <span class="empty-hint">No ECS properties declared. Click <strong>+ Add Property</strong> or <strong>🔄 Auto-Reflect</strong>. Accessible in expressions as <code>this.properties</code>.</span>
              {/if}
            </div>
          </div>"""

if old_ecs_section in text:
    text = text.replace(old_ecs_section, new_ecs_section, 1)

# In the palette, add Published Extensions & Components from Studio
palette_marker = '<div class="palette-scroll">'
new_palette_section = """<div class="palette-scroll">
          {#if customStudioComponents.length > 0}
            <div class="palette-subhead">Studio Components & Extensions</div>
            {#each customStudioComponents as comp}
              <div
                class="palette-card custom-comp-card"
                draggable="true"
                on:click={() => {
                  const id = 'n_' + Date.now().toString(36) + Math.random().toString(36).slice(2, 5);
                  const newProps = {};
                  for (const s of (comp.properties || [])) {
                    newProps[s.name] = s.defaultValue;
                  }
                  nodes = [...nodes, {
                    id,
                    type: 'LogicComponent',
                    name: comp.name,
                    x: 120,
                    y: 120,
                    config: {
                      componentName: comp.name,
                      runtime: comp.language || 'csharp',
                      inputs: comp.inputs || [{ name: 'input', type: 'DNA_Sequence' }],
                      outputs: comp.outputs || [{ name: 'output', type: 'JSON' }],
                      script: comp.code || '',
                      propertySchemas: comp.properties || [],
                      properties: newProps,
                      isMiddleware: comp.isMiddleware
                    }
                  }];
                  selectedId = id;
                }}
                style="border-left: 3px solid #8b5cf6;"
              >
                <span class="palette-icon">{comp.isMiddleware ? '🛡️' : '🧩'}</span>
                <div class="palette-info">
                  <span class="palette-label">{comp.name}</span>
                  <span class="palette-desc">{comp.category} · {comp.language?.toUpperCase()}</span>
                </div>
              </div>
            {/each}
            <div class="palette-subhead" style="margin-top: 0.5rem;">Standard Workflow Nodes</div>
          {/if}"""

if palette_marker in text:
    text = text.replace(palette_marker, new_palette_section, 1)

# Extra CSS styles for the new ECS Inspector widgets and Agent model options
extra_wf_css = """
  /* HYBRID ECS PROPERTY INSPECTOR STYLES */
  .ecs-title-block {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
  }

  .ecs-sub {
    font-size: 0.68rem;
    color: #64748b;
  }

  .ecs-header-btns {
    display: flex;
    gap: 0.4rem;
    align-items: center;
  }

  .btn-reflect-schema-sm {
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(14, 165, 233, 0.3);
    color: #38bdf8;
    font-size: 0.68rem;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .add-prop-inline-card {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.75rem;
    margin-bottom: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .add-prop-title {
    font-size: 0.75rem;
    font-weight: 600;
    color: #f1f5f9;
  }

  .add-prop-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
  }

  .add-prop-field {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .add-prop-field.full {
    grid-column: 1 / -1;
  }

  .add-prop-field label {
    font-size: 0.68rem;
    color: #94a3b8;
  }

  .add-prop-field input, .add-prop-field select {
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    padding: 0.3rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
  }

  .add-prop-actions {
    display: flex;
    justify-content: flex-end;
  }

  .btn-save-custom-prop {
    background: #10b981;
    color: #042f2e;
    border: none;
    border-radius: 0.25rem;
    font-size: 0.72rem;
    font-weight: 700;
    padding: 0.3rem 0.7rem;
    cursor: pointer;
  }

  .ecs-rendered-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .ecs-prop-card {
    background: #090e17;
    border: 1px solid #1e293b;
    border-radius: 0.375rem;
    padding: 0.55rem;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }

  .ecs-prop-card-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .prop-title-row {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .prop-k {
    font-size: 0.75rem;
    color: #f1f5f9;
    font-family: ui-monospace, monospace;
  }

  .prop-t-badge {
    font-size: 0.6rem;
    padding: 0.1rem 0.3rem;
    border-radius: 0.2rem;
    font-weight: 600;
  }

  .prop-t-badge.enum { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }
  .prop-t-badge.vectordb { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
  .prop-t-badge.number { background: rgba(14, 165, 233, 0.15); color: #38bdf8; border: 1px solid rgba(14, 165, 233, 0.3); }
  .prop-t-badge.boolean { background: rgba(168, 85, 247, 0.15); color: #c084fc; border: 1px solid rgba(168, 85, 247, 0.3); }

  .prop-d-text {
    font-size: 0.68rem;
    color: #64748b;
  }

  .ecs-widget-wrapper {
    margin-top: 0.15rem;
  }

  .ecs-widget-select, .ecs-text-widget {
    width: 100%;
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #f1f5f9;
    padding: 0.3rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    box-sizing: border-box;
  }

  .ecs-vectordb-widget {
    background: #07090e;
    border: 1px solid rgba(255, 255, 255, 0.06);
    border-radius: 0.25rem;
    padding: 0.4rem;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
  }

  .vdb-ctrl-row {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  .vdb-provider-select {
    flex: 1;
    background: #0f172a;
    border: 1px solid #1e293b;
    color: #34d399;
    padding: 0.25rem;
    font-size: 0.72rem;
    border-radius: 0.25rem;
  }

  .vdb-meta-row {
    font-size: 0.65rem;
    color: #64748b;
  }

  .vdb-meta-row code {
    color: #94a3b8;
  }

  .ecs-slider-widget {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  .ecs-slider-bar {
    flex: 1;
  }

  .ecs-slider-val {
    font-size: 0.75rem;
    color: #38bdf8;
    font-family: ui-monospace, monospace;
    font-weight: 600;
  }

  .btn-toggle-switch {
    width: 100%;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    color: #94a3b8;
    font-size: 0.72rem;
    padding: 0.35rem 0.6rem;
    border-radius: 0.25rem;
    cursor: pointer;
    text-align: center;
  }

  .btn-toggle-switch.active {
    background: rgba(16, 185, 129, 0.2);
    border-color: rgba(16, 185, 129, 0.4);
    color: #34d399;
    font-weight: 600;
  }

  .field-label-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.3rem;
  }

  .badge-model-ready {
    font-size: 0.65rem;
    color: #10b981;
    font-weight: 600;
  }

  .btn-download-model-sm {
    background: rgba(245, 158, 11, 0.2);
    border: 1px solid rgba(245, 158, 11, 0.4);
    color: #fbbf24;
    font-size: 0.65rem;
    padding: 0.15rem 0.45rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .palette-subhead {
    font-size: 0.68rem;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    padding: 0.3rem 0.2rem;
    font-weight: 600;
  }

  .custom-comp-card {
    background: rgba(139, 92, 246, 0.08);
  }
"""

idx_end_style = text.find('</style>')
if idx_end_style != -1:
    text = text[:idx_end_style] + extra_wf_css + text[idx_end_style:]

with open(wf_path, 'w', encoding='utf-8') as f:
    f.write(text)

print('WorkflowDesigner.svelte updated with ECS property inspector, Agent model switcher, and custom palette!')
