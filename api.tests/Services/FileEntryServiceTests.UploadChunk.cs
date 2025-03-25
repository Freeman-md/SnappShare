using System;
using api.Models;
using api.Models.DTOs;
using api.tests.Builders;
using Microsoft.AspNetCore.Http;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{
    [Fact]
    public async Task UploadChunk_ShouldUploadChunkSuccessfully_AndUpdateDatabase()
    {
        Chunk chunk = new ChunkBuilder().Build();
        FileEntry fileEntry = new FileEntryBuilder().WithLockState(false).Build();

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Dummy content"));
        IFormFile chunkFile = new FormFile(stream, 0, stream.Length, "file", chunk.ChunkHash);

        _fileEntryRepository.Setup(repo => repo.LockFile(fileEntry.Id))
                            .Callback(() =>
                            {
                                fileEntry.IsLocked = true;
                                fileEntry.LockedAt = DateTime.UtcNow;
                            });

        _chunkRepository.Setup(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex))
                        .ReturnsAsync((Chunk)null!);

        _blobService.Setup(x => x.UploadFileAsync(chunkFile, _storageOptions.Value.ContainerName, It.IsAny<DateTimeOffset>()))
                        .ReturnsAsync(("someUrl", chunk.ChunkHash));

        _chunkRepository.Setup(repo => repo.SaveChunk(It.IsAny<Chunk>()))
            .ReturnsAsync(chunk);

        _fileEntryRepository.Setup(repo => repo.UnlockFile(fileEntry.Id))
        .Callback(() =>
        {
            fileEntry.IsLocked = false;
            fileEntry.LockedAt = null;
        });

        var result = await _fileEntryService.UploadChunk(fileEntry.Id, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.SUCCESS, result.Status);
        Assert.Equal(chunk.ChunkIndex, result.UploadedChunk);

        Assert.False(fileEntry.IsLocked);
        Assert.Null(fileEntry.LockedAt);

        _fileEntryRepository.Verify(repo => repo.LockFile(fileEntry.Id), Times.Once);
        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex), Times.Once);
        _blobService.Verify(blob => blob.UploadFileAsync(chunkFile, _storageOptions.Value.ContainerName, It.IsAny<DateTimeOffset>()), Times.Once);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(fileEntry.Id), Times.Once);
    }

    [Fact]
    public async Task UploadChunk_ShouldRejectDuplicateChunk_WhenChunkAlreadyExists()
    {
        Chunk chunk = new ChunkBuilder().Build();
        FileEntry fileEntry = new FileEntryBuilder().WithLockState(false).Build();

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Dummy content"));
        IFormFile chunkFile = new FormFile(stream, 0, stream.Length, "file", chunk.ChunkHash);

        _fileEntryRepository.Setup(repo => repo.LockFile(fileEntry.Id))
                            .Callback(() =>
                            {
                                fileEntry.IsLocked = true;
                                fileEntry.LockedAt = DateTime.UtcNow;
                            });

        _chunkRepository.Setup(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex))
                        .ReturnsAsync(chunk);

        _fileEntryRepository.Setup(repo => repo.UnlockFile(fileEntry.Id))
            .Callback(() =>
            {
                fileEntry.IsLocked = false;
                fileEntry.LockedAt = null;
            });

        var result = await _fileEntryService.UploadChunk(fileEntry.Id, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.SKIPPED, result.Status);

        Assert.False(fileEntry.IsLocked);
        Assert.Null(fileEntry.LockedAt);

        _fileEntryRepository.Verify(repo => repo.LockFile(fileEntry.Id), Times.Once);
        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex), Times.Once);
        _blobService.Verify(blob => blob.UploadFileAsync(chunkFile, _storageOptions.Value.ContainerName, It.IsAny<DateTimeOffset>()), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(fileEntry.Id), Times.Once);
    }

    [Theory]
    [InlineData(null, 0, 1024, "valid-hash")]
    [InlineData("", 0, 1024, "valid-hash")]
    [InlineData("   ", 0, 1024, "valid-hash")]
    [InlineData("file-id", -1, 1024, "valid-hash")]
    [InlineData("file-id", 0, 0, "valid-hash")]
    [InlineData("file-id", 0, -1024, "valid-hash")]
    [InlineData("file-id", 0, 1024, null)]
    [InlineData("file-id", 0, 1024, "")]
    [InlineData("file-id", 0, 1024, "   ")]
    public async Task UploadChunk_ShouldThrowArgumentException_ForInvalidInputs(
    string? fileId, int chunkIndex, long chunkSize, string? chunkHash)
    {
        var stream = new MemoryStream(new byte[chunkSize > 0 ? chunkSize : 1]);
        var chunkFile = new FormFile(stream, 0, chunkSize, "file", "test.chunk");

        var act = async () => await _fileEntryService.UploadChunk(fileId!, chunkIndex, chunkFile, chunkHash!);

        var ex = await Assert.ThrowsAsync<ArgumentException>(act);

        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Never);
        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _blobService.Verify(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.Never);
    }


    [Fact]
    public async Task UploadChunk_ShouldThrowArgumentException_WhenChunkFileIsNull()
    {
        var fileEntry = new FileEntryBuilder().Build();
        IFormFile? chunkFile = null;

        var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, 0, chunkFile!, "valid-hash");

        await Assert.ThrowsAsync<ArgumentException>(act);

        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Never);
        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _blobService.Verify(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadChunk_ShouldThrowArgumentException_WhenChunkFileIsEmpty()
    {
        var fileEntry = new FileEntryBuilder().Build();
        var stream = new MemoryStream(); // 0 bytes
        var chunkFile = new FormFile(stream, 0, 0, "file", "empty.chunk");

        var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, 0, chunkFile, "valid-hash");

        await Assert.ThrowsAsync<ArgumentException>(act);

        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Never);
        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _blobService.Verify(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadChunk_ShouldThrowException_WhenBlobStorageFails()
    {
        var fileEntry = new FileEntryBuilder().Build();
        var chunk = new ChunkBuilder().WithFileEntry(fileEntry).Build();
        var stream = new MemoryStream(new byte[1024]);
        var chunkFile = new FormFile(stream, 0, stream.Length, "file", "test.chunk");

        _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id)).ReturnsAsync(fileEntry);
        _chunkRepository.Setup(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex)).ReturnsAsync((Chunk?)null);
        _blobService.Setup(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ThrowsAsync(new Exception("Blob upload failed"));

        var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

        var ex = await Assert.ThrowsAsync<Exception>(act);
        Assert.Equal("Blob upload failed", ex.Message);

        _fileEntryRepository.Verify(repo => repo.UnlockFile(fileEntry.Id), Times.Once);
    }

    [Fact]
public async Task UploadChunk_ShouldThrowException_WhenChunkRepositoryFailsToSave()
{
    var fileEntry = new FileEntryBuilder().Build();
    var chunk = new ChunkBuilder().WithFileEntry(fileEntry).Build();
    var stream = new MemoryStream(new byte[1024]);
    var chunkFile = new FormFile(stream, 0, stream.Length, "file", "test.chunk");

    _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id)).ReturnsAsync(fileEntry);
    _chunkRepository.Setup(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex)).ReturnsAsync((Chunk?)null);
    _blobService.Setup(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
        .ReturnsAsync(("fakeurl", chunk.ChunkHash));
    _chunkRepository.Setup(repo => repo.SaveChunk(It.IsAny<Chunk>())).ThrowsAsync(new Exception("Save failed"));

    var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("Save failed", ex.Message);

    _fileEntryRepository.Verify(repo => repo.UnlockFile(fileEntry.Id), Times.Once);
}

[Fact]
public async Task UploadChunk_ShouldThrowException_WhenLockOrUnlockFails()
{
    var fileEntry = new FileEntryBuilder().Build();
    var chunk = new ChunkBuilder().WithFileEntry(fileEntry).Build();
    var stream = new MemoryStream(new byte[1024]);
    var chunkFile = new FormFile(stream, 0, stream.Length, "file", "test.chunk");

    _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id)).ReturnsAsync(fileEntry);
    _fileEntryRepository.Setup(repo => repo.LockFile(fileEntry.Id)).ThrowsAsync(new Exception("Lock failed"));

    var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("Lock failed", ex.Message);

    _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.Never);
}





}
