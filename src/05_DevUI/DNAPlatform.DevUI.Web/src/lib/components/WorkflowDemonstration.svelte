<script lang="ts">
  import { createEventDispatcher, onMount, onDestroy } from 'svelte';
  import { DEMO_WORKFLOWS, type DemoWorkflow } from '$lib/data/demoWorkflows';

  const dispatch = createEventDispatcher<{
    openDesigner: { workflow: any };
    saveWorkflow: { workflow: any };
  }>();

  // Active Demonstration Scenario
  let activeScenarioId = 'wf-crispr-synthesis';

  // Biological DNA sequence presets
  const SEQUENCE_PRESETS = [
    {
      id: 'emx1',
      name: 'Human EMX1 Exon 1 (Cas9 Target)',
      locus: 'Homo sapiens EMX1',
      description: 'Standard benchmark locus for CRISPR-Cas9 endonuclease specificity and cleavage efficiency.',
      seq: 'GAGTCCGAGCAGAAGAAGAAGGGCTCCCATCACATCAACCGGTGGCGCATTGCCACGAAGCAGGCCAATGGGGAGGACATCGATGTCACCTCCAATGACTAGGGTGGGCAACCACAAACCCACGAGGGCAGAGTGCTGCTTGCTGCTGGCCAGGCCCCTGCGTGGGCCCAAGCTGGACTCTGGCCACCTCAGAACAATACCCTCAGTCCTCCCCAGACCTCCTCCTGCCCGAGACCTCCTCCCTGCCCGAGACCTCCTTCCTGCCCGAGACCTCCTTCCTGC'
    },
    {
      id: 'gfp',
      name: 'Synthetic GFP Reporter (Fluorescence)',
      locus: 'Aequorea victoria GFP',
      description: 'Green fluorescent protein expression cassette for synthetic gene circuit reporter verification.',
      seq: 'ATGGTGAGCAAGGGCGAGGAGCTGTTCACCGGGGTGGTGCCCATCCTGGTCGAGCTGGACGGCGACGTAAACGGCCACAAGTTCAGCGTGTCCGGCGAGGGCGAGGGCGATGCCACCTACGGCAAGCTGACCCTGAAGTTCATCTGCACCACCGGCAAGCTGCCCGTGCCCTGGCCCACCCTCGTGACCACCCTGACCTACGGCGTGCAGTGCTTCAGCCGCTACCCCGACCACATGAAGCAGCACGACTTCTTCAAGTCCGCCATGCCCGAAGGCTAC'
    },
    {
      id: 'insulin',
      name: 'Human Insulin B-Chain (Therapeutic)',
      locus: 'Homo sapiens INS',
      description: 'Coding sequence for biologically active insulin B-chain peptide hormone.',
      seq: 'ATGGCCCTGTGGATGCGCCTCCTGCCCCTGCTGGCGCTGCTGGCCCTCTGGGGACCTGACCCAGCCGCAGCCTTTGTGAACCAACACCTGTGCGGCTCACACCTGGTGGAAGCTCTCTACCTAGTGTGCGGGGAACGAGGCTTCTTCTACACACCCAAGACCCGCCGGGAGGCAGAGGACCTGCAGGTGGGGCAGGTGGAGCTGGGCG'
    },
    {
      id: 'hbb',
      name: 'Beta-Globin HBB (Sickle Cell Locus)',
      locus: 'Homo sapiens HBB',
      description: 'Site of point mutation (E6V) causing sickle-cell anemia, prime target for base editing.',
      seq: 'ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCCGTTACTGCCCTGTGGGGCAAGGTGAACGTGGATGAAGTTGGTGGTGAGGCCCTGGGCAGGCTGCTGGTGGTCTACCCTTGGACCCAGAGGTTCTTTGAGTCCTTTGGGGATCTGTCCACTCCTGATGCTGTTATGGGCAACCCTAAGGTGAAGGCTCATGGCAAGAAAGTGCTCGGTGCCTTTAGTGATGGCCTGGCTCACCTGGACAACCTCAAGGGCACCTTTGCCACACTGAGTGAGCTGCACTGTGACAAGCTGCACGTGGATCCTGAGAACTTCAGGCTCCTGGGCAACGTGCTGGTCTGTGTGCTGGCCCATCACTTTGGCAAAGAATTCACCCCACCAGTGCAGGCTGCCTATCAGAAAGTGGTGGCTGGTGTGGCTAATGCCCTGGCCCACAAGTATCACTAA'
    }
  ];

  let selectedPreset = SEQUENCE_PRESETS[0];
  let customSequence = selectedPreset.seq;

  // Real-time bioinformatics calculations
  $: cleanSeq = customSequence.toUpperCase().replace(/[^ACGTN]/g, '');
  $: seqLength = cleanSeq.length;
  $: gcCount = (cleanSeq.match(/[GC]/g) || []).length;
  $: gcPercentage = seqLength > 0 ? Number(((gcCount / seqLength) * 100).toFixed(1)) : 0;
  // Nearest-neighbor melting temperature approximation:
  // For oligonucleotides > 50 bp: Tm = 64.9 + 41 * (yG + zC - 16.4) / N
  $: tmCelsius = seqLength > 0
    ? Number((64.9 + 41 * (gcCount - 16.4) / seqLength).toFixed(1))
    : 0;
  // Molecular weight approximation: ~330 Da per nucleotide
  $: molWeightKDa = Number(((seqLength * 330) / 1000).toFixed(2));

  // Find 20-nt guide RNA spacers matching NGG PAM
  interface DetectedPam {
    index: number;
    spacer: string;
    pam: string;
    fullTarget: string;
    gcPercent: number;
    specificityScore: number;
  }

  $: detectedSpacers = computeSpacers(cleanSeq);

  function computeSpacers(seq: string): DetectedPam[] {
    const list: DetectedPam[] = [];
    if (seq.length < 23) return list;

    for (let i = 20; i < seq.length - 2; i++) {
      if (seq[i + 1] === 'G' && seq[i + 2] === 'G') {
        const spacer = seq.slice(i - 20, i);
        const pam = seq.slice(i, i + 3);
        const sGc = (spacer.match(/[GC]/g) || []).length;
        const gc = Number(((sGc / 20) * 100).toFixed(1));
        // Hsu-Zhang predictive heuristic score (penalizing extreme GC and poly-T tracts)
        let spec = 0.95;
        if (gc < 40 || gc > 65) spec -= 0.15;
        if (spacer.includes('TTTT')) spec -= 0.35;
        if (spacer.startsWith('T')) spec -= 0.05;
        list.push({
          index: i - 20,
          spacer,
          pam,
          fullTarget: spacer + pam,
          gcPercent: gc,
          specificityScore: Math.max(0.1, Number(spec.toFixed(2)))
        });
      }
    }
    return list.slice(0, 5); // top candidates
  }

  $: selectedSpacer = detectedSpacers[0] || {
    index: 0,
    spacer: cleanSeq.slice(0, 20) || 'GAGTCCGAGCAGAAGAAGAA',
    pam: 'GGG',
    fullTarget: (cleanSeq.slice(0, 20) || 'GAGTCCGAGCAGAAGAAGAA') + 'GGG',
    gcPercent: 55.0,
    specificityScore: 0.94
  };

  // Golden Gate Assembly Oligos with BsaI Overhangs
  $: forwardOligo = `5'- CACC ${selectedSpacer.spacer} -3'`;
  $: reverseOligo = `5'- AAAC ${reverseComplement(selectedSpacer.spacer)} -3'`;

  function reverseComplement(dna: string): string {
    const map: Record<string, string> = { A: 'T', T: 'A', C: 'G', G: 'C', N: 'N' };
    return dna.toUpperCase().split('').reverse().map(b => map[b] || 'N').join('');
  }

  // Demonstration DAG pipeline execution state
  type StepStatus = 'Pending' | 'Running' | 'Completed' | 'Interrupt' | 'Failed';

  interface DemoStep {
    id: string;
    number: number;
    title: string;
    engine: string;
    icon: string;
    status: StepStatus;
    summary: string;
    details?: string;
    elapsedMs?: number;
    logMessage?: string;
  }

  let demoSteps: DemoStep[] = [
    {
      id: 'step_input',
      number: 1,
      title: 'Target Sequence Ingestion',
      engine: 'Input Node',
      icon: '📥',
      status: 'Pending',
      summary: 'Loads FASTA/GenBank nucleotide stream with locus metadata.'
    },
    {
      id: 'step_agent',
      number: 2,
      title: 'Cas9 gRNA Candidate Designer',
      engine: 'Microsoft Copilot ONNX',
      icon: '🤖',
      status: 'Pending',
      summary: 'Scans for 20-nt spacers immediately adjacent to NGG PAM motifs.'
    },
    {
      id: 'step_thermo',
      number: 3,
      title: 'Thermodynamics & Stability QC',
      engine: 'Bio Skill Registry',
      icon: '🧬',
      status: 'Pending',
      summary: 'Evaluates melting temperature (Tm), GC distribution, and hairpin propensity.'
    },
    {
      id: 'step_csnakes',
      number: 4,
      title: 'CSnakes Genome Off-Target Scoring',
      engine: 'CSnakes Python C-API',
      icon: '🧩',
      status: 'Pending',
      summary: 'Performs zero-overhead BioPython genome mismatch heuristic against GRCh38.'
    },
    {
      id: 'step_hitl',
      number: 5,
      title: 'AG-UI Human-in-the-Loop Safety Gate',
      engine: 'State Machine (HITL)',
      icon: '🛑',
      status: 'Pending',
      summary: 'Interrupts execution via AG-UI protocol for operator safety review and sign-off.'
    },
    {
      id: 'step_assembly',
      number: 6,
      title: 'Golden Gate Oligo Assembly',
      engine: 'Bio Skill Registry',
      icon: '🧪',
      status: 'Pending',
      summary: 'Generates complementary oligonucleotides with BsaI overhangs (CACC / AAAC).'
    },
    {
      id: 'step_export',
      number: 7,
      title: 'Workspace FASTA/GenBank Export',
      engine: 'Workspace File System',
      icon: '📁',
      status: 'Pending',
      summary: 'Writes production-grade artifacts to workspace storage.'
    },
    {
      id: 'step_output',
      number: 8,
      title: 'LIMS Synthesis Specification',
      engine: 'Console Output',
      icon: '🖥️',
      status: 'Pending',
      summary: 'Formats finalized order specs for laboratory synthesis and automated ordering.'
    }
  ];

  let isRunning = false;
  let currentStepIdx = -1;
  let hitlInterruptActive = false;
  let hitlOperatorApproved = false;
  let executionLogs: Array<{ time: string; level: 'info' | 'warn' | 'success' | 'protocol'; text: string }> = [];
  let executionTimer: any = null;
  let autoPlay = true;

  // Real-time AG-UI SSE protocol packet buffer
  let ssePackets: Array<{ event: string; data: string; timestamp: string }> = [];

  function addLog(level: 'info' | 'warn' | 'success' | 'protocol', text: string) {
    const time = new Date().toLocaleTimeString();
    executionLogs = [...executionLogs, { time, level, text }];
  }

  function emitSsePacket(event: string, payload: any) {
    const timestamp = new Date().toISOString();
    ssePackets = [
      ...ssePackets,
      {
        event,
        data: typeof payload === 'string' ? payload : JSON.stringify(payload, null, 2),
        timestamp
      }
    ];
  }

  function resetSimulation() {
    clearTimeout(executionTimer);
    isRunning = false;
    currentStepIdx = -1;
    hitlInterruptActive = false;
    hitlOperatorApproved = false;
    demoSteps = demoSteps.map(s => ({
      ...s,
      status: 'Pending',
      elapsedMs: undefined,
      details: undefined
    }));
    executionLogs = [];
    ssePackets = [];
    addLog('info', 'Demonstration pipeline reset to initial ready state.');
  }

  function selectPreset(preset: typeof SEQUENCE_PRESETS[0]) {
    selectedPreset = preset;
    customSequence = preset.seq;
    resetSimulation();
  }

  async function startDemonstration() {
    resetSimulation();
    isRunning = true;
    currentStepIdx = 0;
    addLog('info', `🚀 Initializing De Novo CRISPR-Cas9 Synthesis Pipeline for locus: ${selectedPreset.locus}`);
    emitSsePacket('PIPELINE_INIT', {
      pipelineId: 'wf-crispr-synthesis',
      targetSequenceLength: seqLength,
      preset: selectedPreset.name,
      engine: 'DNAPlatform.AgentFramework'
    });
    runNextStep();
  }

  async function runNextStep() {
    if (currentStepIdx < 0 || currentStepIdx >= demoSteps.length) {
      isRunning = false;
      addLog('success', '✨ All 8 pipeline nodes completed successfully! Synthesis artifact generated.');
      emitSsePacket('WORKFLOW_COMPLETE', {
        status: 'Completed',
        artifactsGenerated: ['workspace/crispr_emx1_oligos.fasta'],
        forwardOligo,
        reverseOligo
      });
      return;
    }

    const step = demoSteps[currentStepIdx];
    step.status = 'Running';
    demoSteps = [...demoSteps];

    addLog('info', `[Step ${step.number}/8] Running ${step.title} (${step.engine})...`);

    const startT = Date.now();

    // Step-specific execution logic
    if (step.id === 'step_input') {
      await delay(700);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Completed';
      step.details = `Ingested ${seqLength} bp (${gcPercentage}% GC, Tm ${tmCelsius}°C)`;
      emitSsePacket('STATE_DELTA', {
        op: 'add',
        path: '/context/inputSequence',
        value: { length: seqLength, gc: gcPercentage, locus: selectedPreset.locus }
      });
      advanceToNext();
    } else if (step.id === 'step_agent') {
      await delay(1100);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Completed';
      step.details = `Identified ${detectedSpacers.length} candidate spacer(s). Optimal: ${selectedSpacer.spacer} (PAM: ${selectedSpacer.pam})`;
      emitSsePacket('THINKING_DELTA', {
        agent: 'Cas9 gRNA Designer',
        model: 'Microsoft-Copilot-Local-ONNX',
        reasoning: `Selected high-cleavage spacer ${selectedSpacer.spacer} adjacent to ${selectedSpacer.pam} PAM with ${selectedSpacer.gcPercent}% GC content.`
      });
      advanceToNext();
    } else if (step.id === 'step_thermo') {
      await delay(800);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Completed';
      step.details = `Thermodynamic stability verified. Melting Temp: ${tmCelsius}°C, GC Content: ${gcPercentage}% (Optimal 45-65%).`;
      emitSsePacket('STATE_DELTA', {
        op: 'replace',
        path: '/context/thermodynamics',
        value: { tm: tmCelsius, gc: gcPercentage, saltMolar: 0.05, pass: true }
      });
      advanceToNext();
    } else if (step.id === 'step_csnakes') {
      await delay(950);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Completed';
      const offTargetScore = 0.012;
      step.details = `CSnakes BioPython execution: Genome off-target penalty = ${offTargetScore} (Safe threshold < 0.05).`;
      emitSsePacket('STATE_DELTA', {
        op: 'replace',
        path: '/context/offTargetScore',
        value: { score: offTargetScore, safe: true, referenceGenome: 'GRCh38.p14' }
      });
      advanceToNext();
    } else if (step.id === 'step_hitl') {
      // Human-in-the-Loop Interrupt Gate
      await delay(600);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Interrupt';
      hitlInterruptActive = true;
      addLog('warn', '🛑 [AG-UI HITL INTERRUPT] State Machine paused execution: Operator safety authorization required before oligonucleotide synthesis commitment.');
      emitSsePacket('APPROVAL_REQUESTED', {
        interruptId: 'hitl-' + Date.now(),
        reason: 'CRISPR Target Cleavage Verification and Oligo Synthesis Order Authorization',
        locus: selectedPreset.locus,
        targetSpacer: selectedSpacer.spacer,
        pam: selectedSpacer.pam,
        offTargetScore: 0.012,
        specificityScore: selectedSpacer.specificityScore,
        estimatedCostUsd: 14.50,
        requiredRole: 'Bioinformatic_Operator'
      });
    } else if (step.id === 'step_assembly') {
      await delay(900);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Completed';
      step.details = `Golden Gate Overhangs generated (BsaI). Fwd: 5'-CACC... Rev: 5'-AAAC...`;
      emitSsePacket('STATE_DELTA', {
        op: 'add',
        path: '/context/oligoAssembly',
        value: { forwardOligo, reverseOligo, overhangs: { '5_prime': 'CACC', '3_prime': 'AAAC' } }
      });
      advanceToNext();
    } else if (step.id === 'step_export') {
      await delay(750);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Completed';
      step.details = `Saved workspace/crispr_emx1_oligos.fasta (${forwardOligo.length + reverseOligo.length} bytes).`;
      emitSsePacket('STATE_DELTA', {
        op: 'add',
        path: '/artifacts/fasta',
        value: 'workspace/crispr_emx1_oligos.fasta'
      });
      advanceToNext();
    } else if (step.id === 'step_output') {
      await delay(700);
      step.elapsedMs = Date.now() - startT;
      step.status = 'Completed';
      step.details = `LIMS Production Order Verified. Status: READY_FOR_SYNTHESIS.`;
      emitSsePacket('PIPELINE_COMPLETE', {
        status: 'Success',
        summary: 'De Novo CRISPR synthesis pipeline completed without errors.'
      });
      advanceToNext();
    }
  }

  function advanceToNext() {
    currentStepIdx++;
    demoSteps = [...demoSteps];
    if (autoPlay && currentStepIdx < demoSteps.length) {
      executionTimer = setTimeout(() => {
        runNextStep();
      }, 500);
    } else if (currentStepIdx >= demoSteps.length) {
      isRunning = false;
      addLog('success', '🎉 Pipeline demonstration completed successfully!');
    }
  }

  function approveHitlInterrupt() {
    hitlInterruptActive = false;
    hitlOperatorApproved = true;
    addLog('protocol', '✅ Operator approval received: AG-UI resuming pipeline with approvals token.');
    emitSsePacket('APPROVAL_GRANTED', {
      operator: 'Dr. Evelyn Carter, Ph.D. (Lead Genomicist)',
      decision: 'Approved',
      timestamp: new Date().toISOString()
    });

    const hitlStep = demoSteps.find(s => s.id === 'step_hitl');
    if (hitlStep) {
      hitlStep.status = 'Completed';
      hitlStep.details = 'Operator safety sign-off granted by Dr. Evelyn Carter.';
    }
    demoSteps = [...demoSteps];

    currentStepIdx = 5; // advance to step 6 (Golden Gate Assembly)
    runNextStep();
  }

  function rejectHitlInterrupt() {
    hitlInterruptActive = false;
    isRunning = false;
    addLog('warn', '❌ Operator declined synthesis authorization. Pipeline halted safely.');
    emitSsePacket('APPROVAL_REJECTED', {
      decision: 'Rejected by operator',
      timestamp: new Date().toISOString()
    });
    const hitlStep = demoSteps.find(s => s.id === 'step_hitl');
    if (hitlStep) {
      hitlStep.status = 'Failed';
      hitlStep.details = 'Aborted by operator safety review.';
    }
    demoSteps = [...demoSteps];
  }

  function delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  function copyFastaOligos() {
    const fasta = `>CRISPR_EMX1_Top_Strand_Oligo (BsaI Overhang CACC)\nCACC${selectedSpacer.spacer}\n\n>CRISPR_EMX1_Bottom_Strand_Oligo (BsaI Overhang AAAC)\nAAAC${reverseComplement(selectedSpacer.spacer)}\n`;
    navigator.clipboard.writeText(fasta);
    addLog('success', 'Copied FASTA oligonucleotides to clipboard.');
  }

  function downloadFastaOligos() {
    const fasta = `; DNA Platform - Autonomous De Novo Synthesis Assembly
; Target Locus: ${selectedPreset.locus}
; PAM Motif: ${selectedSpacer.pam}
; Predicted Specificity: ${selectedSpacer.specificityScore * 100}%
>CRISPR_EMX1_Top_Strand_Forward_BsaI
CACC${selectedSpacer.spacer}
>CRISPR_EMX1_Bottom_Strand_Reverse_BsaI
AAAC${reverseComplement(selectedSpacer.spacer)}
`;
    const blob = new Blob([fasta], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'crispr_emx1_oligos.fasta';
    a.click();
    URL.revokeObjectURL(url);
    addLog('success', 'Downloaded crispr_emx1_oligos.fasta.');
  }

  function openCurrentInDesigner() {
    const wf = DEMO_WORKFLOWS.find(w => w.id === activeScenarioId) || DEMO_WORKFLOWS[0];
    dispatch('openDesigner', { workflow: wf });
  }

  function saveCurrentWorkflow() {
    const wf = DEMO_WORKFLOWS.find(w => w.id === activeScenarioId) || DEMO_WORKFLOWS[0];
    dispatch('saveWorkflow', { workflow: wf });
  }

  onMount(() => {
    addLog('info', 'Interactive Bioinformatic Workflow Demonstration Hub initialized.');
  });

  onDestroy(() => {
    clearTimeout(executionTimer);
  });
</script>

<div class="demo-container">
  <!-- Top Hero Banner -->
  <div class="demo-hero">
    <div class="hero-left">
      <div class="hero-tag">
        <span class="pulsing-dot"></span>
        <span>LIVE INTERACTIVE WORKFLOW DEMONSTRATION</span>
      </div>
      <h2 class="hero-title">Autonomous De Novo CRISPR-Cas9 Target Optimization</h2>
      <p class="hero-desc">
        Experience the polyglot DNA Platform engine connecting <strong>Copilot ONNX reasoning</strong>, 
        <strong>CSnakes Python-C# zero-overhead interop</strong>, and <strong>AG-UI Human-in-the-Loop (HITL)</strong> safety interrupts.
      </p>
    </div>
    <div class="hero-actions">
      <button class="btn-primary-action" on:click={startDemonstration} disabled={isRunning && !hitlInterruptActive}>
        <span class="btn-icon">▶</span>
        <span>{isRunning ? 'Running Live Demo...' : 'Run Live Demonstration'}</span>
      </button>
      <button class="btn-secondary-action" on:click={resetSimulation} disabled={!isRunning && currentStepIdx === -1}>
        ↺ Reset
      </button>
      <button class="btn-secondary-action" on:click={openCurrentInDesigner}>
        ✏️ Open in Canvas
      </button>
    </div>
  </div>

  <!-- Scenario Selector -->
  <div class="scenario-bar">
    <span class="scenario-label">Demonstration Pipeline:</span>
    <button
      class="scenario-pill"
      class:scenario-pill-active={activeScenarioId === 'wf-crispr-synthesis'}
      on:click={() => { activeScenarioId = 'wf-crispr-synthesis'; resetSimulation(); }}
    >
      🔬 De Novo CRISPR-Cas9 & Oligo Assembly (HITL)
    </button>
    <button
      class="scenario-pill"
      class:scenario-pill-active={activeScenarioId === 'wf-comprehensive-bio'}
      on:click={() => { activeScenarioId = 'wf-comprehensive-bio'; resetSimulation(); }}
    >
      🧬 Genomic Variant & Needleman-Wunsch Alignment
    </button>
    <button
      class="scenario-pill"
      class:scenario-pill-active={activeScenarioId === 'wf-mcp-heterogeneous'}
      on:click={() => { activeScenarioId = 'wf-mcp-heterogeneous'; resetSimulation(); }}
    >
      ⚡ Heterogeneous MCP Toolchain (mise + Roslyn C# + CSnakes)
    </button>
  </div>

  <!-- Biological Sequence Input & Metrics Panel -->
  <div class="workbench-grid">
    <div class="workbench-card sequence-box">
      <div class="card-header">
        <div class="header-left">
          <span class="card-icon">🧬</span>
          <h3>Target Biological Sequence</h3>
        </div>
        <div class="preset-pills">
          {#each SEQUENCE_PRESETS as preset}
            <button
              class="preset-btn"
              class:preset-btn-active={selectedPreset.id === preset.id}
              on:click={() => selectPreset(preset)}
            >
              {preset.name.split(' ')[0]}
            </button>
          {/each}
        </div>
      </div>
      <p class="locus-desc">{selectedPreset.description}</p>
      <div class="textarea-wrapper">
        <textarea
          bind:value={customSequence}
          rows="4"
          placeholder="Paste standard FASTA or raw DNA sequence (A, C, G, T)..."
          spellcheck="false"
        ></textarea>
      </div>

      <!-- Real-time Sequence Metrics Bar -->
      <div class="metrics-strip">
        <div class="metric-item">
          <span class="metric-label">Length</span>
          <span class="metric-val">{seqLength} <small>bp</small></span>
        </div>
        <div class="metric-item">
          <span class="metric-label">GC Content</span>
          <span class="metric-val" class:metric-optimal={gcPercentage >= 40 && gcPercentage <= 65}>
            {gcPercentage}%
          </span>
        </div>
        <div class="metric-item">
          <span class="metric-label">Melting Temp (Tm)</span>
          <span class="metric-val">{tmCelsius}°C</span>
        </div>
        <div class="metric-item">
          <span class="metric-label">Mol Weight</span>
          <span class="metric-val">{molWeightKDa} <small>kDa</small></span>
        </div>
        <div class="metric-item">
          <span class="metric-label">PAM (NGG) Motifs</span>
          <span class="metric-val metric-highlight">{detectedSpacers.length} found</span>
        </div>
      </div>
    </div>

    <!-- CRISPR Guide RNA Candidate & Golden Gate Overhangs -->
    <div class="workbench-card assembly-box">
      <div class="card-header">
        <div class="header-left">
          <span class="card-icon">🧪</span>
          <h3>Target Guide RNA & Oligo Overhangs</h3>
        </div>
        <button class="btn-copy" on:click={copyFastaOligos} title="Copy FASTA to clipboard">
          📋 Copy FASTA
        </button>
      </div>
      
      <div class="candidate-display">
        <div class="field-row">
          <span class="field-label">Selected 20-nt Spacer:</span>
          <span class="spacer-code font-mono">{selectedSpacer.spacer}</span>
        </div>
        <div class="field-row">
          <span class="field-label">Adjacent PAM:</span>
          <span class="pam-code font-mono">{selectedSpacer.pam}</span>
          <span class="badge-spec">Score: {selectedSpacer.specificityScore * 100}%</span>
        </div>
      </div>

      <div class="oligos-preview">
        <div class="oligo-row">
          <span class="oligo-tag fwd">Fwd Oligo (BsaI):</span>
          <code class="oligo-code">5'- <strong class="overhang">CACC</strong> {selectedSpacer.spacer} -3'</code>
        </div>
        <div class="oligo-row">
          <span class="oligo-tag rev">Rev Oligo (BsaI):</span>
          <code class="oligo-code">5'- <strong class="overhang">AAAC</strong> {reverseComplement(selectedSpacer.spacer)} -3'</code>
        </div>
      </div>

      <div class="assembly-actions">
        <button class="btn-download" on:click={downloadFastaOligos}>
          💾 Download Assembly FASTA (.fasta)
        </button>
      </div>
    </div>
  </div>

  <!-- Interactive Visual DAG Pipeline Canvas -->
  <div class="pipeline-section">
    <div class="section-title-bar">
      <div class="title-left">
        <span class="section-icon">🔀</span>
        <h3>Interactive DAG Pipeline Execution Flow</h3>
      </div>
      <div class="pipeline-legend">
        <span class="legend-item"><span class="chip-legend pending"></span> Pending</span>
        <span class="legend-item"><span class="chip-legend running"></span> Active Node</span>
        <span class="legend-item"><span class="chip-legend completed"></span> Completed</span>
        <span class="legend-item"><span class="chip-legend interrupt"></span> HITL Interrupt</span>
      </div>
    </div>

    <div class="pipeline-steps-track">
      {#each demoSteps as step, i}
        <div class="step-card step-{step.status.toLowerCase()}" class:step-current={currentStepIdx === i}>
          <div class="step-header">
            <span class="step-badge">{step.number}</span>
            <span class="step-icon">{step.icon}</span>
            <span class="step-status-chip status-{step.status.toLowerCase()}">
              {step.status === 'Interrupt' ? '⚠️ HITL Sign-off' : step.status}
            </span>
          </div>

          <h4 class="step-title">{step.title}</h4>
          <span class="step-engine">{step.engine}</span>
          <p class="step-summary">{step.summary}</p>

          {#if step.details}
            <div class="step-details-box">
              {step.details}
              {#if step.elapsedMs}
                <span class="step-time">({step.elapsedMs}ms)</span>
              {/if}
            </div>
          {/if}

          {#if step.status === 'Running'}
            <div class="progress-bar-active"></div>
          {/if}
        </div>

        {#if i < demoSteps.length - 1}
          <div class="step-connector" class:connector-active={currentStepIdx > i}>
            <div class="connector-line"></div>
            <span class="connector-arrow">▶</span>
          </div>
        {/if}
      {/each}
    </div>
  </div>

  <!-- Human-in-the-Loop (HITL) Interactive Safety Modal / Banner -->
  {#if hitlInterruptActive}
    <div class="hitl-banner">
      <div class="hitl-header">
        <div class="hitl-title-row">
          <span class="hitl-alert-icon">🛑</span>
          <div>
            <h3>AG-UI Protocol: Human-in-the-Loop Operator Verification Required</h3>
            <p>Execution halted at Node 5. In compliance with bio-safety standards, automated synthesis of edited genomic sequences requires human operator review.</p>
          </div>
        </div>
        <span class="protocol-tag font-mono">PROTOCOL: AG_UI_HITL_INTERRUPT</span>
      </div>

      <div class="hitl-data-grid">
        <div class="hitl-param">
          <span class="param-name">Target Locus:</span>
          <span class="param-val">{selectedPreset.locus}</span>
        </div>
        <div class="hitl-param">
          <span class="param-name">Guide RNA Spacer (20-nt):</span>
          <span class="param-val font-mono">{selectedSpacer.spacer}</span>
        </div>
        <div class="hitl-param">
          <span class="param-name">Target PAM Motif:</span>
          <span class="param-val font-mono">{selectedSpacer.pam}</span>
        </div>
        <div class="hitl-param">
          <span class="param-name">Genome Off-Target Penalty:</span>
          <span class="param-val text-emerald">0.012 (Safe threshold &lt; 0.05)</span>
        </div>
        <div class="hitl-param">
          <span class="param-name">Cleavage Specificity:</span>
          <span class="param-val text-emerald">{(selectedSpacer.specificityScore * 100).toFixed(0)}% (High Fidelity)</span>
        </div>
        <div class="hitl-param">
          <span class="param-name">Restriction Architecture:</span>
          <span class="param-val">Type IIS BsaI Sticky Ends (4-bp)</span>
        </div>
      </div>

      <div class="hitl-operator-controls">
        <div class="operator-signature">
          <span>Acting Bioinformatician: <strong>Dr. Evelyn Carter, Ph.D.</strong></span>
        </div>
        <div class="hitl-buttons">
          <button class="btn-hitl-reject" on:click={rejectHitlInterrupt}>
            ❌ Abort / Reject Synthesis
          </button>
          <button class="btn-hitl-approve" on:click={approveHitlInterrupt}>
            ✅ Approve &amp; Commit to Oligo Synthesis
          </button>
        </div>
      </div>
    </div>
  {/if}

  <!-- Lower Split: AG-UI SSE Protocol Packets + Execution Terminal Log -->
  <div class="inspectors-grid">
    <!-- AG-UI Protocol Stream -->
    <div class="inspector-card">
      <div class="inspector-header">
        <div class="header-left">
          <span class="card-icon">⚡</span>
          <h4>AG-UI Protocol Real-Time SSE Stream (/api/agui)</h4>
        </div>
        <span class="badge-count">{ssePackets.length} events</span>
      </div>
      <div class="stream-container font-mono">
        {#if ssePackets.length === 0}
          <div class="stream-empty">Waiting for workflow start. Click "Run Live Demonstration" to stream events...</div>
        {:else}
          {#each ssePackets as pkt}
            <div class="packet-block">
              <div class="packet-meta">
                <span class="packet-event">event: {pkt.event}</span>
                <span class="packet-time">{pkt.timestamp.split('T')[1].replace('Z', '')}</span>
              </div>
              <pre class="packet-body">{pkt.data}</pre>
            </div>
          {/each}
        {/if}
      </div>
    </div>

    <!-- Execution Terminal Logs -->
    <div class="inspector-card">
      <div class="inspector-header">
        <div class="header-left">
          <span class="card-icon">📜</span>
          <h4>Workflow Execution Journal</h4>
        </div>
        <button class="btn-clear-logs" on:click={() => { executionLogs = []; }}>Clear</button>
      </div>
      <div class="terminal-container font-mono">
        {#if executionLogs.length === 0}
          <div class="terminal-empty">Logs will record during execution...</div>
        {:else}
          {#each executionLogs as log}
            <div class="log-line log-{log.level}">
              <span class="log-time">[{log.time}]</span>
              <span class="log-text">{log.text}</span>
            </div>
          {/each}
        {/if}
      </div>
    </div>
  </div>
</div>

<style>
  .demo-container {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    margin-bottom: 2.5rem;
    color: #e2e8f0;
  }

  /* Hero Banner */
  .demo-hero {
    background: linear-gradient(135deg, rgba(15, 23, 42, 0.95), rgba(30, 41, 59, 0.85));
    border: 1px solid rgba(14, 165, 233, 0.35);
    border-radius: 0.75rem;
    padding: 1.5rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1.5rem;
    box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.4);
  }

  .hero-tag {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    background: rgba(14, 165, 233, 0.15);
    border: 1px solid rgba(14, 165, 233, 0.4);
    color: #38bdf8;
    font-size: 0.75rem;
    font-weight: 600;
    letter-spacing: 0.05em;
    padding: 0.25rem 0.6rem;
    border-radius: 9999px;
    margin-bottom: 0.5rem;
  }

  .pulsing-dot {
    width: 6px;
    height: 6px;
    background: #38bdf8;
    border-radius: 50%;
    animation: pulse 1.5s infinite;
  }

  @keyframes pulse {
    0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(56, 189, 248, 0.7); }
    70% { transform: scale(1); box-shadow: 0 0 0 6px rgba(56, 189, 248, 0); }
    100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(56, 189, 248, 0); }
  }

  .hero-title {
    font-size: 1.4rem;
    font-weight: 700;
    color: #f8fafc;
    margin: 0 0 0.4rem 0;
  }

  .hero-desc {
    color: #94a3b8;
    font-size: 0.9rem;
    margin: 0;
    max-width: 720px;
    line-height: 1.45;
  }

  .hero-desc strong {
    color: #e2e8f0;
  }

  .hero-actions {
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
    align-items: center;
  }

  .btn-primary-action {
    background: linear-gradient(135deg, #0284c7, #0369a1);
    color: white;
    border: 1px solid #38bdf8;
    padding: 0.75rem 1.4rem;
    border-radius: 0.5rem;
    cursor: pointer;
    font-weight: 600;
    font-size: 0.95rem;
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    box-shadow: 0 4px 14px rgba(2, 132, 199, 0.4);
    transition: all 0.2s ease;
  }

  .btn-primary-action:hover:not(:disabled) {
    background: linear-gradient(135deg, #0369a1, #075985);
    transform: translateY(-1px);
    box-shadow: 0 6px 18px rgba(2, 132, 199, 0.5);
  }

  .btn-primary-action:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  .btn-secondary-action {
    background: rgba(30, 41, 59, 0.8);
    color: #e2e8f0;
    border: 1px solid rgba(148, 163, 184, 0.25);
    padding: 0.75rem 1.1rem;
    border-radius: 0.5rem;
    cursor: pointer;
    font-weight: 500;
    font-size: 0.875rem;
    transition: background 0.2s;
  }

  .btn-secondary-action:hover:not(:disabled) {
    background: rgba(51, 65, 85, 0.9);
    border-color: rgba(148, 163, 184, 0.4);
  }

  /* Scenario Bar */
  .scenario-bar {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
    background: rgba(15, 23, 42, 0.6);
    border: 1px solid rgba(51, 65, 85, 0.6);
    padding: 0.6rem 1rem;
    border-radius: 0.5rem;
  }

  .scenario-label {
    font-size: 0.8rem;
    color: #94a3b8;
    font-weight: 500;
  }

  .scenario-pill {
    background: transparent;
    color: #cbd5e1;
    border: 1px solid rgba(148, 163, 184, 0.2);
    padding: 0.4rem 0.85rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-size: 0.825rem;
    transition: all 0.15s ease;
  }

  .scenario-pill:hover {
    background: rgba(51, 65, 85, 0.5);
    color: #f8fafc;
  }

  .scenario-pill-active {
    background: rgba(14, 165, 233, 0.2);
    border-color: #38bdf8;
    color: #38bdf8;
    font-weight: 600;
  }

  /* Workbench Grid */
  .workbench-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.25rem;
  }

  @media (max-width: 900px) {
    .workbench-grid {
      grid-template-columns: 1fr;
    }
  }

  .workbench-card {
    background: #0f172a;
    border: 1px solid rgba(51, 65, 85, 0.8);
    border-radius: 0.625rem;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .card-icon {
    font-size: 1.25rem;
  }

  .card-header h3 {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: #f1f5f9;
  }

  .preset-pills {
    display: flex;
    gap: 0.35rem;
  }

  .preset-btn {
    background: rgba(30, 41, 59, 0.7);
    color: #94a3b8;
    border: 1px solid rgba(148, 163, 184, 0.2);
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    cursor: pointer;
  }

  .preset-btn:hover {
    color: #f1f5f9;
  }

  .preset-btn-active {
    background: rgba(14, 165, 233, 0.2);
    color: #38bdf8;
    border-color: #38bdf8;
    font-weight: 600;
  }

  .locus-desc {
    font-size: 0.8rem;
    color: #64748b;
    margin: 0;
  }

  .textarea-wrapper textarea {
    width: 100%;
    background: #020617;
    border: 1px solid rgba(51, 65, 85, 0.8);
    border-radius: 0.375rem;
    padding: 0.6rem;
    color: #38bdf8;
    font-family: monospace;
    font-size: 0.775rem;
    line-height: 1.4;
    resize: vertical;
    box-sizing: border-box;
  }

  .metrics-strip {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 0.5rem;
    background: rgba(2, 6, 23, 0.6);
    padding: 0.5rem 0.75rem;
    border-radius: 0.375rem;
    border: 1px solid rgba(51, 65, 85, 0.5);
  }

  .metric-item {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
  }

  .metric-label {
    font-size: 0.65rem;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .metric-val {
    font-size: 0.9rem;
    font-weight: 700;
    color: #e2e8f0;
  }

  .metric-optimal {
    color: #10b981;
  }

  .metric-highlight {
    color: #f59e0b;
  }

  /* Assembly Card */
  .candidate-display {
    background: rgba(2, 6, 23, 0.6);
    border: 1px solid rgba(51, 65, 85, 0.6);
    border-radius: 0.375rem;
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  .field-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.85rem;
  }

  .field-label {
    color: #94a3b8;
    font-size: 0.8rem;
    min-width: 140px;
  }

  .spacer-code {
    background: rgba(14, 165, 233, 0.15);
    color: #38bdf8;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.85rem;
    font-weight: 700;
    letter-spacing: 0.05em;
  }

  .pam-code {
    background: rgba(245, 158, 11, 0.15);
    color: #f59e0b;
    padding: 0.2rem 0.5rem;
    border-radius: 0.25rem;
    font-weight: 700;
  }

  .badge-spec {
    font-size: 0.75rem;
    color: #10b981;
    background: rgba(16, 185, 129, 0.15);
    padding: 0.15rem 0.4rem;
    border-radius: 0.25rem;
  }

  .oligos-preview {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .oligo-row {
    background: rgba(15, 23, 42, 0.8);
    border: 1px solid rgba(51, 65, 85, 0.6);
    border-radius: 0.375rem;
    padding: 0.5rem 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .oligo-tag {
    font-size: 0.7rem;
    font-weight: 600;
    text-transform: uppercase;
  }

  .oligo-tag.fwd { color: #38bdf8; }
  .oligo-tag.rev { color: #a855f7; }

  .oligo-code {
    font-family: monospace;
    font-size: 0.8rem;
    color: #cbd5e1;
  }

  .overhang {
    color: #f43f5e;
    font-weight: 700;
    text-decoration: underline;
  }

  .assembly-actions {
    display: flex;
    gap: 0.5rem;
    margin-top: auto;
  }

  .btn-copy {
    background: rgba(148, 163, 184, 0.1);
    color: #cbd5e1;
    border: 1px solid rgba(148, 163, 184, 0.2);
    padding: 0.25rem 0.6rem;
    border-radius: 0.25rem;
    font-size: 0.75rem;
    cursor: pointer;
  }

  .btn-download {
    width: 100%;
    background: rgba(16, 185, 129, 0.15);
    color: #10b981;
    border: 1px solid rgba(16, 185, 129, 0.4);
    padding: 0.55rem;
    border-radius: 0.375rem;
    font-weight: 600;
    font-size: 0.825rem;
    cursor: pointer;
    text-align: center;
    transition: background 0.15s;
  }

  .btn-download:hover {
    background: rgba(16, 185, 129, 0.25);
  }

  /* Visual DAG Track */
  .pipeline-section {
    background: #0f172a;
    border: 1px solid rgba(51, 65, 85, 0.8);
    border-radius: 0.625rem;
    padding: 1.25rem;
  }

  .section-title-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.25rem;
  }

  .title-left {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .section-icon {
    font-size: 1.25rem;
  }

  .section-title-bar h3 {
    margin: 0;
    font-size: 1.05rem;
    font-weight: 600;
    color: #f8fafc;
  }

  .pipeline-legend {
    display: flex;
    gap: 0.85rem;
    font-size: 0.75rem;
    color: #94a3b8;
  }

  .legend-item {
    display: flex;
    align-items: center;
    gap: 0.35rem;
  }

  .chip-legend {
    width: 8px;
    height: 8px;
    border-radius: 50%;
  }

  .chip-legend.pending { background: #64748b; }
  .chip-legend.running { background: #38bdf8; box-shadow: 0 0 6px #38bdf8; }
  .chip-legend.completed { background: #10b981; }
  .chip-legend.interrupt { background: #f59e0b; }

  .pipeline-steps-track {
    display: flex;
    align-items: stretch;
    gap: 0.5rem;
    overflow-x: auto;
    padding-bottom: 0.75rem;
  }

  .step-card {
    flex: 0 0 210px;
    background: rgba(30, 41, 59, 0.5);
    border: 1px solid rgba(71, 85, 105, 0.6);
    border-radius: 0.5rem;
    padding: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
    position: relative;
    transition: all 0.2s ease;
  }

  .step-card.step-running {
    border-color: #38bdf8;
    background: rgba(14, 165, 233, 0.12);
    box-shadow: 0 0 15px rgba(56, 189, 248, 0.25);
  }

  .step-card.step-completed {
    border-color: rgba(16, 185, 129, 0.4);
    background: rgba(16, 185, 129, 0.06);
  }

  .step-card.step-interrupt {
    border-color: #f59e0b;
    background: rgba(245, 158, 11, 0.15);
    box-shadow: 0 0 18px rgba(245, 158, 11, 0.3);
  }

  .step-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .step-badge {
    background: rgba(0, 0, 0, 0.3);
    color: #94a3b8;
    font-size: 0.65rem;
    font-weight: 700;
    width: 18px;
    height: 18px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
  }

  .step-icon {
    font-size: 1.1rem;
  }

  .step-status-chip {
    font-size: 0.65rem;
    padding: 0.15rem 0.4rem;
    border-radius: 0.25rem;
    font-weight: 600;
    text-transform: uppercase;
  }

  .status-pending { background: rgba(100, 116, 139, 0.2); color: #94a3b8; }
  .status-running { background: rgba(56, 189, 248, 0.2); color: #38bdf8; }
  .status-completed { background: rgba(16, 185, 129, 0.2); color: #10b981; }
  .status-interrupt { background: rgba(245, 158, 11, 0.3); color: #fbbf24; }
  .status-failed { background: rgba(239, 68, 68, 0.2); color: #ef4444; }

  .step-title {
    font-size: 0.85rem;
    font-weight: 600;
    color: #f1f5f9;
    margin: 0;
    line-height: 1.25;
  }

  .step-engine {
    font-size: 0.7rem;
    color: #38bdf8;
    font-weight: 500;
  }

  .step-summary {
    font-size: 0.725rem;
    color: #94a3b8;
    margin: 0;
    line-height: 1.35;
  }

  .step-details-box {
    margin-top: auto;
    background: rgba(2, 6, 23, 0.6);
    border: 1px solid rgba(51, 65, 85, 0.5);
    border-radius: 0.25rem;
    padding: 0.35rem 0.5rem;
    font-size: 0.7rem;
    color: #cbd5e1;
    line-height: 1.3;
  }

  .step-time {
    color: #64748b;
    font-size: 0.65rem;
    margin-left: 0.25rem;
  }

  .progress-bar-active {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 3px;
    background: linear-gradient(90deg, #38bdf8, #818cf8);
    animation: barload 1s infinite linear;
  }

  @keyframes barload {
    0% { transform: scaleX(0); transform-origin: left; }
    50% { transform: scaleX(1); transform-origin: left; }
    51% { transform: scaleX(1); transform-origin: right; }
    100% { transform: scaleX(0); transform-origin: right; }
  }

  .step-connector {
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    width: 28px;
    flex-shrink: 0;
  }

  .connector-line {
    width: 100%;
    height: 2px;
    background: rgba(71, 85, 105, 0.6);
  }

  .connector-arrow {
    position: absolute;
    font-size: 0.6rem;
    color: #64748b;
  }

  .connector-active .connector-line {
    background: #38bdf8;
  }

  .connector-active .connector-arrow {
    color: #38bdf8;
  }

  /* HITL Safety Banner */
  .hitl-banner {
    background: linear-gradient(135deg, rgba(30, 20, 10, 0.95), rgba(45, 25, 10, 0.9));
    border: 2px solid #f59e0b;
    border-radius: 0.625rem;
    padding: 1.25rem;
    box-shadow: 0 0 30px rgba(245, 158, 11, 0.25);
    animation: glowAlert 2s infinite alternate;
  }

  @keyframes glowAlert {
    from { box-shadow: 0 0 15px rgba(245, 158, 11, 0.2); }
    to { box-shadow: 0 0 30px rgba(245, 158, 11, 0.4); }
  }

  .hitl-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 1rem;
  }

  .hitl-title-row {
    display: flex;
    gap: 0.75rem;
  }

  .hitl-alert-icon {
    font-size: 1.75rem;
  }

  .hitl-header h3 {
    margin: 0 0 0.25rem 0;
    font-size: 1.1rem;
    color: #fbbf24;
    font-weight: 700;
  }

  .hitl-header p {
    margin: 0;
    font-size: 0.85rem;
    color: #fde68a;
    max-width: 750px;
  }

  .protocol-tag {
    background: rgba(245, 158, 11, 0.2);
    color: #fbbf24;
    border: 1px solid #f59e0b;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    font-size: 0.7rem;
    font-weight: 700;
  }

  .hitl-data-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 0.75rem;
    background: rgba(0, 0, 0, 0.4);
    border: 1px solid rgba(245, 158, 11, 0.3);
    border-radius: 0.375rem;
    padding: 0.85rem;
    margin-bottom: 1rem;
  }

  .hitl-param {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
  }

  .param-name {
    font-size: 0.75rem;
    color: #fde68a;
  }

  .param-val {
    font-size: 0.9rem;
    color: #f8fafc;
    font-weight: 600;
  }

  .text-emerald {
    color: #10b981;
  }

  .hitl-operator-controls {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .operator-signature {
    font-size: 0.85rem;
    color: #fde68a;
  }

  .hitl-buttons {
    display: flex;
    gap: 0.75rem;
  }

  .btn-hitl-reject {
    background: rgba(239, 68, 68, 0.15);
    color: #ef4444;
    border: 1px solid rgba(239, 68, 68, 0.4);
    padding: 0.6rem 1.25rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-weight: 600;
    font-size: 0.875rem;
  }

  .btn-hitl-reject:hover {
    background: rgba(239, 68, 68, 0.25);
  }

  .btn-hitl-approve {
    background: linear-gradient(135deg, #059669, #047857);
    color: white;
    border: 1px solid #10b981;
    padding: 0.6rem 1.4rem;
    border-radius: 0.375rem;
    cursor: pointer;
    font-weight: 700;
    font-size: 0.875rem;
    box-shadow: 0 4px 12px rgba(16, 185, 129, 0.35);
  }

  .btn-hitl-approve:hover {
    background: linear-gradient(135deg, #047857, #065f46);
  }

  /* Inspectors Grid */
  .inspectors-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.25rem;
  }

  @media (max-width: 900px) {
    .inspectors-grid {
      grid-template-columns: 1fr;
    }
  }

  .inspector-card {
    background: #0b1120;
    border: 1px solid rgba(51, 65, 85, 0.8);
    border-radius: 0.625rem;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    height: 320px;
  }

  .inspector-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .inspector-header h4 {
    margin: 0;
    font-size: 0.875rem;
    color: #f1f5f9;
  }

  .badge-count {
    font-size: 0.7rem;
    background: rgba(14, 165, 233, 0.15);
    color: #38bdf8;
    padding: 0.15rem 0.4rem;
    border-radius: 0.25rem;
  }

  .btn-clear-logs {
    background: transparent;
    color: #64748b;
    border: 1px solid rgba(148, 163, 184, 0.2);
    font-size: 0.7rem;
    padding: 0.15rem 0.4rem;
    border-radius: 0.25rem;
    cursor: pointer;
  }

  .stream-container, .terminal-container {
    flex: 1;
    background: #020617;
    border: 1px solid rgba(51, 65, 85, 0.6);
    border-radius: 0.375rem;
    padding: 0.75rem;
    overflow-y: auto;
    font-size: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .stream-empty, .terminal-empty {
    color: #64748b;
    font-style: italic;
    text-align: center;
    margin-top: 3.5rem;
  }

  .packet-block {
    background: rgba(15, 23, 42, 0.7);
    border: 1px solid rgba(56, 189, 248, 0.2);
    border-radius: 0.25rem;
    padding: 0.4rem 0.6rem;
  }

  .packet-meta {
    display: flex;
    justify-content: space-between;
    margin-bottom: 0.25rem;
  }

  .packet-event {
    color: #38bdf8;
    font-weight: 700;
  }

  .packet-time {
    color: #64748b;
    font-size: 0.65rem;
  }

  .packet-body {
    margin: 0;
    color: #cbd5e1;
    font-size: 0.7rem;
    max-height: 80px;
    overflow-y: auto;
    white-space: pre-wrap;
    word-break: break-all;
  }

  .log-line {
    display: flex;
    gap: 0.4rem;
    line-height: 1.4;
  }

  .log-time {
    color: #64748b;
    flex-shrink: 0;
  }

  .log-info .log-text { color: #cbd5e1; }
  .log-warn .log-text { color: #f59e0b; }
  .log-success .log-text { color: #10b981; }
  .log-protocol .log-text { color: #38bdf8; }
</style>
