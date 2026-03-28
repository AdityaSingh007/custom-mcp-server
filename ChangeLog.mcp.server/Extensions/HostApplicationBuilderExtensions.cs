using ChangeLog.mcp.server.Configurations;
using ChangeLog.mcp.server.Interfaces;
using ChangeLog.mcp.server.Services;

namespace ChangeLog.mcp.server.Extensions
{
    public static class HostApplicationBuilderExtensions
    {
        public static IHost BuildApp(this IHostApplicationBuilder builder, McpServerSettings mcpServerSettings)
        {
            builder.AddServiceDefaults();

            builder.Services.AddSingleton<IChangeLogService, ChangeLogService>();

            if (mcpServerSettings.UseStreamableHttp == true)
            {
                //(builder as WebApplicationBuilder)?.WebHost.UseUrls(mcpServerSettings.ServerUrl);

                builder.Services.AddMcpServer()
                                .WithHttpTransport(o => o.Stateless = mcpServerSettings.IsStateless)
                                .WithPromptsFromAssembly()
                                .WithResourcesFromAssembly()
                                .WithToolsFromAssembly();

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy(name: "AllowLocalhostOrigins",
                        policy =>
                        {
                            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                        });
                });

                builder.Services.AddHttpContextAccessor();

                var webApp = (builder as WebApplicationBuilder)!.Build();
                

                // Disable HTTPS redirection in development environment to avoid issues with self-signed certificates
                if (!webApp.Environment.IsDevelopment())
                {
                    webApp.UseHttpsRedirection();
                }

                webApp.UseCors("AllowLocalhostOrigins");
                webApp.MapMcp($"/{mcpServerSettings.McpEndpoint}");
                webApp.MapGet("/health", () => "Healthy");

                return webApp;
            }

            builder.Services.AddMcpServer()
                            .WithStdioServerTransport()
                            .WithPromptsFromAssembly()
                            .WithResourcesFromAssembly()
                            .WithToolsFromAssembly();

            builder.Configuration
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();

            // Configure all logs to go to stderr for MCP server
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = mcpServerSettings.logLevel;
            });

            var consoleApp = (builder as HostApplicationBuilder)!.Build();

            return consoleApp;
        }
    }
}
