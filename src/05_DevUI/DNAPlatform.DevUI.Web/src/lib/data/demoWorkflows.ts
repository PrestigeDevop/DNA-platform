export interface DemoWorkflow {
  id: string;
  name: string;
  description: string;
  status: 'Published' | 'Draft';
  version: number;
  nodes: any[];
  connections: any[];
  publishedVersions?: number;
  createdAt?: string;
  updatedAt?: string;
}

export const DEMO_WORKFLOWS: DemoWorkflow[] = [
  {
    id: 'wf-crispr-synthesis',
    name: 'De Novo CRISPR-Cas9 Target Optimization & Synthesis Assembly',
    description: 'Autonomous end-to-end gene editing pipeline: Cas9 guide RNA candidate selection, thermodynamic stability (Tm/GC), CSnakes off-target evaluation, AG-UI Human-in-the-Loop operator sign-off, and Golden Gate oligo assembly generation.',
    status: 'Published',
    version: 1,
    publishedVersions: 1,
    nodes: [
      {
        id: 'n_target_seq',
        type: 'Input',
        name: 'Target Gene Sequence (EMX1 Exon 1)',
        x: 60,
        y: 140,
        config: {
          value: 'GAGTCCGAGCAGAAGAAGAAGGGCTCCCATCACATCAACCGGTGGCGCATTGCCACGAAGCAGGCCAATGGGGAGGACATCGATGTCACCTCCAATGACTAGGGTGGGCAACCACAAACCCACGAGGGCAGAGTGCTGCTTGCTGCTGGCCAGGCCCCTGCGTGGGCCCAAGCTGGACTCTGGCCACCTCAGAACAATACCCTCAGTCCTCCCCAGACCTCCTCCTGCCCGAGACCTCCTCCCTGCCCGAGACCTCCTTCCTGCCCGAGACCTCCTTCCTGC',
          key: 'sequence',
          entityType: 'DNA_Sequence',
          properties: { organism: 'Homo sapiens', locus: 'EMX1', strand: '+', targetExon: 'Exon 1' }
        }
      },
      {
        id: 'n_crispr_agent',
        type: 'Agent',
        name: 'Cas9 gRNA Candidate Designer',
        x: 340,
        y: 140,
        config: {
          agentId: 'agent-bio-scientist',
          model: 'Microsoft-Copilot-Local-ONNX',
          prompt: 'Scan target DNA sequence for 20-nt CRISPR guide RNA spacers immediately preceding NGG PAM motifs: {{input}}. Prioritize spacers with 40-60% GC content and no poly-T tracts.',
          tools: ['dna-bioinformatics/gc_skew_analysis', 'sequence-aligner'],
          properties: { pamMotif: 'NGG', spacerLength: 20, maxPolyT: 4 }
        }
      },
      {
        id: 'n_thermo_qc',
        type: 'ProcessingSkill',
        name: 'Thermodynamic Stability & GC Analyzer',
        x: 620,
        y: 140,
        config: {
          skillId: 'validation',
          properties: { targetGcMin: 45.0, targetGcMax: 65.0, targetTmMin: 58.0, targetTmMax: 72.0, saltConcentrationM: 0.05 }
        }
      },
      {
        id: 'n_csnakes_offtarget',
        type: 'LogicComponent',
        name: 'CSnakes Off-Target Risk Evaluator',
        x: 900,
        y: 140,
        config: {
          hasExpression: true,
          expressionLang: 'javascript',
          expression: 'const spacer = inputs.selectedSpacer || "GAGTCCGAGCAGAAGAAGAA";\nconst gc = inputs.gcPercentage || 55.0;\nconst tm = 64.2;\nconst offTargetScore = 0.012;\nconst isSafe = offTargetScore < 0.05;\nConsole.log(`[CSnakes BioPython] Evaluated off-target penalty for ${spacer}: score=${offTargetScore} (Safe=${isSafe})`);\nreturn { spacer, gc, tm, offTargetScore, isSafe, approvalRequired: true };',
          inputs: [
            { name: 'selectedSpacer', type: 'String' },
            { name: 'gcPercentage', type: 'Float' }
          ],
          outputs: [
            { name: 'isSafe', type: 'Boolean' },
            { name: 'offTargetScore', type: 'Float' },
            { name: 'approvalRequired', type: 'Boolean' }
          ],
          properties: { maxAllowableRisk: 0.05, referenceGenome: 'GRCh38.p14' }
        }
      },
      {
        id: 'n_hitl_approval',
        type: 'StateMachine',
        name: 'AG-UI Human-in-the-Loop Safety Gate',
        x: 1180,
        y: 140,
        config: {
          activeLayer: 'Safety Layer',
          currentState: 'Verification_Requested',
          targetState: 'Operator_Approved',
          condition: 'isSafe == true && operatorSigned == true',
          properties: {
            interruptProtocol: 'AG_UI_HITL',
            requiresOperatorSignoff: true,
            safetyThreshold: 'BLAS_Level_1'
          }
        }
      },
      {
        id: 'n_oligo_assembly',
        type: 'ProcessingSkill',
        name: 'Golden Gate Oligo Assembly Designer',
        x: 1460,
        y: 140,
        config: {
          skillId: 'data-transform',
          properties: {
            overhang5Prime: 'CACC',
            overhang3Prime: 'AAAC',
            restrictionEnzyme: 'BsaI',
            synthesisLength: 24
          }
        }
      },
      {
        id: 'n_fs_export',
        type: 'WorkspaceFileSystem',
        name: 'Workspace GenBank & FASTA Exporter',
        x: 1740,
        y: 140,
        config: {
          operation: 'write_file',
          filePath: 'crispr_emx1_oligos.fasta',
          properties: { targetDir: 'workspace', format: 'FASTA' }
        }
      },
      {
        id: 'n_lims_out',
        type: 'Output',
        name: 'LIMS Production Synthesis Specification',
        x: 2020,
        y: 140,
        config: {
          prefix: '>> DE NOVO SYNTHESIS SPEC READY FOR LIMS SUBMISSION:'
        }
      }
    ],
    connections: [
      { id: 'c1', from: 'n_target_seq', to: 'n_crispr_agent', sourceNodeId: 'n_target_seq', targetNodeId: 'n_crispr_agent' },
      { id: 'c2', from: 'n_crispr_agent', to: 'n_thermo_qc', sourceNodeId: 'n_crispr_agent', targetNodeId: 'n_thermo_qc' },
      { id: 'c3', from: 'n_thermo_qc', to: 'n_csnakes_offtarget', sourceNodeId: 'n_thermo_qc', targetNodeId: 'n_csnakes_offtarget' },
      { id: 'c4', from: 'n_csnakes_offtarget', to: 'n_hitl_approval', sourceNodeId: 'n_csnakes_offtarget', targetNodeId: 'n_hitl_approval' },
      { id: 'c5', from: 'n_hitl_approval', to: 'n_oligo_assembly', sourceNodeId: 'n_hitl_approval', targetNodeId: 'n_oligo_assembly' },
      { id: 'c6', from: 'n_oligo_assembly', to: 'n_fs_export', sourceNodeId: 'n_oligo_assembly', targetNodeId: 'n_fs_export' },
      { id: 'c7', from: 'n_fs_export', to: 'n_lims_out', sourceNodeId: 'n_fs_export', targetNodeId: 'n_lims_out' }
    ]
  },
  {
    id: 'wf-comprehensive-bio',
    name: 'Genomic Variant Pipeline (Full ECS Demo)',
    description: 'End-to-end pipeline connecting Agent reasoning, Pairwise Alignment, Workspace File System, State Machine QC gates, and CSnakes Logic Component.',
    status: 'Published',
    version: 1,
    publishedVersions: 1,
    nodes: [
      {
        id: 'n_input',
        type: 'Input',
        name: 'Query FASTA Sequence',
        x: 60,
        y: 120,
        config: {
          value: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCCCTGTGGGGCAAGGTG',
          key: 'sequence',
          entityType: 'DNA_Sequence',
          properties: { organism: 'Homo sapiens', locus: 'HBB' }
        }
      },
      {
        id: 'n_agent',
        type: 'Agent',
        name: 'Bioinformatics Scientist',
        x: 320,
        y: 120,
        config: {
          agentId: 'agent-bio-scientist',
          model: 'Microsoft-Copilot-Local-ONNX',
          prompt: 'Verify codon frame and analyze nucleotide sequence: {{input}}',
          tools: ['csharp-runtime/get_current_time', 'sequence-aligner'],
          properties: { verified: true }
        }
      },
      {
        id: 'n_align',
        type: 'ProcessingSkill',
        name: 'Needleman-Wunsch Aligner',
        x: 580,
        y: 120,
        config: {
          skillId: 'sequence-aligner',
          sequence2: 'ATGGTGCACGTGACTCCTGTTGAGAAGTCTGCC',
          properties: { matchScore: 2, mismatchPenalty: -1 }
        }
      },
      {
        id: 'n_fs',
        type: 'WorkspaceFileSystem',
        name: 'Workspace Report Writer',
        x: 840,
        y: 120,
        config: {
          operation: 'write_file',
          filePath: 'alignment_report.txt',
          properties: { targetDir: 'workspace' }
        }
      },
      {
        id: 'n_fsm',
        type: 'StateMachine',
        name: 'Quality Gate State Machine',
        x: 1100,
        y: 120,
        config: {
          activeLayer: 'Base Layer',
          currentState: 'Analyzing',
          targetState: 'Verified',
          condition: 'allowFlow == true',
          properties: { autoAdvance: true }
        }
      },
      {
        id: 'n_logic',
        type: 'LogicComponent',
        name: 'CSnakes Multi-I/O Evaluator',
        x: 1360,
        y: 120,
        config: {
          hasExpression: true,
          expressionLang: 'javascript',
          expression: 'Console.log(Time.now + " " + this.name + " evaluated QC gates");\nthis.properties.verified = true;\nreturn { allowFlow: true, status: "PASSED", timestamp: Time.now };',
          inputs: [
            { name: 'alignmentReport', type: 'String' },
            { name: 'score', type: 'Float' }
          ],
          outputs: [
            { name: 'qcStatus', type: 'JSON' }
          ],
          properties: { verified: false, scoreThreshold: 0.85 }
        }
      },
      {
        id: 'n_out',
        type: 'Output',
        name: 'Console Pipeline Output',
        x: 1620,
        y: 120,
        config: {
          prefix: '>> PIPELINE VERIFIED OUTPUT:'
        }
      }
    ],
    connections: [
      { id: 'c1', from: 'n_input', to: 'n_agent', sourceNodeId: 'n_input', targetNodeId: 'n_agent' },
      { id: 'c2', from: 'n_agent', to: 'n_align', sourceNodeId: 'n_agent', targetNodeId: 'n_align' },
      { id: 'c3', from: 'n_align', to: 'n_fs', sourceNodeId: 'n_align', targetNodeId: 'n_fs' },
      { id: 'c4', from: 'n_fs', to: 'n_fsm', sourceNodeId: 'n_fs', targetNodeId: 'n_fsm' },
      { id: 'c5', from: 'n_fsm', to: 'n_logic', sourceNodeId: 'n_fsm', targetNodeId: 'n_logic' },
      { id: 'c6', from: 'n_logic', to: 'n_out', sourceNodeId: 'n_logic', targetNodeId: 'n_out' }
    ]
  },
  {
    id: 'wf-mcp-heterogeneous',
    name: 'Heterogeneous Polyglot MCP Toolchain (mise + Roslyn C# + CSnakes Python)',
    description: 'Multi-runtime orchestrator validating local mise toolchains, compiling in-process Roslyn C# tools with [Tool] attribute reflection, and executing Python BioPython sequence translation.',
    status: 'Published',
    version: 1,
    publishedVersions: 1,
    nodes: [
      {
        id: 'n_mise_env',
        type: 'ProcessingSkill',
        name: 'mise Toolchain Validator',
        x: 60,
        y: 140,
        config: {
          skillId: 'validation',
          properties: { runtimes: ['python 3.12', 'dotnet 8.0', 'node 20'] }
        }
      },
      {
        id: 'n_roslyn_eval',
        type: 'LogicComponent',
        name: 'Roslyn In-Process C# Compiler',
        x: 340,
        y: 140,
        config: {
          hasExpression: true,
          expressionLang: 'javascript',
          expression: 'Console.log("Roslyn compiler evaluated [Tool] attributes on DnaMetricsService.cs");\nreturn { status: "COMPILED", exportedTools: ["CalculateGcContent", "PredictMeltingTemperature", "DetectRestrictionCutSites"] };',
          properties: { framework: 'net8.0', optimization: 'Release' }
        }
      },
      {
        id: 'n_csnakes_biopython',
        type: 'LogicComponent',
        name: 'CSnakes Zero-Overhead Python Bridge',
        x: 620,
        y: 140,
        config: {
          hasExpression: true,
          expressionLang: 'javascript',
          expression: 'Console.log("CSnakes in-process execution via Python C-API");\nreturn { biopythonLoaded: true, translation: "MVHLTPEEKSAVTALWGKV", molecularWeightDa: 2187.5 };',
          properties: { module: 'Bio.Seq', zeroCopyBuffers: true }
        }
      },
      {
        id: 'n_copilot_audit',
        type: 'Agent',
        name: 'Microsoft Copilot ONNX Reasoning',
        x: 900,
        y: 140,
        config: {
          agentId: 'agent-data-analyst',
          model: 'Microsoft-Copilot-Local-ONNX',
          prompt: 'Audit heterogeneous multi-runtime tool outputs: {{input}} and generate verification certificate.',
          properties: { auditLevel: 'Strict' }
        }
      },
      {
        id: 'n_report_out',
        type: 'Output',
        name: 'Heterogeneous Toolchain Certified Output',
        x: 1180,
        y: 140,
        config: {
          prefix: '>> HETEROGENEOUS TOOLCHAIN AUDIT PASSED:'
        }
      }
    ],
    connections: [
      { id: 'c1', from: 'n_mise_env', to: 'n_roslyn_eval', sourceNodeId: 'n_mise_env', targetNodeId: 'n_roslyn_eval' },
      { id: 'c2', from: 'n_roslyn_eval', to: 'n_csnakes_biopython', sourceNodeId: 'n_roslyn_eval', targetNodeId: 'n_csnakes_biopython' },
      { id: 'c3', from: 'n_csnakes_biopython', to: 'n_copilot_audit', sourceNodeId: 'n_csnakes_biopython', targetNodeId: 'n_copilot_audit' },
      { id: 'c4', from: 'n_copilot_audit', to: 'n_report_out', sourceNodeId: 'n_copilot_audit', targetNodeId: 'n_report_out' }
    ]
  }
];
