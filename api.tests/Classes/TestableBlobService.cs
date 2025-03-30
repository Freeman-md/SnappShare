using System;
using api.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace api.tests.Classes;

public class TestableBlobService : BlobService
{
    private readonly BlockBlobClient _blockBlobClient;

    public TestableBlobService(BlobServiceClient blobServiceClient, BlockBlobClient blockBlobClient) : base(blobServiceClient)
    {
        _blockBlobClient = blockBlobClient;
    }

     protected override BlockBlobClient GetBlockBlobClient(BlobContainerClient container, string blobName)
    {
        return _blockBlobClient;
    }
}
