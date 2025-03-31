using System;
using api.Models;
using api.Models.DTOs;
using api.tests.Builders;
using Azure;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{

    [Fact]
    public async Task FinalizeUpload_ShouldReturnBlobUrl_WhenAllChunksAreUploadedSuccessfully()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                        .WithTotalChunks(3)
                                        .WithUploadedChunks(3)
                                        .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                                .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
                        .ReturnsAsync(fileEntry.Chunks.ToList());

        _blobService.Setup(repo => repo.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                    .Returns(Task.FromResult(Mock.Of<Response>()));

        _fileEntryRepository.Setup(repo => repo.LockFile(fileEntry.Id))
                            .Callback(() =>
                            {
                                fileEntry.IsLocked = true;
                                fileEntry.LockedAt = DateTime.UtcNow;
                            });

        _fileEntryRepository.Setup(repo => repo.UnlockFile(fileEntry.Id))
        .Callback(() =>
        {
            fileEntry.IsLocked = false;
            fileEntry.LockedAt = null;
        });

        _fileEntryRepository.Setup(repo => repo.MarkUploadComplete(fileEntry.Id, fileEntry.FileUrl))
        .Callback(() =>
        {
            fileEntry.Status = FileEntryStatus.Completed;
        });


        var response = await _fileEntryService.FinalizeUpload(fileEntry.Id);

        Assert.NotNull(response);
        Assert.Equal(UploadResponseDtoStatus.COMPLETE, response.Status);
        Assert.Equal(fileEntry.FileUrl, response.FileUrl);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);
        _blobService.Verify(repo => repo.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.AtLeastOnce);
        _fileEntryRepository.Verify(repo => repo.MarkUploadComplete(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task FinalizeUpload_ShouldReturnEarly_WhenFileIsAlreadyCompleted()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                                .WithStatus(FileEntryStatus.Completed)
                                                .WithTotalChunks(3)
                                                .WithUploadedChunks(3)
                                                .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                                .ReturnsAsync(fileEntry);

        var response = await _fileEntryService.FinalizeUpload(fileEntry.Id);

        Assert.NotNull(response);
        Assert.Equal(UploadResponseDtoStatus.COMPLETE, response.Status);
        Assert.Equal(fileEntry.FileUrl, response.FileUrl);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Never);
        _blobService.Verify(repo => repo.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.AtLeastOnce);
        _fileEntryRepository.Verify(repo => repo.MarkUploadComplete(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FinalizeUpload_ShouldReturnIncompleteStatus_WhenChunksAreMissing()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                                        .WithTotalChunks(5)
                                                        .WithUploadedChunks(2)
                                                        .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id))
            .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
                        .ReturnsAsync(fileEntry.Chunks.ToList());

        _fileEntryRepository.Setup(repo => repo.LockFile(fileEntry.Id))
                            .Callback(() =>
                            {
                                fileEntry.IsLocked = true;
                                fileEntry.LockedAt = DateTime.UtcNow;
                            });

        _fileEntryRepository.Setup(repo => repo.UnlockFile(fileEntry.Id))
        .Callback(() =>
        {
            fileEntry.IsLocked = false;
            fileEntry.LockedAt = null;
        });

        var response = await _fileEntryService.FinalizeUpload(fileEntry.Id);

        Assert.NotNull(response);
        Assert.Equal(UploadResponseDtoStatus.FAILED, response.Status);

        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);
        _blobService.Verify(repo => repo.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.AtLeastOnce);
        _fileEntryRepository.Verify(repo => repo.MarkUploadComplete(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FinalizeUpload_ShouldThrowException_WhenFileEntryNotFound()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                                        .WithStatus(FileEntryStatus.Completed)
                                                        .WithTotalChunks(5)
                                                        .WithUploadedChunks(2)
                                                        .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                                .ThrowsAsync(new KeyNotFoundException("File not found"));

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _fileEntryService.FinalizeUpload(fileEntry.Id));

        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Never);
        _blobService.Verify(repo => repo.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.MarkUploadComplete(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task FinalizeUpload_ShouldThrowArgumentException_ForInvalidFileId(string? fileId)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.FinalizeUpload(fileId!));

        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(It.IsAny<string>()), Times.Never);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Never);
        _blobService.Verify(repo => repo.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.MarkUploadComplete(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FinalizeUpload_ShouldThrowException_WhenCommitBlockFails()
    {
        var fileEntry = new FileEntryBuilder()
                                .WithTotalChunks(2)
                                .WithUploadedChunks(2)
                                .Build();

        _fileEntryRepository.Setup(repo => repo.FindFileEntryById(fileEntry.Id))
            .ReturnsAsync(fileEntry);

        _chunkRepository.Setup(repo => repo.GetUploadedChunksByFileId(fileEntry.Id))
            .ReturnsAsync(fileEntry.Chunks.ToList());

        _fileEntryRepository.Setup(repo => repo.LockFile(fileEntry.Id));
        _fileEntryRepository.Setup(repo => repo.UnlockFile(fileEntry.Id));

        _blobService.Setup(blob => blob.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new Exception("Commit failed"));

        await Assert.ThrowsAsync<Exception>(() => _fileEntryService.FinalizeUpload(fileEntry.Id));

        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(It.IsAny<string>()), Times.Once);
        _chunkRepository.Verify(repo => repo.GetUploadedChunksByFileId(It.IsAny<string>()), Times.Once);
        _blobService.Verify(repo => repo.CommitBlockListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.LockFile(It.IsAny<string>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.UnlockFile(It.IsAny<string>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.MarkUploadComplete(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }



}
