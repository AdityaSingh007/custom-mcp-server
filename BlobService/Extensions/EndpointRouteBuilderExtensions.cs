using Asp.Versioning.Builder;
using BlobService.Handlers;

namespace BlobService.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void RegisterFileBlobApiEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            ApiVersionSet apiVersionSet = endpointRouteBuilder.NewApiVersionSet()
                                .HasApiVersion(new Asp.Versioning.ApiVersion(1))
                                .ReportApiVersions()
                                .Build();

            var customerApiEndpoints = endpointRouteBuilder.MapGroup("/api/v{apiVersion:apiVersion}/FileBlob").WithApiVersionSet(apiVersionSet);
            customerApiEndpoints.MapGet("/{fileVersion}", FileBlobMessageHandler.GetFileContentsByName).WithName("GetFileContentsByName");
        }
    }
}
