using System;
using api.Models;

namespace api.Interfaces.Services;

public interface IFileService
{
    public Task<FileUpload> UploadFile(FileUpload file);

    public Task<(FileUpload file, DateTimeOffset expiryTime, bool isExpired)> GetFile(string id);
}
