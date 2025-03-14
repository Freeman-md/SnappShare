using System;
using System.IO;
using System.Threading.Tasks;
using api.Configs;
using api.Enums;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace api.tests.Services
{
    public class FileServiceTests
    {
        private readonly Mock<IBlobService> _mockBlobService;
        private readonly Mock<IFileRepository> _mockFileRepository;
        private readonly Mock<ILogger<FileService>> _mockLogger;
        private readonly IOptions<StorageOptions> _storageOptions;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _mockBlobService = new Mock<IBlobService>();
            _mockFileRepository = new Mock<IFileRepository>();
            _mockLogger = new Mock<ILogger<FileService>>();
            var storage = new StorageOptions { AccountName = "testAccount", ContainerName = "test-container" };
            _storageOptions = Options.Create(storage);

            _fileService = new FileService(
                _mockLogger.Object,
                _mockBlobService.Object,
                _mockFileRepository.Object,
                _storageOptions
            );
        }

        [Fact]
        public async Task UploadFile_ShouldReturnAddedFile()
        {
            var expiryDuration = ExpiryDuration.FiveMinutes;
            var dummyFileName = "dummyFile.txt";
            var uniqueFileName = "dummyFile-unique.txt";
            var sasUrl = "https://example.com/dummyFile-unique.txt?sasToken";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Dummy content"));
            IFormFile formFile = new FormFile(stream, 0, stream.Length, "file", dummyFileName);

            var inputFileUpload = new FileUpload
            {
                File = formFile,
                ExpiryDuration = expiryDuration,
            };

            _mockBlobService
                .Setup(x => x.UploadFileAsync(formFile, _storageOptions.Value.ContainerName, It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(("someUrl", uniqueFileName));

            _mockBlobService
                .Setup(x => x.GenerateSasTokenAsync(uniqueFileName, _storageOptions.Value.ContainerName, It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(sasUrl);

            _mockFileRepository
                .Setup(x => x.AddFile(It.IsAny<FileUpload>()))
                .ReturnsAsync((FileUpload file) => file);

            var result = await _fileService.UploadFile(inputFileUpload);

            Assert.NotNull(result);
            Assert.Equal(sasUrl, result.OriginalUrl);
            Assert.Equal(expiryDuration, result.ExpiryDuration);
            Assert.False(string.IsNullOrEmpty(result.Id));
        }

        [Fact]
        public async Task GetFile_ShouldReturnFileAndCorrectExpiry_WhenFileExists()
        {
            var file = new FileUpload
            {
                Id = "ABC123",
                OriginalUrl = "https://example.com/blob?sasToken",
                CreatedAt = DateTime.UtcNow.AddMinutes(-3),
                ExpiryDuration = ExpiryDuration.FiveMinutes,
            };
            _mockFileRepository.Setup(x => x.GetFile("ABC123")).ReturnsAsync(file);

            var (retrievedFile, expiryTime, isExpired) = await _fileService.GetFile("ABC123");

            Assert.Equal(file, retrievedFile);
            Assert.Equal(file.CreatedAt.AddMinutes((double)file.ExpiryDuration), expiryTime);
            Assert.False(isExpired);
        }

        [Fact]
        public async Task GetFile_ShouldThrowKeyNotFoundException_WhenFileDoesNotExist()
        {
            _mockFileRepository.Setup(x => x.GetFile("NON_EXISTENT")).ReturnsAsync((FileUpload)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _fileService.GetFile("NON_EXISTENT"));
        }
    }
}
