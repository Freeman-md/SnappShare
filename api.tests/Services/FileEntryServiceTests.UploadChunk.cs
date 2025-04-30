using System;
using api.Models;
using api.Models.DTOs;
using api.tests.Builders;
using Azure;
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

        _blobService.Setup(x => x.BlockExistsAsync(It.IsAny<string>(), _storageOptions.Value.ContainerName, chunk.BlockId))
                    .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        _blobService.Setup(x => x.UploadChunkBlockAsync(chunkFile, It.IsAny<string>(), _storageOptions.Value.ContainerName, chunk.BlockId))
        .Returns(Task.FromResult(Mock.Of<Response>()));

        _chunkRepository.Setup(repo => repo.SaveChunk(It.IsAny<Chunk>()))
            .ReturnsAsync(chunk);

        _fileEntryRepository.Setup(repo => repo.UnlockFile(fileEntry.Id))
        .Callback(() =>
        {
            fileEntry.IsLocked = false;
            fileEntry.LockedAt = null;
        });

        var result = await _fileEntryService.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.SUCCESS, result.Status);
        Assert.Equal(chunk.ChunkIndex, result.UploadedChunk);

        Assert.False(fileEntry.IsLocked);
        Assert.Null(fileEntry.LockedAt);

        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex), Times.Once);
        _blobService.Verify(blob => blob.UploadChunkBlockAsync(chunkFile, It.IsAny<string>(), _storageOptions.Value.ContainerName, chunk.BlockId), Times.Once);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Once);
    }

    [Fact]
    public async Task UploadChunk_ShouldSkipUpload_WhenChunkAlreadyExistsInDatabase()
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

        var result = await _fileEntryService.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.SKIPPED, result.Status);

        Assert.False(fileEntry.IsLocked);
        Assert.Null(fileEntry.LockedAt);

        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(fileEntry.Id, chunk.ChunkIndex), Times.Once);
        _blobService.Verify(blob => blob.BlockExistsAsync(It.IsAny<string>(), _storageOptions.Value.ContainerName, chunk.BlockId), Times.Never);
        _blobService.Verify(blob => blob.UploadChunkBlockAsync(chunkFile, It.IsAny<string>(), _storageOptions.Value.ContainerName, chunk.BlockId), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "filename.txt", 0, 1024, "valid-hash")]
    [InlineData("", "filename.txt", 0, 1024, "valid-hash")]
    [InlineData("   ", "filename.txt", 0, 1024, "valid-hash")]
    [InlineData("file-id", null, 0, 1024, "valid-hash")]
    [InlineData("file-id", "", 0, 1024, "valid-hash")]
    [InlineData("file-id", "   ", 0, 1024, "valid-hash")]
    [InlineData("file-id", "filename.txt", -1, 1024, "valid-hash")]
    [InlineData("file-id", "filename.txt", 0, 0, "valid-hash")]
    [InlineData("file-id", "filename.txt", 0, -1024, "valid-hash")]
    [InlineData("file-id", "filename.txt", 0, 1024, null)]
    [InlineData("file-id", "filename.txt", 0, 1024, "")]
    [InlineData("file-id", "filename.txt", 0, 1024, "   ")]
    public async Task UploadChunk_ShouldThrowArgumentException_ForInvalidInputs(
    string? fileId, string? fileName, int chunkIndex, long chunkSize, string? chunkHash)
    {
        var stream = new MemoryStream(new byte[chunkSize > 0 ? chunkSize : 1]);
        var chunkFile = new FormFile(stream, 0, chunkSize, "file", "test.chunk");

        var act = async () => await _fileEntryService.UploadChunk(fileId!, fileName!, chunkIndex, chunkFile, chunkHash!);

        var ex = await Assert.ThrowsAsync<ArgumentException>(act);

        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _blobService.Verify(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
    }



    [Fact]
    public async Task UploadChunk_ShouldThrowArgumentException_WhenChunkFileIsNull()
    {
        var fileEntry = new FileEntryBuilder().Build();
        IFormFile? chunkFile = null;

        var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, fileEntry.FileName, 0, chunkFile!, "valid-hash");

        await Assert.ThrowsAsync<ArgumentException>(act);

        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _blobService.Verify(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
    }

    [Fact]
    public async Task UploadChunk_ShouldThrowArgumentException_WhenChunkFileIsEmpty()
    {
        var fileEntry = new FileEntryBuilder().Build();
        var stream = new MemoryStream(); // 0 bytes
        var chunkFile = new FormFile(stream, 0, 0, "file", "empty.chunk");

        var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, fileEntry.FileName, 0, chunkFile, "valid-hash");

        await Assert.ThrowsAsync<ArgumentException>(act);

        _chunkRepository.Verify(repo => repo.FindChunkByFileIdAndChunkIndex(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _blobService.Verify(blob => blob.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _chunkRepository.Verify(repo => repo.SaveChunk(It.IsAny<Chunk>()), Times.Never);
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
        _blobService.Setup(blob => blob.UploadChunkBlockAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Blob upload failed"));

        var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

        var ex = await Assert.ThrowsAsync<Exception>(act);
        Assert.Equal("Blob upload failed", ex.Message);

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

        var act = async () => await _fileEntryService.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash);

        var ex = await Assert.ThrowsAsync<Exception>(act);
        Assert.Equal("Save failed", ex.Message);
    }
}
