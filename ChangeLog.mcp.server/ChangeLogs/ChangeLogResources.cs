using ChangeLog.mcp.server.Interfaces;
using ChangeLog.mcp.server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace ChangeLog.mcp.server.NewFolder
{
    [McpServerResourceType]
    public class ChangeLogResources
    {
        private readonly IChangeLogService _changeLogService;

        public ChangeLogResources(IChangeLogService changeLogService)
        {
            _changeLogService = changeLogService;
        }

        public const string ResourceApiChangelogsUri = "api://changelogs";
        public const string ResourceApiChangelogsDocumentUri = "api://changelogs/{apiversion}";

        [McpServerResource(
        UriTemplate = ResourceApiChangelogsUri,
        Name = "changelog-documents.json",
        Title = "Api changelog Documents",
        MimeType = "application/json")]
        [Description("Provides a list of changelog documents available to application users.")]
        public async Task<IEnumerable<ResourceContents>> DocumentListResource(CancellationToken cancellationToken)
        {
            var documentInfos = await _changeLogService.GetMarkdownFilesWithMetadataAsync(cancellationToken);

            return documentInfos.Select(info => new TextResourceContents
            {
                Text = JsonSerializer.Serialize(info, McpJsonUtilities.DefaultOptions),
                MimeType = "application/json",
                Uri = ResourceApiChangelogsDocumentUri.Replace("{apiversion}", info.Version),
            });
        }

        [McpServerResource(
        UriTemplate = ResourceApiChangelogsDocumentUri,
        Name = "Api changelog Document by ID",
        MimeType = "text/markdown")]
        [Description("Retrieves a specific change log file")]
        public async Task<ResourceContents> DocumentResourceById(string apiversion, CancellationToken cancellationToken)
        {
            var downloadResult = await _changeLogService.GetChangeLogContentAsync(apiversion, cancellationToken);

            if (string.IsNullOrEmpty(downloadResult))
            {
                throw new McpProtocolException("Change log document content is empty", McpErrorCode.InternalError);
            }

            return new TextResourceContents
            {
                Text = downloadResult,
                MimeType = "text/markdown",
                Uri = ResourceApiChangelogsDocumentUri.Replace("{apiversion}", apiversion),
            };
        }
    }
}
