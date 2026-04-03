using Microsoft.AspNetCore.Http.HttpResults;

namespace BlobService.Handlers
{
    public static class FileBlobMessageHandler
    {
        public static async Task<Results<NotFound, Ok<IResult>>> GetFileContentsByName(
            string fileVersion,
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("BlobService.FileBlobMessageHandler");
            logger.LogInformation("GetFileContentsByName started for fileVersion {FileVersion}", fileVersion);

            try
            {
                var mode = configuration["BlobStorageMode"] ?? throw new ArgumentNullException("BlobStorageMode");
                logger.LogDebug("BlobStorageMode resolved to {BlobStorageMode}", mode);

                if (mode != "local")
                {
                    logger.LogWarning("Blob storage mode is not local; returning NotFound for fileVersion {FileVersion}", fileVersion);
                    return TypedResults.NotFound();
                }

                var directoryPath = configuration["LocalBlobStoragePath"] ?? throw new ArgumentNullException("LocalBlobStoragePath");
                var fileName = $"swagger-{fileVersion}.json";
                var filePath = Path.Combine(directoryPath, fileName);

                logger.LogDebug("Resolved blob file path {FilePath} from directory {DirectoryPath}", filePath, directoryPath);

                if (!System.IO.File.Exists(filePath))
                {
                    logger.LogWarning("Requested blob file was not found at {FilePath} for fileVersion {FileVersion}", filePath, fileVersion);
                    return TypedResults.NotFound();
                }

                var fileInfo = new FileInfo(filePath);
                logger.LogInformation(
                    "Serving blob file {FileName} ({FileSize} bytes, last modified {LastModified}) for fileVersion {FileVersion}",
                    fileInfo.Name,
                    fileInfo.Length,
                    fileInfo.LastWriteTimeUtc,
                    fileVersion);

                var contentType = "application/octet-stream";
                return TypedResults.Ok(Results.File(filePath, contentType, Path.GetFileName(filePath)));
            }
            catch (ArgumentNullException ex)
            {
                logger.LogError(ex, "Missing required configuration while processing fileVersion {FileVersion}", fileVersion);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error while processing fileVersion {FileVersion}", fileVersion);
                throw;
            }
            finally
            {
                logger.LogInformation("GetFileContentsByName completed for fileVersion {FileVersion}", fileVersion);
            }
        }
    }
}
