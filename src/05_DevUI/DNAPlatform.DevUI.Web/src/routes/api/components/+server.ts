import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { CSharpEvaluator } from '$lib/server/csharpEvaluator';

// In-memory / persistent smart component catalog
const defaultSmartComponents = [
  {
    id: 'comp-state-machine-gate',
    name: 'DNA Phase Gate State Machine',
    category: 'State Machine',
    language: 'csharp',
    description: 'Autonomous validation state machine transitioning through SequenceVerification, Alignment, and MutationGate states.',
    properties: [
      { name: 'StrictQualityGate', type: 'boolean', default: true, description: 'Block execution if sequence quality is below threshold' },
      { name: 'MaxAllowedMutations', type: 'number', default: 5, min: 0, max: 20, step: 1, description: 'Maximum tolerable single nucleotide polymorphisms' },
      { name: 'TransitionTimeoutMs', type: 'number', default: 3000, min: 500, max: 10000, step: 250, description: 'Maximum state transition wait duration' }
    ],
    code: `using System;
using System.ComponentModel;

[Category("State Machine")]
[Description("DNA Phase Gate State Machine for validating genomic transitions")]
public class DnaPhaseGateComponent
{
    [Description("Block execution if sequence quality is below threshold")]
    public bool StrictQualityGate { get; set; } = true;

    [Description("Maximum tolerable single nucleotide polymorphisms")]
    public int MaxAllowedMutations { get; set; } = 5;

    [Description("Maximum state transition wait duration")]
    public int TransitionTimeoutMs { get; set; } = 3000;

    [Tool("evaluate_state", "Evaluates DAG transition invariants")]
    public bool EvaluateState(string sequence)
    {
        var dna = new DnaSequence(sequence);
        Console.WriteLine($"[State Machine] Evaluated sequence length: {dna.Length}, GC: {dna.CalculateGc():P2}");
        return dna.CalculateGc() >= 0.40 && dna.CalculateGc() <= 0.65;
    }
}`,
    expression: '$input.gc >= 0.40 && $input.gc <= 0.65',
    createdAt: new Date().toISOString()
  },
  {
    id: 'comp-shell-trimmer',
    name: 'FASTQ Trimmer (Shell Tool)',
    category: 'Shell Tools',
    language: 'bash',
    description: 'High-throughput adapter and quality trimmer command wrapper for raw sequencing reads.',
    properties: [
      { name: 'QualityCutoff', type: 'number', default: 28, min: 10, max: 40, step: 1, description: 'Phred quality score threshold' },
      { name: 'MinReadLength', type: 'number', default: 36, min: 15, max: 150, step: 5, description: 'Minimum post-trimming read length' },
      { name: 'ThreadCount', type: 'number', default: 4, min: 1, max: 32, step: 1, description: 'Parallel processing threads' }
    ],
    code: `#!/usr/bin/env bash
# FASTQ Adapter & Quality Trimmer
QUALITY_CUTOFF=28
MIN_LENGTH=36
THREADS=4

echo "[Shell Tool] Executing fastp quality trimming..."
echo "[Shell Tool] Parameters: -q $QUALITY_CUTOFF -l $MIN_LENGTH -w $THREADS"
echo "[Shell Tool] Reads processed: 1,420,000 | Surviving: 98.4%"`,
    expression: '$input.readLength >= 36',
    createdAt: new Date().toISOString()
  },
  {
    id: 'comp-module-skew',
    name: 'GC Skew & Oligo Density Calculator',
    category: 'Modules',
    language: 'python',
    description: 'Calculates replication origin asymmetry and nucleotide frequency metrics via CSnakes bridge.',
    properties: [
      { name: 'WindowSize', type: 'number', default: 100, min: 20, max: 1000, step: 20, description: 'Sliding window length in base pairs' },
      { name: 'StepSize', type: 'number', default: 10, min: 5, max: 100, step: 5, description: 'Window shift increment' },
      { name: 'NormalizeResult', type: 'boolean', default: true, description: 'Normalize skew values between -1.0 and +1.0' }
    ],
    code: `def calculate_gc_skew(sequence: str, window_size: int = 100, step: int = 10):
    seq = sequence.upper()
    skews = []
    for i in range(0, len(seq) - window_size + 1, step):
        w = seq[i:i + window_size]
        g = w.count('G')
        c = w.count('C')
        skew = (g - c) / (g + c) if (g + c) > 0 else 0
        skews.append(skew)
    avg_skew = sum(skews) / len(skews) if skews else 0
    print(f"[Module] Analyzed {len(skews)} windows. Average GC Skew: {avg_skew:.4f}")
    return avg_skew

calculate_gc_skew("ATGCTAGCTAGCTAGCTA" * 10)`,
    expression: '$input.gcSkew >= -0.2 && $input.gcSkew <= 0.2',
    createdAt: new Date().toISOString()
  },
  {
    id: 'comp-mcp-pdb',
    name: 'PDB Structural Coordinates Server',
    category: 'MCP Server',
    language: 'csharp',
    description: 'Local MCP tool provider fetching macromolecular 3D coordinate matrices from RCSB Protein Data Bank.',
    properties: [
      { name: 'MaxResolutionAngstrom', type: 'number', default: 2.5, min: 0.5, max: 5.0, step: 0.1, description: 'Maximum experimental resolution in Angstroms' },
      { name: 'IncludeLigands', type: 'boolean', default: true, description: 'Extract bound co-factors and small molecules' },
      { name: 'PreferredMethod', type: 'string', default: 'X-Ray Diffraction', description: 'Crystallography or Cryo-EM filter' }
    ],
    code: `using System;
using System.ComponentModel;

[Category("MCP Server")]
[Description("PDB Structural Coordinates MCP Server")]
public class PdbCoordinateMcpServer
{
    [Description("Maximum experimental resolution in Angstroms")]
    public double MaxResolutionAngstrom { get; set; } = 2.5;

    [Description("Extract bound co-factors and small molecules")]
    public bool IncludeLigands { get; set; } = true;

    [Tool("fetch_structure", "Fetches 3D atomic coordinates by 4-letter PDB code")]
    public void FetchStructure(string pdbId)
    {
        Console.WriteLine($"[MCP Server] Connecting to RCSB PDB for entry '{pdbId}'...");
        Console.WriteLine($"[MCP Server] Filter: Resolution <= {MaxResolutionAngstrom}A | Ligands: {IncludeLigands}");
        Console.WriteLine($"[MCP Server] Entry 1A3N: Hemoglobin alpha chain (Homo sapiens) fetched.");
    }
}`,
    expression: '$input.resolution <= 2.5',
    createdAt: new Date().toISOString()
  },
  {
    id: 'comp-agent-mcp-reviewer',
    name: 'Genomic Variant Co-Scientist (Agent as MCP)',
    category: 'Agent as MCP',
    language: 'csharp',
    description: 'Microsoft Agent Framework function tool wrapping an autonomous co-scientist agent for variant triage.',
    properties: [
      { name: 'ClinicalPathogenicityThreshold', type: 'number', default: 0.85, min: 0.1, max: 1.0, step: 0.05, description: 'Confidence threshold for flagging pathogenic variants' },
      { name: 'RequireHumanAuthorization', type: 'boolean', default: true, description: 'Trigger AG-UI APPROVAL_REQUESTED interrupt before reporting' },
      { name: 'ModelProvider', type: 'string', default: 'Microsoft-Copilot-Local-ONNX', description: 'Inference runtime engine' }
    ],
    code: `using System;
using System.ComponentModel;

[Category("Agent as MCP")]
[Description("Agent-as-a-Function Tool wrapper adhering to Microsoft Agent Framework Provider Matrix")]
public class VariantTriageAgentTool
{
    [Description("Confidence threshold for flagging pathogenic variants")]
    public double ClinicalPathogenicityThreshold { get; set; } = 0.85;

    [Description("Trigger AG-UI APPROVAL_REQUESTED interrupt before reporting")]
    public bool RequireHumanAuthorization { get; set; } = true;

    [Tool("triage_variant", "Autonomous co-scientist evaluates clinical severity of nucleotide variant")]
    public void TriageVariant(string gene, string mutation)
    {
        Console.WriteLine($"[Agent as MCP] Co-Scientist reviewing {gene} {mutation}...");
        Console.WriteLine($"[Agent as MCP] Clinical Pathogenicity Score: 0.94 (HIGH)");
        Console.WriteLine($"[Agent as MCP] Human-in-the-Loop Interrupt Dispatched: Approval required.");
    }
}`,
    expression: '$input.pathogenicityScore >= 0.85',
    createdAt: new Date().toISOString()
  }
];

let customComponents: any[] = [...defaultSmartComponents];

// GET /api/components — List all smart components
export const GET: RequestHandler = async () => {
  return json({
    components: customComponents,
    categories: ['State Machine', 'Shell Tools', 'Modules', 'MCP Server', 'Agent as MCP'],
    total: customComponents.length
  });
};

// POST /api/components — Author / Save new smart component
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const code = String(body.code || '').trim();
    const language = String(body.language || 'csharp').toLowerCase();

    // Auto-reflect properties if C# code
    let reflectedProperties = body.properties || [];
    let reflectedCategory = body.category || 'State Machine';
    let reflectedDescription = body.description || '';

    if (language === 'csharp' || language === 'c#') {
      const reflected = CSharpEvaluator.reflectComponent(code);
      if (reflected.properties.length > 0 && (!body.properties || body.properties.length === 0)) {
        reflectedProperties = reflected.properties;
      }
      if (reflected.category && !body.category) {
        reflectedCategory = reflected.category;
      }
      if (reflected.description && !body.description) {
        reflectedDescription = reflected.description;
      }
    }

    const newComponent = {
      id: body.id || `comp_${Date.now()}`,
      name: body.name || 'Custom Smart Component',
      category: reflectedCategory,
      language,
      description: reflectedDescription || 'Custom authored smart component.',
      properties: reflectedProperties,
      code,
      expression: body.expression || '$input.value > 0',
      createdAt: new Date().toISOString()
    };

    const existingIndex = customComponents.findIndex(c => c.id === newComponent.id);
    if (existingIndex >= 0) {
      customComponents[existingIndex] = newComponent;
    } else {
      customComponents.unshift(newComponent);
    }

    return json({
      success: true,
      component: newComponent
    });
  } catch (err: any) {
    return json({ success: false, error: err.message }, { status: 400 });
  }
};
