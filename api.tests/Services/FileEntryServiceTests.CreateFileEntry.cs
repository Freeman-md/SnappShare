using System;
using api.Enums;
using api.Models;
using api.tests.Builders;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{
    [Fact]
    public async Task CreateFileEntry_ShouldCreateFileEntrySuccessfully_AndReturnValidId()
    {
        FileEntry fileEntry = new FileEntryBuilder().WithFileSize(1024).WithTotalChunks(4).Build();

        _fileEntryRepository
                .Setup(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()))
                .ReturnsAsync(fileEntry);


        FileEntry createdFileEntry = await _fileEntryService.CreateFileEntry(fileEntry.FileName, fileEntry.FileHash, fileEntry.FileSize, fileEntry.TotalChunks, fileEntry.ExpiresIn);

        Assert.NotNull(createdFileEntry);
        Assert.True(fileEntry.PropertiesAreEqual(createdFileEntry));

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
    public async Task CreateFileEntry_ShouldThrowArgumentException_ForInvalidInput(string? fileName, string? fileHash, long fileSize, int totalChunks)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.CreateFileEntry(fileName!, fileHash!, fileSize, totalChunks, It.IsAny<ExpiryDuration>()));
    }

    [Theory]
    [InlineData((ExpiryDuration)(-1))]
    [InlineData((ExpiryDuration)(99))]
    public async Task CreateFileEntry_ShouldThrowArgumentOutOfRangeException_ForInvalidInput(ExpiryDuration expiresIn)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _fileEntryService.CreateFileEntry("File Name", "File Hash", 5, 5, expiresIn));
    }

    [Fact]
    public async Task CreateFileEntry_ShouldThrowException_WhenBlobServiceFailsToGenerateSasUrl()
    {
        _blobService
                .Setup(x => x.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .ThrowsAsync(new Exception("Creation of Sas url for blob failed."));

        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.CreateFileEntry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<ExpiryDuration>()));

        _blobService.Verify(service => service.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()), Times.Never);

    }

    [Fact]
    public async Task CreateFileEntry_ShouldThrowException_WhenFileEntryRepositoryFailsToCreateEntry()
    {
        _blobService
                .Setup(x => x.GenerateSasTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                                .ReturnsAsync(It.IsAny<string>());

        _fileEntryRepository
                .Setup(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()))
                .ReturnsAsync(It.IsAny<FileEntry>());

        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.CreateFileEntry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<ExpiryDuration>()));

        _blobService.Verify(service => service.GenerateSasTokenAsync(It.IsAny<string>(), _storageOptions.Value.ContainerName, It.IsAny<DateTimeOffset>()), Times.Never);
        _fileEntryRepository.Verify(repo => repo.CreateFileEntry(It.IsAny<FileEntry>()), Times.Never);

    }
}
