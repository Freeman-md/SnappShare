using System;
using System.Threading.Tasks;
using api.Data;
using api.Enums;
using api.Interfaces.Repositories;
using api.Models;
using api.Repositories;
using api.tests.Builders;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.tests.Repositories;

public class FileRepositoryTests
{
    private readonly SnappshareContext _dbContext;
    private readonly IFileRepository _fileRepository;

    public FileRepositoryTests()
    {
        DbContextOptions<SnappshareContext> options = new DbContextOptionsBuilder<SnappshareContext>()
                                                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test run
                                                .Options;

        _dbContext = new SnappshareContext(options);
        _fileRepository = new FileRepository(_dbContext);
    }


    [Fact]
    public async Task AddFile_ShouldSaveAndReturnFile()
    {
        var fileUpload = new FileUploadBuilder()
                            .WithId("123ABC")
                            .WithOriginalUrl("https://blob.storage/testfile.png")
                            .WithExpiryDuration(ExpiryDuration.FiveMinutes)
                            .Build();

        var result = await _fileRepository.AddFile(fileUpload);

        Assert.NotNull(result);
        Assert.Equal(fileUpload.Id, result.Id);
        Assert.Equal(fileUpload.OriginalUrl, result.OriginalUrl);
    }

    [Fact]
    public async Task GetFile_ShouldReturnFile_WhenExists()
    {
        var fileUpload = new FileUploadBuilder()
                            .WithId("FILE123")
                            .WithOriginalUrl("https://blob.storage/testfile.pdf")
                            .WithExpiryDuration(ExpiryDuration.OneHour)
                            .Build();

        await _fileRepository.AddFile(fileUpload);

        var result = await _fileRepository.GetFile("FILE123");

        Assert.NotNull(result);
        Assert.Equal("FILE123", result!.Id);
    }

    [Fact]
    public async Task GetFile_ShouldReturnNull_WhenFileDoesNotExist()
    {
        var result = await _fileRepository.GetFile("NON_EXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddFile_ShouldThrowException_WhenDbFails()
    {
        var faultyDbContext = new SnappshareContext(new DbContextOptionsBuilder<SnappshareContext>()
                                                .UseInMemoryDatabase(databaseName: "FaultyDB")
                                                .Options);
        faultyDbContext.Dispose();

        var repository = new FileRepository(faultyDbContext);

        var fileUpload = new FileUploadBuilder()
                            .WithId("ERROR_TEST")
                            .WithOriginalUrl("https://blob.storage/failure.txt")
                            .WithExpiryDuration(ExpiryDuration.OneMinute)
                            .Build();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await repository.AddFile(fileUpload));
    }
}
