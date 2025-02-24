using api.Interfaces.Services;

namespace api.Services;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
using System.Threading.Tasks;

public class BlobService : IBlobService
{

    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string containerName, DateTimeOffset expiryTime)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        string uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream);

        var metadata = new Dictionary<string, string>
            {
                { "expiryTime", expiryTime.ToUnixTimeSeconds().ToString() }
            };
        await blobClient.SetMetadataAsync(metadata);

        return (blobClient.Uri.ToString(), uniqueFileName);
    }

    public async Task<string> GenerateSasTokenAsync(string blobName, string containerName, DateTimeOffset expirtyTime)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            throw new InvalidOperationException("Blob does not exist");
        }

        UserDelegationKey delegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, expirtyTime);

        BlobSasBuilder sasBuilder = new BlobSasBuilder
        {
            BlobName = blobName,
            BlobContainerName = containerName,
            ExpiresOn = expirtyTime,
            Protocol = SasProtocol.Https,
            Resource = "b"
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        string sasToken = sasBuilder.ToSasQueryParameters(delegationKey, _blobServiceClient.AccountName).ToString();

        return $"{blobClient.Uri}?{sasToken}";
    }
}