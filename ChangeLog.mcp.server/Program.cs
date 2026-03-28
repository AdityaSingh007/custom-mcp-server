using ChangeLog.mcp.server.Configurations;
using ChangeLog.mcp.server.Extensions;

var builder = WebApplication.CreateBuilder(args);
var mcpConfiguration  = builder.Configuration.GetSection("McpServerSettings").Get<McpServerSettings>();

var app = builder.BuildApp(mcpConfiguration ?? new McpServerSettings());

await app.RunAsync();
