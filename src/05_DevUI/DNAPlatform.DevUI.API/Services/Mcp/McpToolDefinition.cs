using System.Text.Json;

namespace DNAPlatform.DevUI.API.Services.Mcp;

/// <summary>
/// Definition of a single MCP tool, mirroring the JSON schema returned by tools/list.
/// </summary>
public class McpToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement InputSchema { get; set; }
}
