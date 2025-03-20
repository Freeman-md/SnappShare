using api.Models;

namespace api.Interfaces.Repositories;

public interface IFileEntryRepository {
    public Task<FileEntry> FindByFileNameAndExtension(string fileName, string fileExtension);
    public Task<FileEntry> FindById(string fileId);
    public Task<FileEntry> CreateFileEntry(FileEntry fileEntry);
    public Task<FileEntry> UpdateFileEntry(FileEntry fileEntry);
    public Task<bool> MarkUploadComplete(string fileId, string? finalUrl);
    public Task LockFile(string fileId);
    public Task UnlockFile(string fileId);
    public Task DeleteExpiredFiles(DateTime expirationDate);
}