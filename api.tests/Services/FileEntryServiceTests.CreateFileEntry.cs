using System;
using api.Models;
using api.tests.Builders;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{
    [Fact]
    public async Task CreateFileEntry_ShouldCreateFileEntrySuccessfully_AndReturnValidId() {
        FileEntry fileEntry = new FileEntryBuilder().WithFileSize(1024).WithTotalChunks(4).Build();

        _blobService
                .Setup(x => x.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(It.IsAny<string>());

        _fileEntryRepository
                .Setup(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()))
                .ReturnsAsync(fileEntry);


        FileEntry createdFileEntry = await _fileEntryService.CreateFileEntry(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, fileEntry.TotalChunks);

        Assert.NotNull(createdFileEntry);
        Assert.True(fileEntry.PropertiesAreEqual(createdFileEntry));

        _blobService.Verify(service => service.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()), Times.Once);
    }

    [Fact]
    public async Task CreateFileEntry_ShouldGenerateAndStoreSasUrlForNewFileEntry() {
        FileEntry fileEntry = new FileEntryBuilder().WithFileSize(1024).WithTotalChunks(4).Build();

        _blobService
                .Setup(x => x.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(fileEntry.FileUrl!);

        _fileEntryRepository
                .Setup(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()))
                .ReturnsAsync(fileEntry);


        FileEntry createdFileEntry = await _fileEntryService.CreateFileEntry(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, fileEntry.TotalChunks);

        Assert.NotNull(createdFileEntry);
        Assert.Equal(fileEntry.FileUrl, createdFileEntry.FileUrl);

        _blobService.Verify(service => service.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Once);
        _fileEntryRepository.Verify(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()), Times.Once);
    }

    [Theory]
    [InlineData("", "file-hash", 1024, 5)]
    [InlineData("  ", "file-hash", 1024, 5)]
    [InlineData(null, "file-hash", 1024, 5)]
    [InlineData("file-name", "", 1024, 5)]
    [InlineData("file-name", "  ", 1024, 5)]
    [InlineData("file-name", null, 1024, 5)]
    [InlineData("file-name", "file-hash", -1024, 5)]
    [InlineData("file-name", "file-hash", 0, 5)]
    [InlineData("file-name", "file-hash", 1024, 0)]
    [InlineData("file-name", "file-hash", 1024, -1)]
    public async Task CreateFileEntry_ShouldThrowArgumentException_ForInvalidInput(string? fileName, string? fileHash, long fileSize, int totalChunks) {
        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.CreateFileEntry(fileName!, fileHash!, fileSize, totalChunks));
    }

    [Fact]
    public async Task CreateFileEntry_ShouldThrowException_WhenBlobServiceFailsToGenerateSasUrl() {
        _blobService
                .Setup(x => x.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .ThrowsAsync(new Exception("Creation of Sas url for blob failed."));

        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.CreateFileEntry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()));

        _blobService.Verify(service => service.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()), Times.Never);

    }

    [Fact]
    public async Task CreateFileEntry_ShouldThrowException_WhenFileEntryRepositoryFailsToCreateEntry() {
        _blobService
                .Setup(x => x.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                                .ReturnsAsync(It.IsAny<string>());

        _fileEntryRepository
                .Setup(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()))
                .ReturnsAsync(It.IsAny<FileEntry>());

        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.CreateFileEntry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()));

        _blobService.Verify(service => service.GenerateSasTokenAsync(It.IsAny<string>(), _storageOptions.Value.ContainerName, It.IsAny<DateTimeOffset>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()), Times.Never);

    }
}
