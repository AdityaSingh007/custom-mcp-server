var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.ChangeLog_mcp_server>("mcp")
    .WithHttpHealthCheck("/health");

var mcpInspector = builder.AddMcpInspector("mcp-inspector")
       .WithMcpServer(mcp);

builder.AddProject<Projects.BlobService>("blobservice");

builder.Build().Run();
