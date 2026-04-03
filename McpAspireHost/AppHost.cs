var builder = DistributedApplication.CreateBuilder(args);

var blobService = builder.AddProject<Projects.BlobService>("blobservice");

var mcp = builder.AddProject<Projects.ChangeLog_mcp_server>("mcp")
    .WithHttpHealthCheck("/health")
    .WithReference(blobService);

var mcpInspector = builder.AddMcpInspector("mcp-inspector")
       .WithMcpServer(mcp);

builder.Build().Run();
