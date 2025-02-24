namespace api.Interfaces.Services;

public interface IBlobService {
    Task<string> UploadFileAsync(IFormFile file, string containerName);

    Task<string> GenerateSasTokenAsync(string blobName, string containerName, DateTimeOffset expiryTime);
}