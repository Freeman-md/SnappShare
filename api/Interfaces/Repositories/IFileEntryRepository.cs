using api.Models;

namespace api.Interfaces.Repositories;

public interface IFileEntryRepository {
    public Task<FileEntry?> FindFileEntryByFileHash(string fileHash);
    public Task<FileEntry?> FindFileEntryById(string fileId);
    public Task<FileEntry> CreateFileEntry(FileEntry fileEntry);
    public Task<FileEntry> UpdateFileEntry(string fileId, FileEntry fileEntry);
    public Task MarkUploadComplete(string fileId, string? fileUrl);
    public Task LockFile(string fileId);
    public Task UnlockFile(string fileId);
    public Task DeleteExpiredFiles(DateTime expirationDate);
}