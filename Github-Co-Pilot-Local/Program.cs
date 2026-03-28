using GitHub.Copilot.SDK;

await using var client = new CopilotClient();
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-4.1",
    Streaming = true,
    OnPermissionRequest = PromptPermission,
    McpServers = new Dictionary<string, object>
    {
        ["remote-change-log-mcp"] = new McpRemoteServerConfig
        {
            Type = "http",
            Url = "http://0.0.0.0:5000/mcp",
            Tools = new List<string> { "get_version_changes_content" },
        },
    },
}
);


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

    Console.Write("Assistant: ");
    var reply = await session.SendAndWaitAsync(new MessageOptions { Prompt = input });
    Console.WriteLine($"\nAssistant: {reply?.Data.Content}\n");
}


static Task<PermissionRequestResult> PromptPermission(
    PermissionRequest request, PermissionInvocation invocation)
{
    Console.WriteLine($"\n[Permission Request: {request.Kind}]");
    Console.Write("Approve? (y/n): ");

    string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
    PermissionRequestResultKind kind = input is "Y" or "YES" ? PermissionRequestResultKind.Approved : PermissionRequestResultKind.DeniedInteractivelyByUser;

    return Task.FromResult(new PermissionRequestResult { Kind = kind });
}