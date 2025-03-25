using System;
using api.Models;
using api.Models.DTOs;

namespace api.Tests.Interfaces.Services;

public interface IFileEntryService {
    public Task<UploadResponseDto> HandleFileUpload(string fileName, string fileHash, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash);

    public Task<UploadResponseDto> CheckFileUploadStatus(string fileHash);

    public Task<UploadResponseDto> UploadChunk(string fileId, int chunkIndex, IFormFile chunkFile, string chunkHash);

    public Task<UploadResponseDto> FinalizeUpload(string fileId);
}