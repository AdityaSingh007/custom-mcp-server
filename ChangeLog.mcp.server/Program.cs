var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Services.AddMcpServer()
                .WithHttpTransport(options => 
                {
                    options.Stateless = true;
                })
                .WithResourcesFromAssembly()
                .WithPromptsFromAssembly()
                .WithToolsFromAssembly();

builder.Services.AddCors();

builder.Services.AddHttpContextAccessor();

//builder.Configuration
//    .AddUserSecrets<Program>()
//    .AddEnvironmentVariables();

// Configure all logs to go to stderr for MCP server
//builder.Logging.AddConsole(consoleLogOptions =>
//{
//    // Configure all logs to go to stderr
//    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Error;
//});

var app = builder.Build();

//app.UseHttpsRedirection();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.MapMcp(pattern:"/mcp");
app.MapGet("/api/healthz", () => "Healthy");

app.Run();
