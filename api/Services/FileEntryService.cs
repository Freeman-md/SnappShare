using System;
using api.Configs;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Models;
using api.Models.DTOs;
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
    )
    {
        _logger = logger;
        _blobService = blobService;
        _fileEntryRepository = fileEntryRepository;
        _chunkRepository = chunkRepository;

        BlobContainerName = storageOptions.Value.ContainerName;
    }
    public async Task<UploadResponseDto> CheckFileUploadStatus(string fileHash)
    {
        if (string.IsNullOrWhiteSpace(fileHash))
            throw new ArgumentException("File hash must be provided.");

        FileEntry? fileEntry = await _fileEntryRepository.FindFileEntryByFileHash(fileHash);

        if (fileEntry == null)
        {
            return new UploadResponseDto {
                Status = UploadResponseDtoStatus.NEW
            };
        }

        List<Chunk> uploadedChunks = await _chunkRepository.GetUploadedChunksByFileId(fileEntry.Id);
        var uploadedIndexes = uploadedChunks.Select(c => c.ChunkIndex).ToList();

        if (uploadedIndexes.Count == fileEntry.TotalChunks)
        {
            return new UploadResponseDto
            {
                Status = UploadResponseDtoStatus.COMPLETE,
                FileId = fileEntry.Id,
                FileUrl = fileEntry.FileUrl
            };
        }

        return new UploadResponseDto
        {
            Status = UploadResponseDtoStatus.PARTIAL,
            FileId = fileEntry.Id,
            UploadedChunks = uploadedIndexes
        };
    }

    public Task<UploadResponseDto> FinalizeUpload(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<UploadResponseDto> HandleFileUpload(string fileName, string fileHash, long fileSize, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash)
    {
        throw new NotImplementedException();
    }

    public Task<UploadResponseDto> UploadChunk(string fileId, int chunkIndex, IFormFile chunkFile, string chunkHash)
    {
        throw new NotImplementedException();
    }

    private async Task<string> CreateFileEntry(string fileName, string fileHash, long fileSize, int totalChunks) {
        // this should be responsible for generating the sas url for the file initially on the blob container so this will be a public method
        // that will be tested accordingly
        // each new file will have a new container which will store the chunks and the main file if needed. That Container will get the SAS
        // for general access
        var fileEntry = new FileEntry {
            FileName = fileName,
            FileHash = fileHash,
            FileSize = fileSize,
            TotalChunks = totalChunks
        };

        var createdFileEntry = await _fileEntryRepository.CreateFileEntry(fileEntry);

        return createdFileEntry.Id;
    }
}
