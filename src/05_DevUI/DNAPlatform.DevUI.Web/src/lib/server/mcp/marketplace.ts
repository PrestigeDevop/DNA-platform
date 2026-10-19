import { mcpRegistry } from '$lib/server/mcp/registry';

export interface MarketplacePlugin {
  id: string;
  name: string;
  author: string;
  version: string;
  description: string;
  category: 'Extensions' | 'Agents' | 'Smart Components' | 'Bioinformatics' | 'AI & Inference' | 'System' | 'Polyglot' | 'Storage & I/O';
  icon: string;
  installed: boolean;
  featured: boolean;
  tags: string[];
  toolsCount: number;
  agentTemplate?: {
    mode: string;
    description: string;
    options: Record<string, any>;
    requirements: Record<string, any>;
    systemPrompt: string;
  };
}

class MarketplaceService {
  private customPlugins: MarketplacePlugin[] = [];
  private installedIds = new Set<string>([
    'csharp-runtime',
    'microsoft-copilot-onnx',
    'dna-bioinformatics',
    'filesystem-workspace',
    'system-runtime',
    'notebook-data-agent',
    'dna-smart-component',
    'polyglot-jupyter-kernel',
    'genomics-variant-extension'
  ]);

  publishPlugin(plugin: MarketplacePlugin, toolDefinitions?: any[]): boolean {
    const existingIdx = this.customPlugins.findIndex(p => p.id === plugin.id);
    if (existingIdx !== -1) {
      this.customPlugins[existingIdx] = plugin;
    } else {
      this.customPlugins.unshift(plugin);
    }
    this.installedIds.add(plugin.id);

    // Register active local MCP Server if tools are defined
    if (toolDefinitions && toolDefinitions.length > 0) {
      mcpRegistry.register({
        serverId: plugin.id,
        name: plugin.name,
        version: plugin.version || '1.0.0',
        status: 'Running',
        enabled: true,
        description: plugin.description,
        category: plugin.category,
        tools: toolDefinitions.map(t => ({
          name: t.name,
          description: t.description || `Tool ${t.name}`,
          role: t.role || 'worker',
          enabled: true,
          inputSchema: {
            type: 'object',
            properties: (t.parameters || []).reduce((acc: any, p: any) => {
              acc[p.name] = { type: p.type || 'string', description: p.description };
              return acc;
            }, {})
          }
        })),
        executeTool: async (toolName, args) => {
          return {
            status: 'Executed on local server',
            tool: toolName,
            args,
            timestamp: new Date().toISOString()
          };
        }
      });
    }

    return true;
  }

  getPlugins(): MarketplacePlugin[] {
    const mcpServers = mcpRegistry.listServers();

    return [
      ...this.customPlugins,
      {
        id: 'csharp-runtime',
        name: 'C# .NET 8 Runtime & Time Provider',
        author: 'Microsoft / .NET Core',
        version: '8.0.100',
        description: 'Native C# DateTime.UtcNow provider, data transformations, and sequence validation.',
        category: 'System',
        icon: '⏱️',
        installed: this.installedIds.has('csharp-runtime'),
        featured: true,
        tags: ['csharp', 'dotnet', 'datetime', 'skills'],
        toolsCount: mcpServers.find((s) => s.serverId === 'csharp-runtime')?.tools.length || 5
      },
      {
        id: 'genomics-variant-extension',
        name: 'Genomic Variant Caller Extension',
        author: 'DNA Platform Community',
        version: '1.4.0',
        description: 'Fast single-nucleotide polymorphism (SNP) and indel annotation pipeline extension.',
        category: 'Extensions',
        icon: '🔬',
        installed: this.installedIds.has('genomics-variant-extension'),
        featured: true,
        tags: ['extensions', 'snp', 'variant', 'vcf', 'annotation'],
        toolsCount: 3
      },
      {
        id: 'microsoft-copilot-onnx',
        name: 'Microsoft Copilot ONNX Engine',
        author: 'Microsoft / DNA Platform',
        version: '1.2.0',
        description: 'Local neural inference runtime with ONNX session execution, token generation, and Copilot reasoning.',
        category: 'AI & Inference',
        icon: '🤖',
        installed: this.installedIds.has('microsoft-copilot-onnx'),
        featured: true,
        tags: ['onnx', 'copilot', 'local-inference', 'neural'],
        toolsCount: mcpServers.find((s) => s.serverId === 'microsoft-copilot-onnx')?.tools.length || 2
      },
      {
        id: 'dna-bioinformatics',
        name: 'Genomic & DNA Analysis Suite',
        author: 'DNA Platform BioGroup',
        version: '2.1.0',
        description: 'Comprehensive bioinformatic tools for global pairwise alignment, reverse complement, GC skew, and codon translation.',
        category: 'Bioinformatics',
        icon: '🧬',
        installed: this.installedIds.has('dna-bioinformatics'),
        featured: true,
        tags: ['genomics', 'fasta', 'alignment', 'bioinformatics'],
        toolsCount: mcpServers.find((s) => s.serverId === 'dna-bioinformatics')?.tools.length || 5
      },
      {
        id: 'notebook-data-agent',
        name: 'Notebook Data Analysis Agent',
        author: 'DNA Platform Data Studio',
        version: '1.0.0',
        description: 'Notebook-first data analysis agent. Iteratively executes workflow analysis cells and aggregates outputs.',
        category: 'Agents',
        icon: '📊',
        installed: this.installedIds.has('notebook-data-agent'),
        featured: true,
        tags: ['analysis', 'jupyter', 'agent', 'notebook'],
        toolsCount: 3,
        agentTemplate: {
          mode: 'primary',
          description: 'Run notebook-first data analysis by appending and executing cells for each request.',
          options: { displayName: 'Data Analysis', id: 'data' },
          requirements: {
            skills: ['data-transform', 'validation', 'csharp-current-time'],
            mcp_servers: ['csharp-runtime', 'dna-bioinformatics', 'filesystem-workspace']
          },
          systemPrompt:
            'You are an expert notebook data analysis agent. Use workflow nodes and data cells as your active working surface. Keep substantive data work and supporting evidence in your output.'
        }
      },
      {
        id: 'dna-smart-component',
        name: 'Genomic StateMachine Smart Component',
        author: 'DNA Platform Community',
        version: '1.1.0',
        description: 'Logic Component for state machine validation and layered sequence operations.',
        category: 'Smart Components',
        icon: '🧩',
        installed: this.installedIds.has('dna-smart-component'),
        featured: true,
        tags: ['statemachine', 'ecs', 'smart-components', 'logic-component'],
        toolsCount: 2
      },
      {
        id: 'polyglot-jupyter-kernel',
        name: 'Polyglot Jupyter Kernel Engine',
        author: '.NET Interactive & Jupyter',
        version: '2.0.0',
        description: 'Interactive notebook execution runner supporting C#, Python, JavaScript/Node.js, and Bash.',
        category: 'Polyglot',
        icon: '📓',
        installed: this.installedIds.has('polyglot-jupyter-kernel'),
        featured: true,
        tags: ['polyglot', 'jupyter', 'notebook', 'csharp', 'python'],
        toolsCount: 4
      },
      {
        id: 'filesystem-workspace',
        name: 'Workspace File System',
        author: 'DNA Platform Core',
        version: '1.0.0',
        description: 'Read and inspect files, list directories, and manage pipeline artifacts securely in the applet.',
        category: 'Storage & I/O',
        icon: '📁',
        installed: this.installedIds.has('filesystem-workspace'),
        featured: false,
        tags: ['fs', 'workspace', 'io'],
        toolsCount: mcpServers.find((s) => s.serverId === 'filesystem-workspace')?.tools.length || 2
      },
      {
        id: 'system-runtime',
        name: 'Platform Runtime Monitor',
        author: 'System Operations',
        version: '1.0.0',
        description: 'Live host metrics, memory allocation tracking, Node.js process diagnostics, and uptime probing.',
        category: 'System',
        icon: '⚡',
        installed: this.installedIds.has('system-runtime'),
        featured: false,
        tags: ['metrics', 'diagnostics', 'health'],
        toolsCount: mcpServers.find((s) => s.serverId === 'system-runtime')?.tools.length || 1
      }
    ];
  }

  install(id: string): boolean {
    this.installedIds.add(id);
    return true;
  }

  uninstall(id: string): boolean {
    this.installedIds.delete(id);
    return true;
  }

  isInstalled(id: string): boolean {
    return this.installedIds.has(id);
  }
}

const globalForMarketplace = globalThis as unknown as {
  marketPlaceService: MarketplaceService | undefined;
};

export const marketPlaceService = globalForMarketplace.marketPlaceService ?? new MarketplaceService();

if (process.env.NODE_ENV !== 'production') {
  globalForMarketplace.marketPlaceService = marketPlaceService;
}
