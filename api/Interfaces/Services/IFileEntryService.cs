using System;
using api.Enums;
using api.Models;
using api.Models.DTOs;

namespace api.Tests.Interfaces.Services;

public interface IFileEntryService {
    public Task<UploadResponseDto> HandleFileUpload(string fileName, string fileHash, long fileSize, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash, ExpiryDuration expiryDuration);
    public Task<UploadResponseDto> UploadFileEntryChunk(string fileId, string fileName, string fileHash, int chunkIndex, int totalChunks, IFormFile chunkFile, string chunkHash);

    public Task<UploadResponseDto> CheckFileUploadStatus(string fileHash);

    public Task<UploadResponseDto> UploadChunk(string fileId, string fileName, int chunkIndex, IFormFile chunkFile, string chunkHash);

    public Task<UploadResponseDto> FinalizeUpload(string fileId);
    public Task<FileEntry> CreateFileEntry(string fileName, string fileHash, long fileSize, int totalChunks, ExpiryDuration expiresIn);

    public Task<UploadResponseDto> GetFileEntry(string fileId);
}