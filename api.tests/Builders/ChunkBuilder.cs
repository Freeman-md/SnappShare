using System;
using api.Models;

namespace api.tests.Builders
{
    public class ChunkBuilder
    {
        private readonly Chunk _chunk;

        public ChunkBuilder()
        {
            _chunk = new Chunk
            {
                Id = Guid.NewGuid().ToString("N")[..12],
                FileId = Guid.NewGuid().ToString("N")[..12],
                ChunkIndex = 0,
                ChunkSize = 1024,
                ChunkHash = Guid.NewGuid().ToString("N"),
                ChunkUrl = "http://snappshare.com/chunk/0",
                UploadedAt = DateTime.UtcNow,
                FileEntry = new FileEntryBuilder().WithId(_chunk.FileId).Build()
            };
        }

        public ChunkBuilder WithId(string id)
        {
            _chunk.Id = id;
            return this;
        }

        public ChunkBuilder WithFileId(string fileId)
        {
            _chunk.FileId = fileId;
            return this;
        }

        public ChunkBuilder WithFileEntry(FileEntry fileEntry)
        {
            _chunk.FileEntry = fileEntry;
            _chunk.FileId = fileEntry.Id;
            return this;
        }

        public ChunkBuilder WithChunkIndex(int index)
        {
            _chunk.ChunkIndex = index;
            return this;
        }

        public ChunkBuilder WithChunkSize(long size)
        {
            _chunk.ChunkSize = size;
            return this;
        }

        public ChunkBuilder WithChunkHash(string hash)
        {
            _chunk.ChunkHash = hash;
            return this;
        }

        public ChunkBuilder WithChunkUrl(string url)
        {
            _chunk.ChunkUrl = url;
            return this;
        }

        public ChunkBuilder WithUploadedAt(DateTime uploadedAt)
        {
            _chunk.UploadedAt = uploadedAt;
            return this;
        }

        public Chunk Build()
        {
            return new Chunk
            {
                Id = _chunk.Id,
                FileId = _chunk.FileId,
                FileEntry = _chunk.FileEntry,
                ChunkIndex = _chunk.ChunkIndex,
                ChunkSize = _chunk.ChunkSize,
                ChunkHash = _chunk.ChunkHash,
                ChunkUrl = _chunk.ChunkUrl,
                UploadedAt = _chunk.UploadedAt
            };
        }

        public static IEnumerable<Chunk> BuildMany(string fileId, FileEntry fileEntry, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new ChunkBuilder()
                    .WithFileId(fileId)
                    .WithFileEntry(fileEntry)
                    .WithChunkIndex(i)
                    .WithChunkSize(1024)
                    .WithChunkHash(Guid.NewGuid().ToString("N"))
                    .WithChunkUrl($"http://snappshare.com/chunk/{i}")
                    .Build();
            }
        }
    }
}
