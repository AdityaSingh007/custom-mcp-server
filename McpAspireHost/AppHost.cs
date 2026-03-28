var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.ChangeLog_mcp_server>("mcp")
    .WithHttpHealthCheck("/api/healthz");

builder.AddMcpInspector("mcp-inspector")
    .WithMcpServer(mcp);

builder.Build().Run();
