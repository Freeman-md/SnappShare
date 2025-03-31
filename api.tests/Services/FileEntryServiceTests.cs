using System;
using api.Configs;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Services;
using api.Tests.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace api.tests.Services;

public partial class FileEntryServiceTests
{
    private Mock<ILogger<FileEntryService>> _logger;
    private readonly Mock<IBlobService> _blobService;

    private readonly Mock<IFileEntryRepository> _fileEntryRepository;
    private readonly Mock<IChunkRepository> _chunkRepository;
    private readonly IOptions<StorageOptions> _storageOptions;
    private readonly Mock<FileEntryService> _fileEntryServiceMock;
    private readonly FileEntryService _fileEntryService;

    public FileEntryServiceTests()
    {
        _logger = new Mock<ILogger<FileEntryService>>();
        _blobService = new Mock<IBlobService>();
        _fileEntryRepository = new Mock<IFileEntryRepository>();
        _chunkRepository = new Mock<IChunkRepository>();
        var storage = new StorageOptions { AccountName = "testAccount", ContainerName = "test-container" };
        _storageOptions = Options.Create(storage);

        _fileEntryServiceMock = new Mock<FileEntryService>(
            _logger.Object,
            _blobService.Object,
            _fileEntryRepository.Object,
            _chunkRepository.Object,
            _storageOptions
        )
        { CallBase = true };

        _fileEntryService = _fileEntryServiceMock.Object;
    }
}
