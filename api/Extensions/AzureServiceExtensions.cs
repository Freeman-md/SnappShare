using api.Configs;
using api.Interfaces.Services;
using api.Services;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;

namespace api.Extensions;

public static class AzureServiceExtensions
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
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
            })
            );

        services.AddSingleton<BlobServiceClient>(_ => blobServiceClient);

        services.AddScoped<IBlobService, BlobService>();


        return services;
    }

    public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceBusOptions = configuration.GetSection(ServiceBusOptions.Key).Get<ServiceBusOptions>();
        var clientOptions = new ServiceBusClientOptions {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };

        ServiceBusClient serviceBusClient = new ServiceBusClient(
            $"{serviceBusOptions.NamespaceName}.servicebus.windows.net",
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeInteractiveBrowserCredential = true
            }),
            clientOptions
        );

        services.AddSingleton<ServiceBusClient>(_ => serviceBusClient);
        
        services.AddScoped<IServiceBusService, ServiceBusService>();

        return services;
    }
}