namespace ChangeLog.mcp.server.Configurations
{
    public class McpServerSettings
    {
        public string ServerUrl { get; set; } = "http://0.0.0.0:5000";
        public bool UseStreamableHttp { get; set; } = true;
        public bool IsStateless { get; set; } = true;

        public LogLevel logLevel { get; set; }

        public string McpEndpoint { get; set; } = "/mcp";

        public string[] AllowedOrigins { get; set; } = new string[] { "*" };
    }
}
