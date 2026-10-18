/**
 * C# & Polyglot IntelliSense, Autocompletion & Diagnostic Helper
 * Provides real-time autocompletion suggestions, biological type definitions,
 * Unity-style property snippets, and syntax diagnostics in the web editor.
 */

export interface CompletionItem {
  label: string;
  kind: 'keyword' | 'class' | 'method' | 'property' | 'snippet' | 'attribute';
  detail: string;
  insertText: string;
  documentation?: string;
}

export interface DiagnosticIssue {
  line: number;
  message: string;
  severity: 'error' | 'warning' | 'info';
}

export const CSHARP_COMPLETIONS: CompletionItem[] = [
  // Biological Core Types
  {
    label: 'DnaSequence',
    kind: 'class',
    detail: 'DNAPlatform.Core.DnaSequence',
    insertText: 'new DnaSequence("${1:ATGCGATCG}")',
    documentation: 'High-performance biological nucleotide sequence container with GC content and reverse complement calculations.'
  },
  {
    label: 'CalculateGc()',
    kind: 'method',
    detail: 'double DnaSequence.CalculateGc()',
    insertText: 'CalculateGc()',
    documentation: 'Returns the ratio of Guanine and Cytosine bases (G + C) / Total.'
  },
  {
    label: 'CalculateGcSkew()',
    kind: 'method',
    detail: 'double DnaSequence.CalculateGcSkew()',
    insertText: 'CalculateGcSkew()',
    documentation: 'Returns the (G - C) / (G + C) replication asymmetry index.'
  },
  {
    label: 'ReverseComplement()',
    kind: 'method',
    detail: 'string DnaSequence.ReverseComplement()',
    insertText: 'ReverseComplement()',
    documentation: 'Generates the antiparallel reverse complement nucleotide strand.'
  },
  {
    label: 'NeedlemanWunschAligner.Align',
    kind: 'method',
    detail: 'NeedlemanWunschAligner.Align(seq1, seq2)',
    insertText: 'NeedlemanWunschAligner.Align("${1:seq1}", "${2:seq2}")',
    documentation: 'Dynamic programming global pairwise sequence alignment.'
  },

  // Unity-Style Inspector Snippets
  {
    label: 'prop_double (Unity Inspector)',
    kind: 'snippet',
    detail: '[Description("...")] public double Property',
    insertText: '[Description("${1:Description}")]\npublic double ${2:Threshold} { get; set; } = ${3:0.75};',
    documentation: 'Creates a public double property that auto-renders as a slider in the node inspector.'
  },
  {
    label: 'prop_bool (Unity Inspector)',
    kind: 'snippet',
    detail: '[Description("...")] public bool Property',
    insertText: '[Description("${1:Description}")]\npublic bool ${2:IsEnabled} { get; set; } = ${3:true};',
    documentation: 'Creates a public boolean property that auto-renders as an interactive toggle.'
  },
  {
    label: 'prop_int (Unity Inspector)',
    kind: 'snippet',
    detail: '[Description("...")] public int Property',
    insertText: '[Description("${1:Description}")]\npublic int ${2:MaxCount} { get; set; } = ${3:10};',
    documentation: 'Creates a public integer property that auto-renders as a number picker.'
  },

  // Microsoft Agent Framework & MCP Attributes
  {
    label: '[Tool]',
    kind: 'attribute',
    detail: '[Tool("name", "description")]',
    insertText: '[Tool("${1:tool_name}", "${2:Tool description}")]',
    documentation: 'Exposes method as an invocable tool for autonomous AI agents and MCP servers.'
  },
  {
    label: '[Category]',
    kind: 'attribute',
    detail: '[Category("State Machine | Shell Tools | Modules | MCP Server | Agent as MCP")]',
    insertText: '[Category("${1:State Machine}")]',
    documentation: 'Specifies the node palette category for workflow canvas placement.'
  },

  // Standard .NET
  {
    label: 'Console.WriteLine',
    kind: 'method',
    detail: 'void Console.WriteLine(object value)',
    insertText: 'Console.WriteLine(${1:value});',
    documentation: 'Writes the specified data, followed by the current line terminator, to the standard output stream.'
  },
  {
    label: 'DateTime.UtcNow',
    kind: 'property',
    detail: 'DateTime DateTime.UtcNow',
    insertText: 'DateTime.UtcNow',
    documentation: 'Gets a DateTime object that is set to the current date and time on this computer, expressed as the Coordinated Universal Time (UTC).'
  }
];

export function getCompletions(code: string, cursorPos: number): CompletionItem[] {
  const line = code.slice(0, cursorPos).split('\n').pop() || '';
  const tokenMatch = line.match(/([A-Za-z0-9_.]+)$/);
  const token = tokenMatch ? tokenMatch[1].toLowerCase() : '';

  if (!token) return CSHARP_COMPLETIONS.slice(0, 8);

  return CSHARP_COMPLETIONS.filter(
    item =>
      item.label.toLowerCase().includes(token) ||
      item.detail.toLowerCase().includes(token)
  );
}

export function validateCSharpSyntax(code: string): DiagnosticIssue[] {
  const issues: DiagnosticIssue[] = [];
  const lines = code.split('\n');

  let openBraces = 0;
  lines.forEach((lineText, idx) => {
    const lineNum = idx + 1;
    const trimmed = lineText.trim();

    // Check brace balance
    const opens = (lineText.match(/\{/g) || []).length;
    const closes = (lineText.match(/\}/g) || []).length;
    openBraces += opens - closes;

    // Check unclosed strings
    const quotes = (lineText.match(/"/g) || []).length;
    if (quotes % 2 !== 0 && !lineText.includes('"""')) {
      issues.push({
        line: lineNum,
        message: 'Unterminated string literal.',
        severity: 'error'
      });
    }

    // Check missing semicolon on field/statement
    if (
      trimmed.startsWith('public ') &&
      trimmed.includes('{ get; set; }') &&
      !trimmed.endsWith(';')
    ) {
      issues.push({
        line: lineNum,
        message: 'Public auto-property with default initializer must end with a semicolon.',
        severity: 'warning'
      });
    }
  });

  if (openBraces !== 0) {
    issues.push({
      line: lines.length,
      message: `Mismatched curly braces: ${Math.abs(openBraces)} ${openBraces > 0 ? 'unclosed' : 'extra closing'} brace(s).`,
      severity: 'error'
    });
  }

  return issues;
}
