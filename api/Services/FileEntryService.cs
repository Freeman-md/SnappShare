using System;
using System.Text;
using api.Configs;
using api.Enums;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Models;
using api.Models.DTOs;
using api.Tests.Interfaces.Services;
using Helpers.ValidationHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace api.Services;

public class FileEntryService : IFileEntryService
{
    private ILogger<FileEntryService> _logger;
    private readonly IBlobService _blobService;
    private readonly IMessageService _messageService;
    private readonly IFileEntryRepository _fileEntryRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly string BlobContainerName;

    public FileEntryService(
        ILogger<FileEntryService> logger,
        IBlobService blobService,
        IMessageService messageService,
        IFileEntryRepository fileEntryRepository,
        IChunkRepository chunkRepository,
        IOptions<StorageOptions> storageOptions
    )
    {
        _logger = logger;
        _blobService = blobService;
        _messageService = messageService;
        _fileEntryRepository = fileEntryRepository;
        _chunkRepository = chunkRepository;

        BlobContainerName = storageOptions.Value.ContainerName;
    }
    public virtual async Task<UploadResponseDto> CheckFileUploadStatus(string fileHash)
    {
        ValidationHelper.ValidateString(fileHash, nameof(fileHash));

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

        if (uploadedIndexes.Count == fileEntry.TotalChunks) //TODO: Also check if file hasn't expired too.
        {
            return new UploadResponseDto
            {
                Status = UploadResponseDtoStatus.COMPLETE,
                FileId = fileEntry.Id,
                FileUrl = fileEntry.FileUrl,
                FileSize = fileEntry.FileSize,
                FileHash = fileEntry.FileHash,
                Message = "File Uploaded Successfully"
            };
        }

        return new UploadResponseDto
        {
            Status = UploadResponseDtoStatus.PARTIAL,
            FileId = fileEntry.Id,
            FileSize = fileEntry.FileSize,
            FileHash = fileEntry.FileHash,
            UploadedChunks = uploadedIndexes,
            TotalChunks = fileEntry.TotalChunks
        };
    }

    public virtual async Task<FileEntry> CreateFileEntry(string fileName, string fileHash, long fileSize, int totalChunks, ExpiryDuration expiresIn)
    {
        ValidationHelper.ValidateString(fileName, nameof(fileName));
        ValidationHelper.ValidateString(fileHash, nameof(fileHash));
        ValidationHelper.ValidatePositiveNumber(totalChunks, nameof(totalChunks));
        ValidationHelper.ValidatePositiveNumber(fileSize, nameof(fileSize));
        ValidationHelper.ValidateExpiryDuration(expiresIn, nameof(expiresIn));

        var fileEntry = new FileEntry
        {
            FileName = fileName,
            FileHash = fileHash,
            FileSize = fileSize,
            TotalChunks = totalChunks,
            ExpiresIn = expiresIn,
        };

        var createdFileEntry = await _fileEntryRepository.CreateFileEntry(fileEntry);

        return createdFileEntry;
    }

    public virtual async Task<UploadResponseDto> UploadChunk(string fileId, string fileName, int chunkIndex, IFormFile chunkFile, string chunkHash)
    {
        try
        {
            ValidationHelper.ValidateString(fileName, nameof(fileName));
            ValidationHelper.ValidateString(fileId, nameof(fileId));
            ValidationHelper.ValidateString(chunkHash, nameof(chunkHash));
            ValidationHelper.ValidateNonNegativeNumber(chunkIndex, nameof(chunkIndex));
            ValidationHelper.ValidateChunkFile(chunkFile, nameof(chunkFile));

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

            await _blobService.UploadChunkBlockAsync(chunkFile, fileName, BlobContainerName, blockId);

            var chunk = new Chunk
            {
                FileId = fileId,
                ChunkIndex = chunkIndex,
                ChunkHash = chunkHash,
                ChunkSize = chunkFile.Length
            };

            var savedChunk = await _chunkRepository.SaveChunk(chunk);

            return new UploadResponseDto
            {
                Status = UploadResponseDtoStatus.SUCCESS,
                UploadedChunk = savedChunk.ChunkIndex,
                Message = "Chunk staged successfully"
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

    public virtual async Task<UploadResponseDto> FinalizeUpload(string fileId)
    {
        try
        {
            ValidationHelper.ValidateString(fileId, nameof(fileId));

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
                    FileUrl = fileEntry.FileUrl,
                    Message = "Upload finalized successfully. File is ready to access."
                };
            }

            //TODO: File Locks should only be used to lock a file once upload has been finalized based on our current implementation of multiple endpoints - this prevents uploading chunks for complete file though upload chunk has a check. We could do either.

            List<Chunk> uploadedChunks = await _chunkRepository.GetUploadedChunksByFileId(fileId);

            if (uploadedChunks.Count != fileEntry.TotalChunks)
            {
                return new UploadResponseDto
                {
                    Status = UploadResponseDtoStatus.PARTIAL,
                    FileUrl = fileEntry.FileUrl,
                    Message = "Upload incomplete. Some chunks are still missing."
                };
            }

            List<string> blockIds = uploadedChunks
                                        .OrderBy(chunk => chunk.ChunkIndex)
                                        .Select(chunk => chunk.BlockId)
                                        .ToList();

            await _blobService.CommitBlockListAsync(fileEntry.FileName, BlobContainerName, blockIds);

            DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddMinutes((int)fileEntry.ExpiresIn);

            string fileUrl = await _blobService.GenerateSasTokenAsync(fileEntry.FileName, BlobContainerName, expiryTime);

            var deletePayload = new DeleteFileMessage
            {
                FileId = fileEntry.Id,
                FileName = fileEntry.FileName,
                ContainerName = BlobContainerName,
                ExpiresAt = expiryTime
            };

            await _messageService.ScheduleAsync(deletePayload, expiryTime);

            await _fileEntryRepository.MarkUploadComplete(fileId, fileUrl);

            return new UploadResponseDto
            {
                Status = UploadResponseDtoStatus.COMPLETE,
                FileUrl = fileEntry.FileUrl,
                Message = "Upload finalized successfully. File is ready to access."
            };
        }
        catch
        {
            throw;
        }
    }

    //TODO: Implementing Background Upload - there should be a way to provide a URL to the user in the first instance once upload starts. More like a
    // temp url to check upload process. I reckon that will the fileId with a route handling that request so the user can check upload status

    public async Task<UploadResponseDto> HandleFileUpload(string fileName, string fileHash, long fileSize, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash, ExpiryDuration expiresIn)
    {
        ValidationHelper.ValidateString(fileName, nameof(fileName));
        ValidationHelper.ValidateString(fileHash, nameof(fileHash));
        ValidationHelper.ValidateString(chunkHash, nameof(chunkHash));
        ValidationHelper.ValidateNonNegativeNumber(chunkIndex, nameof(chunkIndex));
        ValidationHelper.ValidatePositiveNumber(totalChunks, nameof(totalChunks));
        ValidationHelper.ValidatePositiveNumber(fileSize, nameof(fileSize));
        ValidationHelper.ValidateChunkFile(chunkFile, nameof(chunkFile));
        ValidationHelper.ValidateExpiryDuration(expiresIn, nameof(expiresIn));

        string fileId;
        var fileUploadResponse = await CheckFileUploadStatus(fileHash);

        if (fileUploadResponse.Status == UploadResponseDtoStatus.COMPLETE) return fileUploadResponse;

        if (fileUploadResponse.TotalChunks.HasValue && (fileUploadResponse.TotalChunks.Value != totalChunks))
        {
            long expectedChunkSizeBytes = fileSize / fileUploadResponse.TotalChunks.Value;
            double expectedChunkSizeMB = Math.Round(expectedChunkSizeBytes / (1024.0 * 1024.0), 2);

            throw new Exception($"TotalChunks mismatch for existing file. Expected: {fileUploadResponse.TotalChunks.Value}, Received: {totalChunks}. Approx. expected chunk size: {expectedChunkSizeMB} MB.");

        }

        fileId = fileUploadResponse.Status == UploadResponseDtoStatus.NEW
            ? (await CreateFileEntry(fileName, fileHash, fileSize, totalChunks, expiresIn)).Id
            : fileUploadResponse.FileId!;

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

    public async Task<UploadResponseDto> UploadFileEntryChunk(string fileId, string fileName, string fileHash, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash)
    {
        ValidationHelper.ValidateString(fileId, nameof(fileId));
        ValidationHelper.ValidateString(fileName, nameof(fileName));
        ValidationHelper.ValidateString(fileHash, nameof(fileHash));
        ValidationHelper.ValidateString(chunkHash, nameof(chunkHash));
        ValidationHelper.ValidateNonNegativeNumber(chunkIndex, nameof(chunkIndex));
        ValidationHelper.ValidatePositiveNumber(totalChunks, nameof(totalChunks));
        ValidationHelper.ValidateChunkFile(chunkFile, nameof(chunkFile));

        var fileUploadResponse = await CheckFileUploadStatus(fileHash);

        // This validation is to ensure the right hashed file corresponds to the right file id in the database
        if (fileUploadResponse.FileId != fileId && fileUploadResponse.FileHash == fileHash) throw new ArgumentException("File hash and id do not correspond to existing values in database");

        //TODO: Over here, we could check if file is locked - as the only case when a file is locked is that upload is complete.

        if (fileUploadResponse.Status == UploadResponseDtoStatus.COMPLETE) return fileUploadResponse;

        ValidateTotalChunks(fileUploadResponse, totalChunks);

        var chunkUploadResponse = await UploadChunk(fileId, fileName, chunkIndex, chunkFile, chunkHash);

        return new UploadResponseDto
        {
            Status = chunkUploadResponse.Status,
            UploadedChunk = chunkUploadResponse.UploadedChunk,
            Message = chunkUploadResponse.Message,
            FileId = fileId
        };

    }

    public async Task<UploadResponseDto> GetFileEntry(string fileId)
    {
        ValidationHelper.ValidateString(fileId, nameof(fileId));

        var fileEntry = await _fileEntryRepository.FindFileEntryById(fileId)
                          ?? throw new KeyNotFoundException("No file found with the provided ID.");

        var uploadedChunks = fileEntry.Chunks.Select(c => c.ChunkIndex).ToList();

        var response = new UploadResponseDto
        {
            FileId = fileEntry.Id,
            FileName = fileEntry.FileName,
            FileSize = fileEntry.FileSize,
            TotalChunks = fileEntry.TotalChunks,
            UploadedChunks = uploadedChunks
        };

        switch (fileEntry.Status)
        {
            case FileEntryStatus.Completed:
                response.Status = UploadResponseDtoStatus.COMPLETE;
                response.FileUrl = fileEntry.FileUrl;
                response.Message = "Upload complete. File is ready to access.";
                break;

            case FileEntryStatus.Pending:
                response.Status = uploadedChunks.Count == 0
                    ? UploadResponseDtoStatus.NEW
                    : UploadResponseDtoStatus.PARTIAL;
                response.Message = uploadedChunks.Count == 0
                    ? "Upload has not started yet."
                    : "Upload in progress. Some chunks are still missing.";
                break;

            case FileEntryStatus.Failed:
                response.Status = UploadResponseDtoStatus.FAILED;
                response.Message = "Upload failed. Please retry or contact support.";
                break;

            default:
                response.Status = UploadResponseDtoStatus.FAILED;
                response.Message = "Upload status is unknown. Please try again later.";
                break;
        }

        return response;
    }

    private void ValidateTotalChunks(UploadResponseDto fileUploadResponse, int totalChunks)
{
    if (fileUploadResponse.TotalChunks.HasValue && (fileUploadResponse.TotalChunks.Value != totalChunks))
    {
        long expectedChunkSizeBytes = (fileUploadResponse.FileSize ?? 0) / fileUploadResponse.TotalChunks.Value;
        double expectedChunkSizeMB = Math.Round(expectedChunkSizeBytes / (1024.0 * 1024.0), 2);

        throw new Exception($"TotalChunks mismatch for existing file. Expected: {fileUploadResponse.TotalChunks.Value}, Received: {totalChunks}. Approx. expected chunk size: {expectedChunkSizeMB} MB.");
    }
}

}
