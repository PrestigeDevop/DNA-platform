import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import fs from 'fs';
import path from 'path';

// GET /api/polyglot/notebook — Loads DryLab_Poc_.ipynb or returns starter polyglot notebook
export const GET: RequestHandler = async () => {
  try {
    const root = process.cwd();
    // Try multiple possible paths to DryLab_Poc_.ipynb
    const pathsToTry = [
      path.resolve(root, 'DryLab_Poc_.ipynb'),
      path.resolve(root, '../../DryLab_Poc_.ipynb'),
      path.resolve(root, '../../../DryLab_Poc_.ipynb'),
      '/app/applet/DryLab_Poc_.ipynb'
    ];

    for (const p of pathsToTry) {
      if (fs.existsSync(p)) {
        const raw = fs.readFileSync(p, 'utf-8');
        try {
          const parsed = JSON.parse(raw);
          const cells = (parsed.cells || []).map((c: any, idx: number) => ({
            id: `cell-${idx + 1}`,
            cell_type: c.cell_type || 'code',
            language: c.source?.join('')?.includes('pip') || c.source?.join('')?.includes('import') ? 'python' : 'csharp',
            source: Array.isArray(c.source) ? c.source.join('') : String(c.source || ''),
            outputs: c.outputs || []
          }));
          return json({
            title: 'DryLab_Poc_.ipynb (Jupyter Notebook)',
            kernel: '.NET Interactive / Polyglot',
            cells: cells.slice(0, 15)
          });
        } catch {
          // fall through
        }
      }
    }

    // Default starter cells if file not found
    return json({
      title: 'Polyglot Interactive DryLab Notebook',
      kernel: 'Polyglot (C# / Python / Node)',
      cells: [
        {
          id: 'cell-1',
          cell_type: 'code',
          language: 'csharp',
          source: '// C# DateTime Provider\nusing System;\nConsole.WriteLine($"Current UTC Time: {DateTime.UtcNow:o}");\nConsole.WriteLine($"Framework: .NET 8.0 Invariant");',
          outputs: []
        },
        {
          id: 'cell-2',
          cell_type: 'code',
          language: 'python',
          source: '# Python BioInformatics Cell\nseq = "ATGGTGCACCTGACTCCTGAGGAGAAGTCTGCC"\ngc = (seq.count("G") + seq.count("C")) / len(seq)\nprint(f"DNA Sequence Length: {len(seq)} bp, GC Content: {gc*100:.1f}%")',
          outputs: []
        },
        {
          id: 'cell-3',
          cell_type: 'code',
          language: 'javascript',
          source: '// JavaScript / Node.js Engine\nconst { version } = process;\nconsole.log(`Node.js Runtime: ${version}`);\nconsole.log(`Memory usage: ${Math.round(process.memoryUsage().heapUsed / 1024 / 1024)} MB`);',
          outputs: []
        }
      ]
    });
  } catch (error: any) {
    return json({ error: error.message }, { status: 500 });
  }
};
