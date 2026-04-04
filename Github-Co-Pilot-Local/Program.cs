using GitHub.Copilot.SDK;
using Github_Co_Pilot_Local.CoPilot_Client;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

await RunAsync();

static async Task RunAsync()
{
    CopilotClient? copilotClient = null;

    try
    {
        var configuration = LoadConfiguration();
        var mcpServers = GetMcpServers(configuration);

        var gitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN", EnvironmentVariableTarget.User)
            ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set. Please set it in your user environment variables and try again.");

        var cliServerMode = configuration.GetValue("CliServerMode", "local");

        if (string.Equals("server", cliServerMode))
        {
            var cliUrl = configuration.GetValue<string>("cliServerUrl");
            if (string.IsNullOrWhiteSpace(cliUrl))
            {
                throw new InvalidOperationException("cliServerUrl configuration is required when CliServerMode is set to 'server'. Please check your appsettings.json and try again.");
            }
            copilotClient = new CopilotClient(new CopilotClientOptions()
            {
                CliUrl = cliUrl,
            });
        }
        else
        {
            var cliPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vendor", "copilot", "copilot.exe");

            if (!File.Exists(cliPath))
            {
                throw new FileNotFoundException($"Copilot CLI not found at '{cliPath}'.", cliPath);
            }
            copilotClient = new CopilotClient(new CopilotClientOptions()
            {
                CliPath = cliPath,
                GitHubToken = gitHubToken,
            });
        }
        await copilotClient.StartAsync();

        var sessionConfig = CreateSessionConfig(mcpServers);
        var coPilotService = new CoPilotService(sessionConfig, copilotClient);

        await using var session = await coPilotService.GetCopilotSessionAsync();
        using var subscription = RegisterSessionEventHandlers(session);

        await RunInteractiveLoopAsync(session);
    }
    catch (Exception ex)
    {
        WriteErrorLine(GetFriendlyErrorMessage(ex));
    }
    finally
    {
        if (copilotClient is not null)
        {
            await copilotClient.StopAsync();
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

    return mcpServerSettings.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
}

static SessionConfig CreateSessionConfig(Dictionary<string, object> mcpServers)
{
    return new SessionConfig
    {
        Model = "GPT-5.4",
        Streaming = true,
        OnPermissionRequest = PermissionHandler.ApproveAll,
        //McpServers = mcpServers,
    };
}

static IDisposable RegisterSessionEventHandlers(CopilotSession session)
{
    return session.On(evt =>
    {
        Console.ForegroundColor = ConsoleColor.Blue;

        switch (evt)
        {
            case AssistantReasoningEvent reasoning:
                Console.WriteLine($"\n[reasoning: {reasoning.Data.Content}]");
                break;
            case ToolExecutionStartEvent toolStart:
                Console.WriteLine($"\nRunning: {toolStart.Data.ToolName} ({toolStart.Data.ToolCallId})\n");
                break;
            case ToolExecutionCompleteEvent toolEnd:
                Console.WriteLine($"\nCompleted: {toolEnd.Data.ToolCallId}\n");
                break;
            case SessionIdleEvent sessionIdle:
                Console.WriteLine($"\nRequest completed\n");
                break;
        }

        Console.ResetColor();
    });
}

static async Task RunInteractiveLoopAsync(CopilotSession session)
{
    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        var spinnerCts = new CancellationTokenSource();
        var spinnerTask = RunThinkingSpinnerAsync(spinnerCts.Token);

        try
        {
            var reply = await session.SendAndWaitAsync(
                new MessageOptions { Prompt = input },
                timeout: TimeSpan.FromMinutes(5));

            spinnerCts.Cancel();
            await spinnerTask;
            Console.WriteLine();

            Console.Clear();
            Console.WriteLine($"{reply?.Data.Content}\n");
        }
        catch (Exception ex)
        {
            spinnerCts.Cancel();
            await spinnerTask;
            Console.WriteLine();
            WriteErrorLine($"Error: {ex.Message}");
        }
        finally
        {
            spinnerCts.Dispose();
        }
    }
}