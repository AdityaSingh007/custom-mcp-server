using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.ComponentModel;
using System.Text.Json;

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
            Tools = [AIFunctionFactory.Create(GetVersionChangesAsContent),
                     AIFunctionFactory.Create(GetPackageInformation), 
                     AIFunctionFactory.Create(GetLicenseFromNodeModules)];
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

        [Description(
"""
        Get the list of installed packages along with their version names.
        Use this tool when the user asks for installed packages, dependencies, package inventory, or package versions.
        The tool reads from a file that contains the packages and their version names.
        Extract only the packages intended for the production environment.
        Present the response to the user in a client-facing tabular format.
        """)]
        private byte[] GetPackageInformation()
        {
            _logger.Information("GetPackageInformation started");

            var directoryPath = _configuration["AppPackageDirectoryPath"]
               ?? throw new InvalidOperationException("AppPackageDirectoryPath configuration is missing");

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new InvalidOperationException("AppPackageDirectoryPath configuration is empty");
            }

            _logger.Debug("Resolved package directory path {DirectoryPath}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                _logger.Error("Configured package directory does not exist: {DirectoryPath}", directoryPath);
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            var filePath = Path.Combine(directoryPath, "package.json");

            if (!File.Exists(filePath))
            {
                _logger.Error("Package file not found at {FilePath}", filePath);
                throw new FileNotFoundException($"Package file not found: {filePath}", filePath);
            }

            var fileInfo = new FileInfo(filePath);
            _logger.Information(
                "Reading package file; path={FilePath}; size={FileSize} bytes; lastModified={LastModifiedUtc}",
                filePath,
                fileInfo.Length,
                fileInfo.LastWriteTimeUtc);

            var content = File.ReadAllBytes(filePath);

            _logger.Information(
                "Successfully read package file; bytesRead={BytesRead}",
                content.Length);

            return content;
        }

        [Description(
"""
        Get the license information for an installed package from the local node_modules folder.
        Use this tool when the user asks for a package's license, licensing details, or open-source license information.
        Provide the package name when available.
        The tool searches the local node_modules tree for the package directory and then searches its subdirectories for a license file.
        Return the license name or license file content in a concise client-facing format.
        """)]
        private string GetLicenseFromNodeModules(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Package name is required.", nameof(packageName));
            }

            var nodeModulesDirectoryPath = _configuration["NodeModulesDirectoryPath"]
                ?? throw new InvalidOperationException("NodeModulesDirectoryPath configuration is missing");

            if (string.IsNullOrWhiteSpace(nodeModulesDirectoryPath))
            {
                throw new InvalidOperationException("NodeModulesDirectoryPath configuration is empty");
            }

            if (!Directory.Exists(nodeModulesDirectoryPath))
            {
                _logger.Error("Configured node_modules directory does not exist: {DirectoryPath}", nodeModulesDirectoryPath);
                throw new DirectoryNotFoundException($"Directory not found: {nodeModulesDirectoryPath}");
            }

            _logger.Information(
                "Searching node_modules for package {PackageName} under {DirectoryPath}",
                packageName,
                nodeModulesDirectoryPath);

            var packageDirectoryComponents = packageName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (packageDirectoryComponents.Length == 0)
            {
                throw new ArgumentException("Package name is required.", nameof(packageName));
            }

            var packageDirectoryNameTop = packageDirectoryComponents[0];
            var packageDirectoryNameSub = packageDirectoryComponents.Length > 1
                ? packageDirectoryComponents[^1]
                : packageDirectoryComponents[0];

            _logger.Debug(
                "Resolved package search segments. Top={PackageDirectoryNameTop}, Sub={PackageDirectoryNameSub}",
                packageDirectoryNameTop,
                packageDirectoryNameSub);

            var topLevelDirectory = Directory.EnumerateDirectories(nodeModulesDirectoryPath, "*", SearchOption.AllDirectories)
                .FirstOrDefault(directory =>
                    string.Equals(Path.GetFileName(directory), packageDirectoryNameTop, StringComparison.OrdinalIgnoreCase));

            if (topLevelDirectory is null)
            {
                _logger.Warning(
                    "Package top-level directory not found for {PackageName}; searched for {PackageDirectoryNameTop}",
                    packageName,
                    packageDirectoryNameTop);
                return string.Empty;
            }

            _logger.Information("Matched top-level package directory: {TopLevelDirectory}", topLevelDirectory);

            var packageDirectory = packageDirectoryComponents.Length > 1
                ? Path.Combine(topLevelDirectory, packageDirectoryNameSub)
                : topLevelDirectory;

            if (!Directory.Exists(packageDirectory))
            {
                _logger.Warning(
                    "Package subdirectory not found for {PackageName}; expected {PackageDirectory}",
                    packageName,
                    packageDirectory);
                return string.Empty;
            }

            _logger.Information("Searching for license files under {PackageDirectory}", packageDirectory);

            var licenseFile = Directory.EnumerateFiles(packageDirectory, "*", SearchOption.AllDirectories)
                .FirstOrDefault(file =>
                {
                    var fileName = Path.GetFileName(file);
                    return fileName.StartsWith("license", StringComparison.OrdinalIgnoreCase) ||
                           fileName.StartsWith("copying", StringComparison.OrdinalIgnoreCase);
                });

            if (licenseFile is null)
            {
                _logger.Warning("No license file found under package directory {PackageDirectory}", packageDirectory);
                return string.Empty;
            }

            _logger.Information("License file found for {PackageName}: {LicenseFile}", packageName, licenseFile);

            var licenseContent = File.ReadAllText(licenseFile);
            _logger.Debug("Read license file content for {PackageName}; length={Length}", packageName, licenseContent.Length);

            return licenseContent;
        }
    }
}
