using GitHub.Copilot.SDK;
using Github_Co_Pilot_Local.CoPilot_Client;
using Microsoft.Extensions.Configuration;

await RunAsync();

static async Task RunAsync()
{
    var configuration = LoadConfiguration();
    var mcpServers = GetMcpServers(configuration);

    var copilotClient = new CopilotClient();

    try
    {
        await copilotClient.StartAsync();

        var sessionConfig = CreateSessionConfig(mcpServers);
        var coPilotService = new CoPilotService(sessionConfig, copilotClient);

        await using var session = await coPilotService.GetCopilotSessionAsync();
        using var subscription = RegisterSessionEventHandlers(session);

        await RunInteractiveLoopAsync(session);
    }
    catch (FileNotFoundException)
    {
        Console.WriteLine("Copilot CLI not found. Please install it first.");
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Could not connect to Copilot CLI server.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }
    finally
    {
        await copilotClient.StopAsync();
    }
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
        throw new Exception("Missing mcp server configuration");
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
        McpServers = mcpServers,
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
                Console.WriteLine($"[reasoning: {reasoning.Data.Content}]");
                break;
            case ToolExecutionStartEvent toolStart:
                Console.WriteLine($"  → Running: {toolStart.Data.ToolName} ({toolStart.Data.ToolCallId})");
                break;
            case ToolExecutionCompleteEvent toolEnd:
                Console.WriteLine($"  ✓ Completed: {toolEnd.Data.ToolCallId}");
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

        try
        {
            var reply = await session.SendAndWaitAsync(
                new MessageOptions { Prompt = input },
                timeout: TimeSpan.FromMinutes(5));

            Console.Clear();
            Console.WriteLine($"{reply?.Data.Content}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}




