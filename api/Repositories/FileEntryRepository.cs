using System;
using api.Data;
using api.Interfaces.Repositories;
using api.Models;

namespace api.Repositories;

public class FileEntryRepository : IFileEntryRepository
{
    private readonly SnappshareContext _dbContext;

    public FileEntryRepository(SnappshareContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FileEntry> CreateFileEntry(FileEntry fileEntry)
    {
        throw new NotImplementedException();
    }

    public Task DeleteExpiredFiles(DateTime expirationDate)
    {
        throw new NotImplementedException();
    }

    public Task<FileEntry?> FindFileEntryByFileHash(string fileHash)
    {
        throw new NotImplementedException();
    }

    public Task<FileEntry?> FindFileEntryById(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task LockFile(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> MarkUploadComplete(string fileId, string? fileUrl)
    {
        throw new NotImplementedException();
    }

    public Task UnlockFile(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<FileEntry> UpdateFileEntry(string id, FileEntry fileEntry)
    {
        throw new NotImplementedException();
    }
}
