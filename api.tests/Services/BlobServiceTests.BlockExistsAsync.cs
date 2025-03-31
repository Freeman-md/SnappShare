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

        var blockList = BlobsModelFactory.BlockList(
         uncommittedBlocks: new List<BlobBlock>
         {
        BlobsModelFactory.BlobBlock(name: blockId, size: 10)
         },
         committedBlocks: new List<BlobBlock>()
     );

        _mockBlockBlobClient.Setup(b => b.GetBlockListAsync(BlockListTypes.Uncommitted, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blockList, Mock.Of<Response>()));

        bool result = await _blobService.BlockExistsAsync("somefile.txt", ContainerName, blockId);

        Assert.True(result);
        _mockContainerClient.Verify(client => client.ExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlockExistsAsync_ShouldReturnFalse_IfBlockDoesNotExist()
    {
        string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("000001"));

        var blockList = BlobsModelFactory.BlockList(
           uncommittedBlocks: new List<BlobBlock>(),
           committedBlocks: new List<BlobBlock>()
       );
        _mockBlockBlobClient.Setup(b => b.GetBlockListAsync(BlockListTypes.Uncommitted, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blockList, Mock.Of<Response>()));

        bool result = await _blobService.BlockExistsAsync("somefile.txt", ContainerName, blockId);

        Assert.False(result);
        _mockContainerClient.Verify(client => client.ExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlockExistsAsync_ShouldThrowErrors_WhenExceptionOccurs()
    {
        string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("000001"));

        _mockBlockBlobClient.Setup(b => b.GetBlockListAsync(It.IsAny<BlockListTypes>(), null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Blob error"));

        await Assert.ThrowsAnyAsync<Exception>(async () => await _blobService.BlockExistsAsync("somefile.txt", ContainerName, blockId));
    }

    [Theory]
    [InlineData("", "container-name", "block-id")]
    [InlineData(" ", "container-name", "block-id")]
    [InlineData(null, "container-name", "block-id")]
    [InlineData("blob-name", "", "block-id")]
    [InlineData("blob-name", " ", "block-id")]
    [InlineData("blob-name", null, "block-id")]
    [InlineData("blob-name", "container-name", "")]
    [InlineData("blob-name", "container-name", " ")]
    [InlineData("blob-name", "container-name", null)]
    public async Task BlockExistsAsync_ShouldThrowArgumentException_OnInvalidInputs(string blobName, string containerName, string blockId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _blobService.BlockExistsAsync(blobName, containerName, blockId));
    }

    [Fact]
    public async Task BlockExistsAsync_ShouldThrowInvalidOperationException_IfContainerDoesNotExist()
    {
        string blobName = "blob-name.txt";
        string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes("block-id"));

        _mockContainerClient
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _blobService.BlockExistsAsync(blobName, ContainerName, blockId));
    }





}
