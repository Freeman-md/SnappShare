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
    private (FileEntry fileEntry, Chunk chunk, IFormFile chunkFile) SetupFileEntryChunkAndForm()
    {
        var fileEntry = new FileEntryBuilder().WithTotalChunks(3).WithFileSize(100).Build();
        var chunk = new ChunkBuilder().WithFileEntry(fileEntry).Build();
        var fileStream = new MemoryStream(new byte[1024]);
        var chunkFile = new FormFile(fileStream, 0, fileStream.Length, "file", "chunk1");

        return (fileEntry, chunk, chunkFile);
    }


    [Fact]
    public async Task HandleFileUpload_ShouldCreateFileEntry_WhenUploadStatusIsNew()
    {
        // Arrange
        var (fileEntry, chunk, chunkFile) = SetupFileEntryChunkAndForm();


        _fileEntryServiceMock.Setup(s => s.CheckFileUploadStatus(fileEntry.FileHash))
            .ReturnsAsync(new UploadResponseDto { Status = UploadResponseDtoStatus.NEW });

        _fileEntryServiceMock.Setup(s => s.CreateFileEntry(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, fileEntry.TotalChunks))
            .ReturnsAsync(fileEntry);

        _fileEntryServiceMock.Setup(s => s.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash))
            .ReturnsAsync(new UploadResponseDto { Status = UploadResponseDtoStatus.SUCCESS, UploadedChunk = chunk.ChunkIndex });

        _chunkRepository.Setup(r => r.GetUploadedChunksByFileId(fileEntry.Id))
            .ReturnsAsync(() => new() { chunk });

        // Act
        var response = await _fileEntryService.HandleFileUpload(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, chunk.ChunkIndex, fileEntry.TotalChunks, chunkFile, chunk.ChunkHash);

        // Assert
        Assert.Equal(UploadResponseDtoStatus.SUCCESS, response.Status);
        Assert.Equal(chunk.ChunkIndex, response.UploadedChunk);

        _fileEntryServiceMock.Verify(service => service.CheckFileUploadStatus(fileEntry.FileHash), Times.Once);
        _fileEntryServiceMock.Verify(service => service.CreateFileEntry(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, fileEntry.TotalChunks), Times.Once);
        _fileEntryServiceMock.Verify(service => service.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(fileEntry.Id), Times.Once);
        _fileEntryServiceMock.Verify(service => service.FinalizeUpload(fileEntry.Id), Times.Never);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnSkippedStatus_WhenChunkWasAlreadyUploaded()
    {
        var (fileEntry, chunk, chunkFile) = SetupFileEntryChunkAndForm();


        _fileEntryServiceMock.Setup(s => s.CheckFileUploadStatus(fileEntry.FileHash))
            .ReturnsAsync(new UploadResponseDto { Status = UploadResponseDtoStatus.PARTIAL, FileId = fileEntry.Id });

        _fileEntryServiceMock.Setup(s => s.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash))
            .ReturnsAsync(new UploadResponseDto { Status = UploadResponseDtoStatus.SKIPPED });

        _chunkRepository.Setup(r => r.GetUploadedChunksByFileId(fileEntry.Id))
        .ReturnsAsync(() => new() { chunk });

        var response = await _fileEntryService.HandleFileUpload(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, chunk.ChunkIndex, fileEntry.TotalChunks, chunkFile, chunk.ChunkHash);

        Assert.Equal(UploadResponseDtoStatus.SKIPPED, response.Status);
        _fileEntryServiceMock.Verify(service => service.CheckFileUploadStatus(fileEntry.FileHash), Times.Once);
        _fileEntryServiceMock.Verify(service => service.CreateFileEntry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()), Times.Never);
        _fileEntryServiceMock.Verify(service => service.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(fileEntry.Id), Times.Once);
        _fileEntryServiceMock.Verify(service => service.FinalizeUpload(fileEntry.Id), Times.Never);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldFinalizeUpload_WhenAllChunksAreUploaded()
    {
        var (_, chunk, chunkFile) = SetupFileEntryChunkAndForm();

        var fileEntry = new FileEntryBuilder().WithTotalChunks(3).WithFileSize(100).WithUploadedChunks(3).Build();


        _fileEntryServiceMock.Setup(s => s.CheckFileUploadStatus(fileEntry.FileHash))
            .ReturnsAsync(new UploadResponseDto { Status = UploadResponseDtoStatus.PARTIAL, FileId = fileEntry.Id });

        _fileEntryServiceMock.Setup(s => s.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash))
            .ReturnsAsync(new UploadResponseDto { Status = UploadResponseDtoStatus.SUCCESS });

        _chunkRepository.Setup(r => r.GetUploadedChunksByFileId(fileEntry.Id))
            .ReturnsAsync(fileEntry.Chunks.ToList());

        _fileEntryServiceMock.Setup(s => s.FinalizeUpload(fileEntry.Id))
            .ReturnsAsync(new UploadResponseDto { Status = UploadResponseDtoStatus.COMPLETE });

        var result = await _fileEntryService.HandleFileUpload(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, chunk.ChunkIndex, fileEntry.TotalChunks, chunkFile, chunk.ChunkHash);

        Assert.Equal(UploadResponseDtoStatus.COMPLETE, result.Status);
        _fileEntryServiceMock.Verify(service => service.CheckFileUploadStatus(fileEntry.FileHash), Times.Once);
        _fileEntryServiceMock.Verify(service => service.CreateFileEntry(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, fileEntry.TotalChunks), Times.Never);
        _fileEntryServiceMock.Verify(service => service.UploadChunk(fileEntry.Id, fileEntry.FileName, chunk.ChunkIndex, chunkFile, chunk.ChunkHash), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(fileEntry.Id), Times.Once);
        _fileEntryServiceMock.Verify(service => service.FinalizeUpload(fileEntry.Id), Times.Once);
    }

    [Theory]
    [InlineData(null, "hash", "chunkhash", 0, 1, 1024)] // Invalid fileName
    [InlineData("", "hash", "chunkhash", 0, 1, 1024)]
    [InlineData("   ", "hash", "chunkhash", 0, 1, 1024)]

    [InlineData("file.txt", null, "chunkhash", 0, 1, 1024)] // Invalid fileHash
    [InlineData("file.txt", "", "chunkhash", 0, 1, 1024)]
    [InlineData("file.txt", "   ", "chunkhash", 0, 1, 1024)]

    [InlineData("file.txt", "hash", null, 0, 1, 1024)] // Invalid chunkHash
    [InlineData("file.txt", "hash", "", 0, 1, 1024)]
    [InlineData("file.txt", "hash", "   ", 0, 1, 1024)]

    [InlineData("file.txt", "hash", "chunkhash", -1, 1, 1024)] // Invalid chunkIndex

    [InlineData("file.txt", "hash", "chunkhash", 0, 0, 1024)] // Invalid totalChunks
    [InlineData("file.txt", "hash", "chunkhash", 0, -5, 1024)]

    [InlineData("file.txt", "hash", "chunkhash", 0, 1, 0)] // Invalid fileSize
    [InlineData("file.txt", "hash", "chunkhash", 0, 1, -1024)]
    public async Task HandleFileUpload_ShouldThrowArgumentException_ForInvalidParameters(
    string? fileName,
    string? fileHash,
    string? chunkHash,
    int chunkIndex,
    int totalChunks,
    long fileSize)
    {
        var chunkFile = new FormFile(new MemoryStream(new byte[1]), 0, 1, "file", "chunk1");

        var act = async () => await _fileEntryService.HandleFileUpload(fileName!, fileHash!, fileSize, chunkIndex, totalChunks, chunkFile, chunkHash!);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldThrowArgumentException_WhenChunkFileIsNullOrEmpty()
    {
        var fileEntry = new FileEntryBuilder().Build();
        var emptyFile = new FormFile(new MemoryStream(), 0, 0, "file", "empty.chunk");

        // null chunkFile
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _fileEntryService.HandleFileUpload(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, 0, fileEntry.TotalChunks, null!, "chunkhash"));

        // empty chunkFile
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _fileEntryService.HandleFileUpload(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, 0, fileEntry.TotalChunks, emptyFile, "chunkhash"));
    }


}
