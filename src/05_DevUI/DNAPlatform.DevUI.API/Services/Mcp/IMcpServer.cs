using System.Collections.Generic;

namespace DNAPlatform.DevUI.API.Services.Mcp;

/// <summary>
/// Represents a discovered MCP server with its tool catalogue.
/// </summary>
public interface IMcpServer
{
    string ServerId { get; }
    string Name { get; }
    string Version { get; }
    IReadOnlyList<McpToolDefinition> Tools { get; }
}
