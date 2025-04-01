using System;
using System.Text;
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
    public virtual async Task<UploadResponseDto> CheckFileUploadStatus(string fileHash)
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

    public virtual async Task<UploadResponseDto> FinalizeUpload(string fileId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID must be provided.", nameof(fileId));

            FileEntry? fileEntry = await _fileEntryRepository.FindFileEntryById(fileId);

            if (fileEntry == null)
            {
                throw new KeyNotFoundException("File not found");
            }

            if (fileEntry.Status == FileEntryStatus.Completed)
            {
                return new UploadResponseDto
                {
                    Status = UploadResponseDtoStatus.COMPLETE,
                    FileUrl = fileEntry.FileUrl
                };
            }

            await _fileEntryRepository.LockFile(fileId);

            List<Chunk> uploadedChunks = await _chunkRepository.GetUploadedChunksByFileId(fileId);

            if (uploadedChunks.Count != fileEntry.TotalChunks)
            {
                await _fileEntryRepository.UnlockFile(fileId);

                return new UploadResponseDto
                {
                    Status = UploadResponseDtoStatus.PARTIAL,
                    FileUrl = fileEntry.FileUrl
                };
            }

            List<string> blockIds = uploadedChunks
                                        .OrderBy(chunk => chunk.ChunkIndex)
                                        .Select(chunk => chunk.BlockId)
                                        .ToList();

            await _blobService.CommitBlockListAsync(fileEntry.FileName, BlobContainerName, blockIds);

            await _fileEntryRepository.MarkUploadComplete(fileId, fileEntry.FileUrl);

            await _fileEntryRepository.UnlockFile(fileId);

            return new UploadResponseDto
            {
                Status = UploadResponseDtoStatus.COMPLETE,
                FileUrl = fileEntry.FileUrl
            };
        }
        catch
        {
            throw;
        }
        finally
        {
            await _fileEntryRepository.UnlockFile(fileId);
        }
    }

    public async Task<UploadResponseDto> HandleFileUpload(string fileName, string fileHash, long fileSize, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File Name must be provided.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(fileHash))
            throw new ArgumentException("File Hash must be provided.", nameof(fileHash));

        if (totalChunks <= 0)
            throw new ArgumentException("Total Chunks must be a non-negative integer and not be 0.", nameof(totalChunks));

        if (fileSize <= 0)
            throw new ArgumentException("File size must be a non-negative integer and not be 0.", nameof(fileSize));

        if (chunkIndex < 0)
            throw new ArgumentException("Chunk index must be a non-negative integer.", nameof(chunkIndex));

        if (chunkFile == null || chunkFile.Length <= 0)
            throw new ArgumentException("Chunk file must not be null or empty.", nameof(chunkFile));

        if (string.IsNullOrWhiteSpace(chunkHash))
            throw new ArgumentException("Chunk hash must be provided.", nameof(chunkHash));

        string fileId;
        var fileUploadStatus = await CheckFileUploadStatus(fileHash);

        fileId = fileUploadStatus.Status == UploadResponseDtoStatus.NEW
            ? (await CreateFileEntry(fileName, fileHash, fileSize, totalChunks)).Id
            : fileUploadStatus.FileId!;

        var chunkUploadResponse = await UploadChunk(fileId, fileName, chunkIndex, chunkFile, chunkHash);

        var uploadedChunks = await _chunkRepository.GetUploadedChunksByFileId(fileId);

        if (uploadedChunks.Count == totalChunks)
        {
            var finalizeUploadResponse = await FinalizeUpload(fileId);
            return finalizeUploadResponse;
        }

        return new UploadResponseDto
        {
            Status = chunkUploadResponse.Status,
            UploadedChunk = chunkUploadResponse.UploadedChunk,
            Message = chunkUploadResponse.Message,
            FileId = fileId
        };
    }

    public virtual async Task<UploadResponseDto> UploadChunk(string fileId, string fileName, int chunkIndex, IFormFile chunkFile, string chunkHash)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID must be provided.", nameof(fileId));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File Name must be provided.", nameof(fileName));

            if (chunkIndex < 0)
                throw new ArgumentException("Chunk index must be a non-negative integer.", nameof(chunkIndex));

            if (chunkFile == null || chunkFile.Length <= 0)
                throw new ArgumentException("Chunk file must not be null or empty.", nameof(chunkFile));

            if (string.IsNullOrWhiteSpace(chunkHash))
                throw new ArgumentException("Chunk hash must be provided.", nameof(chunkHash));

            await _fileEntryRepository.LockFile(fileId);

            var existingChunk = await _chunkRepository.FindChunkByFileIdAndChunkIndex(fileId, chunkIndex);
            if (existingChunk != null)
            {
                return new UploadResponseDto
                {
                    Status = UploadResponseDtoStatus.SKIPPED,
                    Message = "Chunk already recorded in database"
                };
            }

            string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(chunkIndex.ToString("D6")));

            bool blockExists = await _blobService.BlockExistsAsync(fileName, BlobContainerName, blockId);

            if (!blockExists)
            {
                await _blobService.UploadChunkBlockAsync(chunkFile, fileName, BlobContainerName, blockId);
            }

            var chunk = new Chunk
            {
                FileId = fileId,
                ChunkIndex = chunkIndex,
                ChunkHash = chunkHash,
            };

            var savedChunk = await _chunkRepository.SaveChunk(chunk);

            return new UploadResponseDto
            {
                Status = UploadResponseDtoStatus.SUCCESS,
                UploadedChunk = savedChunk.ChunkIndex
            };
        }
        catch
        {
            throw;
        }
        finally
        {
            await _fileEntryRepository.UnlockFile(fileId);
        }
    }


    public virtual async Task<FileEntry> CreateFileEntry(string fileName, string fileHash, long fileSize, int totalChunks)
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
