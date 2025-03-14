using System;
using api.Data;
using api.Interfaces.Repositories;
using api.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace api.Repositories;

public class FileRepository : IFileRepository
{
    private readonly SnappshareContext _dbContext;

    public FileRepository(SnappshareContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FileUpload> AddFile(FileUpload file)
    {
        EntityEntry fileEntry = _dbContext.FileUploads.Add(file);
        await _dbContext.SaveChangesAsync();

        return (FileUpload)fileEntry.Entity;
    }

    public async Task<FileUpload?> GetFile(string id)
    {
        return await _dbContext.FileUploads.FindAsync(id);
    }
}
