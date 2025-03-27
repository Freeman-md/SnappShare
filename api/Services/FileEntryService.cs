using System;
using api.Configs;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Models;
using api.Models.DTOs;
using api.Tests.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
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
            return new UploadResponseDto
            {
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

    public async Task<UploadResponseDto> UploadChunk(string fileId, int chunkIndex, IFormFile chunkFile, string chunkHash)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID must be provided.", nameof(fileId));

            if (chunkIndex < 0)
                throw new ArgumentException("Chunk index must be a non-negative integer.", nameof(chunkIndex));

            if (chunkFile == null || chunkFile.Length <= 0)
                throw new ArgumentException("Chunk File must not be empty.", nameof(chunkFile));

            if (string.IsNullOrWhiteSpace(chunkHash))
                throw new ArgumentException("Chunk hash must be provided.", nameof(chunkHash));

            await _fileEntryRepository.LockFile(fileId);

            Chunk? existingChunk = await _chunkRepository.FindChunkByFileIdAndChunkIndex(fileId, chunkIndex);

            if (existingChunk != null)
            {
                await _fileEntryRepository.UnlockFile(fileId);

                return new UploadResponseDto
                {
                    Status = UploadResponseDtoStatus.SKIPPED,
                    Message = "Chunk already uploaded"
                };
            }

            string chunkFileName = $"{Path.GetFileNameWithoutExtension(chunkHash)}-{Guid.NewGuid()}{Path.GetExtension(chunkFile.FileName)}";

            (string chunkUrl, string chunkName) = await _blobService.UploadFileAsync(chunkFile, chunkFileName, BlobContainerName, default!); // we use default expiry time for now. This has to be set though and passed into the uploadchunk method

            Chunk chunk = new Chunk
            {
                FileId = fileId,
                ChunkHash = chunkHash,
                ChunkUrl = chunkUrl
            };

            Chunk createdChunk = await _chunkRepository.SaveChunk(chunk);

            await _fileEntryRepository.UnlockFile(fileId);

            return new UploadResponseDto
            {
                Status = UploadResponseDtoStatus.SUCCESS,
                UploadedChunk = createdChunk.ChunkIndex
            };
        }
        catch (System.Exception)
        {
            throw;
        }
        finally
        {
            await _fileEntryRepository.UnlockFile(fileId);
        }
    }

    public async Task<FileEntry> CreateFileEntry(string fileName, string fileHash, long fileSize, int totalChunks)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File Name must be provided.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(fileHash))
            throw new ArgumentException("File Hash must be provided.", nameof(fileHash));

        if (totalChunks <= 0)
            throw new ArgumentException("Total Chunks must be greater than zero.", nameof(totalChunks));

        if (fileSize <= 0)
            throw new ArgumentException("File Size must be greater than zero.", nameof(fileSize));

        string fileUrl = await _blobService.GenerateSasTokenAsync(fileName, BlobContainerName, default!);

        var fileEntry = new FileEntry
        {
            FileName = fileName,
            FileHash = fileHash,
            FileSize = fileSize,
            TotalChunks = totalChunks,
            FileUrl = fileUrl,
        };

        var createdFileEntry = await _fileEntryRepository.CreateFileEntry(fileEntry);

        return createdFileEntry;
    }
}
