using Microsoft.AspNetCore.Mvc;
using DNAPlatform.DevUI.API.Services;

namespace DNAPlatform.DevUI.API.Controllers;

/// <summary>
/// Exposes the in-memory backend log buffer so the web UI can show the very same
/// lines that appear in the CLI console. Supports incremental polling via <c>sinceId</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly ILogStore _logStore;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogStore logStore, ILogger<LogsController> logger)
    {
        _logStore = logStore;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/logs?sinceId=0&amp;limit=500 - Return buffered backend log entries.
    /// </summary>
    [HttpGet]
    public IActionResult Get([FromQuery] long sinceId = 0, [FromQuery] int limit = 500)
    {
        var entries = _logStore.Get(sinceId, limit);

        return Ok(new
        {
            Count = entries.Count,
            TotalBuffered = _logStore.Count,
            LastId = entries.Count > 0 ? entries[^1].Id : sinceId,
            Logs = entries
        });
    }

    /// <summary>
    /// DELETE /api/logs - Clear the in-memory log buffer.
    /// </summary>
    [HttpDelete]
    public IActionResult Clear()
    {
        var removed = _logStore.Count;
        _logStore.Clear();

        _logger.LogInformation("Log buffer cleared ({Count} entries removed)", removed);

        return Ok(new { Success = true, Removed = removed });
    }

    /// <summary>
    /// POST /api/logs - Accept a log entry pushed from the SvelteKit frontend so both
    /// streams appear in one unified viewer.
    /// </summary>
    [HttpPost]
    public IActionResult Post([FromBody] FrontendLogRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Success = false, Error = "A non-empty 'message' is required." });
        }

        var entry = new LogEntry
        {
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Level = string.IsNullOrWhiteSpace(request.Level) ? "Information" : request.Level!,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Frontend" : request.Category!,
            Message = request.Message,
            Exception = request.Exception,
            Source = "frontend"
        };

        _logStore.Add(entry);

        // Mirror to console so frontend activity shows up in the CLI too.
        _logger.LogInformation("[FRONTEND] {Message}", entry.Message);

        return Ok(new { Success = true, Id = entry.Id });
    }
}

/// <summary>Payload accepted by <c>POST /api/logs</c>.</summary>
public class FrontendLogRequest
{
    public string? Level { get; set; }
    public string? Category { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public DateTime? Timestamp { get; set; }
}