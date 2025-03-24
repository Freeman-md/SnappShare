using System;
using api.Models;

namespace api.Interfaces.Services;

public interface IFileService
{
    public Task<FileUpload> UploadFile(FileUpload file);

    public Task<(FileUpload file, DateTimeOffset expiryTime, bool isExpired)> GetFile(string id);

    public Task<object> HandleFileUpload(string fileName, string fileHash, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash);

    public Task<object> CheckFileUploadStatus(string fileHash);

    public Task<object> UploadChunk(string fileId, int chunkIndex, IFormFile chunkFile, string chunkHash);

    public Task<object> FinalizeUpload(string fileId);
}
