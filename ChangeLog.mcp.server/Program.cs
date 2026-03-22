var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithResourcesFromAssembly()
                .WithPromptsFromAssembly()
                .WithToolsFromAssembly();

builder.Services.AddCors();

// Configure all logs to go to stderr for MCP server
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Error;
});

var app = builder.Build();

//app.UseHttpsRedirection();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

//app.MapMcp();
app.MapGet("/api/healthz", () => "Healthy");

app.Run();
