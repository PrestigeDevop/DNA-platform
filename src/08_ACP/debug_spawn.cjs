const { spawn } = require('node:child_process');
const { join } = require('node:path');
const tsxPath = join(__dirname, 'node_modules', '.bin', 'tsx.cmd');
console.log('tsxPath:', tsxPath);
const proc = spawn('cmd', ['/c', tsxPath, 'src/index.ts'], {
  stdio: ['pipe', 'pipe', 'pipe'],
  env: { ...process.env, DNA_PLATFORM_API_URL: 'http://127.0.0.1:5254' },
});
proc.stderr.on('data', (d) => console.error('[STDERR]', d.toString()));
proc.stdout.on('data', (d) => console.log('[STDOUT]', d.toString()));
proc.on('exit', (code) => console.log('Exit code:', code));
proc.on('error', (err) => console.error('Error:', err));
proc.stdin.write('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":1}}\n');
proc.stdin.end();
setTimeout(() => proc.kill(), 5000);
