using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// Logs every HTTP request/response for <c>/api/*</c> and <c>/health</c>.
/// Writing through <see cref="ILogger"/> is enough for BOTH destinations:
/// the console provider prints it to the CLI and <c>LogStoreLoggerProvider</c>
/// mirrors the same record into the in-memory buffer served by <c>/api/logs</c>.
/// Static asset / swagger traffic is skipped to keep the log window useful.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip noisy assets so the log window stays useful.
        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var stopwatch = Stopwatch.StartNew();

        // IMPORTANT: log ONLY through ILogger. The LogStoreLoggerProvider mirrors every
        // ILogger record into ILogStore, so calling _logStore.Add(...) as well would
        // duplicate each line in the web viewer.
        _logger.LogInformation("→ {Method} {Path}{Query}", method, path, query);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                " {Method} {Path} threw after {Elapsed}ms",
                method, path, stopwatch.ElapsedMilliseconds);
            throw;
        }

        stopwatch.Stop();
        var status = context.Response.StatusCode;
        var elapsed = stopwatch.ElapsedMilliseconds;

        if (status >= 500)
        {
            _logger.LogError(" {Status} {Method} {Path} ({Elapsed}ms)", status, method, path, elapsed);
        }
        else if (status >= 400)
        {
            _logger.LogWarning("⚠ {Status} {Method} {Path} ({Elapsed}ms)", status, method, path, elapsed);
        }
        else
        {
            _logger.LogInformation("← {Status} {Method} {Path} ({Elapsed}ms)", status, method, path, elapsed);
        }
    }

    private static bool ShouldSkip(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        // The web log viewer polls /api/logs on a timer. Logging those polls would
        // create a self-feeding loop that floods the buffer, so skip them entirely.
        if (path.StartsWith("/api/logs", StringComparison.OrdinalIgnoreCase)) return true;

        // Always log API + health traffic.
        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }
}