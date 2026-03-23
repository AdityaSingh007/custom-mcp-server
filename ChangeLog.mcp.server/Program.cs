using ChangeLog.mcp.server.Configurations;
using ChangeLog.mcp.server.Extensions;
using ChangeLog.mcp.server.Interfaces;
using ChangeLog.mcp.server.Services;

var builder = WebApplication.CreateBuilder(args);
var mcpConfiguration  = builder.Configuration.GetSection("McpServerSettings").Get<McpServerSettings>();

builder.Services.AddSingleton<IChangeLogService, ChangeLogService>();

var app = builder.BuildApp(mcpConfiguration ?? new McpServerSettings());

await app.RunAsync();
