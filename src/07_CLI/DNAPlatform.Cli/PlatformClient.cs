using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DNAPlatform.Cli;

/// <summary>
/// HTTP client for the platform (docs/refactor.md section 6).
///
/// <para>
/// This is the CLI's only way to reach the platform. It mirrors
/// <c>src/lib/services/api.ts</c> and <c>environment.ts</c> wire shapes exactly,
/// camelCase included — ASP.NET Core's default JSON naming is camelCase, so .NET
/// DTOs declared in PascalCase arrive as camelCase and must be read that way.
/// </para>
///
/// <para>
/// <b>Two base URLs.</b> Most of the platform's surface is served by SvelteKit
/// (<c>/api/workflows</c>, <c>/api/executions</c>, <c>/api/snippets</c>) and
/// reaches the browser only on port 5173; <c>/api/environment</c>,
/// <c>/api/skills</c> and <c>POST /api/executions/{id}/cancel</c> exist on the
/// .NET host on port 5254 and are never proxied for <c>cancel</c>. So
/// <see cref="BaseUrl"/> is the DevUI origin and <see cref="ApiBaseUrl"/> is the
/// .NET origin; only <c>cancel</c> needs the latter. Pass an explicit
/// <c>apiBaseUrl</c> (or <c>--url http://127.0.0.1:5254</c>) to talk to the
/// backend directly.
/// </para>
///
/// <para>
/// The client deliberately has no knowledge of the workflow engine: it does not
/// reference DNAPlatform.AgentFramework.
/// </para>
/// </summary>
public class PlatformClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly HttpClient _api;

    public PlatformClient(string baseUrl, string? apiBaseUrl = null)
    {
        // Trailing slashes would double up in every composed URI.
        BaseUrl = baseUrl.TrimEnd('/');
        ApiBaseUrl = (apiBaseUrl ?? baseUrl).TrimEnd('/');

        _http = CreateClient();

        // Only used when the two origins differ; sharing one client when they do
        // keeps connection pooling to a single place.
        _api = ReferenceEquals(ApiBaseUrl, BaseUrl) ? _http : CreateClient();
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient
        {
            // Long enough for a real workflow run; the /health poll pattern in
            // verify-stack.bat uses the same 90 s ceiling.
            Timeout = TimeSpan.FromSeconds(90)
        };

        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }

    public string BaseUrl { get; }

    /// <summary>The .NET origin, used for the routes SvelteKit does not expose.</summary>
    public string ApiBaseUrl { get; }

    /// <summary>
    /// Why the last reachability probe failed. Set alongside a false return so
    /// callers can report the real cause instead of a bare "unreachable" — the
    /// difference between "wrong port" and "TLS interception on localhost" is
    /// usually the only thing the user needs.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>True when the DevUI origin (<c>BaseUrl</c>) answers /health.</summary>
    public Task<bool> IsReachableAsync(CancellationToken ct = default)
        => ProbeAsync(_http, BaseUrl, ct);

    /// <summary>True when the .NET origin (<c>ApiBaseUrl</c>) answers /health.</summary>
    public Task<bool> IsApiReachableAsync(CancellationToken ct = default)
        => ProbeAsync(_api, ApiBaseUrl, ct);

    private async Task<bool> ProbeAsync(HttpClient client, string baseUrl, CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync($"{baseUrl}/health", ct);

            if (response.IsSuccessStatusCode)
            {
                LastError = null;
                return true;
            }

            LastError = $"{baseUrl}/health answered {(int)response.StatusCode} {response.ReasonPhrase}";
            return false;
        }
        catch (HttpRequestException ex)
        {
            LastError = $"{baseUrl}: {ex.Message}";
            return false;
        }
        catch (TaskCanceledException ex)
        {
            // TaskCanceledException also covers the HttpClient timeout, which is
            // worth distinguishing from a refused connection.
            LastError = ex.InnerException is { } inner ? $"{baseUrl}: {inner.Message}" : $"{baseUrl}: request timed out";
            return false;
        }
    }

    // ---------------------------------------------------------------- workflows

    public async Task<JsonElement> ListWorkflowsAsync(CancellationToken ct = default)
    {
        // SvelteKit envelope: { workflows: [...] }.
        return await GetJsonAsync("/api/workflows", ct);
    }

    public async Task<JsonElement> GetWorkflowAsync(string id, CancellationToken ct = default)
    {
        return await GetJsonAsync($"/api/workflows/{Uri.EscapeDataString(id)}", ct);
    }

    /// <summary>
    /// Runs the Test step. SvelteKit answers with <c>{ workflow }</c>; the proxy
    /// forwards to .NET's <c>POST /api/workflows/validate</c> which answers
    /// <c>{ isValid, errors, warnings, checks, summary }</c> directly.
    /// </summary>
    public async Task<JsonElement> ValidateWorkflowAsync(string id, CancellationToken ct = default)
    {
        var workflow = await GetWorkflowAsync(id, ct);

        if (!workflow.TryGetProperty("workflow", out var definition))
        {
            definition = workflow;
        }

        // Only name/nodes/connections matter to the validator; the envelope's other
        // fields are ignored by the endpoint.
        var payload = new
        {
            name = Str(definition, "name"),
            nodes = Raw(definition, "nodes") ?? new object[0],
            connections = Raw(definition, "connections") ?? new object[0]
        };

        return await PostJsonAsync("/api/workflows/validate", payload, ct);
    }

    public async Task<JsonElement> ExecuteWorkflowAsync(string id, CancellationToken ct = default)
    {
        return await PostJsonAsync($"/api/workflows/{Uri.EscapeDataString(id)}/execute", null, ct);
    }

    public async Task<JsonElement> ListExecutionsAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/api/executions", ct);
    }

    // ---------------------------------------------------------------- skills

    public async Task<JsonElement> ListSkillsAsync(CancellationToken ct = default)
    {
        return await GetJsonAsync("/api/skills", ct);
    }

    public async Task<JsonElement> GetSkillAsync(string skillId, CancellationToken ct = default)
    {
        // Unwrapped GetMetadata() object, per AGENTS.md.
        return await GetJsonAsync($"/api/skills/{Uri.EscapeDataString(skillId)}", ct);
    }

    // ---------------------------------------------------------------- environment

    /// <summary>
    /// Environment/mise introspection, exactly as EnvironmentConfigure.svelte reads it.
    ///
    /// <para>
    /// Pass an explicit <paramref name="origin"/> to read it from a specific host.
    /// The default is the DevUI origin, which proxies the route to .NET.
    /// </para>
    /// </summary>
    public async Task<JsonElement> GetEnvironmentAsync(string? origin = null, CancellationToken ct = default)
    {
        var (client, baseUrl) = Resolve(origin);
        return await GetJsonAsync(client, baseUrl, "/api/environment", ct);
    }

    // ---------------------------------------------------------------- logs

    public async Task<JsonElement> GetLogsAsync(long sinceId = 0, int limit = 200, CancellationToken ct = default)
    {
        // Proxied to .NET LogsController: { count, logs }.
        return await GetJsonAsync($"/api/logs?sinceId={sinceId}&limit={limit}", ct);
    }

    public async Task ClearLogsAsync(CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"{BaseUrl}/api/logs", ct);
        response.EnsureSuccessStatusCode();
    }

    // ---------------------------------------------------------------- execution control

    /// <summary>
    /// Requests cancellation. Sends to the .NET origin: the route lives on
    /// ExecutionsController and is not in the vite proxy list, so a request
    /// through port 5173 would be swallowed by SvelteKit's 404.
    ///
    /// <para>
    /// Returns the server's own JSON answer so the caller can echo its message
    /// rather than assert success. Note the controller answers 200 even for an id
    /// the orchestrator never saw, so the message is the only real signal.
    /// </para>
    /// </summary>
    public async Task<JsonElement> CancelExecutionAsync(string executionId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{ApiBaseUrl}/api/executions/{Uri.EscapeDataString(executionId)}/cancel")
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        };

        using var response = await _api.SendAsync(request, ct);

        // A 404 is a real answer: the endpoint exists but this run is unknown to
        // the orchestrator. Surface it rather than throwing a bare HttpRequestException.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new PlatformException(
                $"POST /api/executions/{executionId}/cancel answered 404 at {ApiBaseUrl}. " +
                "The route exists, so the running execution is unknown to the orchestrator — " +
                "it either already finished or ran on a different process.",
                (int)response.StatusCode);
        }

        await EnsureSuccessAsync(response, "POST", $"/api/executions/{executionId}/cancel");
        return await ReadJsonAsync(response);
    }

    // ---------------------------------------------------------------- plumbing

    /// <summary>Picks the client and origin for a request, defaulting to the DevUI host.</summary>
    private (HttpClient Client, string BaseUrl) Resolve(string? origin)
    {
        if (string.IsNullOrEmpty(origin) || string.Equals(origin, BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return (_http, BaseUrl);
        }

        if (string.Equals(origin, ApiBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return (_api, ApiBaseUrl);
        }

        // A one-off origin (a tunnel, a different host). Kept short-lived on purpose:
        // it is a diagnostic path, not a hot path.
        var adhoc = new HttpClient
        {
            BaseAddress = null,
            Timeout = TimeSpan.FromSeconds(30)
        };

        adhoc.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        return (adhoc, origin.TrimEnd('/'));
    }

    private async Task<JsonElement> GetJsonAsync(string path, CancellationToken ct)
        => await GetJsonAsync(_http, BaseUrl, path, ct);

    private static async Task<JsonElement> GetJsonAsync(
        HttpClient client, string baseUrl, string path, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{baseUrl}{path}", ct);
        await EnsureSuccessAsync(response, "GET", path);
        return await ReadJsonAsync(response);
    }

    private async Task<JsonElement> PostJsonAsync(string path, object? payload, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{path}")
        {
            Content = JsonContent(payload)
        };

        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "POST", path);
        return await ReadJsonAsync(response);
    }

    private static StringContent JsonContent(object? payload)
    {
        // camelCase matches what the server camelCases back, and keeps node types
        // and node ids spelled the way the designer stores them.
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = payload is null ? "{}" : JsonSerializer.Serialize(payload, options);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string method, string path)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new PlatformException(
            $"{method} {path} failed: {(int)response.StatusCode} {response.ReasonPhrase}. {Describe(body)}",
            (int)response.StatusCode);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Pulls the useful part out of an error body. SvelteKit uses
    /// <c>{ error }</c>; .NET uses <c>{ message }</c>, <c>{ title }</c> or a
    /// problem-details document.
    /// </summary>
    private static string Describe(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "(empty body)";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            foreach (var key in new[] { "error", "message", "title", "detail", "Message", "Error" })
            {
                if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString() ?? body;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON; fall through.
        }

        return body.Length > 500 ? body[..500] + "…" : body;
    }

    private static string? Str(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Returns an element as-is for re-serialization. The caller passes it straight
    /// back to the server, so no lossy re-typing happens.
    /// </summary>
    private static object? Raw(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value : null;

    public void Dispose()
    {
        _http.Dispose();
        if (!ReferenceEquals(_api, _http))
        {
            _api.Dispose();
        }
    }
}

/// <summary>An HTTP-level failure from the platform, with the server's own message.</summary>
public class PlatformException : Exception
{
    public PlatformException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int? StatusCode { get; }
}