using api.Interfaces.Services;

namespace api.Services;

using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class BlobService : IBlobService {

    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(BlobServiceClient blobServiceClient) {
        _blobServiceClient = blobServiceClient;
    }

	public Task<string> UploadFileAsync(IFormFile file, string containerName) {
        throw new NotImplementedException();
	}
}