using ChangeLog.mcp.server.Configurations;
using ChangeLog.mcp.server.Interfaces;
using ChangeLog.mcp.server.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ChangeLog.mcp.server.Extensions
{
    public static class HostApplicationBuilderExtensions
    {
        public static IHost BuildApp(this IHostApplicationBuilder builder, McpServerSettings mcpServerSettings)
        {
            builder.AddServiceDefaults();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(ToSerilogLevel(mcpServerSettings.logLevel))
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: Path.Combine(AppContext.BaseDirectory, "logs", "changelog-.json"),
                    rollingInterval: RollingInterval.Day,
                    shared: true)
                .CreateLogger();

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: true);

            builder.Services.AddSingleton<IChangeLogService, ChangeLogService>();
            builder.Services.AddHttpClient("BlobServiceClient")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });

            if (mcpServerSettings.UseStreamableHttp == true)
            {
                //(builder as WebApplicationBuilder)!.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

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

            var consoleApp = (builder as HostApplicationBuilder)!.Build();

            return consoleApp;
        }

        private static LogEventLevel ToSerilogLevel(LogLevel level) => level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
