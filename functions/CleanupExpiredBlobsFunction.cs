using System;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class CleanupExpiredBlobsFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public CleanupExpiredBlobsFunction(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<CleanupExpiredBlobsFunction>();
            _blobServiceClient = blobServiceClient;
        }

        [Function("CleanupExpiredBlobsFunction")]
        public async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"Function executed at: {DateTime.UtcNow}");

            var containerClient = _blobServiceClient.GetBlobContainerClient("snappshare");
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var properties = await blobClient.GetPropertiesAsync();

                if (properties.Value.Metadata.TryGetValue("expiryTime", out var expiryTimeStr) &&
                    long.TryParse(expiryTimeStr, out var expiryUnixTime))
                {
                    var expiryTime = DateTimeOffset.FromUnixTimeSeconds(expiryUnixTime);
                    if (expiryTime < DateTimeOffset.UtcNow)
                    {
                        await blobClient.DeleteIfExistsAsync();
                        _logger.LogInformation($"Deleted expired blob: {blobItem.Name}");
                    }
                }
            }
        }
    }
}
