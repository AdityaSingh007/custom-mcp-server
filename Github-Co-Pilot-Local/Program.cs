using GitHub.Copilot.SDK;
using Github_Co_Pilot_Local.CoPilot_Client;
using Github_Co_Pilot_Local.McpUtils;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfigurationRoot configuration = builder.Build();
var mcpServerSettings = configuration.GetSection("McpServers").Get<Dictionary<string, McpRemoteServerConfig>>();

// If configuration is missing or empty, fall back to a default MCP server config.
if (mcpServerSettings == null || mcpServerSettings.Count == 0)
{
    throw new Exception("Missing mcp server configuration");
}

// Convert to the expected Dictionary<string, object> for the session config.
var mcpServersForSession = mcpServerSettings.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

var coPilot_Service = new CoPilotService(new SessionConfig
{
    Model = "gpt-4.1",
    Streaming = true,
    OnPermissionRequest = McpPermission.PromptPermission,
    McpServers = mcpServersForSession,
});

await using var session = await coPilot_Service.GetCopilotSessionAsync();


using var _ = session.On(evt =>
{
    Console.ForegroundColor = ConsoleColor.Blue;
    switch (evt)
    {
        case AssistantReasoningEvent reasoning:
            Console.WriteLine($"[reasoning: {reasoning.Data.Content}]");
            break;
        case ToolExecutionStartEvent tool:
            Console.WriteLine($"[tool: {tool.Data.ToolName}]");
            break;
    }
    Console.ResetColor();
});


while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();

    if (string.IsNullOrEmpty(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var reply = await session.SendAndWaitAsync(new MessageOptions { Prompt = input }, timeout: TimeSpan.FromMinutes(5));
        // print the assistant reply on the same line after the prefix
        Console.WriteLine($"{reply?.Data.Content}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    finally
    {

    }

}


