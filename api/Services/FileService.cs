using api.Configs;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Models;
using Microsoft.Extensions.Options;

namespace api.Services;

public class FileService : IFileService {
    private ILogger<FileService> _logger;
    private readonly IBlobService _blobService;

    private readonly IFileRepository _fileRepository;
    private readonly string BlobContainerName;

    public FileService(
        ILogger<FileService> logger,
        IBlobService blobService,
        IFileRepository fileRepository,
        IOptions<StorageOptions> storageOptions
    ) {
        _logger = logger;
        _blobService = blobService;
        _fileRepository = fileRepository;

        BlobContainerName = storageOptions.Value.ContainerName;
    }

    public async Task<FileUpload> UploadFile(FileUpload fileUpload) {
            DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddMinutes((double)fileUpload.ExpiryDuration);

            (_, string uniqueFileName) = await _blobService.UploadFileAsync(fileUpload.File!, BlobContainerName, expiryTime);

            string sasUrl = await _blobService.GenerateSasTokenAsync(uniqueFileName, BlobContainerName, expiryTime);

            var newFile = new FileUpload
            {
                Id = Guid.NewGuid().ToString()[..6],
                OriginalUrl = sasUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiryDuration = fileUpload.ExpiryDuration,
            };

            return await _fileRepository.AddFile(newFile);
    }

    public async Task<(FileUpload file, DateTimeOffset expiryTime, bool isExpired)> GetFile(string id)
    {
        var file = await _fileRepository.GetFile(id);

        if (file == null)
        {
            throw new KeyNotFoundException();
        }

        // Calculate expiry time
        DateTimeOffset expiryTime = file.CreatedAt.AddMinutes((double)file.ExpiryDuration);
        bool isExpired = DateTimeOffset.UtcNow >= expiryTime;

        return (file, expiryTime, isExpired);
    }
}