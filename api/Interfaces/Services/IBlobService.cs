namespace api.Interfaces.Services;

public interface IBlobService {
    Task<string> UploadFileAsync(IFormFile file, string containerName, DateTimeOffset expiryTime);

    Task<string> GenerateSasTokenAsync(string blobName, string containerName, DateTimeOffset expiryTime);
}