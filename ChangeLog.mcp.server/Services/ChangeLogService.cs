using ChangeLog.mcp.server.Interfaces;
using ChangeLog.mcp.server.Models;
using System.Text.RegularExpressions;

namespace ChangeLog.mcp.server.Services;

public class ChangeLogService : IChangeLogService
{
    private readonly IConfiguration _configuration;

    public ChangeLogService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ChangeLogDirPath => _configuration["changeLogDirPath"] ?? throw new ArgumentNullException("changeLogDirPath");

    public async Task<List<MarkdownFileInfo>> GetMarkdownFilesWithMetadataAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => GetMarkdownFilesWithMetadata(ChangeLogDirPath), cancellationToken);
    }

    public async Task<string> GetChangeLogContentAsync(string apiversion, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync($"{ChangeLogDirPath}\\api-v{apiversion}.md", cancellationToken);
    }

    private static List<MarkdownFileInfo> GetMarkdownFilesWithMetadata(string directoryPath)
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
}
