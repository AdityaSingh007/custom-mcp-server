using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChangeLog.mcp.server.ChangeLogs
{
    [McpServerToolType]
    public class ChangelogTools
    {
        private readonly IConfiguration _configuration;

        // Inject IConfiguration through the constructor
        public ChangelogTools(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [McpServerTool(Title = "Get api version changes"), Description(
       """
        Help get information about api version changes for a specific version.
        The tool helps get information about api version changes for a specific version.
        Prompt for the version unless they provided it already. 
        Present the response to the user in a client-facing format.
        """)]
        public async Task<IEnumerable<ContentBlock>> GetVersionChanges(
        [Description("Provided by the user")] string versionName,
        CancellationToken cancellationToken)
        {
            var filePath = $"{_configuration["changeLogDirPath"] ?? throw new ArgumentNullException("changeLogDirPath")}\\api-v{versionName}.md";
            var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            return [
            new TextContentBlock
            {
                Text="You can find the api version changes below."
            },
            //new EmbeddedResourceBlock
            //{
            //    Resource = new TextResourceContents()
            //    {
            //        Text = fileContent,
            //        MimeType = "text/markdown",
            //        Uri = $"api://changelogs/{versionName}"
            //    }
            //},

            new ResourceLinkBlock
            {
                Name = "View raw markdown",
                MimeType = "text/markdown",
                Uri = $"api://changelogs/{versionName}"
            }
           ];

        }

        [McpServerTool(Name = "get_version_changes_content", Title = "Get api version changes"), Description(
       """
        Help get information about api version changes for a specific version.
        The tool helps get information about api version changes for a specific version by extracting information from a swagger file.
        If the file is too large use techniques for efficient read.
        Prompt for the version unless they provided it already. 
        Present the response to the user in a client-facing format.
        """)]
        public async Task<BlobResourceContents> GetVersionChangesAsContent(
        [Description("Provided by the user")] string versionName,
        CancellationToken cancellationToken)
        {
            var filePath = $"{_configuration["changeLogDirPath"] ?? throw new ArgumentNullException("changeLogDirPath")}\\swagger-{versionName}.json";
            try
            {
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, cancellationToken);

                return new BlobResourceContents
                {
                    Blob = fileBytes,
                    MimeType = "application/octet-stream",
                    Uri = $"file://{filePath}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new BlobResourceContents
                {
                    Blob = Array.Empty<byte>(),
                    MimeType = "text/plain",
                    Uri = $"error://{filePath}"
                };
            }
            finally
            {

            }

        }
    }
}
