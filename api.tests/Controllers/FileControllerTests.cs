using System;
using System.Threading.Tasks;
using api.Controllers;
using api.Enums;
using api.Interfaces.Services;
using api.Models;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Azure;
using api.Data;
using Microsoft.EntityFrameworkCore;

namespace api.tests.Controllers
{
    public class FileControllerTests
    {
        private readonly Mock<ILogger<FileController>> _mockLogger;
        private readonly Mock<IBlobService> _mockBlobService;
        private readonly Mock<IFileService> _mockFileService;
        private readonly SnappshareContext _dbContext;
        private readonly FileController _controller;

        public FileControllerTests()
        {
            _mockLogger = new Mock<ILogger<FileController>>();
            _mockBlobService = new Mock<IBlobService>();
            _mockFileService = new Mock<IFileService>();

            DbContextOptions<SnappshareContext> options = new DbContextOptionsBuilder<SnappshareContext>()
                                               .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                                               .Options;

            _dbContext = new SnappshareContext(options);

            _controller = new FileController(
                _mockLogger.Object,
                _mockBlobService.Object,
                _mockFileService.Object,
                _dbContext
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("example.com");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task UploadFile_ShouldReturnSuccess_WhenFileIsUploaded()
        {
            var fileUpload = new FileUpload
            {
                File = new FormFile(Stream.Null, 0, 1024, "file", "testfile.txt"),
                ExpiryDuration = ExpiryDuration.FiveMinutes
            };

            var uploadedFile = new FileUpload
            {
                Id = "123ABC",
                OriginalUrl = "https://example.com/file/123ABC",
                CreatedAt = DateTime.UtcNow,
                ExpiryDuration = ExpiryDuration.FiveMinutes
            };

            _mockFileService
                .Setup(x => x.UploadFile(It.IsAny<FileUpload>()))
                .ReturnsAsync(uploadedFile);

            var result = await _controller.UploadFile(fileUpload);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SuccessApiResponse<object>>(okResult.Value);

            Assert.NotNull(response.Data);
            Assert.Contains("Expires in", response.Data.ToString());
            Assert.Contains("FileAccessUrl", response.Data.ToString());
        }

        [Fact]
        public async Task UploadFile_ShouldReturnBadRequest_WhenAzureStorageFails()
        {
            var fileUpload = new FileUpload
            {
                File = new FormFile(Stream.Null, 0, 1024, "file", "testfile.txt"),
                ExpiryDuration = ExpiryDuration.FiveMinutes
            };

            _mockFileService
                .Setup(x => x.UploadFile(It.IsAny<FileUpload>()))
                .ThrowsAsync(new RequestFailedException("Blob Storage Error"));

            var result = await _controller.UploadFile(fileUpload);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ErrorApiResponse<object>>(badRequestResult.Value);

            Assert.Equal(400, response.StatusCode);
            Assert.Contains("Blob Storage Error", response.Message);
        }

        [Fact]
        public async Task GetFileDetails_ShouldReturnFile_WhenFileExists()
        {
            var file = new FileUpload
            {
                Id = "123ABC",
                OriginalUrl = "https://example.com/file/123ABC",
                CreatedAt = DateTime.UtcNow,
                ExpiryDuration = ExpiryDuration.FiveMinutes
            };
            DateTimeOffset expiryTime = file.CreatedAt.AddMinutes((double)file.ExpiryDuration);
            bool isExpired = false;

            _mockFileService
                .Setup(x => x.GetFile("123ABC"))
                .ReturnsAsync((file, expiryTime, isExpired));

            var result = await _controller.GetFileDetails("123ABC");
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SuccessApiResponse<object>>(okResult.Value);

            Assert.NotNull(response.Data);
            Assert.Contains("ExpiresAt", response.Data.ToString());
            Assert.Contains("OriginalUrl", response.Data.ToString());
        }

        [Fact]
        public async Task GetFileDetails_ShouldReturnNotFound_WhenFileDoesNotExist()
        {
            _mockFileService
                .Setup(x => x.GetFile("NON_EXISTENT"))
                .ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.GetFileDetails("NON_EXISTENT");
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);

            Assert.Equal(404, response.StatusCode);
        }
    }
}