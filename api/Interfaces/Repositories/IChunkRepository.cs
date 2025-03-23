using api.Models;

namespace api.Interfaces.Repositories;

public interface IChunkRepository {
    public Task<Chunk> FindByFileIdAndChunkIndex(string fileId, int chunkIndex);

    public Task<List<Chunk>> GetUploadedChunksByFileId(string fileId);

    public Task<Chunk> SaveChunk(Chunk chunk);
    public Task DeleteChunksByFileId(string fileId);
}