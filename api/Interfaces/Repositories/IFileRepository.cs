using System;
using api.Models;

namespace api.Interfaces.Repositories;

public interface IFileRepository
{
    public Task<FileUpload> AddFile(FileUpload file);

    public Task<FileUpload?> GetFile(string id);

}
