using System;
using System.Text;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Moq;

namespace api.tests.Services;

public partial class BlobServiceTests
{
    [Fact]
    public async Task BlockExistsAsync_ShouldReturnTrue_IfBlockExists()
    {
        string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("000001"));
        var mockBlobClient = new Mock<BlockBlobClient>();

        var blockList = BlobsModelFactory.BlockList(
         uncommittedBlocks: new List<BlobBlock>
         {
        BlobsModelFactory.BlobBlock(name: blockId, size: 10)
         },
         committedBlocks: new List<BlobBlock>()
     );

        mockBlobClient.Setup(b => b.GetBlockListAsync(BlockListTypes.Uncommitted, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blockList, Mock.Of<Response>()));

        bool result = await _blobService.BlockExistsAsync("somefile.txt", ContainerName, blockId);

        Assert.True(result);
    }

    [Fact]
    public async Task BlockExistsAsync_ShouldReturnFalse_IfBlockDoesNotExist()
    {
        string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("000001"));
        var mockBlobClient = new Mock<BlockBlobClient>();

        var blockList = BlobsModelFactory.BlockList(
           uncommittedBlocks: new List<BlobBlock>(),
           committedBlocks: new List<BlobBlock>()
       );
        mockBlobClient.Setup(b => b.GetBlockListAsync(BlockListTypes.Uncommitted, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blockList, Mock.Of<Response>()));

        bool result = await _blobService.BlockExistsAsync("somefile.txt", ContainerName, blockId);

        Assert.False(result);
    }

    [Fact]
    public async Task BlockExistsAsync_ShouldThrowErrors_WhenExceptionOccurs()
    {
        string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("000001"));
        var mockBlobClient = new Mock<BlockBlobClient>();

        _mockContainerClient.Setup(c => c.GetBlockBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        mockBlobClient.Setup(b => b.GetBlockListAsync(It.IsAny<BlockListTypes>(), null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Blob error"));

        await Assert.ThrowsAnyAsync<Exception>(async () => await _blobService.BlockExistsAsync("somefile.txt", ContainerName, blockId));
    }



}
