namespace api.Interfaces.Services;

public interface IBlobService {
    Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string containerName, DateTimeOffset expiryTime);

    Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string fileName, string containerName, DateTimeOffset expiryTime);

    Task<string> GenerateSasTokenAsync(string blobName, string containerName, DateTimeOffset expiryTime);

    Task UploadChunkBlockAsync(IFormFile file, string blobName, string containerName, string blockId);
    Task CommitBlockListAsync(string blobName, string containerName, IEnumerable<string> blockIds);
    Task<bool> BlockExistsAsync(string blobName, string containerName, string blockId);
    Task<List<string>> GetUncommittedBlockIdsAsync(string blobName, string containerName);
}