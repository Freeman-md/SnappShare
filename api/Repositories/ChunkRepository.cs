using System;
using api.Data;
using api.Interfaces.Repositories;
using api.Models;

namespace api.Repositories;

public class ChunkRepository : IChunkRepository
{
    private readonly SnappshareContext _dbContext;

    public ChunkRepository(SnappshareContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task DeleteChunksByFileId(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<Chunk> FindByFileIdAndChunkIndex(string fileId, int chunkIndex)
    {
        throw new NotImplementedException();
    }

    public Task<List<Chunk>> GetUploadedChunksByFileId(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<Chunk> SaveChunk(Chunk chunk)
    {
        throw new NotImplementedException();
    }
}
