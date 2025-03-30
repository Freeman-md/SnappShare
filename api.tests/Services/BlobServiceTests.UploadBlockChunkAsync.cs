using System;
using api.Models;
using api.tests.Builders;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Moq;

namespace api.tests.Services;

public partial class BlobServiceTests
{
    [Fact]
    public async Task UploadChunkBlockAsync_ShouldUploadChunkBlockSuccessfully()
    {
        FileEntry fileEntry = new FileEntryBuilder().Build();
        Chunk chunk = new ChunkBuilder().Build();
        var fileStream = new MemoryStream(new byte[1024]);
        IFormFile chunkFile = new FormFile(fileStream, 0, fileStream.Length, chunk.ChunkHash, fileEntry.FileName);

        _mockBlockBlobClient.Setup(client => client.StageBlockAsync(It.IsAny<string>(), It.IsAny<Stream>(), null, default(CancellationToken)))
                            .ReturnsAsync(Mock.Of<Azure.Response<BlockInfo>>());

        await _blobService.UploadChunkBlockAsync(chunkFile, fileEntry.FileName, ContainerName, chunk.BlockId);

        _mockContainerClient.Verify(client => client.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockBlockBlobClient.Verify(client => client.StageBlockAsync(It.IsAny<string>(), It.IsAny<Stream>(), null, default), Times.Once);
    }

    [Fact]
    public async Task UploadChunkBlockAsync_ShouldThrowException_WhenContainerNotFound()
    {

        _mockContainerClient.Setup(client => client.CreateIfNotExistsAsync(PublicAccessType.None, null, null, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Container not found"));

        await Assert.ThrowsAnyAsync<Exception>(
            async () => await _blobService.UploadChunkBlockAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            ));

        _mockContainerClient.Verify(client => client.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()), Times.Never);
        _mockBlockBlobClient.Verify(client => client.StageBlockAsync(It.IsAny<string>(), It.IsAny<Stream>(), null, default), Times.Never);
    }

    [Fact]
    public async Task UploadChunkBlockAsync_ShouldThrowException_WhenBlobNotFound()
    {

        _mockBlockBlobClient.Setup(client => client.StageBlockAsync(It.IsAny<string>(), It.IsAny<Stream>(), null, default))
        .ThrowsAsync(new Exception("Block blob upload failed"));

        await Assert.ThrowsAnyAsync<Exception>(
            async () => await _blobService.UploadChunkBlockAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            ));

        _mockContainerClient.Verify(client => client.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()), Times.Never);
        _mockBlockBlobClient.Verify(client => client.StageBlockAsync(It.IsAny<string>(), It.IsAny<Stream>(), null, default), Times.Never);
    }

    [Theory]
    [InlineData("", "container-name", "block-id")]
    [InlineData("  ", "container-name", "block-id")]
    [InlineData(null, "container-name", "block-id")]
    [InlineData("blob-name", "", "block-id")]
    [InlineData("blob-name", " ", "block-id")]
    [InlineData("blob-name", null, "block-id")]
    [InlineData("blob-name", "container-name", "")]
    [InlineData("blob-name", "container-name", " ")]
    [InlineData("blob-name", "container-name", null)]
    public async Task UploadChunkBlockAsync_ShouldThrowArgumentException_OnInvalidInputs(string blobName, string containerName, string blockId) {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _blobService.UploadChunkBlockAsync(It.IsAny<IFormFile>(), blobName, containerName, blockId));
    }

    [Fact]
    public async Task UploadChunkBlockAsync_ShouldThrowArgumentException_OnInvalidFile() {
        FileEntry fileEntry = new FileEntryBuilder().Build();
        Chunk chunk = new ChunkBuilder().Build();
        using var stream = new MemoryStream();
        IFormFile chunkFile = new FormFile(stream, 0, stream.Length, chunk.ChunkHash, fileEntry.FileName);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _blobService.UploadChunkBlockAsync(chunkFile, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
    }
}
