using System;
using api.Models;
using api.Models.DTOs;
using api.tests.Builders;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{
    [Fact]
    public async Task CheckFileUploadStatus_ShouldReturnComplete_WhenAllChunksUploaded()
    {
        const int TOTAL_CHUNKS = 3;
        const int UPLOADED_CHUNKS = 3;

        var fileEntry = new FileEntryBuilder()
            .WithTotalChunks(TOTAL_CHUNKS)
            .WithUploadedChunks(UPLOADED_CHUNKS)
            .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryByFileHash(fileEntry.FileHash))
            .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
            .ReturnsAsync(fileEntry.Chunks.ToList());

        var result = await _fileEntryService.CheckFileUploadStatus(fileEntry.FileHash);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryByFileHash(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);
        Assert.NotNull(result);
        UploadResponseDto res = result!;
        Assert.Equal(UploadResponseDtoStatus.COMPLETE, res.Status);
        Assert.Equal(fileEntry.Id, res.FileId);
    }

    [Fact]
    public async Task CheckFileUploadStatus_ShouldReturnPartialStatus_WithUploadedChunks_WhenUploadIsInProgress()
    {
        const int TOTAL_CHUNKS = 3;
        const int UPLOADED_CHUNKS = 2;

        var fileEntry = new FileEntryBuilder()
                            .WithTotalChunks(TOTAL_CHUNKS)
                            .WithUploadedChunks(UPLOADED_CHUNKS)
                            .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryByFileHash(fileEntry.FileHash))
                                .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
                            .ReturnsAsync(fileEntry.Chunks.ToList());

        var result = await _fileEntryService.CheckFileUploadStatus(fileEntry.FileHash);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryByFileHash(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);
        Assert.NotNull(result);
        UploadResponseDto res = result!;
        Assert.Equal(UploadResponseDtoStatus.PARTIAL, res.Status);
        Assert.Equal(UPLOADED_CHUNKS, res.UploadedChunks?.Count);
        Assert.Equal(fileEntry.Id, res.FileId);
    }

    [Fact]
    public async Task CheckFileUploadStatus_ShouldCreateNewFileEntry_WhenFileDoesNotExist()
    {
        const int TOTAL_CHUNKS = 3;
        string fileHash = Guid.NewGuid().ToString("N");

        _fileEntryRepository.Setup(repo => repo.FindFileEntryByFileHash(fileHash))
                            .ReturnsAsync((FileEntry?)null);

        _fileEntryRepository.Setup(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()))
                            .ReturnsAsync((FileEntry fileEntry) => fileEntry);

        var result = await _fileEntryService.CheckFileUploadStatus(fileHash);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryByFileHash(It.IsAny<string>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Never);
        Assert.NotNull(result);
        UploadResponseDto res = result!;
        Assert.Equal(UploadResponseDtoStatus.NEW, res.Status);
        Assert.NotNull(res.FileId);
        Assert.Empty(res.UploadedChunks!);
    }

    [Fact]
    public async Task CheckFileUploadStatus_ShouldReturnPartialStatus_WithNoChunks_WhenNoChunksUploadedYet()
    {
        const int TOTAL_CHUNKS = 3;

        var fileEntry = new FileEntryBuilder()
                            .WithTotalChunks(TOTAL_CHUNKS)
                            .WithUploadedChunks(0)
                            .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryByFileHash(fileEntry.FileHash))
                            .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
                        .ReturnsAsync(new List<Chunk>());

        var result = await _fileEntryService.CheckFileUploadStatus(fileEntry.FileHash);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryByFileHash(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);
        Assert.NotNull(result);
        UploadResponseDto res = result!;
        Assert.Equal(UploadResponseDtoStatus.PARTIAL, res.Status);
        Assert.Empty(res.UploadedChunks!);
        Assert.Equal(fileEntry.Id, res.FileId);
    }

    [Fact]
    public async Task CheckFileUploadStatus_ShouldReturnPartialStatus_WithNonContiguousChunks()
    {
        const int TOTAL_CHUNKS = 5;

        var fileEntry = new FileEntryBuilder()
                            .WithTotalChunks(TOTAL_CHUNKS)
                            .Build();

        var nonContiguousChunks = new List<Chunk>
    {
        new ChunkBuilder().WithFileEntry(fileEntry).WithChunkIndex(0).Build(),
        new ChunkBuilder().WithFileEntry(fileEntry).WithChunkIndex(2).Build(),
        new ChunkBuilder().WithFileEntry(fileEntry).WithChunkIndex(4).Build()
    };

        _fileEntryRepository.Setup(repo => repo.FindFileEntryByFileHash(fileEntry.FileHash))
                            .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
                        .ReturnsAsync(nonContiguousChunks);

        var result = await _fileEntryService.CheckFileUploadStatus(fileEntry.FileHash);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryByFileHash(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);
        Assert.NotNull(result);
        UploadResponseDto res = result!;
        Assert.Equal(UploadResponseDtoStatus.PARTIAL, res.Status);
        Assert.Equal(3, res.UploadedChunks!.Count);
        Assert.Contains(0, res.UploadedChunks);
        Assert.Contains(2, res.UploadedChunks);
        Assert.Contains(4, res.UploadedChunks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckFileUploadStatus_ShouldThrowException_WhenFileHashIsNullOrEmpty(string? fileHash)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _fileEntryService.CheckFileUploadStatus(fileHash!));
    }

    [Fact]
    public async Task CheckFileUploadStatus_ShouldThrowException_IfRepositoryThrowsDuringFileLookup()
    {
        string fileHash = Guid.NewGuid().ToString("N");

        _fileEntryRepository.Setup(repo => repo.FindFileEntryByFileHash(fileHash))
                            .ThrowsAsync(new Exception("Database failure during file lookup"));

        _fileEntryRepository.Verify(repo => repo.FindFileEntryByFileHash(It.IsAny<string>()), Times.Once);
        await Assert.ThrowsAsync<Exception>(async () =>
            await _fileEntryService.CheckFileUploadStatus(fileHash));
    }

    [Fact]
    public async Task CheckFileUploadStatus_ShouldThrowException_IfChunkRepositoryFailsDuringProgressCheck()
    {
        var fileEntry = new FileEntryBuilder()
                            .WithTotalChunks(3)
                            .WithUploadedChunks(2)
                            .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryByFileHash(fileEntry.FileHash))
                            .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
                        .ThrowsAsync(new Exception("Chunk DB failure"));

        _fileEntryRepository.Verify(repo => repo.FindFileEntryByFileHash(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);

        await Assert.ThrowsAsync<Exception>(async () =>
            await _fileEntryService.CheckFileUploadStatus(fileEntry.FileHash));
    }


}
