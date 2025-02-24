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

    public async Task<string> UploadFileAsync(IFormFile file, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(file.FileName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream);

        return blobClient.Uri.ToString();
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