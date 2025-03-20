using System;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces.Repositories;
using api.Models;
using api.Repositories;
using api.tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace api.tests.Repositories;

public class FileEntryRepositoryTests
{
    private readonly SnappshareContext _dbContext;

    private readonly IFileEntryRepository _fileEntryRepository;

    public FileEntryRepositoryTests()
    {
        DbContextOptions<SnappshareContext> options = new DbContextOptionsBuilder<SnappshareContext>()
                                                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                                                .Options;

        _dbContext = new SnappshareContext(options);
        _fileEntryRepository = new FileEntryRepository(_dbContext);
    }

    [Fact]
    public async Task AddFileEntry_ShouldSaveAndReturnFile_WhenSuccessful()
    {
        FileEntry unsavedFileEntry = new FileEntryBuilder().Build();

        FileEntry savedFileEntry = await _fileEntryRepository.CreateFileEntry(unsavedFileEntry);

        Assert.NotNull(savedFileEntry);
        Assert.Equal(unsavedFileEntry.FileName, savedFileEntry.FileName);
        Assert.Equal(unsavedFileEntry.FileHash, savedFileEntry.FileHash);
    }

    [Fact]
    public async Task AddFileEntry_ShouldThrowException_WhenDbFails()
    {
        var faultyDbContext = new SnappshareContext(new DbContextOptionsBuilder<SnappshareContext>()
                                                .UseInMemoryDatabase(databaseName: "FaultyDB")
                                                .Options);
        faultyDbContext.Dispose();

        var repository = new FileEntryRepository(_dbContext);

        var fileEntry = new FileEntryBuilder().Build();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await repository.CreateFileEntry(fileEntry));
    }

    [Fact]
    public async Task FindFileEntryByFileHash_ShouldReturnFile_WhenExists()
    {
        FileEntry savedFileEntry = await _fileEntryRepository.CreateFileEntry(new FileEntryBuilder().Build());

        FileEntry? retrievedFileEntry = await _fileEntryRepository.FindFileEntryByFileHash(savedFileEntry.FileHash);

        Assert.NotNull(retrievedFileEntry);

        Assert.Equal(savedFileEntry.FileName, retrievedFileEntry.FileName);
        Assert.Equal(savedFileEntry.FileHash, retrievedFileEntry.FileHash);
    }

    [Fact]
    public async Task FindFileEntryById_ShouldReturnFile_WhenExists()
    {
        FileEntry savedFileEntry = await _fileEntryRepository.CreateFileEntry(new FileEntryBuilder().Build());

        FileEntry? retrievedFileEntry = await _fileEntryRepository.FindFileEntryById(savedFileEntry.Id);

        Assert.NotNull(retrievedFileEntry);

        Assert.Equal(savedFileEntry.FileName, retrievedFileEntry.FileName);
        Assert.Equal(savedFileEntry.FileHash, retrievedFileEntry.FileHash);
    }

    [Fact]
    public async Task FindFileEntryByFileHash_ShouldReturnNull_WhenFileDoesNotExist()
    {
        FileEntry? retrievedFileEntry = await _fileEntryRepository.FindFileEntryByFileHash(Guid.NewGuid().ToString());

        Assert.Null(retrievedFileEntry);
    }

    [Fact]
    public async Task FindFileEntryById_ShouldReturnNull_WhenFileDoesNotExist()
    {
        FileEntry? retrievedFileEntry = await _fileEntryRepository.FindFileEntryById(Guid.NewGuid().ToString());

        Assert.Null(retrievedFileEntry);
    }

    [Fact]
    public async Task UpdateFileEntry_ShouldUpdateFileEntrySuccessfully()
    {
        FileEntry unsavedFileEntry = new FileEntryBuilder().Build();
        FileEntry savedFileEntry = await _fileEntryRepository.CreateFileEntry(unsavedFileEntry);
        savedFileEntry.FileName = Guid.NewGuid().ToString();
        savedFileEntry.FileHash = Guid.NewGuid().ToString();

        FileEntry updatedFileEntry = await _fileEntryRepository.UpdateFileEntry(savedFileEntry.Id, savedFileEntry);

        Assert.NotNull(updatedFileEntry);
        Assert.Equal(savedFileEntry.FileName, updatedFileEntry.FileName);
        Assert.Equal(savedFileEntry.FileHash, updatedFileEntry.FileHash);
        Assert.NotEqual(unsavedFileEntry.FileName, updatedFileEntry.FileName);
        Assert.NotEqual(unsavedFileEntry.FileHash, updatedFileEntry.FileHash);
    }

    [Fact]
    public async Task UpdateFileEntry_ShouldThrowException_WhenFileDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _fileEntryRepository.UpdateFileEntry(Guid.NewGuid().ToString(), new FileEntryBuilder().Build()));
    }

    [Fact]
    public async Task LockFile_ShouldApplyLockSuccessfully()
    {
        FileEntry savedFileEntry = await _fileEntryRepository.CreateFileEntry(new FileEntryBuilder().Build());

        await _fileEntryRepository.LockFile(savedFileEntry.Id);

        FileEntry? lockedFileEntry = await _fileEntryRepository.FindFileEntryById(savedFileEntry.Id);

        Assert.True(lockedFileEntry?.IsLocked);
    }

    // TODO: LockFile_ShouldThrowError_WhenFileAlreadyLocked
    // This test isn't necessary because the implementation first checks if the file is locked before attempting to lock or unlock it.
    // As long as our tests for this and the service logic are correct, this scenario should rarely or not even occur.


    [Fact]
    public async Task UnlockFile_ShouldReleaseFileLock_AfterUploadOrFailure() {
        FileEntry savedFileEntry = await _fileEntryRepository.CreateFileEntry(new FileEntryBuilder().Build());

        await _fileEntryRepository.LockFile(savedFileEntry.Id);

        FileEntry? lockedFileEntry = await _fileEntryRepository.FindFileEntryById(savedFileEntry.Id);
        Assert.True(lockedFileEntry?.IsLocked);

        await _fileEntryRepository.UnlockFile(savedFileEntry.Id);

        FileEntry? unlockedFileEntry = await _fileEntryRepository.FindFileEntryById(savedFileEntry.Id);
        Assert.False(unlockedFileEntry?.IsLocked);
    }

    [Fact]
    public async Task MarkUploadComplete_ShouldSetFileAsCompleted_AndStoreFileUrl() {
        string FILE_URL = Guid.NewGuid().ToString();
        FileEntry savedFileEntry = await _fileEntryRepository.CreateFileEntry(new FileEntryBuilder().Build());
        
        await _fileEntryRepository.MarkUploadComplete(savedFileEntry.Id, FILE_URL);

        FileEntry? retrievedFileEntry = await _fileEntryRepository.FindFileEntryById(savedFileEntry.Id);

        Assert.NotNull(retrievedFileEntry);
        Assert.Equal(FileEntryStatus.Completed, retrievedFileEntry.Status);
        Assert.Equal(FILE_URL, retrievedFileEntry.FileUrl);
    }

    [Fact]
    public async Task MarkUploadComplete_ShouldTheowException_WhenFileDoesNotExist() {        
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _fileEntryRepository.MarkUploadComplete(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
    }
}
