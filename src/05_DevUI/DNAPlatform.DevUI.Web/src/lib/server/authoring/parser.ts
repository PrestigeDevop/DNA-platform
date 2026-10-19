export interface ParsedTool {
  name: string;
  description: string;
  role: 'worker' | 'evaluator' | 'system' | 'assistant';
  parameters: Array<{ name: string; type: string; description?: string }>;
}

export interface ParsedProperty {
  name: string;
  type: 'enum' | 'vectordb' | 'number' | 'boolean' | 'string' | 'custom';
  description?: string;
  defaultValue?: any;
  options?: string[]; // For enum
  vectorDbConfig?: {
    provider: 'pgvector' | 'chroma' | 'qdrant';
    collection: string;
    dimension: number;
    connectionUri: string;
  };
}

export interface MiseRequirements {
  runtimes: Record<string, string>;
  dependencies: {
    nuget?: string[];
    pip?: string[];
    npm?: string[];
  };
  miseToml: string;
}

export interface ParseResult {
  componentName: string;
  category: string;
  language: string;
  isMiddleware: boolean;
  tools: ParsedTool[];
  properties: ParsedProperty[];
  mise: MiseRequirements;
  inputs: Array<{ name: string; type: string }>;
  outputs: Array<{ name: string; type: string }>;
}

export function parseComponentCode(code: string, language: string): ParseResult {
  const lang = (language || 'csharp').toLowerCase();
  
  if (lang === 'csharp' || lang === 'c#') {
    return parseCSharpCode(code);
  } else if (lang === 'python' || lang === 'py') {
    return parsePythonCode(code);
  } else {
    return parseJavaScriptCode(code);
  }
}

function parseCSharpCode(code: string): ParseResult {
  const tools: ParsedTool[] = [];
  const properties: ParsedProperty[] = [];
  const nugetPackages: string[] = [];
  const runtimes: Record<string, string> = { dotnet: '8.0' };

  // Component Name & Category
  let componentName = 'CustomCSharpLogic';
  const classMatch = code.match(/public\s+class\s+([A-Za-z0-9_]+)/);
  if (classMatch) componentName = classMatch[1];

  let category = 'Logic Component';
  const catMatch = code.match(/\[Category\("([^"]+)"\)\]/);
  if (catMatch) category = catMatch[1];

  // Check for Microsoft Agent Framework Middleware
  const isMiddleware = code.includes('IAgentMiddleware') || code.includes('AgentMiddleware');
  if (isMiddleware) {
    nugetPackages.push('Microsoft.Agents.Core', 'Microsoft.Agents.Protocols');
  }

  // Parse [Tool("name", "description")]
  const toolRegex = /\[Tool\("([^"]+)"(?:,\s*"([^"]+)")?\)\]\s*(?:public\s+[^\(]+\s+([A-Za-z0-9_]+)\s*\(([^\)]*)\))?/g;
  let tMatch;
  while ((tMatch = toolRegex.exec(code)) !== null) {
    const tName = tMatch[1];
    const tDesc = tMatch[2] || `Tool ${tName}`;
    const paramsStr = tMatch[4] || '';
    const params = paramsStr.split(',').filter(Boolean).map(p => {
      const parts = p.trim().split(/\s+/);
      return { name: parts[parts.length - 1] || 'param', type: parts[0] || 'string' };
    });

    tools.push({
      name: tName,
      description: tDesc,
      role: tName.includes('validate') ? 'evaluator' : 'worker',
      parameters: params
    });
  }

  // Parse Enum definitions
  const enumRegex = /public\s+enum\s+([A-Za-z0-9_]+)\s*\{([^}]+)\}/g;
  const declaredEnums = new Map<string, string[]>();
  let eMatch;
  while ((eMatch = enumRegex.exec(code)) !== null) {
    const eName = eMatch[1];
    const members = eMatch[2].split(',').map(m => m.trim().split('=')[0].trim()).filter(Boolean);
    declaredEnums.set(eName, members);
  }

  // Parse Properties: public <Type> <Name> { get; set; } = <Default>;
  const propRegex = /(?:\[Description\("([^"]+)"\)\]\s*)?public\s+([A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\{\s*get;\s*set;\s*\}\s*(?:=\s*([^;]+);)?/g;
  let pMatch;
  while ((pMatch = propRegex.exec(code)) !== null) {
    const pDesc = pMatch[1];
    const pType = pMatch[2];
    const pName = pMatch[3];
    const pDefaultRaw = pMatch[4]?.trim();

    // Check if property is VectorDB / pgvector
    if (pName.toLowerCase().includes('vectordb') || pName.toLowerCase().includes('embedding') || pType.toLowerCase().includes('vector')) {
      properties.push({
        name: pName,
        type: 'vectordb',
        description: pDesc || 'Vector Database connection and collection schema (default pgvector)',
        defaultValue: 'pgvector_default',
        vectorDbConfig: {
          provider: 'pgvector',
          collection: 'genomic_embeddings',
          dimension: 1536,
          connectionUri: 'postgresql://postgres:postgres@localhost:5432/dnaplatform'
        }
      });
      nugetPackages.push('Pgvector', 'Npgsql');
      continue;
    }

    // Check if property is an Enum
    if (declaredEnums.has(pType)) {
      const options = declaredEnums.get(pType) || [];
      properties.push({
        name: pName,
        type: 'enum',
        description: pDesc || `Enum selection for ${pName}`,
        options,
        defaultValue: pDefaultRaw ? pDefaultRaw.replace(/^[^.]+\./, '') : options[0]
      });
      continue;
    }

    // Standard types
    let propType: ParsedProperty['type'] = 'string';
    let defVal: any = pDefaultRaw;

    if (pType === 'bool' || pType === 'Boolean') {
      propType = 'boolean';
      defVal = pDefaultRaw === 'true';
    } else if (pType === 'double' || pType === 'float' || pType === 'int' || pType === 'Int32') {
      propType = 'number';
      defVal = pDefaultRaw ? parseFloat(pDefaultRaw) : 0;
    }

    properties.push({
      name: pName,
      type: propType,
      description: pDesc,
      defaultValue: defVal
    });
  }

  // Check nuget packages in code
  const nugetRegex = /#r\s+"nuget:\s*([^,"]+)/g;
  let nuMatch;
  while ((nuMatch = nugetRegex.exec(code)) !== null) {
    nugetPackages.push(nuMatch[1].trim());
  }

  // Mise TOML generation
  const miseToml = generateMiseToml(runtimes, { nuget: [...new Set(nugetPackages)] });

  return {
    componentName,
    category,
    language: 'csharp',
    isMiddleware,
    tools,
    properties,
    mise: {
      runtimes,
      dependencies: { nuget: [...new Set(nugetPackages)] },
      miseToml
    },
    inputs: [{ name: 'input', type: 'DNA_Sequence' }],
    outputs: [{ name: 'output', type: 'JSON' }]
  };
}

function parsePythonCode(code: string): ParseResult {
  const tools: ParsedTool[] = [];
  const properties: ParsedProperty[] = [];
  const pipPackages: string[] = ['csnakes'];
  const runtimes: Record<string, string> = { python: '3.11' };

  let componentName = 'CustomPythonModule';
  const classMatch = code.match(/class\s+([A-Za-z0-9_]+)/);
  if (classMatch) componentName = classMatch[1];

  // Detect pip packages
  if (code.includes('Bio') || code.includes('SeqIO') || code.includes('pairwise2')) pipPackages.push('biopython');
  if (code.includes('numpy') || code.includes('np.')) pipPackages.push('numpy');
  if (code.includes('pandas') || code.includes('pd.')) pipPackages.push('pandas');
  if (code.includes('pgvector') || code.includes('psycopg')) pipPackages.push('pgvector', 'psycopg2-binary');

  // Parse @tool decorator: @tool(name="...", description="...")
  const toolRegex = /@tool(?:\((?:name="([^"]+)")?(?:,\s*description="([^"]+)")?\))?\s*\n\s*def\s+([A-Za-z0-9_]+)\s*\(([^\)]*)\)/g;
  let tMatch;
  while ((tMatch = toolRegex.exec(code)) !== null) {
    const tName = tMatch[1] || tMatch[3];
    const tDesc = tMatch[2] || `Python tool ${tName}`;
    const paramsStr = tMatch[4] || '';
    const params = paramsStr.split(',').filter(Boolean).map(p => {
      const parts = p.trim().split(':');
      return { name: parts[0].trim(), type: parts[1]?.trim() || 'str' };
    }).filter(p => p.name !== 'self');

    tools.push({
      name: tName,
      description: tDesc,
      role: tName.includes('validate') ? 'evaluator' : 'worker',
      parameters: params
    });
  }

  // Parse VectorDB default
  if (code.includes('vector') || code.includes('embedding') || code.includes('pgvector')) {
    properties.push({
      name: 'vectorStore',
      type: 'vectordb',
      description: 'Default pgvector instance for biological embeddings',
      defaultValue: 'pgvector_default',
      vectorDbConfig: {
        provider: 'pgvector',
        collection: 'genomic_embeddings',
        dimension: 1536,
        connectionUri: 'postgresql://postgres:postgres@localhost:5432/dnaplatform'
      }
    });
  }

  // Parse threshold / config variables: threshold: float = 0.85
  const propRegex = /([A-Za-z0-9_]+)\s*:\s*([A-Za-z0-9_]+)\s*=\s*([^#\n]+)/g;
  let pMatch;
  while ((pMatch = propRegex.exec(code)) !== null) {
    const pName = pMatch[1];
    const pType = pMatch[2].toLowerCase();
    const pVal = pMatch[3].trim();

    if (pName === 'self') continue;

    let propType: ParsedProperty['type'] = 'string';
    let defVal: any = pVal;

    if (pType === 'bool') {
      propType = 'boolean';
      defVal = pVal.toLowerCase() === 'true';
    } else if (pType === 'float' || pType === 'int') {
      propType = 'number';
      defVal = parseFloat(pVal);
    }

    properties.push({
      name: pName,
      type: propType,
      defaultValue: defVal
    });
  }

  const miseToml = generateMiseToml(runtimes, { pip: [...new Set(pipPackages)] });

  return {
    componentName,
    category: 'CSnakes & Python Modules',
    language: 'python',
    isMiddleware: code.includes('middleware') || code.includes('intercept'),
    tools,
    properties,
    mise: {
      runtimes,
      dependencies: { pip: [...new Set(pipPackages)] },
      miseToml
    },
    inputs: [{ name: 'sequence', type: 'DNA_Sequence' }],
    outputs: [{ name: 'result', type: 'JSON' }]
  };
}

function parseJavaScriptCode(code: string): ParseResult {
  const tools: ParsedTool[] = [];
  const properties: ParsedProperty[] = [];
  const npmPackages: string[] = [];
  const runtimes: Record<string, string> = { node: '20.10' };

  let componentName = 'CustomJsLogic';
  const classMatch = code.match(/class\s+([A-Za-z0-9_]+)/);
  if (classMatch) componentName = classMatch[1];

  // Parse JSDoc @tool or decorator
  const jsdocToolRegex = /\/\*\*[\s\S]*?@tool\s+([A-Za-z0-9_-]+)(?:\s*-\s*([^\n*]+))?[\s\S]*?\*\/\s*(?:async\s+)?function\s+([A-Za-z0-9_]+)?/g;
  let jMatch;
  while ((jMatch = jsdocToolRegex.exec(code)) !== null) {
    const tName = jMatch[1];
    const tDesc = jMatch[2]?.trim() || `JS Tool ${tName}`;
    tools.push({
      name: tName,
      description: tDesc,
      role: 'worker',
      parameters: [{ name: 'input', type: 'any' }]
    });
  }

  // Parse pgvector or Vector DB
  if (code.includes('vector') || code.includes('pgvector')) {
    properties.push({
      name: 'vectorDb',
      type: 'vectordb',
      defaultValue: 'pgvector_default',
      vectorDbConfig: {
        provider: 'pgvector',
        collection: 'genomic_embeddings',
        dimension: 1536,
        connectionUri: 'postgresql://postgres:postgres@localhost:5432/dnaplatform'
      }
    });
    npmPackages.push('pg', 'pgvector');
  }

  const isMiddleware = code.includes('middleware') || code.includes('next()');

  const miseToml = generateMiseToml(runtimes, { npm: [...new Set(npmPackages)] });

  return {
    componentName,
    category: 'Node.js Extensions',
    language: 'javascript',
    isMiddleware,
    tools,
    properties,
    mise: {
      runtimes,
      dependencies: { npm: [...new Set(npmPackages)] },
      miseToml
    },
    inputs: [{ name: 'input', type: 'JSON' }],
    outputs: [{ name: 'output', type: 'JSON' }]
  };
}

function generateMiseToml(runtimes: Record<string, string>, deps: Record<string, string[]>): string {
  let toml = '# Generated by DNA Platform Extension Studio (.mise.toml)\n[tools]\n';
  for (const [k, v] of Object.entries(runtimes)) {
    toml += `${k} = "${v}"\n`;
  }

  if (deps.pip && deps.pip.length > 0) {
    toml += '\n# Python Dependencies (pip via CSnakes)\n# pip install ' + deps.pip.join(' ') + '\n';
  }
  if (deps.nuget && deps.nuget.length > 0) {
    toml += '\n# .NET Packages (nuget)\n# dotnet add package ' + deps.nuget.join(' ') + '\n';
  }
  if (deps.npm && deps.npm.length > 0) {
    toml += '\n# Node.js Packages (npm)\n# npm install ' + deps.npm.join(' ') + '\n';
  }

  return toml;
}
