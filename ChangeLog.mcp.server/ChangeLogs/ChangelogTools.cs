using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChangeLog.mcp.server.ChangeLogs
{
    [McpServerToolType]
    public class ChangelogTools
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChangelogTools> _logger;

        // Inject IConfiguration through the constructor
        public ChangelogTools(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ChangelogTools> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
            _logger.LogInformation("GetVersionChanges started for version {VersionName}", versionName);

            try
            {
                var changeLogDirPath = _configuration["changeLogDirPath"] ?? throw new ArgumentNullException("changeLogDirPath");
                var filePath = $"{changeLogDirPath}\\api-v{versionName}.md";

                _logger.LogDebug("Resolved changelog file path for version {VersionName}: {FilePath}", versionName, filePath);

                var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
                _logger.LogInformation("Successfully read changelog file for version {VersionName}; content length {ContentLength}", versionName, fileContent.Length);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetVersionChanges failed for version {VersionName}", versionName);
                throw;
            }

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
            _logger.LogInformation("GetVersionChangesAsContent started for version {VersionName}", versionName);

            try
            {
                var httpClient = _httpClientFactory.CreateClient("BlobServiceClient");
                var requestUri = $"https://blobservice/api/v1/FileBlob/{versionName}";

                _logger.LogDebug("Requesting blob content from {RequestUri}", requestUri);

                var response = await httpClient.GetAsync(requestUri, cancellationToken);
                _logger.LogInformation("BlobService responded with status code {StatusCode} for version {VersionName}", (int)response.StatusCode, versionName);
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                _logger.LogInformation("Retrieved blob content for version {VersionName}; byte length {ByteLength}; content type {ContentType}",
                    versionName,
                    fileBytes.Length,
                    response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");

                return new BlobResourceContents
                {
                    Blob = fileBytes,
                    MimeType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                    Uri = $"file://swagger-{versionName}.json"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetVersionChangesAsContent failed for version {VersionName}", versionName);
                return new BlobResourceContents
                {
                    Blob = Array.Empty<byte>(),
                    MimeType = "text/plain",
                    Uri = $"error://swagger-{versionName}.json"
                };
            }
        }
    }
}
