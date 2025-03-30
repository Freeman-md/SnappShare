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
    public async Task CommitBlockListAsync_ShouldCommitBlockListSuccessfully()
    {
        FileEntry fileEntry = new FileEntryBuilder()
                                    .WithUploadedChunks(3)
                                    .Build();

        IEnumerable<string> blockIds = fileEntry.Chunks.Select(chunk => chunk.BlockId).ToList();

        _mockBlockBlobClient.Setup(client => client.CommitBlockListAsync(blockIds, null, null, null, null, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());

        await _blobService.CommitBlockListAsync(fileEntry.FileName, ContainerName, blockIds);

        _mockContainerClient.Verify(client => client.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockBlockBlobClient.Verify(client => client.CommitBlockListAsync(It.IsAny<IEnumerable<string>>(), null, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task CommitBlockListAsync_ShouldThrowException_IfBlobClientErrors()
    {
        IEnumerable<string> blockIds = new List<string> { "block-001", "block-002" };

        _mockBlockBlobClient.Setup(c => c.CommitBlockListAsync(blockIds, null, null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        await Assert.ThrowsAsync<Exception>(() =>
            _blobService.CommitBlockListAsync("blob.txt", ContainerName, blockIds)
        );
    }

}
