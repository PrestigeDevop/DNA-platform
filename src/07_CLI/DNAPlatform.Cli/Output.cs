using System.Text.Json;

namespace DNAPlatform.Cli;

/// <summary>
/// Terminal rendering for CLI output.
///
/// <para>
/// Two rules throughout: diagnostics and errors go to stderr (the ACP adapter's
/// stdout discipline applies here too, and it keeps piping this CLI's stdout safe),
/// and status is never conveyed by colour alone — a word is always printed.
/// </para>
/// </summary>
public static class Output
{
    // ----------------------------------------------------------- element access

    public static bool Valid(JsonElement result) => Bool(result, "isValid");

    public static string StatusOf(JsonElement result) => Str(result, "status") ?? "unknown";

    /// <summary>
    /// Reads a property case-tolerantly. ASP.NET Core camelCases responses, but the
    /// engine's nested <c>ExecutionError</c> can arrive with a PascalCase
    /// <c>Message</c> depending on the serialization path, so both spellings are
    /// accepted rather than silently rendering blank.
    /// </summary>
    public static string? Str(JsonElement element, string name)
    {
        foreach (var candidate in new[] { name, char.ToUpperInvariant(name[0]) + name[1..] })
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(candidate, out var value))
            {
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    _ => value.ToString()
                };
            }
        }

        return null;
    }

    public static bool Bool(JsonElement element, string name)
    {
        foreach (var candidate in new[] { name, char.ToUpperInvariant(name[0]) + name[1..] })
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(candidate, out var value) &&
                value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }
        }

        return false;
    }

    private static JsonElement? Prop(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var candidate in new[] { name, char.ToUpperInvariant(name[0]) + name[1..] })
        {
            if (element.TryGetProperty(candidate, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static void Dump(JsonElement element, bool pretty = true)
    {
        // UnsafeRelaxedJsonEscaping: the default encoder turns '>' and '->' into
        // > and ->, which is unreadable in the graph and cycle messages the
        // validator returns. This is a terminal, not a browser.
        Console.WriteLine(JsonSerializer.Serialize(
            element,
            new JsonSerializerOptions
            {
                WriteIndented = pretty,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
    }

    /// <summary>
    /// A one-line status marker: symbol + word. Used everywhere a state appears, so
    /// the state survives a monochrome terminal, a screen reader, and a log file.
    /// </summary>
    private static string Mark(string status)
    {
        var symbol = status.ToLowerInvariant() switch
        {
            "completed" => "OK",
            "failed" => "FAIL",
            "running" => "RUN",
            "skipped" => "SKIP",
            "pending" => "WAIT",
            "waiting" => "WAIT",
            _ => "--"
        };

        return $"[{symbol}] {status}";
    }

    // ----------------------------------------------------------- run

    public static void WriteExecution(JsonElement result, bool asJson)
    {
        if (asJson)
        {
            Dump(result);
            return;
        }

        var status = StatusOf(result);
        Console.WriteLine($"execution: {Str(result, "executionId") ?? "(no id)"}");
        Console.WriteLine($"status:    {Mark(status)}");
        Console.WriteLine($"duration:  {Duration(result, "duration")}");

        // Per-node state, straight from the engine's NodeExecutions map. The execute
        // route also returns a flattened nodeResults array; prefer it when present.
        var flat = Prop(result, "nodeResults");
        if (flat is { ValueKind: JsonValueKind.Array })
        {
            Console.WriteLine();
            Console.WriteLine("nodes:");
            foreach (var node in flat.Value.EnumerateArray())
            {
                WriteNode(node);
            }

            return;
        }

        var full = Prop(result, "fullExecution");
        var executions = full is null ? null : Prop(full.Value, "nodeExecutions");
        if (executions is not { ValueKind: JsonValueKind.Object })
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("nodes:");
        foreach (var node in executions.Value.EnumerateObject())
        {
            WriteNode(node.Value, node.Name);
        }
    }

    private static void WriteNode(JsonElement node, string? idFallback = null)
    {
        var id = Str(node, "nodeId") ?? idFallback ?? "(no id)";
        var nodeStatus = Str(node, "status") ?? "unknown";

        // The flattened nodeResults array omits `duration`; the engine's own
        // NodeExecutions entries carry it. Either way, print it only when present.
        var timing = Milliseconds(node, "duration") is { } duration ? $"  ({duration})" : string.Empty;

        Console.WriteLine($"  {Mark(nodeStatus),-16} {id}{timing}");

        if (Error(node) is { } error)
        {
            Console.WriteLine($"      error: {error}");
        }
    }

    /// <summary>
    /// The node's error text, or null. A null/blank <c>error</c> is the normal case
    /// for a node that succeeded, so it must not print an empty line.
    /// </summary>
    private static string? Error(JsonElement node)
    {
        if (Prop(node, "error") is not { } error)
        {
            return null;
        }

        if (error.ValueKind != JsonValueKind.Object && error.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = error.ValueKind == JsonValueKind.String
            ? error.GetString()
            : Str(error, "message") ?? error.ToString();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>
    /// Formats a duration, or "-" when there is nothing to report.
    ///
    /// <para>
    /// .NET serializes <c>TimeSpan</c> as a string like <c>"00:00:00.2246541"</c>
    /// (see AGENTS.md), so a bare string is a TimeSpan, not a number, and printing
    /// it verbatim would be unreadable. A JSON number is milliseconds.
    /// </para>
    /// </summary>
    private static string Duration(JsonElement element, string name)
        => Milliseconds(element, name) ?? "-";

    /// <summary>
    /// The same formatting, but null when the property is absent, null, or zero.
    /// A zero duration is what an in-memory or unmeasured run reports, and
    /// distinguishing "no measurement" from "measured as nothing" matters.
    /// </summary>
    private static string? Milliseconds(JsonElement element, string name)
    {
        if (Prop(element, name) is not { } duration ||
            duration.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        double? ms = duration.ValueKind switch
        {
            JsonValueKind.Number => duration.GetDouble(),

            JsonValueKind.String when TimeSpan.TryParse(
                duration.GetString(), out var span) => span.TotalMilliseconds,

            _ => null
        };

        // Not a number and not a TimeSpan: show it as-is rather than mislabel it.
        if (ms is null)
        {
            return duration.ToString();
        }

        if (ms == 0)
        {
            return null;
        }

        return ms < 1 ? $"{Math.Round(ms.Value, 3)} ms" : $"{Math.Round(ms.Value)} ms";
    }

    // ----------------------------------------------------------- validate

    public static void WriteValidation(JsonElement result, bool asJson)
    {
        if (asJson)
        {
            Dump(result);
            return;
        }

        var valid = Valid(result);
        Console.WriteLine($"result:    {(valid ? "VALID" : "INVALID")}");

        var summary = Str(result, "summary");
        if (summary is not null)
        {
            Console.WriteLine($"summary:   {summary}");
        }

        PrintList("errors", result, "errors");
        PrintList("warnings", result, "warnings");

        var checks = Prop(result, "checks");
        if (checks is { ValueKind: JsonValueKind.Array } && checks.Value.GetArrayLength() > 0)
        {
            Console.WriteLine();
            Console.WriteLine("checks:");
            foreach (var check in checks.Value.EnumerateArray())
            {
                var passed = Bool(check, "passed");
                var name = Str(check, "name") ?? "(unnamed)";
                var detail = Str(check, "detail");
                var suffix = passed || detail is null ? string.Empty : $" — {detail}";
                Console.WriteLine($"  {(passed ? "PASS" : "FAIL"),-5} {name}{suffix}");
            }
        }
    }

    private static void PrintList(string label, JsonElement element, string property)
    {
        var array = Prop(element, property);
        if (array is not { ValueKind: JsonValueKind.Array } || array.Value.GetArrayLength() == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{label}:");
        foreach (var item in array.Value.EnumerateArray())
        {
            Console.WriteLine($"  - {Str(item, "message") ?? item.ToString()}");
        }
    }

    // ----------------------------------------------------------- status

    public static void WriteStatus(JsonElement result, bool all, bool asJson)
    {
        if (asJson)
        {
            Dump(result);
            return;
        }

        // SvelteKit envelope: { executions: [...] }.
        var executions = Prop(result, "executions");
        if (executions is not { ValueKind: JsonValueKind.Array })
        {
            Console.WriteLine("No executions returned.");
            return;
        }

        var rows = new List<(string Workflow, string Status, string When, string Duration)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var execution in executions.Value.EnumerateArray())
        {
            var workflow = Str(execution, "workflowName")
                           ?? Str(execution, "workflowId")
                           ?? "(unknown workflow)";

            if (!all && !seen.Add(workflow))
            {
                continue;
            }

            rows.Add((
                workflow,
                StatusOf(execution),
                Str(execution, "startTime") ?? "-",
                Duration(execution, "duration")));
        }

        if (rows.Count == 0)
        {
            Console.WriteLine(all
                ? "No executions recorded."
                : "No executions recorded. Pass --all to include every run.");
            return;
        }

        Console.WriteLine($"{"WORKFLOW",-28} {"STATUS",-14} {"STARTED",-26} DURATION");
        foreach (var row in rows)
        {
            Console.WriteLine($"{Trim(row.Workflow, 28),-28} {Trim(Mark(row.Status), 14),-14} {row.When,-26} {row.Duration}");
        }
    }

    // ----------------------------------------------------------- logs

    public static void WriteLogs(JsonElement result, bool asJson)
    {
        if (asJson)
        {
            Dump(result);
            return;
        }

        // .NET LogsController envelope: { count, logs }.
        var logs = Prop(result, "logs");
        if (logs is not { ValueKind: JsonValueKind.Array })
        {
            Console.WriteLine("No log records returned.");
            return;
        }

        foreach (var log in logs.Value.EnumerateArray())
        {
            var level = Str(log, "level") ?? "Information";
            var stamp = Str(log, "timestamp") ?? "-";
            var category = Str(log, "category") ?? "-";
            var message = Str(log, "message") ?? string.Empty;

            // Log lines are diagnostics, not results.
            Console.Error.WriteLine($"{stamp}  {level,-11} {Trim(category, 28),-28} {message}");
        }
    }

    // ----------------------------------------------------------- env

    public static void WriteEnvironment(JsonElement result, bool asJson)
    {
        if (asJson)
        {
            Dump(result);
            return;
        }

        // Shape mirrors lib/services/environment.ts exactly: flat miseInstalled /
        // miseVersion / overallStatus, with a `runtimes` array (not `tools`).
        var name = Str(result, "name");
        if (name is not null)
        {
            Console.WriteLine($"environment: {name}");
        }

        var root0 = Str(result, "projectRoot");
        if (root0 is not null)
        {
            Console.WriteLine($"root:       {root0}");
        }

        var installed = Bool(result, "miseInstalled");
        Console.WriteLine($"mise:       {(installed ? "installed" : "not installed")}");

        var version = Str(result, "miseVersion");
        if (installed && !string.IsNullOrWhiteSpace(version))
        {
            Console.WriteLine($"version:    {version}");
        }

        // mise mcp needs MISE_EXPERIMENTAL=1; surface it because its absence is a
        // common cause of "why is the MCP server dead".
        if (Bool(result, "experimentalEnabled"))
        {
            Console.WriteLine("experimental: MISE_EXPERIMENTAL is on (mise mcp would start)");
        }

        var runtimes = Prop(result, "runtimes");
        if (runtimes is { ValueKind: JsonValueKind.Array })
        {
            Console.WriteLine();
            Console.WriteLine($"{"RUNTIME",-18} {"REQUESTED",-10} {"RESOLVED",-14} {"STATUS",-8} SOURCE  NOTE");

            foreach (var runtime in runtimes.Value.EnumerateArray())
            {
                var status = Str(runtime, "status") ?? "unknown";
                var id = Str(runtime, "id") ?? "-";
                var requested = Str(runtime, "requestedVersion") ?? "-";
                var resolved = Str(runtime, "resolvedVersion");
                var source = Str(runtime, "source") ?? "-";
                var note = Str(runtime, "message") ?? string.Empty;

                var marker = status.ToLowerInvariant() switch
                {
                    "green" => "OK",
                    "yellow" => "WARN",
                    "red" => "BAD",
                    _ => "--"
                };

                Console.WriteLine(
                    $"{Trim(id, 18),-18} {Trim(requested, 10),-10} {Trim(resolved ?? "not resolved", 14),-14} " +
                    $"{marker,-8} {Trim(source, 15),-15} {note}");
            }
        }

        var tasks = Prop(result, "tasks");
        if (tasks is { ValueKind: JsonValueKind.Array } && tasks.Value.GetArrayLength() > 0)
        {
            Console.WriteLine();
            Console.WriteLine("tasks:");
            foreach (var task in tasks.Value.EnumerateArray())
            {
                Console.WriteLine(
                    $"  {Trim(Str(task, "name") ?? "-", 20),-20} {Str(task, "description") ?? string.Empty}");
            }
        }
        else if (tasks is { ValueKind: JsonValueKind.Array })
        {
            Console.WriteLine("tasks:      none declared");
        }

        var overall = Str(result, "overallStatus");
        var checkedAt = Str(result, "checkedAt");

        if (overall is not null || checkedAt is not null)
        {
            Console.WriteLine();
            Console.WriteLine($"overall:    {Level(overall)}" +
                              (checkedAt is null ? string.Empty : $"  (checked {checkedAt})"));
        }
    }

    /// <summary>Spells out a status level, with a word for the colour.</summary>
    private static string Level(string? level) => level?.ToLowerInvariant() switch
    {
        "green" => "GREEN  every requested runtime resolved",
        "yellow" => "YELLOW missing but a platform default can serve it",
        "red" => "RED    unavailable or unresolvable",
        _ => level ?? "unknown"
    };

    private static string Trim(string value, int width)
        => value.Length <= width ? value : value[..(width - 1)] + "…";
}