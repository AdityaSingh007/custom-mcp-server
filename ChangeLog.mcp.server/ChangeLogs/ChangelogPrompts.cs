using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChangeLog.mcp.server.ChangeLogs
{
    [McpServerPromptType]
    public class ChangelogPrompts
    {
        [McpServerPrompt(Title = "Get Api Version Changes", Name = "Get Api Version Changes")]
        [Description("Uses the get api version changes tool to find changes made in a specific api version.")]
        public static string GetApiVersionChangesPrompt(
        [Description("api version name (e.g. '1.0.0')")]
        string versionName)
        {
            return $"Using the Get api version changes tool , please get the list of changes made. The api version is  {versionName}.";
        }
    }
}
