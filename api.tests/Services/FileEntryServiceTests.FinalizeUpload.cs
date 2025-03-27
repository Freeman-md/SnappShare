using System;
using api.Models;
using api.tests.Builders;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{

    [Fact]
    public async Task FinalizeUpload_ShouldFinalizeUploadSuccessfully_WhenAllChunksAreUploaded()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                        .WithTotalChunks(3)
                                        .WithUploadedChunks(3)
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

        _fileEntryRepository.Setup(repo => repo.MarkUploadComplete(fileEntry.Id, fileEntry.FileUrl))
        .Callback(() =>
        {
            fileEntry.Status = FileEntryStatus.Completed;
        });
    }

}
