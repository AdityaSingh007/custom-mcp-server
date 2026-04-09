using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.ComponentModel;
using System.Text;

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
                     AIFunctionFactory.Create(GetLicenseFromNodeModules),
                     AIFunctionFactory.Create(GetCoPilotLogInformation)];
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
        private async Task<byte[]> GetVersionChangesAsContent(string fileVersion)
        {
            if (string.IsNullOrWhiteSpace(fileVersion))
            {
                _logger.Error("GetVersionChangesAsContent called without a version value");
                return Encoding.UTF8.GetBytes("A version value is required to read the Swagger file.");
            }

            try
            {
                _logger.Information("GetVersionChangesAsContent started for version {FileVersion}", fileVersion);

                var directoryPath = _configuration["LocalBlobStoragePath"];
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    _logger.Error("LocalBlobStoragePath configuration is missing or empty");
                    return Encoding.UTF8.GetBytes("Swagger file location is not configured.");
                }

                if (!Directory.Exists(directoryPath))
                {
                    _logger.Error("Configured swagger directory does not exist: {DirectoryPath}", directoryPath);
                    return Encoding.UTF8.GetBytes($"Swagger directory was not found: {directoryPath}");
                }

                var fileName = $"swagger-{fileVersion}.json";
                var filePath = Path.Combine(directoryPath, fileName);

                _logger.Debug("Resolved swagger file path {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    _logger.Warning("Swagger file not found for version {FileVersion} at {FilePath}", fileVersion, filePath);
                    return Encoding.UTF8.GetBytes($"Swagger file was not found for version {fileVersion}.");
                }

                var fileInfo = new FileInfo(filePath);
                _logger.Information(
                    "Reading swagger file for version {FileVersion}; path={FilePath}; size={FileSize} bytes; lastModified={LastModifiedUtc}",
                    fileVersion,
                    filePath,
                    fileInfo.Length,
                    fileInfo.LastWriteTimeUtc);

                var content = await File.ReadAllBytesAsync(filePath);

                _logger.Information(
                    "Successfully read swagger file for version {FileVersion}; bytesRead={BytesRead}",
                    fileVersion,
                    content.Length);

                return content;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read swagger file for version {FileVersion}", fileVersion);
                return Encoding.UTF8.GetBytes($"Unable to read Swagger file for version {fileVersion}.");
            }
        }

        [Description(
"""
        Get the list of installed packages along with their version names.
        Use this tool when the user asks for installed packages, dependencies, package inventory, or package versions.
        The tool reads from a file that contains the packages and their version names.
        Extract only the packages intended for the production environment.
        Present the response to the user in a client-facing tabular format.
        """)]
        private async Task<byte[]> GetPackageInformation()
        {
            try
            {
                _logger.Information("GetPackageInformation started");

                var directoryPath = _configuration["AppPackageDirectoryPath"];
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    _logger.Error("AppPackageDirectoryPath configuration is missing or empty");
                    return Encoding.UTF8.GetBytes("Package directory configuration is missing.");
                }

                _logger.Debug("Resolved package directory path {DirectoryPath}", directoryPath);

                if (!Directory.Exists(directoryPath))
                {
                    _logger.Error("Configured package directory does not exist: {DirectoryPath}", directoryPath);
                    return Encoding.UTF8.GetBytes($"Package directory was not found: {directoryPath}");
                }

                var filePath = Path.Combine(directoryPath, "package.json");

                if (!File.Exists(filePath))
                {
                    _logger.Error("Package file not found at {FilePath}", filePath);
                    return Encoding.UTF8.GetBytes($"Package file was not found: {filePath}");
                }

                var fileInfo = new FileInfo(filePath);
                _logger.Information(
                    "Reading package file; path={FilePath}; size={FileSize} bytes; lastModified={LastModifiedUtc}",
                    filePath,
                    fileInfo.Length,
                    fileInfo.LastWriteTimeUtc);

                var content = await File.ReadAllBytesAsync(filePath);

                _logger.Information(
                    "Successfully read package file; bytesRead={BytesRead}",
                    content.Length);

                return content;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read package information");
                return Encoding.UTF8.GetBytes("Unable to read package information.");
            }
        }

        [Description(
"""
        Get the license information for an installed package from the local node_modules folder.
        Use this tool when the user asks for a package's license, licensing details, or open-source license information.
        Provide the package name when available.
        The tool searches the local node_modules tree for the package directory and then searches its subdirectories for a license file.
        Return the license name or license file content in a concise client-facing format.
        """)]
        private async Task<string> GetLicenseFromNodeModules(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                _logger.Error("GetLicenseFromNodeModules called without a package name");
                return "A package name is required to search for license information.";
            }

            try
            {
                var nodeModulesDirectoryPath = _configuration["NodeModulesDirectoryPath"];
                if (string.IsNullOrWhiteSpace(nodeModulesDirectoryPath))
                {
                    _logger.Error("NodeModulesDirectoryPath configuration is missing or empty");
                    return "NodeModulesDirectoryPath configuration is missing.";
                }

                if (!Directory.Exists(nodeModulesDirectoryPath))
                {
                    _logger.Error("Configured node_modules directory does not exist: {DirectoryPath}", nodeModulesDirectoryPath);
                    return $"node_modules directory was not found: {nodeModulesDirectoryPath}";
                }

                _logger.Information(
                    "Searching node_modules for package {PackageName} under {DirectoryPath}",
                    packageName,
                    nodeModulesDirectoryPath);

                var packageDirectoryComponents = packageName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (packageDirectoryComponents.Length == 0)
                {
                    _logger.Error("Invalid package name provided to GetLicenseFromNodeModules: {PackageName}", packageName);
                    return "A valid package name is required.";
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
                    return $"Package '{packageName}' was not found in node_modules.";
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
                    return $"Package directory for '{packageName}' was not found.";
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
                    return $"No license file was found for package '{packageName}'.";
                }

                _logger.Information("License file found for {PackageName}: {LicenseFile}", packageName, licenseFile);

                var licenseContent = await File.ReadAllTextAsync(licenseFile);
                _logger.Debug("Read license file content for {PackageName}; length={Length}", packageName, licenseContent.Length);

                return licenseContent;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read license information for package {PackageName}", packageName);
                return $"Unable to read license information for package '{packageName}'.";
            }
        }

        [Description(
"""
        Get Copilot log information for a specific day.
        Use this tool when the user asks to search, inspect, or review Copilot logs by day.
        Provide the day number as input, and return the matching log content for that day in a client-facing format.
        """)]
        private async Task<string> GetCoPilotLogInformation(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                _logger.Error("GetCoPilotLogInformation called without a date value");
                return "A day or date value is required to search Copilot logs.";
            }

            try
            {
                if (!DateTime.TryParse(date, out var targetDate))
                {
                    _logger.Error("Invalid day/date input provided to GetCoPilotLogInformation: {DateValue}", date);
                    return "Invalid date value was provided. Please provide a valid day or date to search Copilot logs.";
                }

                var logDate = targetDate.ToString("yyyyMMdd");
                var logFilePath = Path.Combine(AppContext.BaseDirectory, "logs", $"github-copilot-local-{logDate}.json");

                _logger.Information(
                    "GetCoPilotLogInformation started for day {Day}; resolved date {LogDate}; path {LogFilePath}",
                    date,
                    logDate,
                    logFilePath);

                if (!File.Exists(logFilePath))
                {
                    _logger.Warning("Log file not found for day {Day} at {LogFilePath}", date, logFilePath);
                    return $"No Copilot log file was found for the requested day ({date}). Expected file: {Path.GetFileName(logFilePath)}";
                }

                var logContent = await File.ReadAllTextAsync(logFilePath);

                _logger.Information(
                    "Successfully read Copilot log file for day {Day}; bytesRead={BytesRead}",
                    date,
                    logContent.Length);

                return logContent;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read Copilot logs for day {Day}", date);
                return "Unable to read Copilot logs for the requested day due to an unexpected error. Please verify the date and try again.";
            }

        }
    }
}
