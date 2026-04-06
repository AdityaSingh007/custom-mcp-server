using GitHub.Copilot.SDK;
using Github_Co_Pilot_Local.CoPilot_Client;
using Github_Co_Pilot_Local.LocalTools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Github-Co-Pilot-Local")
    .WriteTo.File(
        formatter: new CompactJsonFormatter(),
        path: Path.Combine(AppContext.BaseDirectory, "logs", "github-copilot-local-.json"),
        rollingInterval: RollingInterval.Day,
        shared: true)
    .CreateLogger();

try
{
    Log.Information("Application starting");
    await RunAsync();
    Log.Information("Application finished successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    WriteErrorLine(GetFriendlyErrorMessage(ex));
}
finally
{
    Log.Information("Flushing logs and shutting down");
    Log.CloseAndFlush();
}

static async Task RunAsync()
{
    CopilotClient? copilotClient = null;

    try
    {
        Log.Information("Loading configuration");
        var configuration = LoadConfiguration();
        var customTools = new CustomTools(configuration, Log.ForContext<CustomTools>());
        Log.Information("Custom tools initialized");

        var mcpServers = GetMcpServers(configuration);
        Log.Information("Loaded {McpServerCount} MCP server configuration(s)", mcpServers.Count);

        var gitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN", EnvironmentVariableTarget.User)
            ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set. Please set it in your user environment variables and try again.");

        var cliServerMode = configuration.GetValue("CliServerMode", "local");
        Log.Information("CLI server mode resolved to {CliServerMode}", cliServerMode);

        if (string.Equals("server", cliServerMode, StringComparison.OrdinalIgnoreCase))
        {
            var cliUrl = configuration.GetValue<string>("cliServerUrl");
            if (string.IsNullOrWhiteSpace(cliUrl))
            {
                throw new InvalidOperationException("cliServerUrl configuration is required when CliServerMode is set to 'server'. Please check your appsettings.json and try again.");
            }

            Log.Information("Using Copilot CLI server at {CliUrl}", cliUrl);
            copilotClient = new CopilotClient(new CopilotClientOptions()
            {
                CliUrl = cliUrl,
            });
        }
        else
        {
            var cliPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vendor", "copilot", "copilot.exe");
            Log.Information("Using local Copilot CLI at {CliPath}", cliPath);

            if (!File.Exists(cliPath))
            {
                throw new FileNotFoundException($"Copilot CLI not found at '{cliPath}'.", cliPath);
            }

            copilotClient = new CopilotClient(new CopilotClientOptions()
            {
                CliPath = cliPath,
                GitHubToken = gitHubToken,
                UseLoggedInUser = false,
            });
        }

        Log.Information("Starting Copilot client");
        await copilotClient.StartAsync();
        Log.Information("Copilot client started");

        var isServerMode = string.Equals("server", cliServerMode, StringComparison.OrdinalIgnoreCase);
        var sessionConfig = CreateSessionConfig(mcpServers, customTools.Tools, isServerMode: isServerMode);
        Log.Information("Session configuration created for {Mode} mode", isServerMode ? "server" : "local");

        var coPilotService = new CoPilotService(sessionConfig, copilotClient);
        Log.Information("Copilot service created");

        await using var session = await coPilotService.GetCopilotSessionAsync();
        Log.Information("Copilot session opened");

        using var subscription = RegisterSessionEventHandlers(session);

        await RunInteractiveLoopAsync(session);
        Log.Information("Interactive loop exited");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "RunAsync failed");
        WriteErrorLine(GetFriendlyErrorMessage(ex));
    }
    finally
    {
        if (copilotClient is not null)
        {
            Log.Information("Stopping Copilot client");
            await copilotClient.StopAsync();
            Log.Information("Copilot client stopped");
        }
    }
}

static void WriteErrorLine(string message)
{
    var previousColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ForegroundColor = previousColor;
}

static async Task RunThinkingSpinnerAsync(CancellationToken cancellationToken)
{
    var frames = new[] { "|", "/", "-", "\\" };
    var index = 0;

    while (!cancellationToken.IsCancellationRequested)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"\rThinking... {frames[index++ % frames.Length]} ");
        Console.ForegroundColor = previousColor;

        try
        {
            await Task.Delay(20, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            break;
        }
    }
}

static string GetFriendlyErrorMessage(Exception ex)
{
    return ex switch
    {
        FileNotFoundException fileNotFound => fileNotFound.Message,
        InvalidOperationException invalidOperation => invalidOperation.Message,
        UnauthorizedAccessException => "Access was denied. Please check your permissions and try again.",
        HttpRequestException httpRequestException when httpRequestException.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) =>
            "Could not connect to Copilot CLI server. Please make sure it is running and try again.",
        HttpRequestException => "A network error occurred while communicating with Copilot CLI.",
        JsonException => "The configuration file is invalid. Please check appsettings.json and try again.",
        _ => $"An unexpected error occurred: {ex.Message}"
    };
}

static IConfigurationRoot LoadConfiguration()
{
    Log.Debug("Loading appsettings.json from {CurrentDirectory}", Directory.GetCurrentDirectory());

    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
}

static Dictionary<string, object> GetMcpServers(IConfiguration configuration)
{
    var mcpServerSettings = configuration.GetSection("McpServers").Get<Dictionary<string, McpRemoteServerConfig>>();

    if (mcpServerSettings is null || mcpServerSettings.Count == 0)
    {
        throw new InvalidOperationException("Missing MCP server configuration in appsettings.json.");
    }

    Log.Debug("MCP servers found: {McpServerNames}", string.Join(", ", mcpServerSettings.Keys));

    return mcpServerSettings.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
}

static SessionConfig CreateSessionConfig(Dictionary<string, object> mcpServers, AIFunction[] tools, bool isServerMode = false)
{
    Log.Debug(
        "Creating session config. IsServerMode={IsServerMode}, ToolCount={ToolCount}, McpServerCount={McpServerCount}",
        isServerMode,
        tools.Length,
        mcpServers.Count);

    if (isServerMode)
    {
        return new SessionConfig
        {
            Model = "GPT-5.4",
            Streaming = true,
            OnPermissionRequest = PermissionHandler.ApproveAll,
            Tools = tools
        };
    }
    else
    {
        return new SessionConfig
        {
            Model = "GPT-5.4",
            Streaming = true,
            OnPermissionRequest = PermissionHandler.ApproveAll,
            //McpServers = mcpServers,
            Tools = tools
        };
    }
}

static IDisposable RegisterSessionEventHandlers(CopilotSession session)
{
    Log.Debug("Registering session event handlers");

    return session.On(evt =>
    {
        Console.ForegroundColor = ConsoleColor.Blue;

        switch (evt)
        {
            case AssistantReasoningEvent reasoning:
                Log.Debug("Assistant reasoning event received");
                Console.WriteLine($"\n[reasoning: {reasoning.Data.Content}]");
                break;
            case ToolExecutionStartEvent toolStart:
                Log.Information("Tool execution started: {ToolName} ({ToolCallId})", toolStart.Data.ToolName, toolStart.Data.ToolCallId);
                Console.WriteLine($"\nRunning: {toolStart.Data.ToolName} ({toolStart.Data.ToolCallId})\n");
                break;
            case ToolExecutionCompleteEvent toolEnd:
                Log.Information("Tool execution completed: {ToolCallId}", toolEnd.Data.ToolCallId);
                Console.WriteLine($"\nCompleted: {toolEnd.Data.ToolCallId}\n");
                break;
            case SessionIdleEvent:
                Log.Information("Session idle; request completed");
                Console.WriteLine("\nRequest completed\n");
                break;
        }

        Console.ResetColor();
    });
}

static async Task RunInteractiveLoopAsync(CopilotSession session)
{
    Log.Information("Interactive loop started");

    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Interactive loop exit requested by user");
            break;
        }

        Log.Information("User input received; length={InputLength}", input.Length);

        var spinnerCts = new CancellationTokenSource();
        var spinnerTask = RunThinkingSpinnerAsync(spinnerCts.Token);

        try
        {
            Log.Information("Sending prompt to Copilot session");
            var reply = await session.SendAndWaitAsync(
                new MessageOptions { Prompt = input },
                timeout: TimeSpan.FromMinutes(5));

            spinnerCts.Cancel();
            await spinnerTask;
            Console.WriteLine();

            var responseLength = reply?.Data.Content?.Length ?? 0;
            Log.Information("Copilot response received; length={ResponseLength}", responseLength);

            Console.Clear();
            Console.WriteLine($"{reply?.Data.Content}\n");
        }
        catch (Exception ex)
        {
            spinnerCts.Cancel();
            await spinnerTask;
            Console.WriteLine();

            Log.Error(ex, "Failed to process user prompt");
            WriteErrorLine($"Error: {ex.Message}");
        }
        finally
        {
            spinnerCts.Dispose();
        }
    }

    Log.Information("Interactive loop ended");
}