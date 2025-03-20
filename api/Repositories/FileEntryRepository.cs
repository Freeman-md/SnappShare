using System;
using api.Interfaces.Repositories;
using api.Models;

namespace api.Repositories;

public class FileEntryRepository : IFileEntryRepository
{
    public Task<FileEntry> CreateFileEntry(FileEntry fileEntry)
    {
        throw new NotImplementedException();
    }

    public Task DeleteExpiredFiles(DateTime expirationDate)
    {
        throw new NotImplementedException();
    }

    public Task<FileEntry> FindByFileNameAndExtension(string fileName, string fileExtension)
    {
        throw new NotImplementedException();
    }

    public Task<FileEntry> FindById(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task LockFile(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> MarkUploadComplete(string fileId, string? finalUrl)
    {
        throw new NotImplementedException();
    }

    public Task UnlockFile(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<FileEntry> UpdateFileEntry(FileEntry fileEntry)
    {
        throw new NotImplementedException();
    }
}
