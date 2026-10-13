const { spawn } = require('node:child_process');
const { join } = require('node:path');
const tsxPath = join(__dirname, 'node_modules', '.bin', 'tsx.cmd');
const proc = spawn('cmd', ['/c', tsxPath, 'src/index.ts'], {
  stdio: ['pipe', 'pipe', 'pipe'],
  env: { ...process.env, DNA_PLATFORM_API_URL: 'http://127.0.0.1:5254' },
});
proc.stderr.on('data', (d) => console.error('[STDERR]', d.toString()));
proc.stdout.on('data', (d) => console.log('[STDOUT]', d.toString()));
proc.on('exit', (code) => console.log('Exit code:', code));
proc.on('error', (err) => console.error('Error:', err));

// Send initialize
proc.stdin.write('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":1}}\n');

// Wait a bit then send newSession
setTimeout(() => {
  proc.stdin.write('{"jsonrpc":"2.0","id":2,"method":"session/new","params":{"cwd":"C:\\\\Users\\\\prest","mcpServers":[]}}\n');
}, 100);

// Wait a bit then send prompt
setTimeout(() => {
  const workflow = { nodes: [], connections: [] };
  proc.stdin.write('{"jsonrpc":"2.0","id":3,"method":"session/prompt","params":{"sessionId":"test","prompt":[{"type":"text","text":' + JSON.stringify(JSON.stringify(workflow)) + '}]}}\n');
}, 200);

// Wait a bit then send cancel
setTimeout(() => {
  proc.stdin.write('{"jsonrpc":"2.0","method":"session/cancel","params":{"sessionId":"test"}}\n');
}, 300);

// Wait a bit then send another initialize
setTimeout(() => {
  proc.stdin.write('{"jsonrpc":"2.0","id":4,"method":"initialize","params":{"protocolVersion":1}}\n');
}, 400);

setTimeout(() => proc.stdin.end(), 500);
setTimeout(() => proc.kill(), 2000);
