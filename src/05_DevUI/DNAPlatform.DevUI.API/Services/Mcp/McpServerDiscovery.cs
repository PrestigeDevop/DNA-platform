using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DNAPlatform.DevUI.API.Services.Mcp;
using DNAPlatform.Skills;

namespace DNAPlatform.DevUI.API.Services.Mcp;

/// <summary>
/// Configuration options for MCP server discovery.
/// </summary>
public class McpDiscoveryOptions
{
    /// <summary>
    /// Directory to scan for MCP server manifest files (.json).
    /// Defaults to <projectRoot>/mcp-servers/.
    /// </summary>
    public string ManifestDirectory { get; set; } = "mcp-servers";

    /// <summary>
    /// Timeout for each server's initialize and tools/list handshake.
    /// </summary>
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Runtime status of a discovered MCP server, surfaced to the UI so the
/// Skills page can list MCP-discovered tools alongside built-in skills without
/// guessing whether the server process is alive.
/// </summary>
public enum McpServerStatus
{
    Pending,
    Running,
    Error,
    Stopped
}

/// <summary>
/// Manifest file format for an MCP server.
/// </summary>
internal class McpServerManifest
{
    public string ServerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Command { get; set; } = string.Empty;
    public string[]? Args { get; set; }
    public Dictionary<string, string>? Env { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 request envelope.
/// </summary>
internal class JsonRpcRequest
{
    public string Jsonrpc { get; set; } = "2.0";
    public int Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public JsonNode? Params { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 response envelope.
/// </summary>
internal class JsonRpcResponse
{
    public string Jsonrpc { get; set; } = string.Empty;
    public int Id { get; set; }
    public JsonNode? Result { get; set; }
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 error object.
/// </summary>
internal class JsonRpcError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public JsonNode? Data { get; set; }
}

/// <summary>
/// MCP initialize request parameters.
/// </summary>
internal class McpInitializeParams
{
    public string ProtocolVersion { get; set; } = "2024-11-05";
    public McpClientCapabilities Capabilities { get; set; } = new();
    public McpClientInfo ClientInfo { get; set; } = new();
}

/// <summary>
/// MCP client capabilities.
/// </summary>
internal class McpClientCapabilities
{
    public McpToolsCapability Tools { get; set; } = new();
}

/// <summary>
/// MCP tools capability.
/// </summary>
internal class McpToolsCapability
{
    public bool ListChanged { get; set; } = true;
}

/// <summary>
/// MCP client info.
/// </summary>
internal class McpClientInfo
{
    public string Name { get; set; } = "DNA Platform";
    public string Version { get; set; } = "0.3.0-alpha";
}

/// <summary>
/// MCP initialize response result.
/// </summary>
internal class McpInitializeResult
{
    public string ProtocolVersion { get; set; } = string.Empty;
    public McpServerCapabilities Capabilities { get; set; } = new();
    public McpServerInfo ServerInfo { get; set; } = new();
}

/// <summary>
/// MCP server capabilities.
/// </summary>
internal class McpServerCapabilities
{
    public McpToolsCapability? Tools { get; set; }
}

/// <summary>
/// MCP server info.
/// </summary>
internal class McpServerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// MCP tools/list response result.
/// </summary>
internal class McpToolsListResult
{
    public McpTool[] Tools { get; set; } = Array.Empty<McpTool>();
}

/// <summary>
/// MCP tool definition from server.
/// </summary>
internal class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement InputSchema { get; set; }
}

/// <summary>
/// Tools/call parameters.
/// </summary>
internal class ToolsCallParams
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object>? Arguments { get; set; }
}

/// <summary>
/// Discovered MCP server implementation.
/// </summary>
internal class DiscoveredMcpServer : IMcpServer
{
    public string ServerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public IReadOnlyList<McpToolDefinition> Tools { get; set; } = Array.Empty<McpToolDefinition>();
}

/// <summary>
/// Adapter that wraps an MCP tool as an ISkill so it can be used in ProcessingSkill nodes.
/// </summary>
internal class McpToolSkill : ISkill
{
    private readonly IMcpServer _server;
    private readonly McpToolDefinition _tool;
    private readonly McpServerDiscovery _discovery;

    public McpToolSkill(IMcpServer server, McpToolDefinition tool, McpServerDiscovery discovery)
    {
        _server = server;
        _tool = tool;
        _discovery = discovery;
    }

    public string SkillId => $"mcp.{_server.ServerId}.{_tool.Name}";
    public string Name => _tool.Name;
    public string Description => _tool.Description;
    public string Category => $"MCP: {_server.Name}";
    public string Icon => "??";
    public string Version => _server.Version;

    public Dictionary<string, string> GetInputSchema()
    {
        var schema = new Dictionary<string, string>();
        if (_tool.InputSchema.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in _tool.InputSchema.EnumerateObject())
            {
                schema[prop.Name] = prop.Value.ValueKind.ToString();
            }
        }
        return schema;
    }

    public Dictionary<string, string> GetOutputSchema()
    {
        return new Dictionary<string, string> { { "result", "object" } };
    }

    public List<SkillInputField> GetInputFields()
    {
        var fields = new List<SkillInputField>();
        if (_tool.InputSchema.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in _tool.InputSchema.EnumerateObject())
            {
                var field = new SkillInputField
                {
                    Name = prop.Name,
                    Type = GetFieldType(prop.Value),
                    Label = prop.Name,
                    Description = prop.Value.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    Required = prop.Value.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.Array && req.EnumerateArray().Any(r => r.GetString() == prop.Name)
                };
                fields.Add(field);
            }
        }
        return fields;
    }

    private static string GetFieldType(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return "string";
        if (element.TryGetProperty("type", out var type))
        {
            return type.GetString() ?? "string";
        }
        return "string";
    }

    public SkillMetadata GetMetadata()
    {
        return new SkillMetadata
        {
            SkillId = SkillId,
            Name = Name,
            Description = Description,
            Category = Category,
            Icon = Icon,
            Version = Version,
            InputSchema = GetInputSchema(),
            OutputSchema = GetOutputSchema(),
            InputFields = GetInputFields()
        };
    }

    public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
    {
        return await _discovery.CallToolAsync(_server.ServerId, _tool.Name, inputs ?? new Dictionary<string, object>());
    }
}

/// <summary>
/// Hosted service that discovers MCP servers on startup and registers their tools as skills.
/// </summary>
public class McpServerDiscovery : IHostedService, IDisposable
{
    private readonly ILogger<McpServerDiscovery> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly McpDiscoveryOptions _options;
    private readonly ConcurrentDictionary<string, IMcpServer> _servers = new();
    private readonly ConcurrentDictionary<string, Process> _processes = new();
    private readonly ConcurrentDictionary<string, McpServerStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, string> _statusMessages = new();
    private readonly SemaphoreSlim _discoveryLock = new(1, 1);
    private bool _disposed;
    private int _nextRequestId = 2;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public McpServerDiscovery(
        ILogger<McpServerDiscovery> logger,
        IServiceProvider serviceProvider,
        IOptions<McpDiscoveryOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public IReadOnlyDictionary<string, IMcpServer> Servers => _servers;

    /// <summary>Per-server runtime status (Pending / Running / Error / Stopped).</summary>
    public IReadOnlyDictionary<string, McpServerStatus> Statuses => _statuses;

    /// <summary>Human-readable status detail for each server (e.g. the last error message).</summary>
    public IReadOnlyDictionary<string, string> StatusMessages => _statusMessages;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP server discovery...");
        _statuses.Clear();
        _statusMessages.Clear();
        await DiscoverAndRegisterServersAsync(cancellationToken);
        _logger.LogInformation("MCP server discovery completed. Found {Count} servers.", _servers.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MCP servers...");
        foreach (var (serverId, process) in _processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    await process.WaitForExitAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping MCP server {ServerId}", serverId);
            }
        }
        _processes.Clear();
        _servers.Clear();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            foreach (var process in _processes.Values)
            {
                try { process.Kill(true); } catch { }
            }
            _processes.Clear();
            _servers.Clear();
            _discoveryLock.Dispose();
        }
    }

    /// <summary>
    /// Imports a single MCP server manifest (uploaded by the UI), writes it to the
    /// manifest directory and discovers it through the same code path as startup.
    /// Returns the discovered server, or null when the manifest is invalid or the
    /// server fails to handshake.
    /// </summary>
    public async Task<IMcpServer?> ImportManifestAsync(string manifestJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            return null;
        }

        McpServerManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<McpServerManifest>(manifestJson, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize manifest");
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(manifest.ServerId))
        {
            manifest.ServerId = (manifest.Name ?? "imported") + "-" + Guid.NewGuid().ToString("N")[..8];
        }
        if (string.IsNullOrWhiteSpace(manifest.Command))
        {
            return null;
        }

        try
        {
            var manifestDir = Path.GetFullPath(_options.ManifestDirectory);
            Directory.CreateDirectory(manifestDir);
            var safeId = string.Join("-", manifest.ServerId.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(manifestDir, safeId + ".json");
            await File.WriteAllTextAsync(path, manifestJson, cancellationToken);
        }
        catch
        {
            // Best-effort persistence; discovery still runs in memory.
        }

        return await DiscoverServerFromManifestAsync(
            Path.Combine(Path.GetFullPath(_options.ManifestDirectory),
                string.Join("-", manifest.ServerId.Split(Path.GetInvalidFileNameChars())) + ".json"),
            cancellationToken);
    }

    private async Task DiscoverAndRegisterServersAsync(CancellationToken cancellationToken)
    {
        var manifestDir = Path.GetFullPath(_options.ManifestDirectory);
        if (!Directory.Exists(manifestDir))
        {
            _logger.LogInformation("MCP manifest directory does not exist: {Dir}", manifestDir);
            return;
        }

        var manifestFiles = Directory.GetFiles(manifestDir, "*.json");
        if (manifestFiles.Length == 0)
        {
            _logger.LogInformation("No MCP manifest files found in {Dir}", manifestDir);
            return;
        }

        var tasks = manifestFiles.Select(file => DiscoverServerFromManifestAsync(file, cancellationToken));
        var results = await Task.WhenAll(tasks);

        foreach (var server in results.Where(s => s != null))
        {
            _servers.TryAdd(server!.ServerId, server);
            await RegisterServerToolsAsSkillsAsync(server!);
        }
    }

    private async Task<IMcpServer?> DiscoverServerFromManifestAsync(string manifestPath, CancellationToken cancellationToken)
    {
        McpServerManifest manifest;
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            manifest = JsonSerializer.Deserialize<McpServerManifest>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize manifest");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse MCP manifest {Path}", manifestPath);
            return null;
        }

        if (string.IsNullOrWhiteSpace(manifest.Command))
        {
            _logger.LogWarning("MCP manifest {Path} missing required 'command' field", manifestPath);
            return null;
        }

        var process = StartServerProcess(manifest);
        if (process == null)
        {
            _statuses.TryAdd(manifest.ServerId, McpServerStatus.Error);
            _statusMessages.TryAdd(manifest.ServerId, "Failed to start server process");
            return null;
        }

        _processes.TryAdd(manifest.ServerId, process);
        _statuses.TryAdd(manifest.ServerId, McpServerStatus.Pending);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.HandshakeTimeout);

            await PerformHandshakeAsync(process, manifest.ServerId, cts.Token);
            _statuses.TryAdd(manifest.ServerId, McpServerStatus.Running);
            var tools = await ListToolsAsync(process, manifest.ServerId, cts.Token);

            var server = new DiscoveredMcpServer
            {
                ServerId = manifest.ServerId,
                Name = manifest.Name,
                Version = manifest.Version,
                Tools = tools
            };

            _logger.LogInformation("Discovered MCP server: {ServerId} ({Name}) with {ToolCount} tools",
                server.ServerId, server.Name, server.Tools.Count);

            return server;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MCP server {ServerId} handshake timed out after {Timeout}",
                manifest.ServerId, _options.HandshakeTimeout);
            _statuses.TryAdd(manifest.ServerId, McpServerStatus.Error);
            _statusMessages.TryAdd(manifest.ServerId, "Handshake timed out after " + _options.HandshakeTimeout);
            TryKillProcess(manifest.ServerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MCP server {ServerId} failed during discovery", manifest.ServerId);
            _statuses.TryAdd(manifest.ServerId, McpServerStatus.Error);
            _statusMessages.TryAdd(manifest.ServerId, ex.Message);
            TryKillProcess(manifest.ServerId);
            return null;
        }
    }

    private Process? StartServerProcess(McpServerManifest manifest)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = manifest.Command,
                Arguments = manifest.Args != null ? string.Join(" ", manifest.Args) : "",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (manifest.Env != null)
            {
                foreach (var (key, value) in manifest.Env)
                {
                    startInfo.Environment[key] = value;
                }
            }

            var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start MCP server process: {Command}", manifest.Command);
                return null;
            }

            // Capture stderr for diagnostics without corrupting stdout JSON-RPC stream
            _ = Task.Run(async () =>
            {
                try
                {
                    using var reader = process.StandardError;
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _logger.LogDebug("[MCP:{ServerId}:stderr] {Line}", manifest.ServerId, line);
                        }
                    }
                }
                catch { }
            });

            return process;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start MCP server {ServerId}", manifest.ServerId);
            return null;
        }
    }

    private async Task PerformHandshakeAsync(Process process, string serverId, CancellationToken cancellationToken)
    {
        var requestId = 1;
        var initializeRequest = new JsonRpcRequest
        {
            Id = requestId++,
            Method = "initialize",
            Params = JsonSerializer.SerializeToNode(new McpInitializeParams(), JsonOptions)
        };

        await SendRequestAsync(process, initializeRequest, cancellationToken);
        var initResponse = await ReadResponseAsync(process, cancellationToken);

        if (initResponse?.Error != null)
        {
            throw new InvalidOperationException($"Initialize failed: {initResponse.Error.Message}");
        }

        // Send initialized notification (no response expected)
        var initializedNotification = new JsonRpcRequest
        {
            Method = "notifications/initialized"
        };
        await SendRequestAsync(process, initializedNotification, cancellationToken);
    }

    private async Task<List<McpToolDefinition>> ListToolsAsync(Process process, string serverId, CancellationToken cancellationToken)
    {
        var requestId = 2;
        var listRequest = new JsonRpcRequest
        {
            Id = requestId,
            Method = "tools/list"
        };

        await SendRequestAsync(process, listRequest, cancellationToken);
        var response = await ReadResponseAsync(process, cancellationToken);

        if (response?.Error != null)
        {
            throw new InvalidOperationException($"tools/list failed: {response.Error.Message}");
        }

        var result = response?.Result?.Deserialize<McpToolsListResult>(JsonOptions);
        if (result?.Tools == null)
        {
            return new List<McpToolDefinition>();
        }

        return result.Tools.Select(t => new McpToolDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema
        }).ToList();
    }

    private async Task SendRequestAsync(Process process, JsonRpcRequest request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        await process.StandardInput.WriteLineAsync(json);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    private async Task<JsonRpcResponse?> ReadResponseAsync(Process process, CancellationToken cancellationToken)
    {
        var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }
        return JsonSerializer.Deserialize<JsonRpcResponse>(line, JsonOptions);
    }

    private async Task RegisterServerToolsAsSkillsAsync(IMcpServer server)
    {
        using var scope = _serviceProvider.CreateScope();
        var skillRegistry = scope.ServiceProvider.GetRequiredService<ISkillRegistry>();

        foreach (var tool in server.Tools)
        {
            var skill = new McpToolSkill(server, tool, this);
            await skillRegistry.RegisterSkill(skill.SkillId, skill);
            _logger.LogInformation("Registered MCP tool as skill: {SkillId}", skill.SkillId);
        }
    }

    /// <summary>
    /// Calls an MCP tool on the specified server.
    /// </summary>
    public async Task<SkillOutput> CallToolAsync(string serverId, string toolName, Dictionary<string, object> arguments)
    {
        if (!_processes.TryGetValue(serverId, out var process) || process.HasExited)
        {
            return SkillOutput.CreateError($"MCP server '{serverId}' is not running");
        }

        try
        {
            var requestId = Interlocked.Increment(ref _nextRequestId);
            var request = new JsonRpcRequest
            {
                Id = requestId,
                Method = "tools/call",
                Params = JsonSerializer.SerializeToNode(new ToolsCallParams { Name = toolName, Arguments = arguments }, JsonOptions)
            };

            await SendRequestAsync(process, request, CancellationToken.None);
            var response = await ReadResponseAsync(process, CancellationToken.None);

            if (response?.Error != null)
            {
                return SkillOutput.CreateError($"MCP tool '{toolName}' failed: {response.Error.Message}");
            }

            var resultData = new Dictionary<string, object>();
            if (response.Result != null)
            {
                // Convert JSON result to dictionary
                var jsonStr = response.Result.ToJsonString();
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr, JsonOptions);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        resultData[kvp.Key] = kvp.Value;
                    }
                }
            }

            return SkillOutput.CreateSuccess(resultData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MCP tool {ToolName} on server {ServerId}", toolName, serverId);
            return SkillOutput.CreateError($"MCP tool call failed: {ex.Message}");
        }
    }

    private void TryKillProcess(string serverId)
    {
        if (_processes.TryRemove(serverId, out var process))
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
        }
    }
}
