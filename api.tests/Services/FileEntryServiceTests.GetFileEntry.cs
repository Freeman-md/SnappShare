using System;
using System.Threading.Tasks;
using api.Models;
using api.Models.DTOs;
using api.tests.Builders;
using Azure;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{
    [Fact]
    public async Task GetFileEntry_ReturnsCompleteDetailsWithFileUrl_WhenFileUploadIsComplete()
    {
        const int EXPECTED_TOTAL_CHUNKS = 4;
        const int EXPECTED_UPLOADED_CHUNKS = 4;
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithTotalChunks(EXPECTED_TOTAL_CHUNKS)
                                    .WithUploadedChunks(EXPECTED_UPLOADED_CHUNKS)
                                    .WithStatus(FileEntryStatus.Completed)
                                    .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);


        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);


        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.COMPLETE, result.Status);
        Assert.Equal(EXPECTED_TOTAL_CHUNKS, result.TotalChunks);
        Assert.Equal(EXPECTED_UPLOADED_CHUNKS, result.UploadedChunks!.Count);
        Assert.NotNull(result.FileId);
        Assert.NotNull(result.FileName);
        Assert.NotNull(result.FileSize);
        Assert.NotNull(result.FileUrl);
        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(fileEntry.Id), Times.Once);
    }

    [Fact]
    public async Task GetFileEntry_ReturnsPartialDetails_WhenFileUploadIsInProgress()
    {
        const int EXPECTED_TOTAL_CHUNKS = 4;
        const int EXPECTED_UPLOADED_CHUNKS = 2;
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithTotalChunks(EXPECTED_TOTAL_CHUNKS)
                                    .WithUploadedChunks(EXPECTED_UPLOADED_CHUNKS)
                                    .WithStatus(FileEntryStatus.Pending)
                                    .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);


        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);


        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.PARTIAL, result.Status);
        Assert.Equal(EXPECTED_TOTAL_CHUNKS, result.TotalChunks);
        Assert.Equal(EXPECTED_UPLOADED_CHUNKS, result.UploadedChunks!.Count);
        Assert.NotNull(result.FileId);
        Assert.NotNull(result.FileName);
        Assert.NotNull(result.FileSize);
        Assert.Null(result.FileUrl);
        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(fileEntry.Id), Times.Once);
    }

    [Theory]
    [InlineData(UploadResponseDtoStatus.NEW, FileEntryStatus.Pending, 4, 0)]
    [InlineData(UploadResponseDtoStatus.FAILED, FileEntryStatus.Failed, 4, 3)]
    public async Task GetFileEntry_ReturnsCorrectStatus_AndChunkProgress(UploadResponseDtoStatus uploadResponseDtoStatus, FileEntryStatus fileEntryStatus, int totalChunks, int uploadedChunks)
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                            .WithTotalChunks(totalChunks)
                                            .WithUploadedChunks(uploadedChunks)
                                            .WithStatus(fileEntryStatus)
                                            .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);


        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);


        Assert.NotNull(result);
        Assert.Equal(uploadResponseDtoStatus, result.Status);
        Assert.Equal(totalChunks, result.TotalChunks);
        Assert.Equal(uploadedChunks, result.UploadedChunks!.Count);
        Assert.Null(result.FileUrl);
        _fileEntryRepository.Verify(repo => repo.FindFileEntryById(fileEntry.Id), Times.Once);
    }

    [Fact]
    public async Task GetFileEntry_ThrowsKeyNotFoundException_WhenFileIdDoesNotExist()
    {
        FileEntry fileEntry = new FileEntryBuilder().Build();
        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _fileEntryService.GetFileEntry(fileEntry.Id));
    }

    [Fact]
    public async Task GetFileEntry_ThrowsArgumentException_WhenFileIdIsNullOrEmpty()
    {
        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(null!));

        await Assert.ThrowsAsync<ArgumentException>(async () => await _fileEntryService.GetFileEntry(null!));
    }

    [Fact]
    public async Task GetFileEntry_ThrowsException_WhenRepositoryFails()
    {
        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(It.IsAny<string>()))
                    .ThrowsAsync(new Exception());

        await Assert.ThrowsAnyAsync<Exception>(async () => await _fileEntryService.GetFileEntry(It.IsAny<string>()));
    }

    [Fact]
    public async Task GetFileEntry_ReturnsZeroChunksUploaded_WhenUploadJustStarted()
    {
        const int EXPECTED_TOTAL_CHUNKS = 5;
        const int EXPECTED_UPLOADED_CHUNKS = 0;
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithTotalChunks(EXPECTED_TOTAL_CHUNKS)
                                    .WithUploadedChunks(EXPECTED_UPLOADED_CHUNKS)
                                    .WithStatus(FileEntryStatus.Pending)
                                    .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);

        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.NEW, result.Status);
        Assert.Equal(EXPECTED_UPLOADED_CHUNKS, result.UploadedChunks?.Count);
        Assert.Null(result.FileUrl);
    }

    [Fact]
    public async Task GetFileEntry_ReturnsLastKnownProgress_WhenUploadWasInterrupted()
    {
        const int EXPECTED_TOTAL_CHUNKS = 6;
        const int EXPECTED_UPLOADED_CHUNKS = 3;
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithTotalChunks(EXPECTED_TOTAL_CHUNKS)
                                    .WithUploadedChunks(EXPECTED_UPLOADED_CHUNKS)
                                    .WithStatus(FileEntryStatus.Pending)
                                    .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);

        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.PARTIAL, result.Status);
        Assert.Equal(EXPECTED_TOTAL_CHUNKS, result.TotalChunks);
        Assert.Equal(EXPECTED_UPLOADED_CHUNKS, result.UploadedChunks?.Count);
        Assert.Null(result.FileUrl);
    }

    [Fact]
    public async Task GetFileEntry_HandlesFileWithUnexpectedStatusGracefully()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithTotalChunks(2)
                                    .WithUploadedChunks(1)
                                    .WithStatus((FileEntryStatus)999) // unknown enum value
                                    .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);

        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.FAILED, result.Status);
        Assert.Null(result.FileUrl);
    }

    [Fact]
    public async Task GetFileEntry_HandlesMaxChunkCountCorrectly_ForLargeFiles()
    {
        const int MAX_CHUNKS = 1000;
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithTotalChunks(MAX_CHUNKS)
                                    .WithUploadedChunks(MAX_CHUNKS)
                                    .WithStatus(FileEntryStatus.Completed)
                                    .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);

        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.COMPLETE, result.Status);
        Assert.Equal(MAX_CHUNKS, result.UploadedChunks?.Count);
        Assert.NotNull(result.FileUrl);
    }

    [Fact]
    public async Task GetFileEntry_DoesNotExposeSecureUrl_IfFileIsNotComplete()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithTotalChunks(5)
                                    .WithUploadedChunks(4)
                                    .WithStatus(FileEntryStatus.Pending)
                                    .Build();

        _fileEntryRepository
                    .Setup(repo => repo.FindFileEntryById(fileEntry.Id))
                    .ReturnsAsync(fileEntry);

        UploadResponseDto result = await _fileEntryService.GetFileEntry(fileEntry.Id);

        Assert.NotNull(result);
        Assert.Equal(UploadResponseDtoStatus.PARTIAL, result.Status);
        Assert.Null(result.FileUrl); // ✅ no access until fully uploaded
    }

}
