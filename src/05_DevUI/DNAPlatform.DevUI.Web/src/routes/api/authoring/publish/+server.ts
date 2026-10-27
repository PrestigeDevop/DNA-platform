import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { marketPlaceService } from '$lib/server/mcp/marketplace';
import { logStore } from '$lib/server/logStore';
import fs from 'fs';
import path from 'path';

// POST /api/authoring/publish — Publish annotated component / middleware to local marketplace server
export const POST: RequestHandler = async ({ request }) => {
  try {
    const body = await request.json();
    const name = String(body.name || 'Custom Extension').trim();
    const category = body.category || 'Extensions';
    const language = body.language || 'csharp';
    const description = body.description || 'Custom authored logic component & tool suite';
    const code = body.code || '';
    const tools = Array.isArray(body.tools) ? body.tools : [];
    const properties = Array.isArray(body.properties) ? body.properties : [];
    const mise = body.mise || {};
    const isMiddleware = Boolean(body.isMiddleware);

    const pluginId = `ext-${name.toLowerCase().replace(/[^a-z0-9]/g, '-')}-${Date.now().toString(36).slice(-4)}`;

    const plugin = {
      id: pluginId,
      name,
      author: 'Local Developer',
      version: '1.0.0',
      description,
      category: (category as any) || 'Extensions',
      icon: isMiddleware ? '🛡️' : language === 'python' ? '🐍' : language === 'csharp' ? '⚡' : '🟨',
      installed: true,
      featured: false,
      tags: [language, isMiddleware ? 'middleware' : 'tool', ...tools.map((t: any) => t.name)],
      toolsCount: tools.length
    };

    // 1. Register with in-memory & local marketplace service and MCP Registry
    marketPlaceService.publishPlugin(plugin, tools);

    // 2. Persist extension to disk in ./workspace/extensions/
    try {
      const workspaceExtDir = path.resolve(process.cwd(), 'workspace', 'extensions');
      if (!fs.existsSync(workspaceExtDir)) {
        fs.mkdirSync(workspaceExtDir, { recursive: true });
      }
      const extFile = path.resolve(workspaceExtDir, `${pluginId}.json`);
      fs.writeFileSync(extFile, JSON.stringify({
        ...plugin,
        code,
        tools,
        properties,
        mise,
        publishedAt: new Date().toISOString()
      }, null, 2), 'utf-8');

      // Also write .mise.toml companion file
      if (mise.miseToml) {
        const miseFile = path.resolve(workspaceExtDir, `${pluginId}.mise.toml`);
        fs.writeFileSync(miseFile, mise.miseToml, 'utf-8');
      }
    } catch (fsErr) {
      console.warn('Could not persist extension to workspace disk:', fsErr);
    }

    logStore.add({
      timestamp: new Date().toISOString(),
      level: 'Information',
      category: 'Marketplace',
      message: `[Local Marketplace] Published component/middleware "${name}" (${pluginId}) with ${tools.length} tool(s)`,
      source: 'backend'
    });

    return json({
      success: true,
      pluginId,
      name,
      toolsCount: tools.length,
      isMiddleware,
      miseToml: mise.miseToml || '',
      status: 'Published to local server marketplace & registered with runtime'
    });
  } catch (err: any) {
    return json({
      success: false,
      error: err.message || 'Failed to publish component'
    }, { status: 500 });
  }
};
