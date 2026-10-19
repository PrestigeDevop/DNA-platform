<script lang="ts">
  import { onMount, onDestroy, createEventDispatcher } from 'svelte';
  import { browser } from '$app/environment';

  export let value: string = '';
  export let language: string = 'javascript';
  export let readOnly: boolean = false;
  export let height: string = '240px';
  export let lineNumbers: 'on' | 'off' = 'on';
  export let minimap: boolean = false;
  export let placeholder: string = '';

  const dispatch = createEventDispatcher();
  let containerEl: HTMLDivElement;
  let editor: any = null;
  let monacoInstance: any = null;
  let isMounted = false;

  onMount(async () => {
    if (!browser) return;
    isMounted = true;

    try {
      // Dynamic import to avoid SSR errors
      monacoInstance = await import('monaco-editor');

      // Define our custom DNA Platform dark theme
      monacoInstance.editor.defineTheme('dna-dark-theme', {
        base: 'vs-dark',
        inherit: true,
        rules: [
          { token: 'comment', foreground: '64748b', fontStyle: 'italic' },
          { token: 'keyword', foreground: '38bdf8' },
          { token: 'identifier', foreground: 'e2e8f0' },
          { token: 'string', foreground: '34d399' },
          { token: 'number', foreground: 'fbbf24' },
          { token: 'delimiter', foreground: '94a3b8' }
        ],
        colors: {
          'editor.background': '#07090e',
          'editor.foreground': '#e2e8f0',
          'editorCursor.foreground': '#38bdf8',
          'editor.lineHighlightBackground': '#0f172a',
          'editorLineNumber.foreground': '#334155',
          'editorLineNumber.activeForeground': '#38bdf8',
          'editor.selectionBackground': '#1e3a5f',
          'editor.inactiveSelectionBackground': '#0f2744'
        }
      });

      const langMap: Record<string, string> = {
        javascript: 'javascript',
        js: 'javascript',
        csharp: 'csharp',
        cs: 'csharp',
        python: 'python',
        py: 'python',
        bash: 'shell',
        shell: 'shell',
        json: 'json'
      };

      editor = monacoInstance.editor.create(containerEl, {
        value: value || '',
        language: langMap[language.toLowerCase()] || 'javascript',
        theme: 'dna-dark-theme',
        automaticLayout: true,
        readOnly,
        lineNumbers,
        minimap: { enabled: minimap },
        fontSize: 13,
        fontFamily: "'JetBrains Mono', 'Fira Code', Menlo, Consolas, monospace",
        scrollBeyondLastLine: false,
        wordWrap: 'on',
        tabSize: 2,
        padding: { top: 8, bottom: 8 },
        suggestOnTriggerCharacters: true,
        quickSuggestions: true
      });

      editor.onDidChangeModelContent(() => {
        const val = editor.getValue();
        if (val !== value) {
          value = val;
          dispatch('change', val);
        }
      });
    } catch (err) {
      console.warn('[MonacoEditor] Failed to load Monaco locally, using fallback:', err);
    }
  });

  $: if (editor && value !== undefined && value !== editor.getValue()) {
    editor.setValue(value || '');
  }

  $: if (editor && monacoInstance && language) {
    const model = editor.getModel();
    if (model) {
      const langMap: Record<string, string> = {
        javascript: 'javascript',
        js: 'javascript',
        csharp: 'csharp',
        cs: 'csharp',
        python: 'python',
        py: 'python',
        bash: 'shell',
        shell: 'shell',
        json: 'json'
      };
      monacoInstance.editor.setModelLanguage(model, langMap[language.toLowerCase()] || 'javascript');
    }
  }

  onDestroy(() => {
    if (editor) {
      editor.dispose();
      editor = null;
    }
  });
</script>

<div class="monaco-wrapper" style="height: {height};" bind:this={containerEl}>
  {#if !editor}
    <textarea
      class="fallback-textarea font-mono"
      style="height: {height};"
      bind:value
      on:input={() => dispatch('change', value)}
      {placeholder}
      {readOnly}
    ></textarea>
  {/if}
</div>

<style>
  .monaco-wrapper {
    width: 100%;
    position: relative;
    border: 1px solid #1e293b;
    border-radius: 6px;
    overflow: hidden;
    background: #07090e;
  }
  .fallback-textarea {
    width: 100%;
    height: 100%;
    background: #07090e;
    color: #38bdf8;
    border: none;
    font-size: 13px;
    line-height: 1.5;
    padding: 10px;
    resize: none;
    outline: none;
    box-sizing: border-box;
  }
</style>
