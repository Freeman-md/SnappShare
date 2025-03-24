using System;
using api.Models;

namespace api.Tests.Interfaces.Services;

public interface IFileEntryService {
    public Task<object> HandleFileUpload(string fileName, string fileHash, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash);

    public Task<object> CheckFileUploadStatus(string fileHash);

    public Task<object> UploadChunk(string fileId, int chunkIndex, IFormFile chunkFile, string chunkHash);

    public Task<object> FinalizeUpload(string fileId);
}