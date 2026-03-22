using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChangeLog.mcp.server.NewFolder
{
    [McpServerResourceType]
    public class ChangeLogResources
    {
        private readonly IConfiguration _configuration;

        // Inject IConfiguration through the constructor
        public ChangeLogResources(IConfiguration configuration)
        {
            _configuration = configuration;
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
            var documentInfos = await GetMarkdownFilesWithMetadataAsync(_configuration["changeLogDirPath"] ?? throw new ArgumentNullException("changeLogDirPath"), cancellationToken);

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
            var downloadResult = await File.ReadAllTextAsync($"{_configuration["changeLogDirPath"] ?? throw new ArgumentNullException("changeLogDirPath")}\\api-v{apiversion}.md", cancellationToken);

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

        public List<MarkdownFileInfo> GetMarkdownFilesWithMetadata(string directoryPath)
        {
            var markdownFiles = new List<MarkdownFileInfo>();

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            var mdFiles = Directory.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories);

            foreach (var filePath in mdFiles)
            {
                var fileInfo = new FileInfo(filePath);

                // Try to extract a version from file name. Expected pattern: api-v{version}.md
                string version = string.Empty;
                var match = Regex.Match(fileInfo.Name, @"api-v(?<version>[\w\.\-]+)\.md$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    version = match.Groups["version"].Value;
                }

                if (!string.IsNullOrWhiteSpace(version))
                {
                    markdownFiles.Add(new MarkdownFileInfo
                    {
                        FileName = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        RelativePath = Path.GetRelativePath(directoryPath, fileInfo.FullName),
                        SizeInBytes = fileInfo.Length,
                        CreatedDate = fileInfo.CreationTime,
                        LastModifiedDate = fileInfo.LastWriteTime,
                        LastAccessedDate = fileInfo.LastAccessTime,
                        IsReadOnly = fileInfo.IsReadOnly,
                        Version = version
                    });
                }


            }

            return markdownFiles;
        }

        public async Task<List<MarkdownFileInfo>> GetMarkdownFilesWithMetadataAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => GetMarkdownFilesWithMetadata(directoryPath), cancellationToken);
        }
    }

    public class MarkdownFileInfo
    {
        public required string FileName { get; set; }
        public required string FullPath { get; set; }
        public required string RelativePath { get; set; }
        public long SizeInBytes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public DateTime LastAccessedDate { get; set; }
        public bool IsReadOnly { get; set; }
        public required string Version { get; set; }
    }
}
