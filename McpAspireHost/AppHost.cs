var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.ChangeLog_mcp_server>("mcp")
    .WithHttpHealthCheck("/health");

builder.AddMcpInspector("mcp-inspector")
    .WithMcpServer(mcp);

builder.Build().Run();
