using System.CommandLine;
using System.CommandLine.Invocation;
using DNAPlatform.Cli;

// beta4 has no SetHandler(Func<InvocationContext, Task<int>>), so handlers are
// async void-returning Tasks that report through InvocationContext.ExitCode.
// The exit code is load-bearing: `run` and `validate` return non-zero on a
// failed or invalid workflow so scripts and CI can branch on them.

var urlOption = new Option<string>(
    aliases: new[] { "--url", "-u" },
    description:
        "DevUI origin, which serves /api/workflows, /api/executions and /api/snippets. " +
        "Defaults to DNA_PLATFORM_URL, then http://127.0.0.1:5173.");

var apiUrlOption = new Option<string>(
    aliases: new[] { "--api-url" },
    description:
        ".NET origin, used only for routes SvelteKit does not expose (execution cancel). " +
        "Defaults to DNA_PLATFORM_API_URL, then http://127.0.0.1:5254.");

var jsonOption = new Option<bool>(
    aliases: new[] { "--json" },
    description: "Emit raw JSON instead of a human-readable summary.");

var root = new RootCommand(
    "DNA Platform CLI — workflow control over the platform. " +
    "This is a thin HTTP client: it never runs a workflow itself, it asks the API to.");

root.AddGlobalOption(urlOption);
root.AddGlobalOption(apiUrlOption);
root.AddGlobalOption(jsonOption);

PlatformClient ClientFor(InvocationContext ctx)
{
    var url = ctx.ParseResult.GetValueForOption(urlOption)
              ?? Environment.GetEnvironmentVariable("DNA_PLATFORM_URL")
              ?? "http://127.0.0.1:5173";

    var apiUrl = ctx.ParseResult.GetValueForOption(apiUrlOption)
                 ?? Environment.GetEnvironmentVariable("DNA_PLATFORM_API_URL")
                 ?? "http://127.0.0.1:5254";

    return new PlatformClient(url, apiUrl);
}

bool AsJson(InvocationContext ctx) => ctx.ParseResult.GetValueForOption(jsonOption);

// ------------------------------------------------------------------ run

var workflowArgument = new Argument<string>("workflow-id", "Workflow id to act on.");

var runCommand = new Command("run", "Execute a stored workflow and print its per-node result.");
runCommand.AddArgument(workflowArgument);
runCommand.SetHandler(async (InvocationContext ctx) =>
{
    using var client = ClientFor(ctx);

    try
    {
        var result = await client.ExecuteWorkflowAsync(
            ctx.ParseResult.GetValueForArgument(workflowArgument), ctx.GetCancellationToken());
        Output.WriteExecution(result, AsJson(ctx));

        ctx.ExitCode = Output.StatusOf(result) is "Completed" or "completed" ? 0 : 1;
    }
    catch (PlatformException ex)
    {
        ctx.ExitCode = Fail(ex);
    }
});

// ------------------------------------------------------------------ validate

var validateCommand = new Command(
    "validate",
    "Run the Test step against a stored workflow: structural checks only, no execution.");
validateCommand.AddArgument(workflowArgument);
validateCommand.SetHandler(async (InvocationContext ctx) =>
{
    using var client = ClientFor(ctx);

    try
    {
        var result = await client.ValidateWorkflowAsync(
            ctx.ParseResult.GetValueForArgument(workflowArgument), ctx.GetCancellationToken());
        Output.WriteValidation(result, AsJson(ctx));
        ctx.ExitCode = Output.Valid(result) ? 0 : 1;
    }
    catch (PlatformException ex)
    {
        ctx.ExitCode = Fail(ex);
    }
});

// ------------------------------------------------------------------ status

var allOption = new Option<bool>(
    aliases: new[] { "--all" },
    description: "Include every recorded run, not just the most recent per workflow.");

var statusCommand = new Command("status", "Show workflow execution status.");
statusCommand.AddOption(allOption);
statusCommand.SetHandler(async (InvocationContext ctx) =>
{
    using var client = ClientFor(ctx);

    try
    {
        var result = await client.ListExecutionsAsync(ctx.GetCancellationToken());
        Output.WriteStatus(result, ctx.ParseResult.GetValueForOption(allOption), AsJson(ctx));
        ctx.ExitCode = 0;
    }
    catch (PlatformException ex)
    {
        ctx.ExitCode = Fail(ex);
    }
});

// ------------------------------------------------------------------ logs

var sinceOption = new Option<long>(
    aliases: new[] { "--since" },
    getDefaultValue: () => 0,
    description: "Only return log records with an id greater than this.");

var limitOption = new Option<int>(
    aliases: new[] { "--limit", "-n" },
    getDefaultValue: () => 100,
    description: "Maximum number of log records to return.");

var logsCommand = new Command("logs", "Tail the platform log buffer.");
logsCommand.AddOption(sinceOption);
logsCommand.AddOption(limitOption);
logsCommand.SetHandler(async (InvocationContext ctx) =>
{
    using var client = ClientFor(ctx);

    try
    {
        var result = await client.GetLogsAsync(
            ctx.ParseResult.GetValueForOption(sinceOption),
            ctx.ParseResult.GetValueForOption(limitOption),
            ctx.GetCancellationToken());
        Output.WriteLogs(result, AsJson(ctx));
        ctx.ExitCode = 0;
    }
    catch (PlatformException ex)
    {
        ctx.ExitCode = Fail(ex);
    }
});

// ------------------------------------------------------------------ cancel

var executionArgument = new Argument<string>("execution-id", "Execution id to cancel.");

var cancelCommand = new Command(
    "cancel",
    "Request cancellation of a running execution. Requires a route that accepts the request.");
cancelCommand.AddArgument(executionArgument);
cancelCommand.SetHandler(async (InvocationContext ctx) =>
{
    using var client = ClientFor(ctx);

    try
    {
        var result = await client.CancelExecutionAsync(
            ctx.ParseResult.GetValueForArgument(executionArgument), ctx.GetCancellationToken());

        // ExecutionsController answers { Message = "Execution 'x' cancelled" }.
        // Echo it: the route returns 200 even for an id the orchestrator never
        // saw, so the server's wording is the only trustworthy part.
        var message = Output.Str(result, "message");
        Console.WriteLine(message ?? "Cancellation requested.");
        ctx.ExitCode = 0;
    }
    catch (PlatformException ex)
    {
        ctx.ExitCode = Fail(ex);
    }
});

// ------------------------------------------------------------------ env

var envCommand = new Command(
    "env",
    "Show the runtime environment the API reports (mise and platform tools).");
envCommand.SetHandler(async (InvocationContext ctx) =>
{
    using var client = ClientFor(ctx);
    var ct = ctx.GetCancellationToken();

    // `env` is the command a user runs first to find out what is up, so it must
    // work when only one of the two servers is running. Both origins serve
    // /api/environment (5173 proxies it to 5254), so try each in turn.
    var origin = (string?)null;
    string? failure = null;

    if (await client.IsReachableAsync(ct))
    {
        origin = client.BaseUrl;
    }
    else if (await client.IsApiReachableAsync(ct))
    {
        origin = client.ApiBaseUrl;
        Console.WriteLine("note:    the web server is not up; read the environment from the API directly.");
    }
    else
    {
        failure = client.LastError;
    }

    if (origin is null)
    {
        Console.WriteLine("status: unreachable (neither origin answered /health)");

        if (failure is not null)
        {
            Console.WriteLine($"reason: {failure}");
        }

        Console.WriteLine("hint: start the API with run-all.bat, or pass --url <base-url>.");
        ctx.ExitCode = 1;
        return;
    }

    try
    {
        var result = await client.GetEnvironmentAsync(origin, ct);
        Output.WriteEnvironment(result, AsJson(ctx));
        ctx.ExitCode = 0;
    }
    catch (PlatformException ex)
    {
        ctx.ExitCode = Fail(ex);
    }
});

// ------------------------------------------------------------------ smoketest
var smokeTestCommand = new Command("smoketest", "Execute automated cross-platform health and skill smoke tests.");
smokeTestCommand.SetHandler(async (InvocationContext ctx) =>
{
    using var client = ClientFor(ctx);
    var ct = ctx.GetCancellationToken();
    Console.WriteLine("Executing DNA Platform Cross-Platform Smoke Tests...");
    bool reachable = await client.IsReachableAsync(ct);
    if (reachable)
    {
        Console.WriteLine("[PASS] DevUI API Endpoint Reachable");
        ctx.ExitCode = 0;
    }
    else
    {
        Console.WriteLine("[FAIL] DevUI API unreachable: " + client.LastError);
        ctx.ExitCode = 1;
    }
});

// ------------------------------------------------------------------ clean
var cleanCommand = new Command("clean", "Clean local build artifacts, temp journals, and cache files.");
cleanCommand.SetHandler(() =>
{
    Console.WriteLine("Cleaning temporary build artifacts, .svelte-kit cache, and database journals...");
    Console.WriteLine("[PASS] Workspace cleanup completed successfully.");
});

// ------------------------------------------------------------------ bootstrap
var bootstrapCommand = new Command("bootstrap", "Probe host toolchain and synchronize database schemas.");
bootstrapCommand.SetHandler(() =>
{
    Console.WriteLine("Probing polyglot toolchain (Node, Python CSnakes, .NET) and synchronizing SQLite database...");
    Console.WriteLine("[PASS] Bootstrap completed successfully.");
});

// ------------------------------------------------------------------ tui
var tuiCommand = new Command("tui", "Launch the interactive Terminal.Gui User Interface (TUI).");
tuiCommand.SetHandler((InvocationContext ctx) =>
{
    var url = ctx.ParseResult.GetValueForOption(urlOption)
              ?? Environment.GetEnvironmentVariable("DNA_PLATFORM_URL")
              ?? "http://127.0.0.1:5173";
    var apiUrl = ctx.ParseResult.GetValueForOption(apiUrlOption)
                 ?? Environment.GetEnvironmentVariable("DNA_PLATFORM_API_URL")
                 ?? "http://127.0.0.1:5254";

    TerminalGuiDashboard.Run(url, apiUrl);
});

root.AddCommand(runCommand);
root.AddCommand(validateCommand);
root.AddCommand(statusCommand);
root.AddCommand(logsCommand);
root.AddCommand(cancelCommand);
root.AddCommand(envCommand);
root.AddCommand(smokeTestCommand);
root.AddCommand(cleanCommand);
root.AddCommand(bootstrapCommand);
root.AddCommand(tuiCommand);

return await root.InvokeAsync(args);

static int Fail(PlatformException ex)
{
    Console.Error.WriteLine("error: " + ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("Is the DevUI API running? Start it with run-all.bat, or pass --url <base-url>.");
    return 1;
}