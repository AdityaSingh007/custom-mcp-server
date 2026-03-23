namespace ChangeLog.mcp.server.Models;

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
