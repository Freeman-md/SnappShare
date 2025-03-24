using System;
using api.Data;
using api.Interfaces.Repositories;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public class ChunkRepository : IChunkRepository
{
    private readonly SnappshareContext _dbContext;

    public ChunkRepository(SnappshareContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task DeleteChunksByFileId(string fileId)
    {
        ValidateFileId(fileId);

        var chunksToDelete = await _dbContext.Chunks
            .Where(c => c.FileId == fileId)
            .ToListAsync();

        if (chunksToDelete.Any())
        {
            _dbContext.Chunks.RemoveRange(chunksToDelete);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<Chunk?> FindChunkByFileIdAndChunkIndex(string fileId, int chunkIndex)
    {
        ValidateFileId(fileId);

        return await _dbContext.Chunks
            .AsNoTracking()
            .Include(c => c.FileEntry)
            .Where(c => c.FileId == fileId && c.ChunkIndex == chunkIndex)
            .FirstOrDefaultAsync();
    }


    public async Task<List<Chunk>> GetUploadedChunksByFileId(string fileId)
    {
        ValidateFileId(fileId);

        IQueryable<Chunk> query = _dbContext.Chunks.AsNoTracking();

        query = query.Where(chunk => chunk.FileId == fileId);

        return await query.ToListAsync();
    }

    public async Task<Chunk> SaveChunk(Chunk chunk)
    {

        _dbContext.Chunks.Add(chunk);

        await _dbContext.SaveChangesAsync();

        return chunk;
    }

    private static void ValidateFileId(string fileId)
{
    if (string.IsNullOrWhiteSpace(fileId))
        throw new ArgumentException($"File ID '{fileId}' is invalid.");
}

}
