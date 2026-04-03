using Microsoft.AspNetCore.Http.HttpResults;

namespace BlobService.Handlers
{
    public static class FileBlobMessageHandler
    {
        public static async Task<Results<NotFound, Ok<IResult>>> GetFileContentsByName(string fileVersion, IConfiguration configuration)
        {
            var mode = configuration["BlobStorageMode"] ?? throw new ArgumentNullException("BlobStorageMode");
            if (mode != "local")
            {
                return TypedResults.NotFound();
            }
            var directoryPath = configuration["LocalBlobStoragePath"] ?? throw new ArgumentNullException("LocalBlobStoragePath");
            var fileName = $"swagger-{fileVersion}.json";
            var filePath = Path.Combine(directoryPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return TypedResults.NotFound();
            }

            var contentType = "application/octet-stream";
            return TypedResults.Ok(Results.File(filePath, contentType, Path.GetFileName(filePath)));
        }
    }
}
