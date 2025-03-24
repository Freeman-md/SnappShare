using System;
using api.Data;
using api.Interfaces.Repositories;
using api.Models;
using api.Repositories;
using api.tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace api.tests.Repositories;

public class ChunkRepositoryTests
{
    private readonly SnappshareContext _dbContext;
    private readonly IChunkRepository _chunkRepository;
    private readonly IFileEntryRepository _fileEntryRepository;

    public ChunkRepositoryTests()
    {
        DbContextOptions<SnappshareContext> options = new DbContextOptionsBuilder<SnappshareContext>()
                                        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                                        .Options;

        _dbContext = new SnappshareContext(options);
        _chunkRepository = new ChunkRepository(_dbContext);
        _fileEntryRepository = new FileEntryRepository(_dbContext);
    }

    [Fact]
    public async Task SaveChunk_ShouldStoreChunkSuccessfully()
    {
        Chunk chunk = new ChunkBuilder().Build();


        Chunk savedChunk = await _chunkRepository.SaveChunk(chunk);


        Assert.NotNull(savedChunk);
        Assert.True(savedChunk.PropertiesAreEqual(chunk));
    }

    [Fact]
    public async Task SaveChunk_ShouldThrowException_WhenDatabaseFails()
    {
        var faultyDbContext = new SnappshareContext(new DbContextOptionsBuilder<SnappshareContext>().Options);
        faultyDbContext.Dispose();


        var repository = new ChunkRepository(faultyDbContext);


        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await repository.SaveChunk(new ChunkBuilder().Build()));
    }

    [Fact]
    public async Task FindChunkByFileIdAndChunkIndex_ShouldReturnChunk_WhenExists()
    {
        FileEntry fileEntry = new FileEntryBuilder().Build();
        Chunk chunk = new ChunkBuilder().WithFileEntry(fileEntry).Build();

        await _fileEntryRepository.CreateFileEntry(fileEntry);
        await _chunkRepository.SaveChunk(chunk);


        Chunk? retrievedChunk = await _chunkRepository.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex);


        Assert.NotNull(retrievedChunk);
        Assert.True(retrievedChunk.PropertiesAreEqual(chunk, ["FileEntry"]));
        Assert.True(retrievedChunk.FileEntry.PropertiesAreEqual(fileEntry, "Chunks"));
    }

    [Fact]
    public async Task FindChunkByFileIdAndChunkIndex_ShouldReturnNull_WhenChunkDoesNotExist()
    {
        Chunk? chunk = await _chunkRepository.FindChunkByFileIdAndChunkIndex(Guid.NewGuid().ToString(), new Random().Next());

        Assert.Null(chunk);
    }

    [Fact]
    public async Task FindChunkByFileIdAndChunkIndex_ShouldThrowException_WhenFileIdIsInvalid()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await _chunkRepository.FindChunkByFileIdAndChunkIndex(null!, new Random().Next()));
    }

    [Fact]
    public async Task GetUploadedChunksByFileId_ShouldReturnAllUploadedChunks()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                .WithTotalChunks(3)
                                .WithUploadedChunks(3)
                                .Build();

        await _fileEntryRepository.CreateFileEntry(fileEntry);


        List<Chunk> uploadedChunks = await _chunkRepository.GetUploadedChunksByFileId(fileEntry.Id);


        Assert.NotNull(uploadedChunks);
        Assert.Equal(fileEntry.TotalChunks, uploadedChunks.Count);

        foreach (var chunk in fileEntry.Chunks)
        {
            Assert.Contains(uploadedChunks, c => c.ChunkIndex == chunk.ChunkIndex);
        }
        Assert.Contains(uploadedChunks, c => c.ChunkIndex == 2);
    }

    [Fact]
    public async Task GetUploadedChunksByFileId_ShouldReturnEmptyList_WhenNoChunksUploaded()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                .Build();
        await _fileEntryRepository.CreateFileEntry(fileEntry);


        List<Chunk> uploadedChunks = await _chunkRepository.GetUploadedChunksByFileId(fileEntry.Id);


        Assert.NotNull(uploadedChunks);
        Assert.Empty(uploadedChunks);
    }

    [Fact]
    public async Task GetUploadedChunksByFileId_ShouldThrowException_WhenFileIdIsInvalid()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await _chunkRepository.GetUploadedChunksByFileId(null!));
    }

    [Fact]
    public async Task DeleteChunksByFileId_ShouldDeleteAllChunksSuccessfully()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                .WithTotalChunks(3)
                                .WithUploadedChunks(0)
                                .Build();

        await _fileEntryRepository.CreateFileEntry(fileEntry);
        
        
        await _chunkRepository.DeleteChunksByFileId(fileEntry.Id);

        List<Chunk> remainingChunks = await _chunkRepository.GetUploadedChunksByFileId(fileEntry.Id);


        Assert.Empty(remainingChunks);
    }


    [Fact]
    public async Task DeleteChunksByFileId_ShouldThrowException_WhenFileIdIsInvalid()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await _chunkRepository.DeleteChunksByFileId(null!));
    }
}
