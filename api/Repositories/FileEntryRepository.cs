using System;
using api.Data;
using api.Interfaces.Repositories;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace api.Repositories;

public class FileEntryRepository : IFileEntryRepository
{
    private readonly SnappshareContext _dbContext;

    public FileEntryRepository(SnappshareContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FileEntry> CreateFileEntry(FileEntry fileEntry)
    {
        _dbContext.FileEntries.Add(fileEntry);
        await _dbContext.SaveChangesAsync();

        return fileEntry;
    }

    public Task DeleteExpiredFiles(DateTime expirationDate)
    {
        throw new NotImplementedException();
    }

    public async Task<FileEntry?> FindFileEntryByFileHash(string fileHash)
    {
        return await _dbContext.FileEntries.FirstOrDefaultAsync((fileEntry) => fileEntry.FileHash == fileHash);
    }

    public async Task<FileEntry?> FindFileEntryById(string fileId)
    {
        return await _dbContext.FileEntries.FindAsync(fileId);
    }

    public async Task<FileEntry?> FindFileEntryByIdWithChunks(string fileId)
    {
        return await _dbContext.FileEntries
                .Include(f => f.Chunks)
                .FirstOrDefaultAsync(f => f.Id == fileId);
    }

    public async Task LockFile(string fileId)
    {
        FileEntry? fileEntry = await FindFileEntryById(fileId);

        if (fileEntry == null) throw new KeyNotFoundException($"File with ID, {fileId} does not exist");

        if (fileEntry.IsLocked) throw new InvalidOperationException("File is already locked.");

        fileEntry.IsLocked = true;
        fileEntry.LockedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    public async Task UnlockFile(string fileId)
    {
        FileEntry? fileEntry = await FindFileEntryById(fileId);

        if (fileEntry == null) throw new KeyNotFoundException($"File with ID, {fileId} does not exist");

        if (!fileEntry.IsLocked) throw new InvalidOperationException("File is not locked.");

        fileEntry.IsLocked = false;
        fileEntry.LockedAt = null;

        await _dbContext.SaveChangesAsync();
    }

    public async Task MarkUploadComplete(string fileId, string? fileUrl)
    {
        FileEntry? fileEntry = await FindFileEntryById(fileId);

        if (fileEntry == null) throw new KeyNotFoundException($"File with {fileId} does not exist");

        fileEntry.Status = FileEntryStatus.Completed;
        fileEntry.FileUrl = fileUrl!;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<FileEntry> UpdateFileEntry(string fileId, FileEntry fileEntry)
    {
        FileEntry? existingFileEntry = await FindFileEntryById(fileId);

        if (existingFileEntry == null) throw new KeyNotFoundException($"File with {fileId} does not exist");

        _dbContext.Entry(existingFileEntry).CurrentValues.SetValues(fileEntry);

        await _dbContext.SaveChangesAsync();

        return existingFileEntry;
    }
}
