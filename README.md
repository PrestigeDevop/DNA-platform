# 🧬 DeNovoAutomata

**Autonomous Polyglot Computational Biology & Workflow Automation Platform**

[![Status: Active](https://img.shields.io/badge/Status-Active_Alpha-brightgreen.svg)](#)
[![Runtime: .NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Frontend: SvelteKit 2](https://img.shields.io/badge/Frontend-SvelteKit_2-orange.svg)](https://kit.svelte.dev/)
[![Core: CSnakes](https://img.shields.io/badge/Core-CSnakes_Python_Interop-yellow.svg)](#)
[![Protocol: AG--UI](https://img.shields.io/badge/Protocol-AG--UI_v1.0.0-purple.svg)](https://docs.ag-ui.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

---

## 🎯 Overview

**DeNovoAutomata** is an open-source, local-first biological workflow orchestration platform. Inspired by visual automation tools like **n8n**, it empowers computational biologists, researchers, and developers to visually compose, validate, and execute complex biological pipelines with intelligent AI agent governance.

- **Visual Workflow Canvas**: Build and test directed acyclic graph (DAG) pipelines via an intuitive drag-and-drop designer.
- **Polyglot Execution with CSnakes**: Execute compiled C# .NET algorithms and Python bioinformatics packages (BioPython, NumPy) side-by-side with zero-overhead data transfer.
- **Human-in-the-Loop (HITL) Governance**: AI agents stream reasoning traces and pause automatically before sensitive operations (e.g., sequence mutations, system execution) to request operator sign-off.
- **Dual Interfaces**: Work seamlessly in your browser via the SvelteKit web studio or in the terminal using the native **Terminal.Gui** console dashboard.

---

## 🚀 The 3 Simple Commands

DeNovoAutomata is cross-platform across **Linux**, **macOS**, and **Windows**.

### 1. First-Time Setup
Probes your toolchain, sets up configurations, and initializes the local SQLite database.
```bash
npm run bootstrap
# or: node scripts/1-bootstrap.mjs
```

### 2. Launch the Platform
Boots instantly in under one second without reinstalling. Starts the Web Studio on port 3000, API on port 5254, and AG-UI server on port 5000.
```bash
npm start
# or: node scripts/3-denovo-automata.mjs
```
*Tip: On Windows, you can also double-click `run-all.bat`.*

### 3. Test & Clean
Runs the automated end-to-end verification suite across all 10 platform subsystems. Use `--clean` to remove temporary files before pushing to Git.
```bash
# Run test suite
npm test
# or: node scripts/2-test-dev.mjs

# Run tests and clean build artifacts
npm run test:clean
# or: node scripts/2-test-dev.mjs --clean
```

---

## ✨ Key Features

### 🔄 Visual Workflow Canvas (n8n-Inspired)
- Compose multi-step computational pipelines visually.
- Native node types: **Input**, **ProcessingSkill**, **Agent**, **Loop**, **Conditional**, and **Output**.
- Structural DAG verification prevents infinite cycles and unlinked dependencies before execution.

### 🤖 AI Agent Framework & AG-UI Hosting
- Native implementation of the **AG-UI Protocol** ([docs.ag-ui.com](https://docs.ag-ui.com/sdk/dotnet/hosting/overview)).
- Streams real-time cognitive deliberation traces (`THINKING_DELTA`) and tokens (`TEXT_DELTA`).
- Built-in **Human-in-the-Loop (HITL)**: Sensitive actions trigger an `APPROVAL_REQUESTED` interrupt, allowing operators to inspect arguments, authorize, or decline directly from the UI.
- Local reasoning powered by Microsoft Copilot ONNX runtime.

### 🐍 CSnakes Polyglot Core
- Direct zero-copy interop between .NET 8 and Python 3.
- Fast biological calculations for GC content, GC skew, nucleotide frequencies, and reverse complements.
- Pure C# fallback ensures high reliability on environments without Python installed.

### 🧩 Bioinformatic Skill System
- **Needleman-Wunsch Aligner**: Global pairwise sequence alignment with dynamic programming.
- **C# High-Precision Clock Provider**: Accurate microsecond timing for algorithmic benchmarking.
- **IUPAC Sequence Validator**: Validates DNA, RNA, and degenerate nucleotide strings.
- **Interactive Skill Authoring**: Live code editor with interactive verification and AI annotation helper.

### 🖥️ Native Terminal.Gui Console TUI
- Built on `Terminal.Gui` (`gui.cs`) in C#.
- Provides windowed console dashboards, menus, hotkey controls (`[R]`estart, `[T]`est, `[C]`lean, `[O]`pen, `[Q]`uit), and live health monitors.

---

## 📚 Documentation & Architecture

- **[System Architecture (`docs/architecture.md`)](./docs/architecture.md)** — Low-code/no-code bioinformatic platform architecture, folder structure, agentic development guide, developmental gaps, gotchas, and final intent.
- **[Development Guide & Roadmap (`docs/development.md`)](./docs/development.md)** — Consolidated development logs, task lists (TODO), revised phase milestones, and verification gates.

---

## 📄 License

DeNovoAutomata is licensed under the [MIT License](./LICENSE).
