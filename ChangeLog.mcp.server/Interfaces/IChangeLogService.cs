using ChangeLog.mcp.server.Models;

namespace ChangeLog.mcp.server.Interfaces;

public interface IChangeLogService
{
    Task<List<MarkdownFileInfo>> GetMarkdownFilesWithMetadataAsync(CancellationToken cancellationToken = default);

    Task<string> GetChangeLogContentAsync(string apiversion, CancellationToken cancellationToken = default);
}
