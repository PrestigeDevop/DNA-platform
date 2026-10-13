using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DNAPlatform.DevUI.API.Services.Mcp;

namespace DNAPlatform.DevUI.API.Controllers;

/// <summary>
/// REST surface for MCP plugin discovery. The discovery itself lives in
/// McpServerDiscovery (a hosted service); this controller is the only thing the
/// SvelteKit frontend talks to, so GET /api/mcp/servers and POST
/// /api/mcp/servers/import are the two buttons wired in McpPluginShell.svelte.
/// </summary>
[ApiController]
[Route("api/mcp/servers")]
[Produces("application/json")]
public class McpServersController : ControllerBase
{
    private readonly McpServerDiscovery _discovery;
    private readonly ILogger<McpServersController> _logger;

    public McpServersController(
        McpServerDiscovery discovery,
        ILogger<McpServersController> logger)
    {
        _discovery = discovery;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/mcp/servers — every discovered server with its runtime status and
    /// tool catalogue. Status is one of Pending / Running / Error / Stopped.
    /// </summary>
    [HttpGet]
    public ActionResult GetServers()
    {
        var servers = _discovery.Servers.Values
            .Select(s => new
            {
                s.ServerId,
                s.Name,
                s.Version,
                Status = _discovery.Statuses.TryGetValue(s.ServerId, out var st) ? st.ToString() : "Pending",
                Message = _discovery.StatusMessages.TryGetValue(s.ServerId, out var msg) ? msg : null,
                Tools = s.Tools.Select(t => new
                {
                    t.Name,
                    t.Description,
                    InputSchema = t.InputSchema.ValueKind == System.Text.Json.JsonValueKind.Object
                        ? t.InputSchema.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString())
                        : null
                }).ToList()
            })
            .ToList();

        return Ok(new { Count = servers.Count, Servers = servers });
    }

    /// <summary>
    /// POST /api/mcp/servers/import — accept a server manifest JSON body, write it to
    /// the manifest directory and discover it. Returns the discovered server (or a
    /// 400 with an error body when the manifest is missing a command or the server
    /// fails to handshake).
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] string? manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            return BadRequest(new { Error = "Manifest JSON body is required." });
        }

        try
        {
            var server = await _discovery.ImportManifestAsync(manifestJson);
            if (server == null)
            {
                return BadRequest(new
                {
                    Error = "Failed to import MCP server. Check that the manifest has a 'command' field and the server responds to the MCP initialize handshake."
                });
            }

            return Ok(new
            {
                server.ServerId,
                server.Name,
                server.Version,
                Status = _discovery.Statuses.TryGetValue(server.ServerId, out var st) ? st.ToString() : "Pending",
                Tools = server.Tools.Select(t => new { t.Name, t.Description }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing MCP server manifest");
            return StatusCode(500, new { Error = "Internal error importing MCP server manifest." });
        }
    }
}


