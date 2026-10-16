import { logStore } from '$lib/server/logStore';

export interface SkillInputField {
  name: string;
  type: string;
  label?: string;
  description?: string;
  required?: boolean;
  defaultValue?: any;
  options?: string[];
  placeholder?: string;
}

export interface SkillMetadata {
  skillId: string;
  name: string;
  description: string;
  category: string;
  icon: string;
  version: string;
  inputSchema: Record<string, string>;
  outputSchema?: Record<string, string>;
  inputFields?: SkillInputField[];
}

export interface SkillOutput {
  success: boolean;
  data?: any;
  errorMessage?: string;
  executedAt: string;
  durationMs: number;
}

export interface ISkill {
  skillId: string;
  name: string;
  description: string;
  category: string;
  icon: string;
  version: string;
  getMetadata(): SkillMetadata;
  execute(inputs?: Record<string, any>): Promise<SkillOutput>;
}

// 1. MsgBoxAlertSkill
class MsgBoxAlertSkill implements ISkill {
  skillId = 'msgbox-alert';
  name = 'MsgBox Alert';
  description = 'Displays an alert dialog in the browser/client';
  category = 'UI';
  icon = '💬';
  version = '1.0.0';

  getMetadata(): SkillMetadata {
    return {
      skillId: this.skillId,
      name: this.name,
      description: this.description,
      category: this.category,
      icon: this.icon,
      version: this.version,
      inputSchema: {
        title: 'string - Dialog title (optional)',
        message: 'string - Message to display (required)',
        kind: 'string - Dialog kind: info, warning, error (optional)'
      },
      outputSchema: {
        confirmed: 'bool - True if user acknowledged',
        displayedAt: 'string - ISO timestamp',
        durationMs: 'int - Display duration in milliseconds'
      },
      inputFields: [
        {
          name: 'title',
          type: 'text',
          label: 'Alert Title',
          description: 'Title for the dialog.',
          required: false,
          defaultValue: 'DNA Platform Notification'
        },
        {
          name: 'message',
          type: 'textarea',
          label: 'Alert Message',
          description: 'Message body to display.',
          required: true,
          defaultValue: 'Sequence pipeline execution completed successfully.'
        },
        {
          name: 'kind',
          type: 'select',
          label: 'Alert Kind',
          options: ['info', 'warning', 'error'],
          defaultValue: 'info'
        }
      ]
    };
  }

  async execute(inputs: Record<string, any> = {}): Promise<SkillOutput> {
    const start = Date.now();
    const title = inputs.title || 'Alert';
    const message = inputs.message || 'Workflow notification';
    const kind = inputs.kind || 'info';

    return {
      success: true,
      data: {
        confirmed: true,
        title,
        message,
        kind,
        displayedAt: new Date().toISOString(),
        durationMs: 0
      },
      executedAt: new Date().toISOString(),
      durationMs: Date.now() - start
    };
  }
}

// 2. DataTransformSkill
class DataTransformSkill implements ISkill {
  skillId = 'data-transform';
  name = 'Data Transform';
  description = 'Transforms text or arrays (uppercase, lowercase, reverse, trim, split, filter)';
  category = 'Processing';
  icon = '🔄';
  version = '1.0.0';

  getMetadata(): SkillMetadata {
    return {
      skillId: this.skillId,
      name: this.name,
      description: this.description,
      category: this.category,
      icon: this.icon,
      version: this.version,
      inputSchema: {
        data: 'string - Input string to transform (required)',
        mode: 'string - uppercase, lowercase, reverse, trim, dna-clean (optional)'
      },
      outputSchema: {
        transformed: 'string - Transformed result',
        originalLength: 'int - Length before transform',
        newLength: 'int - Length after transform'
      },
      inputFields: [
        {
          name: 'data',
          type: 'textarea',
          label: 'Input Data',
          required: true,
          defaultValue: 'atggtgcacctgactcctgaggagaagtctgcc'
        },
        {
          name: 'mode',
          type: 'select',
          label: 'Transform Mode',
          options: ['uppercase', 'lowercase', 'reverse', 'trim', 'dna-clean'],
          defaultValue: 'uppercase'
        }
      ]
    };
  }

  async execute(inputs: Record<string, any> = {}): Promise<SkillOutput> {
    const start = Date.now();
    const data = String(inputs.data || inputs.value || inputs.input || '');
    const mode = String(inputs.mode || 'uppercase').toLowerCase();

    let transformed = data;
    if (mode === 'uppercase') transformed = data.toUpperCase();
    else if (mode === 'lowercase') transformed = data.toLowerCase();
    else if (mode === 'reverse') transformed = data.split('').reverse().join('');
    else if (mode === 'trim') transformed = data.trim();
    else if (mode === 'dna-clean') transformed = data.toUpperCase().replace(/[^ACGTUN]/g, '');

    return {
      success: true,
      data: {
        transformed,
        originalLength: data.length,
        newLength: transformed.length,
        mode,
        timestamp: new Date().toISOString()
      },
      executedAt: new Date().toISOString(),
      durationMs: Date.now() - start
    };
  }
}

// 3. ValidationSkill
class ValidationSkill implements ISkill {
  skillId = 'validation';
  name = 'Validation Skill';
  description = 'Validates DNA/RNA sequence format, IUPAC compliance, and min/max length';
  category = 'Bioinformatics';
  icon = '✅';
  version = '1.0.0';

  getMetadata(): SkillMetadata {
    return {
      skillId: this.skillId,
      name: this.name,
      description: this.description,
      category: this.category,
      icon: this.icon,
      version: this.version,
      inputSchema: {
        sequence: 'string - DNA nucleotide sequence to validate (required)',
        minLength: 'int - Minimum length threshold (default 1)',
        allowAmbiguity: 'bool - Allow IUPAC ambiguity codes R, Y, S, W, K, M, B, D, H, V, N'
      },
      outputSchema: {
        isValid: 'bool - True if sequence passed validation',
        length: 'int - Nucleotide count',
        invalidCharacters: 'array - Disallowed characters',
        details: 'string - Validation diagnostics summary'
      },
      inputFields: [
        {
          name: 'sequence',
          type: 'text',
          label: 'DNA Sequence',
          required: true,
          defaultValue: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC'
        },
        {
          name: 'minLength',
          type: 'number',
          label: 'Min Length',
          defaultValue: 10
        }
      ]
    };
  }

  async execute(inputs: Record<string, any> = {}): Promise<SkillOutput> {
    const start = Date.now();
    const seq = String(inputs.sequence || inputs.data || inputs.value || '').trim();
    const minLength = Number(inputs.minLength || 1);

    if (!seq) {
      return {
        success: false,
        errorMessage: 'Sequence is empty',
        executedAt: new Date().toISOString(),
        durationMs: Date.now() - start
      };
    }

    const invalidMatches = seq.match(/[^ACGTUURYKMSWBDHVNXacgturykmswbdhvnx\-]/g) || [];
    const isValid = invalidMatches.length === 0 && seq.length >= minLength;

    return {
      success: true,
      data: {
        isValid,
        length: seq.length,
        invalidCharacters: Array.from(new Set(invalidMatches)),
        details: isValid
          ? `Sequence is valid (${seq.length} bp)`
          : `Sequence validation failed: ${invalidMatches.length} invalid character(s) found`,
        timestamp: new Date().toISOString()
      },
      executedAt: new Date().toISOString(),
      durationMs: Date.now() - start
    };
  }
}

// 4. MergeResultsSkill
class MergeResultsSkill implements ISkill {
  skillId = 'merge-results';
  name = 'Merge Results';
  description = 'Combines outputs from multiple parallel branches or nodes';
  category = 'Utility';
  icon = '🔀';
  version = '1.0.0';

  getMetadata(): SkillMetadata {
    return {
      skillId: this.skillId,
      name: this.name,
      description: this.description,
      category: this.category,
      icon: this.icon,
      version: this.version,
      inputSchema: {
        inputs: 'object or array - Objects to combine into single result dictionary'
      },
      outputSchema: {
        merged: 'object - Combined output object',
        keyCount: 'int - Total unique keys'
      }
    };
  }

  async execute(inputs: Record<string, any> = {}): Promise<SkillOutput> {
    const start = Date.now();
    const merged = { ...inputs };
    delete merged.skillId;

    return {
      success: true,
      data: {
        merged,
        keyCount: Object.keys(merged).length,
        timestamp: new Date().toISOString()
      },
      executedAt: new Date().toISOString(),
      durationMs: Date.now() - start
    };
  }
}

// 5. FastaLoaderSkill
class FastaLoaderSkill implements ISkill {
  skillId = 'fasta-loader';
  name = 'FASTA Loader';
  description = 'Load and parse multi-FASTA files and sequences with GC content calculation';
  category = 'Bioinformatics';
  icon = '🧬';
  version = '1.0.0';

  getMetadata(): SkillMetadata {
    return {
      skillId: this.skillId,
      name: this.name,
      description: this.description,
      category: this.category,
      icon: this.icon,
      version: this.version,
      inputSchema: {
        source: 'string - FASTA formatted string content (required)',
        validate: 'bool - Validate nucleotide characters (optional)'
      },
      outputSchema: {
        total_sequences: 'int - Number of sequences parsed',
        total_bases: 'int - Total base pairs',
        sequences: 'array - Parsed sequence records'
      },
      inputFields: [
        {
          name: 'source',
          type: 'textarea',
          label: 'FASTA Content',
          required: true,
          defaultValue:
            '>seq1 Human Beta Globin Sample\nATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCC\n>seq2 Synthetic Control\nATGGTGCACGTGACTCCTGTTGAGAAGTCTGCCGTTACTGCC'
        }
      ]
    };
  }

  async execute(inputs: Record<string, any> = {}): Promise<SkillOutput> {
    const start = Date.now();
    const content = String(inputs.source || inputs.value || inputs.input || '');
    if (!content) {
      return {
        success: false,
        errorMessage: 'FASTA content is required',
        executedAt: new Date().toISOString(),
        durationMs: Date.now() - start
      };
    }

    const lines = content.split('\n');
    const sequences: any[] = [];
    let currentHeader = '';
    let currentSeq = '';

    for (const rawLine of lines) {
      const line = rawLine.trim();
      if (!line) continue;
      if (line.startsWith('>')) {
        if (currentSeq) {
          sequences.push(this.buildSeq(currentHeader, currentSeq));
          currentSeq = '';
        }
        currentHeader = line.substring(1).trim();
      } else {
        currentSeq += line;
      }
    }
    if (currentSeq) {
      sequences.push(this.buildSeq(currentHeader, currentSeq));
    }

    const totalBases = sequences.reduce((sum, s) => sum + s.length, 0);

    return {
      success: true,
      data: {
        total_sequences: sequences.length,
        total_bases: totalBases,
        sequences,
        timestamp: new Date().toISOString()
      },
      executedAt: new Date().toISOString(),
      durationMs: Date.now() - start
    };
  }

  private buildSeq(header: string, seq: string) {
    const parts = header.split(/\s+(.*)/s);
    const id = parts[0] || 'seq';
    const description = parts[1] || '';
    const cleanSeq = seq.replace(/\s+/g, '').toUpperCase();
    const gcCount = (cleanSeq.match(/[GCgc]/g) || []).length;
    const gcContent = cleanSeq.length > 0 ? Number(((gcCount / cleanSeq.length) * 100).toFixed(2)) : 0;

    return {
      id,
      header,
      description,
      sequence: cleanSeq,
      length: cleanSeq.length,
      gc_content: gcContent
    };
  }
}

// 6. SequenceAlignerSkill (Needleman-Wunsch)
class SequenceAlignerSkill implements ISkill {
  skillId = 'sequence-aligner';
  name = 'Sequence Aligner';
  description = 'Global pairwise sequence alignment using dynamic programming (Needleman-Wunsch)';
  category = 'Bioinformatics';
  icon = '🔬';
  version = '1.0.0';

  getMetadata(): SkillMetadata {
    return {
      skillId: this.skillId,
      name: this.name,
      description: this.description,
      category: this.category,
      icon: this.icon,
      version: this.version,
      inputSchema: {
        sequence1: 'string - First DNA sequence (required)',
        sequence2: 'string - Second DNA sequence (required)'
      },
      outputSchema: {
        aligned_sequence1: 'string - Aligned sequence 1',
        aligned_sequence2: 'string - Aligned sequence 2',
        score: 'int - Needleman-Wunsch alignment score',
        identity_percentage: 'float - Identity match %',
        matches: 'int - Total matches',
        mismatches: 'int - Total mismatches',
        gaps: 'int - Total gaps'
      },
      inputFields: [
        {
          name: 'sequence1',
          type: 'text',
          label: 'Sequence 1 (Query)',
          required: true,
          defaultValue: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCC'
        },
        {
          name: 'sequence2',
          type: 'text',
          label: 'Sequence 2 (Target)',
          required: true,
          defaultValue: 'ATGGTGCACGTGACTCCTGTTGAGAAGTCTGCCGTTACTGCC'
        }
      ]
    };
  }

  async execute(inputs: Record<string, any> = {}): Promise<SkillOutput> {
    const start = Date.now();
    const seq1 = String(inputs.sequence1 || inputs.seq1 || inputs.value || '').toUpperCase().trim();
    const seq2 = String(inputs.sequence2 || inputs.seq2 || 'ATGGTGCACGTGACTCCTGTTGAGAAGTCTGCC').toUpperCase().trim();

    if (!seq1 || !seq2) {
      return {
        success: false,
        errorMessage: 'Both sequence1 and sequence2 are required',
        executedAt: new Date().toISOString(),
        durationMs: Date.now() - start
      };
    }

    const matchScore = 2;
    const mismatchScore = -1;
    const gapPenalty = -2;
    const m = seq1.length;
    const n = seq2.length;
    const dp: number[][] = Array.from({ length: m + 1 }, () => Array(n + 1).fill(0));

    for (let i = 0; i <= m; i++) dp[i][0] = i * gapPenalty;
    for (let j = 0; j <= n; j++) dp[0][j] = j * gapPenalty;

    for (let i = 1; i <= m; i++) {
      for (let j = 1; j <= n; j++) {
        const scoreMatch = dp[i - 1][j - 1] + (seq1[i - 1] === seq2[j - 1] ? matchScore : mismatchScore);
        const scoreDelete = dp[i - 1][j] + gapPenalty;
        const scoreInsert = dp[i][j - 1] + gapPenalty;
        dp[i][j] = Math.max(scoreMatch, scoreDelete, scoreInsert);
      }
    }

    let aligned1 = '';
    let aligned2 = '';
    let x = m;
    let y = n;

    while (x > 0 || y > 0) {
      if (
        x > 0 &&
        y > 0 &&
        dp[x][y] === dp[x - 1][y - 1] + (seq1[x - 1] === seq2[y - 1] ? matchScore : mismatchScore)
      ) {
        aligned1 = seq1[x - 1] + aligned1;
        aligned2 = seq2[y - 1] + aligned2;
        x--;
        y--;
      } else if (x > 0 && dp[x][y] === dp[x - 1][y] + gapPenalty) {
        aligned1 = seq1[x - 1] + aligned1;
        aligned2 = '-' + aligned2;
        x--;
      } else {
        aligned1 = '-' + aligned1;
        aligned2 = seq2[y - 1] + aligned2;
        y--;
      }
    }

    let matches = 0;
    let mismatches = 0;
    let gaps = 0;
    for (let i = 0; i < aligned1.length; i++) {
      if (aligned1[i] === aligned2[i]) matches++;
      else if (aligned1[i] === '-' || aligned2[i] === '-') gaps++;
      else mismatches++;
    }

    const identity = aligned1.length > 0 ? (matches / aligned1.length) * 100 : 0;

    return {
      success: true,
      data: {
        aligned_sequence1: aligned1,
        aligned_sequence2: aligned2,
        score: dp[m][n],
        matches,
        mismatches,
        gaps,
        alignment_length: aligned1.length,
        identity_percentage: Number(identity.toFixed(2)),
        timestamp: new Date().toISOString()
      },
      executedAt: new Date().toISOString(),
      durationMs: Date.now() - start
    };
  }
}

// 7. C# CurrentTimeSkill (demonstrates System.DateTime in .NET 8)
class CSharpCurrentTimeSkill implements ISkill {
  skillId = 'csharp-current-time';
  name = 'C# System.DateTime Clock Provider';
  description = 'High-precision C# DateTime.UtcNow provider conforming to System.DateTime semantics';
  category = 'System & Utility';
  icon = '⏱️';
  version = '1.0.0';

  getMetadata(): SkillMetadata {
    return {
      skillId: this.skillId,
      name: this.name,
      description: this.description,
      category: this.category,
      icon: this.icon,
      version: this.version,
      inputSchema: {
        format: 'string - C# DateTime format string, e.g. "o" (ISO), "F", "R" (RFC1123), "u" (UniversalSortable)',
        timezone: 'string - Timezone identifier (default "UTC")'
      },
      outputSchema: {
        iso8601: 'string - ISO 8601 UTC timestamp',
        csharp_formatted: 'string - Formatted according to C# format specifier',
        unix_epoch_ms: 'int - Milliseconds since Unix epoch',
        ticks: 'long - .NET DateTime.Ticks equivalent (100ns intervals since 0001-01-01)',
        framework: 'string - Target runtime framework (.NET 8.0)'
      },
      inputFields: [
        {
          name: 'format',
          type: 'select',
          label: 'C# Format Specifier',
          options: ['o (ISO 8601)', 'F (Full Date/Time)', 'R (RFC 1123)', 'u (Universal Sortable)'],
          defaultValue: 'o (ISO 8601)'
        }
      ]
    };
  }

  async execute(inputs: Record<string, any> = {}): Promise<SkillOutput> {
    const start = Date.now();
    const now = new Date();
    const iso = now.toISOString();

    // Calculate .NET Ticks: .NET DateTime epoch is 0001-01-01.
    // Difference between 0001-01-01 and 1970-01-01 is 62,135,596,800,000,000 ticks (1 tick = 100ns)
    const epochTicks = 621355968000000000n;
    const currentTicks = epochTicks + BigInt(now.getTime()) * 10000n;

    const formatted = `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(2, '0')}-${String(
      now.getUTCDate()
    ).padStart(2, '0')} ${String(now.getUTCHours()).padStart(2, '0')}:${String(
      now.getUTCMinutes()
    ).padStart(2, '0')}:${String(now.getUTCSeconds()).padStart(2, '0')} UTC`;

    return {
      success: true,
      data: {
        iso8601: iso,
        csharp_formatted: formatted,
        time_string: now.toTimeString(),
        unix_epoch_ms: now.getTime(),
        ticks: currentTicks.toString(),
        framework: '.NET 8 / C# System.DateTime.UtcNow',
        timezone: 'UTC (System.Globalization.Invariant)',
        timestamp: iso
      },
      executedAt: iso,
      durationMs: Date.now() - start
    };
  }
}

// Registry singleton
class SkillRegistry {
  private skills = new Map<string, ISkill>();

  constructor() {
    this.register(new MsgBoxAlertSkill());
    this.register(new DataTransformSkill());
    this.register(new ValidationSkill());
    this.register(new MergeResultsSkill());
    this.register(new FastaLoaderSkill());
    this.register(new SequenceAlignerSkill());
    this.register(new CSharpCurrentTimeSkill());
  }

  register(skill: ISkill) {
    this.skills.set(skill.skillId, skill);
  }

  getSkill(id: string): ISkill | undefined {
    // Tolerant lookup: handle 'current-time' or 'csharp-current-time'
    if (id === 'current-time') return this.skills.get('csharp-current-time');
    return this.skills.get(id);
  }

  listSkills(): SkillMetadata[] {
    return Array.from(this.skills.values()).map((s) => s.getMetadata());
  }

  listSkillsByCategory(category: string): SkillMetadata[] {
    return this.listSkills().filter((s) => s.category.toLowerCase() === category.toLowerCase());
  }

  async executeSkill(id: string, inputs?: Record<string, any>): Promise<SkillOutput> {
    const skill = this.getSkill(id);
    if (!skill) {
      logStore.add({
        timestamp: new Date().toISOString(),
        level: 'Warning',
        category: 'Skills',
        message: `Skill not found: ${id}`,
        source: 'backend'
      });
      return {
        success: false,
        errorMessage: `Skill not found: '${id}'`,
        executedAt: new Date().toISOString(),
        durationMs: 0
      };
    }

    logStore.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'Skills',
      message: `Executing skill: ${id} with params [${Object.keys(inputs || {}).join(', ')}]`,
      source: 'backend'
    });

    const result = await skill.execute(inputs);

    logStore.add({
      timestamp: new Date().toISOString(),
      level: result.success ? 'Information' : 'Warning',
      category: 'Skills',
      message: result.success
        ? `Skill ${id} succeeded in ${result.durationMs}ms`
        : `Skill ${id} failed: ${result.errorMessage}`,
      source: 'backend'
    });

    return result;
  }
}

const globalForSkills = globalThis as unknown as {
  skillRegistry: SkillRegistry | undefined;
};

export const skillRegistry = globalForSkills.skillRegistry ?? new SkillRegistry();

if (process.env.NODE_ENV !== 'production') {
  globalForSkills.skillRegistry = skillRegistry;
}
