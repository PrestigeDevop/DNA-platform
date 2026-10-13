using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// The three dependency states from docs/refactor.md section 4 (lines 141-161).
/// </summary>
public static class EnvironmentStatusValues
{
    /// <summary>The requested tool/version resolves and is installed and active.</summary>
    public const string Green = "green";

    /// <summary>The dependency is missing but a platform-provided default can serve as fallback.</summary>
    public const string Yellow = "yellow";

    /// <summary>The dependency is unavailable, incompatible, or cannot be resolved.</summary>
    public const string Red = "red";
}

/// <summary>Status of one runtime (a mise tool) for the current project.</summary>
public record RuntimeStatus
{
    /// <summary>mise tool id, e.g. "dotnet" or "node".</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>Version request found in mise.toml, or the platform default request.</summary>
    public string RequestedVersion { get; init; } = string.Empty;

    /// <summary>Concrete version mise resolved, or "" when nothing is resolved.</summary>
    public string ResolvedVersion { get; init; } = string.Empty;

    public bool Installed { get; init; }

    /// <summary>True when mise reports this version as the active one for the project.</summary>
    public bool Active { get; init; }

    /// <summary>"mise" when mise provides the version, "platform-default" for the embedded fallback.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>One of <see cref="EnvironmentStatusValues"/>.</summary>
    public string Status { get; init; } = EnvironmentStatusValues.Yellow;

    public string Message { get; init; } = string.Empty;
}

/// <summary>A mise task declared in mise.toml (or inherited from a parent/global config).</summary>
public record TaskSummary
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Local polyglot execution environment as shown by the workflow Configure view
/// (docs/refactor.md section 4).
/// </summary>
public record EnvironmentStatus
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Directory holding mise.toml, or "" when none was found above the API output.</summary>
    public string ProjectRoot { get; init; } = string.Empty;

    public bool MiseInstalled { get; init; }

    public string MiseVersion { get; init; } = string.Empty;

    /// <summary>True when MISE_EXPERIMENTAL is on (required by the experimental mise MCP server).</summary>
    public bool ExperimentalEnabled { get; init; }

    /// <summary>Worst of the per-runtime states: red &gt; yellow &gt; green.</summary>
    public string OverallStatus { get; init; } = EnvironmentStatusValues.Yellow;

    public IReadOnlyList<RuntimeStatus> Runtimes { get; init; } = Array.Empty<RuntimeStatus>();

    public IReadOnlyList<TaskSummary> Tasks { get; init; } = Array.Empty<TaskSummary>();

    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Outcome of writing a mise.toml through the import endpoint.</summary>
public record MiseImportResult
{
    public bool Success { get; init; }

    /// <summary>Absolute path written, or "" on failure.</summary>
    public string Path { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Read/write access to the mise-based environment description. Every member degrades
/// gracefully: mise may be missing, off PATH, or unresponsive.
/// </summary>
public interface IMiseService
{
    Task<EnvironmentStatus> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Raw mise.toml content, or "" when the project has none.</summary>
    Task<string> ExportConfigurationAsync(CancellationToken ct = default);

    Task<MiseImportResult> ImportConfigurationAsync(string content, CancellationToken ct = default);

    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

/// <summary>
/// Introspects the local execution environment by shelling out to the mise CLI
/// (<c>mise --version</c>, <c>mise ls --json</c>, <c>mise tasks ls --json</c>).
///
/// Graceful-degradation contract: this service NEVER throws because of the
/// environment. A missing/mis-pathed/hung mise yields a well-formed
/// <see cref="EnvironmentStatus"/> with <c>MiseInstalled = false</c> and a
/// "yellow" overall status, so the Configure view always renders.
///
/// GREEN / YELLOW / RED mapping (docs/refactor.md section 4, lines 141-161):
/// <list type="bullet">
/// <item><description><b>green</b> - the requested tool+version resolves and mise reports it installed and active.</description></item>
/// <item><description><b>yellow</b> - mise does not provide it, but the platform-provided default can serve as fallback. Platform defaults are only .NET 8 and Node.js LTS (lines 154-159); anything else has no fallback.</description></item>
/// <item><description><b>red</b> - unavailable, incompatible, or unresolvable: a requested tool that mise cannot resolve and for which the platform has no default.</description></item>
/// </list>
/// </summary>
public sealed class MiseService : IMiseService
{
    private const string ConfigFileName = "mise.toml";
    private const int CommandTimeoutMs = 5000;
    private const int MaxImportBytes = 256 * 1024;

    /// <summary>
    /// Platform-provided defaults (docs/refactor.md lines 154-159). These are the only
    /// runtimes the platform can fall back to, so do not add entries without an
    /// embedded runtime in the repository.
    /// </summary>
    private static readonly (string Id, string Name, string Version)[] PlatformDefaults =
    {
        ("dotnet", ".NET 8", "8"),
        ("node", "Node.js LTS", "lts")
    };

    private readonly ILogger<MiseService> _logger;

    /// <summary>Cached result of the upward search for mise.toml.</summary>
    private string? _projectRoot;

    public MiseService(ILogger<MiseService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Nearest ancestor of the API output directory that contains mise.toml, or null.
    /// The file name is fixed, so every write goes to that directory and nowhere else.
    /// </summary>
    public string? ProjectRoot => _projectRoot ??= FindProjectRoot();

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        var run = await RunAsync("mise", new[] { "--version" }, ct).ConfigureAwait(false);
        return run is not null && run.ExitCode == 0 && !string.IsNullOrWhiteSpace(run.StandardOutput);
    }

    /// <summary>
    /// Builds the full environment picture: mise availability, per-runtime
    /// GREEN/YELLOW/RED states, and the task list. Never throws for environment reasons.
    /// </summary>
    public async Task<EnvironmentStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var projectRoot = ProjectRoot;
        var configText = await ReadConfigAsync(ct).ConfigureAwait(false);
        var requested = ParseRequestedTools(configText);

        var versionRun = await RunAsync("mise", new[] { "--version" }, ct).ConfigureAwait(false);
        var miseInstalled = versionRun is not null
            && versionRun.ExitCode == 0
            && !string.IsNullOrWhiteSpace(versionRun.StandardOutput);
        var miseVersion = miseInstalled ? FirstLine(versionRun!.StandardOutput) : string.Empty;

        var entries = miseInstalled
            ? await ListToolsAsync(projectRoot, ct).ConfigureAwait(false)
            : new Dictionary<string, ToolEntry>(StringComparer.OrdinalIgnoreCase);

        var tasks = miseInstalled
            ? await ListTasksAsync(projectRoot, ct).ConfigureAwait(false)
            : Array.Empty<TaskSummary>();

        var runtimes = new List<RuntimeStatus>();
        foreach (var toolId in requested.Keys.ToList())
        {
            runtimes.Add(BuildRuntimeStatus(toolId, requested, entries, miseInstalled));
        }

        // Platform defaults that mise.toml does not mention are still reported so the UI
        // shows what the workflow will actually execute with.
        foreach (var (id, name, version) in PlatformDefaults)
        {
            if (!requested.ContainsKey(id))
            {
                runtimes.Add(PlatformDefaultRuntime(id, name, version, entries, miseInstalled));
            }
        }

        return new EnvironmentStatus
        {
            Name = "Local polyglot execution environment",
            ProjectRoot = projectRoot ?? string.Empty,
            MiseInstalled = miseInstalled,
            MiseVersion = miseVersion,
            ExperimentalEnabled = IsExperimentalEnabled(configText),
            OverallStatus = OverallStatus(runtimes),
            Runtimes = runtimes,
            Tasks = tasks,
            CheckedAt = DateTime.UtcNow
        };
    }

    public async Task<string> ExportConfigurationAsync(CancellationToken ct = default)
        => await ReadConfigAsync(ct).ConfigureAwait(false);

    /// <summary>
    /// Writes <paramref name="content"/> to &lt;project root&gt;/mise.toml. Content is
    /// sanitized first (non-empty, no null bytes, at most 256 KB) and the target path is
    /// verified to stay inside the project root.
    /// </summary>
    public async Task<MiseImportResult> ImportConfigurationAsync(string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Failure("Configuration content is empty.");
        }

        if (content.IndexOf('\0') >= 0)
        {
            return Failure("Configuration content contains null bytes.");
        }

        var byteCount = Encoding.UTF8.GetByteCount(content);
        if (byteCount > MaxImportBytes)
        {
            return Failure($"Configuration is too large ({byteCount} bytes, limit {MaxImportBytes} bytes).");
        }

        var root = ProjectRoot;
        if (string.IsNullOrEmpty(root))
        {
            return Failure("No mise.toml found above the API output directory, so the project root is unknown.");
        }

        var fullRoot = Path.GetFullPath(root);
        var path = Path.GetFullPath(Path.Combine(fullRoot, ConfigFileName));
        if (!path.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("Refusing to write outside the project root.");
        }

        try
        {
            await File.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _logger.LogError(ex, "Failed to write {Path}", path);
            return Failure($"Could not write mise configuration: {ex.Message}");
        }

        _logger.LogInformation("Imported mise configuration ({Bytes} bytes) to {Path}", byteCount, path);
        return new MiseImportResult
        {
            Success = true,
            Path = path,
            Message = $"Wrote {byteCount} bytes to {ConfigFileName}. Run 'mise install' to apply it."
        };
    }

    private static MiseImportResult Failure(string message)
        => new() { Success = false, Path = string.Empty, Message = message };

    private static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ConfigFileName)))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private async Task<string> ReadConfigAsync(CancellationToken ct)
    {
        var root = ProjectRoot;
        if (string.IsNullOrEmpty(root))
        {
            return string.Empty;
        }

        var path = Path.Combine(root, ConfigFileName);
        try
        {
            return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _logger.LogWarning(ex, "Could not read {Path}", path);
            return string.Empty;
        }
    }

    /// <summary>
    /// Minimal `[tools]` scanner. Reads plain string requests and the version field of an
    /// inline table; anything else is skipped rather than guessed at.
    /// </summary>
    private static Dictionary<string, string> ParseRequestedTools(string configText)
    {
        var requested = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(configText))
        {
            return requested;
        }

        var inTools = false;
        foreach (var rawLine in configText.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("[", StringComparison.Ordinal))
            {
                inTools = line.Equals("[tools]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inTools)
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line.Substring(0, separator).Trim().Trim('"', '\'');
            if (key.Length == 0)
            {
                continue;
            }

            requested[key] = NormalizeVersionRequest(line.Substring(separator + 1).Trim());
        }

        return requested;
    }

    private static string NormalizeVersionRequest(string rawValue)
    {
        var value = rawValue;
        if (value.StartsWith("{", StringComparison.Ordinal))
        {
            var marker = value.IndexOf("version", StringComparison.OrdinalIgnoreCase);
            value = marker >= 0 ? value.Substring(marker) : value;
        }

        var comment = value.IndexOf('#');
        if (comment >= 0)
        {
            value = value.Substring(0, comment);
        }

        value = value.Trim().TrimEnd(',').Trim();
        if (value.StartsWith("\"", StringComparison.Ordinal))
        {
            var end = value.IndexOf('"', 1);
            if (end > 0)
            {
                return value.Substring(1, end - 1);
            }
        }

        if (value.StartsWith("'", StringComparison.Ordinal))
        {
            var end = value.IndexOf('\'', 1);
            if (end > 0)
            {
                return value.Substring(1, end - 1);
            }
        }

        var space = value.IndexOf(' ');
        if (space > 0)
        {
            value = value.Substring(0, space);
        }

        return value.Trim();
    }

    private RuntimeStatus BuildRuntimeStatus(
        string toolId,
        Dictionary<string, string> requested,
        Dictionary<string, ToolEntry> entries,
        bool miseInstalled)
    {
        var requestedVersion = requested.TryGetValue(toolId, out var request) ? request : string.Empty;
        var platformDefault = FindPlatformDefault(toolId);
        var name = platformDefault?.Name ?? toolId;

        if (entries.TryGetValue(toolId, out var entry) && entry.Active && entry.Installed)
        {
            return new RuntimeStatus
            {
                Id = toolId,
                Name = name,
                RequestedVersion = requestedVersion.Length > 0 ? requestedVersion : entry.Version,
                ResolvedVersion = entry.Version,
                Installed = true,
                Active = true,
                Source = "mise",
                Status = EnvironmentStatusValues.Green,
                Message = $"mise resolved {toolId} {entry.Version} and reports it active."
            };
        }

        if (platformDefault is not null)
        {
            return new RuntimeStatus
            {
                Id = toolId,
                Name = name,
                RequestedVersion = requestedVersion.Length > 0 ? requestedVersion : platformDefault.Value.Version,
                ResolvedVersion = string.Empty,
                Installed = false,
                Active = false,
                Source = "platform-default",
                Status = EnvironmentStatusValues.Yellow,
                Message = miseInstalled
                    ? $"mise does not report an installed active {toolId}; falling back to the platform-provided default ({platformDefault.Value.Version})."
                    : $"mise is unavailable; using the platform-provided default ({platformDefault.Value.Version})."
            };
        }

        return new RuntimeStatus
        {
            Id = toolId,
            Name = name,
            RequestedVersion = requestedVersion,
            ResolvedVersion = string.Empty,
            Installed = false,
            Active = false,
            Source = "none",
            Status = EnvironmentStatusValues.Red,
            Message = miseInstalled
                ? $"mise cannot resolve an installed active {toolId}, and the platform provides no default for it."
                : $"mise is unavailable, so the requested {toolId} cannot be resolved, and the platform provides no default for it."
        };
    }

    private RuntimeStatus PlatformDefaultRuntime(
        string id,
        string name,
        string version,
        Dictionary<string, ToolEntry> entries,
        bool miseInstalled)
    {
        if (entries.TryGetValue(id, out var entry) && entry.Active && entry.Installed)
        {
            return new RuntimeStatus
            {
                Id = id,
                Name = name,
                RequestedVersion = entry.Version,
                ResolvedVersion = entry.Version,
                Installed = true,
                Active = true,
                Source = "mise",
                Status = EnvironmentStatusValues.Green,
                Message = $"mise resolved {id} {entry.Version} and reports it active."
            };
        }

        return new RuntimeStatus
        {
            Id = id,
            Name = name,
            RequestedVersion = version,
            ResolvedVersion = string.Empty,
            Installed = false,
            Active = false,
            Source = "platform-default",
            Status = EnvironmentStatusValues.Yellow,
            Message = miseInstalled
                ? $"not declared in mise.toml and not active in mise; the platform-provided default ({version}) serves it."
                : $"mise is unavailable; the platform-provided default ({version}) serves it."
        };
    }

    private static (string Name, string Version)? FindPlatformDefault(string toolId)
    {
        foreach (var (id, name, version) in PlatformDefaults)
        {
            if (string.Equals(id, toolId, StringComparison.OrdinalIgnoreCase))
            {
                return (name, version);
            }
        }

        return null;
    }

    private static string OverallStatus(List<RuntimeStatus> runtimes)
    {
        if (runtimes.Count == 0)
        {
            return EnvironmentStatusValues.Yellow;
        }

        if (runtimes.Any(r => r.Status == EnvironmentStatusValues.Red))
        {
            return EnvironmentStatusValues.Red;
        }

        return runtimes.Any(r => r.Status == EnvironmentStatusValues.Yellow)
            ? EnvironmentStatusValues.Yellow
            : EnvironmentStatusValues.Green;
    }

    private async Task<Dictionary<string, ToolEntry>> ListToolsAsync(string? projectRoot, CancellationToken ct)
    {
        var entries = new Dictionary<string, ToolEntry>(StringComparer.OrdinalIgnoreCase);

        var run = await RunAsync("mise", ProjectArgs(projectRoot, "ls", "--json"), ct).ConfigureAwait(false);
        if (run is null || run.ExitCode != 0 || string.IsNullOrWhiteSpace(run.StandardOutput))
        {
            _logger.LogDebug("mise ls --json returned no usable output (exit {ExitCode}); treating every runtime as unresolved.", run?.ExitCode);
            return entries;
        }

        try
        {
            using var doc = JsonDocument.Parse(run.StandardOutput);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return entries;
            }

            foreach (var tool in doc.RootElement.EnumerateObject())
            {
                if (tool.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in tool.Value.EnumerateArray())
                {
                    var entry = ReadToolEntry(item);
                    if (entry is null)
                    {
                        continue;
                    }

                    if (!entries.TryGetValue(tool.Name, out var existing)
                        || (entry.Active && !existing.Active)
                        || (entry.Active == existing.Active && entry.Installed && !existing.Installed))
                    {
                        entries[tool.Name] = entry;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse 'mise ls --json' output.");
        }

        return entries;
    }

    private async Task<IReadOnlyList<TaskSummary>> ListTasksAsync(string? projectRoot, CancellationToken ct)
    {
        var tasks = new List<TaskSummary>();

        var run = await RunAsync("mise", ProjectArgs(projectRoot, "tasks", "ls", "--json"), ct).ConfigureAwait(false);
        if (run is null || run.ExitCode != 0 || string.IsNullOrWhiteSpace(run.StandardOutput))
        {
            return tasks;
        }

        try
        {
            using var doc = JsonDocument.Parse(run.StandardOutput);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return tasks;
            }

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = ReadString(item, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                tasks.Add(new TaskSummary
                {
                    Name = name!,
                    Description = ReadString(item, "description") ?? string.Empty
                });
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse 'mise tasks ls --json' output.");
        }

        return tasks;
    }

    private static ToolEntry? ReadToolEntry(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var version = ReadString(item, "version");
        if (string.IsNullOrEmpty(version))
        {
            return null;
        }

        var installPath = ReadString(item, "install_path");
        return new ToolEntry(
            version!,
            ReadBool(item, "installed") ?? !string.IsNullOrEmpty(installPath),
            ReadBool(item, "active") ?? false);
    }


    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool? ReadBool(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    /// <summary>
    /// True when the mise experimental surface is enabled: MISE_EXPERIMENTAL=1 in the API
    /// process, or <c>experimental = true</c> in mise.toml. Required by `mise mcp` only.
    /// </summary>
    private static bool IsExperimentalEnabled(string configText)
    {
        var flag = Environment.GetEnvironmentVariable("MISE_EXPERIMENTAL");
        if (!string.IsNullOrWhiteSpace(flag)
            && (flag == "1" || flag.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return !string.IsNullOrEmpty(configText)
            && configText.IndexOf("experimental = true", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string[] ProjectArgs(string? projectRoot, params string[] args)
    {
        if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot))
        {
            return args;
        }

        var all = new List<string>(args.Length + 2) { "--cd", projectRoot };
        all.AddRange(args);
        return all.ToArray();
    }

    private static string FirstLine(string text)
    {
        var trimmed = text.Trim();
        var newline = trimmed.IndexOfAny(new[] { '\r', '\n' });
        return newline >= 0 ? trimmed.Substring(0, newline).Trim() : trimmed;
    }

    /// <summary>
    /// Runs an executable with a hard 5 second budget. Returns null when the process
    /// cannot be started (mise missing or not on PATH) and never throws for that case.
    /// </summary>
    private static async Task<ProcessResult?> RunAsync(string fileName, string[] args, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return null;
            }
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException or NotSupportedException)
        {
            return null;
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(CommandTimeoutMs);
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or Win32Exception)
            {
                // Process already gone; nothing left to do.
            }

            return new ProcessResult(-1, string.Empty, $"'{fileName}' exceeded the {CommandTimeoutMs} ms budget.");
        }

        return new ProcessResult(
            process.ExitCode,
            await stdoutTask.ConfigureAwait(false),
            await stderrTask.ConfigureAwait(false));
    }

    private sealed record ToolEntry(string Version, bool Installed, bool Active);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
