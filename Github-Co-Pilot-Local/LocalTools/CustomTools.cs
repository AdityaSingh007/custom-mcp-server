using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.ComponentModel;

namespace Github_Co_Pilot_Local.LocalTools
{
    public class CustomTools
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public CustomTools(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.Debug("Initializing {ToolType}", nameof(CustomTools));
            Tools = [AIFunctionFactory.Create(GetVersionChangesAsContent)];
            _logger.Information("Registered {ToolCount} AI tool(s)", Tools.Length);
        }

        public AIFunction[] Tools { get; }

        [Description(
       """
        Help get information about api version changes for a specific version.
        The tool helps get information about api version changes for a specific version by extracting information from a swagger file.
        If the file is too large use techniques for efficient read.
        Prompt for the version unless they provided it already. 
        Present the response to the user in a client-facing format.
        """)]
        private byte[] GetVersionChangesAsContent(string fileVersion)
        {
            if (string.IsNullOrWhiteSpace(fileVersion))
            {
                throw new ArgumentException("A version value is required.", nameof(fileVersion));
            }

            _logger.Information("GetVersionChangesAsContent started for version {FileVersion}", fileVersion);

            var directoryPath = _configuration["LocalBlobStoragePath"]
                ?? throw new ArgumentNullException("LocalBlobStoragePath");

            if (!Directory.Exists(directoryPath))
            {
                _logger.Error("Configured swagger directory does not exist: {DirectoryPath}", directoryPath);
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            var fileName = $"swagger-{fileVersion}.json";
            var filePath = Path.Combine(directoryPath, fileName);

            _logger.Debug("Resolved swagger file path {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.Warning("Swagger file not found for version {FileVersion} at {FilePath}", fileVersion, filePath);
                throw new FileNotFoundException($"Swagger file not found: {filePath}", filePath);
            }

            var fileInfo = new FileInfo(filePath);
            _logger.Information(
                "Reading swagger file for version {FileVersion}; path={FilePath}; size={FileSize} bytes; lastModified={LastModifiedUtc}",
                fileVersion,
                filePath,
                fileInfo.Length,
                fileInfo.LastWriteTimeUtc);

            var content = File.ReadAllBytes(filePath);

            _logger.Information(
                "Successfully read swagger file for version {FileVersion}; bytesRead={BytesRead}",
                fileVersion,
                content.Length);

            return content;
        }
    }
}
