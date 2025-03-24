using System;
using api.Configs;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Tests.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace api.Services;

public class FileEntryService : IFileEntryService
{
     private ILogger<FileEntryService> _logger;
    private readonly IBlobService _blobService;

    private readonly IFileEntryRepository _fileEntryRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly string BlobContainerName;

    public FileEntryService(
        ILogger<FileEntryService> logger,
        IBlobService blobService,
        IFileEntryRepository fileEntryRepository,
        IChunkRepository chunkRepository,
        IOptions<StorageOptions> storageOptions
    ) {
        _logger = logger;
        _blobService = blobService;
        _fileEntryRepository = fileEntryRepository;
        _chunkRepository = chunkRepository;

        BlobContainerName = storageOptions.Value.ContainerName;
    }
    public Task<object> CheckFileUploadStatus(string fileHash)
    {
        throw new NotImplementedException();
    }

    public Task<object> FinalizeUpload(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<object> HandleFileUpload(string fileName, string fileHash, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash)
    {
        throw new NotImplementedException();
    }

    public Task<object> UploadChunk(string fileId, int chunkIndex, IFormFile chunkFile, string chunkHash)
    {
        throw new NotImplementedException();
    }
}
