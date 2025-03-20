using System;
using api.Interfaces.Repositories;
using api.Models;

namespace api.Repositories;

public class ChunkRepository : IChunkRepository
{
    public Task DeleteChunksByFileId(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<Chunk> FindByFileIdAndChunkIndex(string fileId, int chunkIndex)
    {
        throw new NotImplementedException();
    }

    public Task<Chunk> GetUploadedChunksByFileId(string fileId)
    {
        throw new NotImplementedException();
    }

    public Task<Chunk> SaveChunk(Chunk chunk)
    {
        throw new NotImplementedException();
    }
}
