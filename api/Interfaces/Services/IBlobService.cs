namespace api.Interfaces.Services;

public interface IBlobService {
    Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string containerName, DateTimeOffset expiryTime);

    Task<(string fileUrl, string fileName)> UploadFileAsync(IFormFile file, string fileName, string containerName, DateTimeOffset expiryTime);

    Task<string> GenerateSasTokenAsync(string blobName, string containerName, DateTimeOffset expiryTime);
}