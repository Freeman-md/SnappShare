using api.Configs;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace api.Extensions;

public static class AzureServiceExtensions
{
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var storageOptions = configuration.GetSection(StorageOptions.Key).Get<StorageOptions>();

        BlobServiceClient blobServiceClient = new BlobServiceClient(
            new Uri($"https://{storageOptions?.AccountName}.blob.core.windows.net"),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeInteractiveBrowserCredential = true
            }));

        services.AddSingleton<BlobServiceClient>(_ => blobServiceClient);


        return services;
    }
}