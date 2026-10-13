using Microsoft.AspNetCore.Mvc;
using DNAPlatform.DevUI.API.Services;

namespace DNAPlatform.DevUI.API.Controllers;

/// <summary>
/// Local execution-environment introspection for the workflow Configure view
/// (docs/refactor.md section 4). Pure backend: there is no SvelteKit
/// /api/environment route, so these are proxied from vite straight to :5254.
///
/// The service behind these endpoints degrades gracefully, so a missing or hung
/// mise produces a 200 with a "yellow" status rather than a 5xx.
/// </summary>
[ApiController]
[Route("api/environment")]
[Produces("application/json")]
public class EnvironmentController : ControllerBase
{
    private readonly IMiseService _miseService;
    private readonly ILogger<EnvironmentController> _logger;

    public EnvironmentController(
        IMiseService miseService,
        ILogger<EnvironmentController> logger)
    {
        _miseService = miseService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/environment - Current runtimes, mise availability, and per-dependency
    /// GREEN/YELLOW/RED status. Always 200, even when mise is not installed.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        return Ok(await _miseService.GetStatusAsync(ct));
    }

    /// <summary>
    /// POST /api/environment/mise/export - Export the project mise.toml.
    /// Returns 404 when the project has no mise.toml.
    /// </summary>
    [HttpPost("mise/export")]
    public async Task<IActionResult> ExportMise(CancellationToken ct)
    {
        var content = await _miseService.ExportConfigurationAsync(ct);
        if (string.IsNullOrWhiteSpace(content))
        {
            return NotFound(new { Message = "No mise.toml found for this project." });
        }

        return Ok(new { Content = content });
    }

    /// <summary>
    /// POST /api/environment/mise/import - Write a mise.toml into the project root.
    /// Returns 400 for empty, null-byte-bearing, or oversized (>256 KB) content.
    /// </summary>
    [HttpPost("mise/import")]
    public async Task<IActionResult> ImportMise([FromBody] MiseImportRequest? request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { Success = false, Error = "A non-empty 'content' field is required." });
        }

        var result = await _miseService.ImportConfigurationAsync(request.Content, ct);
        if (!result.Success)
        {
            return BadRequest(new { Success = false, Error = result.Message });
        }

        _logger.LogInformation("mise configuration imported to {Path}", result.Path);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/environment/verify - Re-run every check and return fresh status.
    /// Same payload as GET /api/environment; kept separate so the Configure view can
    /// poll it without a cache.
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(CancellationToken ct)
    {
        _logger.LogInformation("Environment verification requested");
        return Ok(await _miseService.GetStatusAsync(ct));
    }
}

/// <summary>Payload accepted by <c>POST /api/environment/mise/import</c>.</summary>
public class MiseImportRequest
{
    /// <summary>Full mise.toml content to write.</summary>
    public string Content { get; set; } = string.Empty;
}
