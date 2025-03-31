using api.Interfaces.Services;

namespace api.Services;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
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
        string uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        return await UploadFileAsync(file, uniqueFileName, containerName, expiryTime);
    }

    public async Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string fileName, string containerName, DateTimeOffset expiryTime)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(fileName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream);

        var metadata = new Dictionary<string, string>
            {
                { "expiryTime", expiryTime.ToUnixTimeSeconds().ToString() }
            };
        await blobClient.SetMetadataAsync(metadata);

        return (blobClient.Uri.ToString(), fileName);
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

    public async Task UploadChunkBlockAsync(IFormFile file, string blobName, string containerName, string blockId)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name must be provided.", nameof(blobName));

        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name must be provided.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blockId))
            throw new ArgumentException("Block ID must be provided.", nameof(blockId));

        if (file == null || file.Length <= 0)
            throw new ArgumentException("File must not be empty.", nameof(file));

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        BlockBlobClient blockBlobClient = GetBlockBlobClient(containerClient, blobName);

        using var stream = file.OpenReadStream();
        await blockBlobClient.StageBlockAsync(blockId, stream);
    }

    public async Task CommitBlockListAsync(string blobName, string containerName, IEnumerable<string> blockIds)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name must be provided.", nameof(blobName));

        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name must be provided.", nameof(containerName));

        if (!blockIds.Any())
        {
            throw new ArgumentException("Block IDs must be provided.", nameof(blockIds));
        }

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        if (!await containerClient.ExistsAsync())
        {
            throw new InvalidOperationException("Container does not exist");
        }

        BlockBlobClient blockBlobClient = GetBlockBlobClient(containerClient, blobName);

        await blockBlobClient.CommitBlockListAsync(blockIds);
    }

    public async Task<bool> BlockExistsAsync(string blobName, string containerName, string blockId)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name must be provided.", nameof(blobName));

        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name must be provided.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blockId))
            throw new ArgumentException("Block ID must be provided.", nameof(blockId));

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        if (!await containerClient.ExistsAsync())
        {
            throw new InvalidOperationException("Container does not exist");
        }

        BlockBlobClient blockBlobClient = GetBlockBlobClient(containerClient, blobName);

        var response = await blockBlobClient.GetBlockListAsync(BlockListTypes.Uncommitted);
        var uncommittedBlocks = response.Value.UncommittedBlocks;

        return uncommittedBlocks.Any(block => block.Name == blockId);
    }

    public async Task<List<string>> GetUncommittedBlockIdsAsync(string blobName, string containerName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name must be provided.", nameof(blobName));

        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name must be provided.", nameof(containerName));

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        if (!await containerClient.ExistsAsync())
        {
            throw new InvalidOperationException("Container does not exist");
        }

        BlockBlobClient blockBlobClient = GetBlockBlobClient(containerClient, blobName);

        var response = await blockBlobClient.GetBlockListAsync(BlockListTypes.Uncommitted);

        return response.Value.UncommittedBlocks.Select(
            block => block.Name
        ).ToList();
    }

    protected virtual BlockBlobClient GetBlockBlobClient(BlobContainerClient container, string blobName)
    {
        return container.GetBlockBlobClient(blobName);
    }

}