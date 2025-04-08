using System;
using System.Collections.Generic;
using api.Enums;
using api.Models;

namespace api.tests.Builders;

public class FileEntryBuilder
{
    private readonly FileEntry _fileEntry;

    public FileEntryBuilder()
    {
        _fileEntry = new FileEntry
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            FileName = "default.txt",
            FileExtension = "txt",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = FileEntryStatus.Pending,
            IsLocked = false,
            Chunks = new List<Chunk>(),
            FileHash = Guid.NewGuid().ToString("N"),
            FileUrl = "http://snappshare.com/default.txt",
            ExpiresIn = ExpiryDuration.ThirtyMinutes
        };
    }

    public FileEntryBuilder WithId(string id)
    {
        _fileEntry.Id = id;
        return this;
    }

    public FileEntryBuilder WithFileName(string fileName)
    {
        _fileEntry.FileName = fileName;
        return this;
    }

    public FileEntryBuilder WithFileExtension(string fileExtension)
    {
        _fileEntry.FileExtension = fileExtension;
        return this;
    }

    public FileEntryBuilder WithTotalChunks(int totalChunks)
    {
        _fileEntry.TotalChunks = totalChunks;
        return this;
    }

    public FileEntryBuilder WithUploadedChunks(int uploadedChunksCount)
    {
        _fileEntry.Chunks = new List<Chunk>();
        for (int i = 0; i < uploadedChunksCount; i++)
        {
            _fileEntry.Chunks.Add(new Chunk 
            { 
                ChunkIndex = i, 
                FileId = _fileEntry.Id, 
                FileEntry = _fileEntry, 
                ChunkHash = Guid.NewGuid().ToString("N"), 
                ChunkUrl = $"http://snappshare.com/chunk/{i}" 
            });
        }
        return this;
    }

    public FileEntryBuilder WithFileSize(long fileSize)
    {
        _fileEntry.FileSize = fileSize;
        return this;
    }

    public FileEntryBuilder WithFileHash(string fileHash)
    {
        _fileEntry.FileHash = fileHash;
        return this;
    }

    public FileEntryBuilder WithFileUrl(string fileUrl)
    {
        _fileEntry.FileUrl = fileUrl;
        return this;
    }

    public FileEntryBuilder WithStatus(FileEntryStatus status)
    {
        _fileEntry.Status = status;
        return this;
    }

    public FileEntryBuilder WithLockState(bool isLocked, DateTime? lockedAt = null)
    {
        _fileEntry.IsLocked = isLocked;
        _fileEntry.LockedAt = lockedAt;
        return this;
    }

    public FileEntryBuilder WithCreatedAt(DateTime createdAt)
    {
        _fileEntry.CreatedAt = createdAt;
        return this;
    }

    public FileEntryBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _fileEntry.UpdatedAt = updatedAt;
        return this;
    }

    public FileEntryBuilder WithExpiration(ExpiryDuration ExpiresIn)
    {
        _fileEntry.ExpiresIn = ExpiresIn;
        return this;
    }

    public FileEntry Build()
    {
        return new FileEntry
        {
            Id = _fileEntry.Id,
            FileName = _fileEntry.FileName,
            FileExtension = _fileEntry.FileExtension,
            TotalChunks = _fileEntry.TotalChunks,
            FileSize = _fileEntry.FileSize,
            FileHash = _fileEntry.FileHash,
            FileUrl = _fileEntry.FileUrl,
            Status = _fileEntry.Status,
            IsLocked = _fileEntry.IsLocked,
            CreatedAt = _fileEntry.CreatedAt,
            UpdatedAt = _fileEntry.UpdatedAt,
            LockedAt = _fileEntry.LockedAt,
            ExpiresIn = _fileEntry.ExpiresIn,
            Chunks = _fileEntry.Chunks
        };
    }

    public static IEnumerable<FileEntry> BuildMany(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            yield return new FileEntryBuilder()
                .WithFileName($"TestFile_{i}")
                .WithFileExtension("txt")
                .WithTotalChunks(5)
                .WithUploadedChunks(3)
                .WithFileSize(1024 * i)
                .WithFileHash(Guid.NewGuid().ToString("N"))
                .WithStatus(FileEntryStatus.Pending)
                .WithExpiration(ExpiryDuration.FiveMinutes)
                .Build();
        }
    }
}
