using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using api.Services;
using Azure.Storage.Blobs.Specialized;
using api.tests.Classes;

namespace api.tests.Services
{
    public partial class BlobServiceTests
    {
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
        private readonly Mock<BlobContainerClient> _mockContainerClient;
        private readonly Mock<BlobClient> _mockBlobClient;
        private readonly Mock<BlockBlobClient> _mockBlockBlobClient;
        private readonly BlobService _blobService;
        private const string ContainerName = "test-container";

        public BlobServiceTests()
        {
            _mockBlobServiceClient = new Mock<BlobServiceClient>();
            _mockContainerClient = new Mock<BlobContainerClient>();
            _mockBlobClient = new Mock<BlobClient>();
            _mockBlockBlobClient = new Mock<BlockBlobClient>();

            _mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(ContainerName))
                .Returns(_mockContainerClient.Object);

            _mockContainerClient
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_mockBlobClient.Object);

            _mockContainerClient
                .Setup(x => x.CreateIfNotExistsAsync(PublicAccessType.None, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Azure.Response<BlobContainerInfo>>());

            _mockContainerClient
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));


            _blobService = new TestableBlobService(_mockBlobServiceClient.Object, _mockBlockBlobClient.Object);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldUploadFileAndReturnUrl()
        {
            var fileName = "testfile.txt";
            var expiryTime = DateTimeOffset.UtcNow.AddMinutes(30);
            var fileStream = new MemoryStream(new byte[1024]);
            var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", fileName);

            _mockBlobClient
                .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());

            _mockBlobClient
                .Setup(x => x.SetMetadataAsync(It.IsAny<IDictionary<string, string>>(), default, default))
                .ReturnsAsync(Mock.Of<Azure.Response<BlobInfo>>());

            _mockBlobClient.Setup(x => x.Uri).Returns(new Uri("https://example.com/blob"));

            var (fileUrl, returnedFileName) = await _blobService.UploadFileAsync(formFile, ContainerName, expiryTime);

            Assert.Equal("https://example.com/blob", fileUrl);
            Assert.StartsWith(Path.GetFileNameWithoutExtension(fileName) + "-", returnedFileName);
            Assert.EndsWith(Path.GetExtension(fileName), returnedFileName);
        }

        [Fact]
        public async Task GenerateSasTokenAsync_ShouldThrowException_WhenBlobDoesNotExist()
        {
            var blobName = "nonexistent.txt";
            var expiryTime = DateTimeOffset.UtcNow.AddMinutes(30);

            _mockBlobClient.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, null!));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _blobService.GenerateSasTokenAsync(blobName, ContainerName, expiryTime));
        }
    }
}
