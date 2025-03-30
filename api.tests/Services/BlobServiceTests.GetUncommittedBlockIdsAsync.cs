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
    public async Task GetUncommittedBlockIdsAsync_ShouldReturnAllUncommittedBlockIds()
    {
        string blobName = "sample-file.txt";
        string blockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes("000001"));
        string blockId2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("000002"));

        var mockBlobClient = new Mock<BlockBlobClient>();

        var blockList = BlobsModelFactory.BlockList(
            uncommittedBlocks: new List<BlobBlock>
            {
            BlobsModelFactory.BlobBlock(name: blockId1, size: 10),
            BlobsModelFactory.BlobBlock(name: blockId2, size: 15)
            },
            committedBlocks: new List<BlobBlock>()
        );

        mockBlobClient.Setup(b => b.GetBlockListAsync(BlockListTypes.Uncommitted, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blockList, Mock.Of<Response>()));

        var result = await _blobService.GetUncommittedBlockIdsAsync(blobName, ContainerName);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(blockId1, result);
        Assert.Contains(blockId2, result);
    }

    [Fact]
    public async Task GetUncommittedBlockIdsAsync_ShouldReturnEmptyList_WhenNoBlocksExist()
    {
        string blobName = "sample-file.txt";

        var mockBlobClient = new Mock<BlockBlobClient>();

        var blockList = BlobsModelFactory.BlockList(
            uncommittedBlocks: new List<BlobBlock>(),
            committedBlocks: new List<BlobBlock>()
        );

        _mockContainerClient.Setup(c => c.GetBlockBlobClient(blobName))
            .Returns(mockBlobClient.Object);

        mockBlobClient.Setup(b => b.GetBlockListAsync(BlockListTypes.Uncommitted, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blockList, Mock.Of<Response>()));

        var result = await _blobService.GetUncommittedBlockIdsAsync(blobName, ContainerName);

        Assert.NotNull(result);
        Assert.Empty(result);
    }


}
