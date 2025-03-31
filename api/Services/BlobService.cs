using api.Interfaces.Services;

namespace api.Services;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;

public class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string containerName, DateTimeOffset expiryTime)
    {
        var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        return await UploadFileAsync(file, uniqueFileName, containerName, expiryTime);
    }

    public async Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string fileName, string containerName, DateTimeOffset expiryTime)
    {
        var blobClient = await GetBlobClientAsync(containerName, fileName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream);

        await blobClient.SetMetadataAsync(new Dictionary<string, string>
        {
            { "expiryTime", expiryTime.ToUnixTimeSeconds().ToString() }
        });

        return (blobClient.Uri.ToString(), fileName);
    }

    public async Task<string> GenerateSasTokenAsync(string blobName, string containerName, DateTimeOffset expiryTime)
    {
        var blobClient = await GetBlobClientAsync(containerName, blobName);

        if (!await blobClient.ExistsAsync())
            throw new InvalidOperationException("Blob does not exist.");

        var delegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, expiryTime);

        var sasBuilder = new BlobSasBuilder
        {
            BlobName = blobName,
            BlobContainerName = containerName,
            ExpiresOn = expiryTime,
            Protocol = SasProtocol.Https,
            Resource = "b"
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = sasBuilder.ToSasQueryParameters(delegationKey, _blobServiceClient.AccountName).ToString();
        return $"{blobClient.Uri}?{sasToken}";
    }

    public async Task UploadChunkBlockAsync(IFormFile file, string blobName, string containerName, string blockId)
    {
        ValidateInputs(blobName, containerName, blockId);

        if (file == null || file.Length <= 0)
            throw new ArgumentException("File must not be empty.", nameof(file));

        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();

        var blockBlobClient = GetBlockBlobClient(container, blobName);

        using var stream = file.OpenReadStream();
        await blockBlobClient.StageBlockAsync(blockId, stream);
    }

    public async Task CommitBlockListAsync(string blobName, string containerName, IEnumerable<string> blockIds)
    {
        ValidateInputs(blobName, containerName);

        if (blockIds == null || !blockIds.Any())
            throw new ArgumentException("Block IDs must be provided.", nameof(blockIds));

        var container = await GetContainerClientAsync(containerName);
        var blockBlobClient = GetBlockBlobClient(container, blobName);

        await blockBlobClient.CommitBlockListAsync(blockIds);
    }

    public async Task<bool> BlockExistsAsync(string blobName, string containerName, string blockId)
    {
        ValidateInputs(blobName, containerName, blockId);

        var container = await GetContainerClientAsync(containerName);
        var blockBlobClient = GetBlockBlobClient(container, blobName);

        var blockList = await blockBlobClient.GetBlockListAsync(BlockListTypes.Uncommitted);
        return blockList.Value.UncommittedBlocks.Any(b => b.Name == blockId);
    }

    public async Task<List<string>> GetUncommittedBlockIdsAsync(string blobName, string containerName)
    {
        ValidateInputs(blobName, containerName);

        var container = await GetContainerClientAsync(containerName);
        var blockBlobClient = GetBlockBlobClient(container, blobName);

        var blockList = await blockBlobClient.GetBlockListAsync(BlockListTypes.Uncommitted);
        return blockList.Value.UncommittedBlocks.Select(b => b.Name).ToList();
    }

    // 🔧 Helpers

    private void ValidateInputs(params string[] inputs)
    {
        foreach (var input in inputs)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException($"{input} must be provided and non-empty.");
        }
    }

    private async Task<BlobClient> GetBlobClientAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None);
        return container.GetBlobClient(blobName);
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(string containerName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        if (!await container.ExistsAsync())
            throw new InvalidOperationException("Container does not exist.");
        return container;
    }

    protected virtual BlockBlobClient GetBlockBlobClient(BlobContainerClient container, string blobName)
    {
        return container.GetBlockBlobClient(blobName);
    }
}
