#!/usr/bin/env node

/**
 * Script 3: DeNovoAutomata — Autonomous Polyglot Workflow Engine
 * 
 * Lightweight cross-platform replacement for runall.bat.
 * - Boots instantly without reinstalling from scratch
 * - Launches development servers
 * - Live animated DNA terminal banner with color spectrum
 * - Interactive keybindings: [R]estart, [T]est, [C]lean, [O]pen, [Q]uit
 */

import { spawn } from 'child_process';
import readline from 'readline';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '..');

const C = {
  reset: '\x1b[0m',
  bold: '\x1b[1m',
  dim: '\x1b[2m',
  cyan: '\x1b[36m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  red: '\x1b[31m',
  magenta: '\x1b[35m',
  blue: '\x1b[34m',
  gray: '\x1b[90m'
};

const HELIX_FRAMES = [
  '  🧬  A ═══════ T  🧬  ',
  '   🧬  G ═════ C  🧬   ',
  '    🧬  T ═══ A  🧬    ',
  '     🧬  C ═ G  🧬     ',
  '      🧬  ═══  🧬      ',
  '     🧬  G ═ C  🧬     ',
  '    🧬  A ═══ T  🧬    ',
  '   🧬  C ═════ G  🧬   ',
  '  🧬  T ═══════ A  🧬  '
];

let frameIdx = 0;
let devProcess = null;

function renderBanner() {
  console.clear();
  const helix = HELIX_FRAMES[frameIdx % HELIX_FRAMES.length];

  console.log(`
${C.cyan}${C.bold}╔════════════════════════════════════════════════════════════════════════════╗
║                           🧬 DeNovoAutomata 🧬                             ║
║      Autonomous Polyglot Computational Biology & Agentic DAG Engine        ║
╚════════════════════════════════════════════════════════════════════════════╝${C.reset}
${C.magenta}${C.bold}${helix}${C.reset}
${C.gray}Ports:${C.reset} DevUI: ${C.green}http://localhost:3000${C.reset} · API: ${C.cyan}http://localhost:5254${C.reset} · AG-UI: ${C.yellow}http://localhost:5000${C.reset}
${C.gray}Seams:${C.reset} CSnakes Polyglot in Core · AG-UI Protocol with Human-in-the-Loop (HITL)
────────────────────────────────────────────────────────────────────────────
${C.bold}Interactive Controls & Keybindings:${C.reset}
  [${C.cyan}R${C.reset}] Restart Server Stack       [${C.cyan}T${C.reset}] Run Agentic Smoke Test
  [${C.cyan}C${C.reset}] Clean Workspace Artifacts  [${C.cyan}L${C.reset}] Display Diagnostic Logs
  [${C.cyan}O${C.reset}] Open Browser (Port 3000)   [${C.cyan}Q${C.reset}] Graceful Shutdown (or Ctrl+C)
────────────────────────────────────────────────────────────────────────────
`);
}

function startDevServer() {
  const webDir = path.join(ROOT_DIR, 'src/05_DevUI/DNAPlatform.DevUI.Web');
  console.log(`${C.green}● Launching DeNovoAutomata application stack...${C.reset}\n`);

  devProcess = spawn('npm', ['run', 'dev'], {
    cwd: webDir,
    stdio: 'inherit',
    shell: true
  });

  devProcess.on('exit', (code) => {
    if (code !== 0 && code !== null) {
      console.log(`\n${C.yellow}⚠ Server process exited with code ${code}${C.reset}`);
    }
  });
}

function runSmokeTest() {
  console.log(`\n${C.magenta}▶ Dispatching Agentic E2E Test Suite...${C.reset}`);
  const testProc = spawn('node', [path.join(ROOT_DIR, 'scripts/2-test-dev.mjs')], {
    cwd: ROOT_DIR,
    stdio: 'inherit'
  });
  testProc.on('exit', () => {
    console.log(`\n${C.gray}Press any control key [R/T/C/L/O/Q]...${C.reset}`);
  });
}

function runClean() {
  console.log(`\n${C.yellow}▶ Running Workspace Artifact Cleanup...${C.reset}`);
  const cleanProc = spawn('node', [path.join(ROOT_DIR, 'scripts/2-test-dev.mjs'), '--clean'], {
    cwd: ROOT_DIR,
    stdio: 'inherit'
  });
  cleanProc.on('exit', () => {
    console.log(`\n${C.gray}Press any control key [R/T/C/L/O/Q]...${C.reset}`);
  });
}

// Keybindings handler
function setupKeybindings() {
  if (process.stdin.isTTY) {
    process.stdin.setRawMode(true);
    process.stdin.resume();
    process.stdin.setEncoding('utf8');

    process.stdin.on('data', (key) => {
      // Ctrl+C or 'q' or 'Q'
      if (key === '\u0003' || key.toLowerCase() === 'q') {
        console.log(`\n${C.yellow}Shutting down DeNovoAutomata...${C.reset}`);
        if (devProcess) {
          devProcess.kill();
        }
        process.exit(0);
      }

      switch (key.toLowerCase()) {
        case 'r':
          console.log(`\n${C.cyan}↻ Restarting servers...${C.reset}`);
          if (devProcess) {
            devProcess.kill();
          }
          setTimeout(() => {
            renderBanner();
            startDevServer();
          }, 600);
          break;

        case 't':
          runSmokeTest();
          break;

        case 'c':
          runClean();
          break;

        case 'l':
          console.log(`\n${C.blue}📜 Diagnostic Log Stream: http://localhost:3000/api/logs${C.reset}`);
          break;

        case 'o':
          console.log(`\n${C.green}🌐 DevUI URL: http://localhost:3000${C.reset}`);
          break;
      }
    });
  }
}

// Main execution
renderBanner();
setupKeybindings();

// Gentle background helix animation
const animInterval = setInterval(() => {
  frameIdx++;
}, 800);

startDevServer();

process.on('SIGINT', () => {
  clearInterval(animInterval);
  if (devProcess) devProcess.kill();
  process.exit(0);
});
