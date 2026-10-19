/**
 * C#, Polyglot & Workflow IntelliSense, Autocompletion & Diagnostic Helper
 * Provides real-time autocompletion suggestions, biological type definitions,
 * dot-notation object member introspection, ghost text suggestions, and syntax diagnostics.
 */

export interface CompletionItem {
  label: string;
  kind: 'keyword' | 'class' | 'method' | 'property' | 'snippet' | 'attribute' | 'variable';
  detail: string;
  insertText: string;
  documentation?: string;
  category?: string;
}

export interface DiagnosticIssue {
  line: number;
  message: string;
  severity: 'error' | 'warning' | 'info';
}

// --------------------------------------------------------------------------
// OBJECT MEMBER DICTIONARY (For Dot Notation Introspection: obj.)
// --------------------------------------------------------------------------
export const OBJECT_MEMBERS: Record<string, CompletionItem[]> = {
  // DNA Sequence & Bioinformatics Objects
  dna: [
    { label: 'CalculateGc()', kind: 'method', detail: 'double DnaSequence.CalculateGc()', insertText: 'CalculateGc()', documentation: 'Returns the ratio of Guanine and Cytosine bases: (G + C) / Total.' },
    { label: 'CalculateGcSkew()', kind: 'method', detail: 'double DnaSequence.CalculateGcSkew()', insertText: 'CalculateGcSkew()', documentation: 'Calculates the nucleotide skew index: (G - C) / (G + C).' },
    { label: 'ReverseComplement()', kind: 'method', detail: 'string DnaSequence.ReverseComplement()', insertText: 'ReverseComplement()', documentation: 'Generates the antiparallel 5\'->3\' reverse complement strand.' },
    { label: 'Transcribe()', kind: 'method', detail: 'string DnaSequence.Transcribe()', insertText: 'Transcribe()', documentation: 'Transcribes DNA nucleotide string to RNA replacing Thymine (T) with Uracil (U).' },
    { label: 'Mutate(pos, base)', kind: 'method', detail: 'DnaSequence Mutate(int position, string mutation)', insertText: 'Mutate(${1:0}, "${2:A}")', documentation: 'Applies a single-nucleotide polymorphism or mutation at specified index.' },
    { label: 'Length', kind: 'property', detail: 'int DnaSequence.Length', insertText: 'Length', documentation: 'Total count of nucleotide base pairs in the sequence.' },
    { label: 'ToString()', kind: 'method', detail: 'string DnaSequence.ToString()', insertText: 'ToString()', documentation: 'Returns the raw uppercase nucleotide character sequence.' }
  ],
  dnasequence: [], // will alias to dna
  seq: [],        // will alias to dna

  // Console Object
  console: [
    { label: 'WriteLine(value)', kind: 'method', detail: 'void Console.WriteLine(object value)', insertText: 'WriteLine(${1:value});', documentation: 'Writes text or objects followed by line terminator to stdout stream.' },
    { label: 'Write(value)', kind: 'method', detail: 'void Console.Write(object value)', insertText: 'Write(${1:value});', documentation: 'Writes text or objects without appending line terminator.' },
    { label: 'log(args)', kind: 'method', detail: 'console.log(...args)', insertText: 'log(${1:data});', documentation: 'Standard JavaScript/Node output logger.' },
    { label: 'error(args)', kind: 'method', detail: 'console.error(...args)', insertText: 'error(${1:err});', documentation: 'Standard error output logger.' }
  ],

  // Math Object
  math: [
    { label: 'Round(val, decimals)', kind: 'method', detail: 'double Math.Round(double a, int decimals)', insertText: 'Round(${1:value}, ${2:2})', documentation: 'Rounds floating-point value to specified number of fractional digits.' },
    { label: 'Max(a, b)', kind: 'method', detail: 'double Math.Max(double val1, double val2)', insertText: 'Max(${1:a}, ${2:b})', documentation: 'Returns the larger of two specified numbers.' },
    { label: 'Min(a, b)', kind: 'method', detail: 'double Math.Min(double val1, double val2)', insertText: 'Min(${1:a}, ${2:b})', documentation: 'Returns the smaller of two specified numbers.' },
    { label: 'Abs(val)', kind: 'method', detail: 'double Math.Abs(double value)', insertText: 'Abs(${1:value})', documentation: 'Returns absolute magnitude value.' },
    { label: 'Sqrt(d)', kind: 'method', detail: 'double Math.Sqrt(double d)', insertText: 'Sqrt(${1:val})', documentation: 'Returns square root of specified number.' },
    { label: 'Pow(x, y)', kind: 'method', detail: 'double Math.Pow(double x, double y)', insertText: 'Pow(${1:x}, ${2:y})', documentation: 'Calculates number raised to specified power.' }
  ],

  // Workflow $input / input Payload
  input: [
    { label: 'sequence', kind: 'property', detail: 'string $input.sequence', insertText: 'sequence', documentation: 'Incoming nucleotide or peptide sequence string.' },
    { label: 'gc', kind: 'property', detail: 'number $input.gc', insertText: 'gc', documentation: 'Precomputed Guanine-Cytosine content ratio (0.0 to 1.0).' },
    { label: 'length', kind: 'property', detail: 'number $input.length', insertText: 'length', documentation: 'Sequence length in base pairs.' },
    { label: 'quality', kind: 'property', detail: 'number $input.quality', insertText: 'quality', documentation: 'Mean Phred sequencing quality score.' },
    { label: 'score', kind: 'property', detail: 'number $input.score', insertText: 'score', documentation: 'Calculated algorithmic alignment or confidence score.' },
    { label: 'metadata', kind: 'property', detail: 'object $input.metadata', insertText: 'metadata', documentation: 'Genomic locus metadata, chromosome, and annotations.' }
  ],
  $input: [], // will alias to input

  // Configured Component Public Properties
  properties: [
    { label: 'Threshold', kind: 'property', detail: 'double properties.Threshold', insertText: 'Threshold', documentation: 'Configured numeric threshold for filter or branch gate.' },
    { label: 'StrictQualityGate', kind: 'property', detail: 'bool properties.StrictQualityGate', insertText: 'StrictQualityGate', documentation: 'Flag requiring strict validation before advancing flow.' },
    { label: 'MaxAllowedMutations', kind: 'property', detail: 'int properties.MaxAllowedMutations', insertText: 'MaxAllowedMutations', documentation: 'Maximum tolerable mutation count.' },
    { label: 'TransitionTimeoutMs', kind: 'property', detail: 'int properties.TransitionTimeoutMs', insertText: 'TransitionTimeoutMs', documentation: 'Timeout duration in milliseconds.' },
    { label: 'FilterMode', kind: 'property', detail: 'string properties.FilterMode', insertText: 'FilterMode', documentation: 'Active biological filter mode setting.' }
  ],
  props: [], // will alias to properties

  // State Machine Behaviour API
  statemachine: [
    { label: 'SetVariable(name, val)', kind: 'method', detail: 'void StateMachine.SetVariable(string name, object val)', insertText: 'SetVariable("${1:varName}", ${2:value})', documentation: 'Updates a state machine parameter variable dynamically.' },
    { label: 'GetVariable(name)', kind: 'method', detail: 'object StateMachine.GetVariable(string name)', insertText: 'GetVariable("${1:varName}")', documentation: 'Reads the current value of an FSM variable.' },
    { label: 'PlayState(stateName)', kind: 'method', detail: 'void StateMachine.PlayState(string stateName)', insertText: 'PlayState("${1:StateName}")', documentation: 'Transitions the active layer directly into the designated state.' },
    { label: 'ActiveState', kind: 'property', detail: 'string StateMachine.ActiveState', insertText: 'ActiveState', documentation: 'Identifier of the current executing state in the active layer.' },
    { label: 'ActiveLayer', kind: 'property', detail: 'string StateMachine.ActiveLayer', insertText: 'ActiveLayer', documentation: 'Identifier of the active hierarchical state machine layer.' }
  ],
  fsm: [] // will alias to statemachine
};

// Aliases
OBJECT_MEMBERS.dnasequence = OBJECT_MEMBERS.dna;
OBJECT_MEMBERS.seq = OBJECT_MEMBERS.dna;
OBJECT_MEMBERS.$input = OBJECT_MEMBERS.input;
OBJECT_MEMBERS.props = OBJECT_MEMBERS.properties;
OBJECT_MEMBERS.fsm = OBJECT_MEMBERS.statemachine;

// --------------------------------------------------------------------------
// GENERAL COMPLETIONS (C#, Python, JavaScript, Bash)
// --------------------------------------------------------------------------
export const CSHARP_COMPLETIONS: CompletionItem[] = [
  { label: 'DnaSequence', kind: 'class', detail: 'DNAPlatform.Core.DnaSequence', insertText: 'new DnaSequence("${1:ATGCGATCG}")', documentation: 'High-performance biological nucleotide sequence container.' },
  { label: 'NeedlemanWunschAligner.Align', kind: 'method', detail: 'NeedlemanWunschAligner.Align(seq1, seq2)', insertText: 'NeedlemanWunschAligner.Align("${1:seq1}", "${2:seq2}")', documentation: 'Dynamic programming global pairwise sequence alignment.' },
  { label: 'Console.WriteLine', kind: 'method', detail: 'void Console.WriteLine(object value)', insertText: 'Console.WriteLine(${1:value});', documentation: 'Writes text to stdout.' },
  { label: 'DateTime.UtcNow', kind: 'property', detail: 'DateTime DateTime.UtcNow', insertText: 'DateTime.UtcNow', documentation: 'Gets Coordinated Universal Time (UTC).' },
  { label: '[Tool]', kind: 'attribute', detail: '[Tool("name", "description")]', insertText: '[Tool("${1:tool_name}", "${2:Description}")]', documentation: 'Exposes method as tool for autonomous AI agents and MCP servers.' },
  { label: '[Category]', kind: 'attribute', detail: '[Category("category")]', insertText: '[Category("${1:State Machine}")]', documentation: 'Specifies palette category: State Machine, Shell Tools, Modules, MCP Server, Agent as MCP.' },
  { label: 'prop_double (Logic Component)', kind: 'snippet', detail: '[Description("...")] public double Property', insertText: '[Description("${1:Description}")]\npublic double ${2:Threshold} { get; set; } = ${3:0.75};', documentation: 'Creates a public double property that auto-renders as a slider in the inspector.' },
  { label: 'prop_bool (Logic Component)', kind: 'snippet', detail: '[Description("...")] public bool Property', insertText: '[Description("${1:Description}")]\npublic bool ${2:IsEnabled} { get; set; } = ${3:true};', documentation: 'Creates a public boolean property that auto-renders as an interactive toggle.' },
  { label: 'prop_int (Logic Component)', kind: 'snippet', detail: '[Description("...")] public int Property', insertText: '[Description("${1:Description}")]\npublic int ${2:MaxCount} { get; set; } = ${3:10};', documentation: 'Creates a public integer property that auto-renders as a number picker.' }
];

export const PYTHON_COMPLETIONS: CompletionItem[] = [
  { label: 'len(seq)', kind: 'method', detail: 'int len(obj)', insertText: 'len(${1:seq})', documentation: 'Returns length of biological sequence or list.' },
  { label: 'print(value)', kind: 'method', detail: 'print(*values)', insertText: 'print(${1:value})', documentation: 'Prints formatted output to stdout.' },
  { label: 'csnakes.reverse_complement', kind: 'method', detail: 'csnakes.reverse_complement(seq)', insertText: 'csnakes.reverse_complement("${1:seq}")', documentation: 'Zero-overhead C# interop reverse complement via CSnakes.' },
  { label: 'csnakes.calculate_gc', kind: 'method', detail: 'csnakes.calculate_gc(seq)', insertText: 'csnakes.calculate_gc("${1:seq}")', documentation: 'Zero-overhead C# interop GC fraction calculation.' },
  { label: 'json.dumps(obj)', kind: 'method', detail: 'json.dumps(obj, indent=2)', insertText: 'json.dumps(${1:obj}, indent=2)', documentation: 'Serializes Python dictionary to JSON string.' }
];

export const JS_COMPLETIONS: CompletionItem[] = [
  { label: 'console.log', kind: 'method', detail: 'console.log(...args)', insertText: 'console.log(${1:value});', documentation: 'Logs to output stream.' },
  { label: 'JSON.stringify', kind: 'method', detail: 'JSON.stringify(val, null, 2)', insertText: 'JSON.stringify(${1:obj}, null, 2)', documentation: 'Converts JavaScript object to JSON string.' },
  { label: 'JSON.parse', kind: 'method', detail: 'JSON.parse(text)', insertText: 'JSON.parse(${1:text})', documentation: 'Parses JSON string into JavaScript object.' },
  { label: 'Math.max', kind: 'method', detail: 'Math.max(...values)', insertText: 'Math.max(${1:a}, ${2:b})', documentation: 'Returns highest number among arguments.' },
  { label: 'Math.min', kind: 'method', detail: 'Math.min(...values)', insertText: 'Math.min(${1:a}, ${2:b})', documentation: 'Returns lowest number among arguments.' }
];

export const BASH_COMPLETIONS: CompletionItem[] = [
  { label: 'echo "message"', kind: 'snippet', detail: 'echo output', insertText: 'echo "${1:message}"', documentation: 'Prints string to terminal.' },
  { label: 'grep -E "pattern"', kind: 'snippet', detail: 'grep regex match', insertText: 'grep -E "${1:pattern}" ${2:file}', documentation: 'Searches for pattern matching lines.' },
  { label: 'awk \'{print $1}\'', kind: 'snippet', detail: 'awk column extraction', insertText: 'awk \'{print $${1:1}}\' ${2:file}', documentation: 'Processes text line-by-line.' }
];

/**
 * Retrieves autocomplete items based on language, cursor position, and token context (including dot notation)
 */
export function getCompletions(code: string, cursorPos: number, language: string = 'csharp'): CompletionItem[] {
  const line = code.slice(0, cursorPos).split('\n').pop() || '';
  const lang = (language || 'csharp').toLowerCase();

  // 1. Check for Dot Notation: e.g. "dna.", "Console.", "$input.", "properties."
  const dotMatch = line.match(/([$A-Za-z0-9_]+)\.([A-Za-z0-9_]*)$/);
  if (dotMatch) {
    const objectName = dotMatch[1].toLowerCase();
    const memberPrefix = dotMatch[2].toLowerCase();

    const members = OBJECT_MEMBERS[objectName] || [];
    if (members.length > 0) {
      if (!memberPrefix) return members;
      return members.filter(m => m.label.toLowerCase().includes(memberPrefix));
    }
  }

  // 2. Language-Specific General Completions
  const tokenMatch = line.match(/([$A-Za-z0-9_]+)$/);
  const token = tokenMatch ? tokenMatch[1].toLowerCase() : '';

  let pool: CompletionItem[] = CSHARP_COMPLETIONS;
  if (lang.includes('python') || lang.includes('py')) {
    pool = PYTHON_COMPLETIONS;
  } else if (lang.includes('javascript') || lang.includes('node') || lang.includes('js')) {
    pool = JS_COMPLETIONS;
  } else if (lang.includes('bash') || lang.includes('sh')) {
    pool = BASH_COMPLETIONS;
  }

  if (!token) return pool.slice(0, 8);

  return pool.filter(
    item =>
      item.label.toLowerCase().includes(token) ||
      item.detail.toLowerCase().includes(token)
  );
}

/**
 * Calculates ghost text suggestion for inline Tab completion
 */
export function getGhostText(code: string, cursorPos: number, language: string = 'csharp'): { prefix: string; suggestion: string; suffix: string } | null {
  const line = code.slice(0, cursorPos).split('\n').pop() || '';
  const tokenMatch = line.match(/([$A-Za-z0-9_.]+)$/);
  if (!tokenMatch) return null;

  const currentToken = tokenMatch[1];
  if (currentToken.length < 2) return null;

  const completions = getCompletions(code, cursorPos, language);
  if (completions.length === 0) return null;

  const topMatch = completions[0];
  const label = topMatch.label.replace(/\(.*\)/, '()');

  // If token is dot notation, match suffix against label
  if (currentToken.includes('.')) {
    const parts = currentToken.split('.');
    const typedMember = parts[parts.length - 1];
    if (label.toLowerCase().startsWith(typedMember.toLowerCase())) {
      const suffix = label.slice(typedMember.length);
      if (suffix) {
        return { prefix: currentToken, suggestion: label, suffix };
      }
    }
  } else {
    if (label.toLowerCase().startsWith(currentToken.toLowerCase())) {
      const suffix = label.slice(currentToken.length);
      if (suffix) {
        return { prefix: currentToken, suggestion: label, suffix };
      }
    }
  }

  return null;
}

export function validateCSharpSyntax(code: string): DiagnosticIssue[] {
  const issues: DiagnosticIssue[] = [];
  const lines = code.split('\n');

  let openBraces = 0;
  lines.forEach((lineText, idx) => {
    const lineNum = idx + 1;
    const trimmed = lineText.trim();

    const opens = (lineText.match(/\{/g) || []).length;
    const closes = (lineText.match(/\}/g) || []).length;
    openBraces += opens - closes;

    const quotes = (lineText.match(/"/g) || []).length;
    if (quotes % 2 !== 0 && !lineText.includes('"""')) {
      issues.push({
        line: lineNum,
        message: 'Unterminated string literal.',
        severity: 'error'
      });
    }

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
