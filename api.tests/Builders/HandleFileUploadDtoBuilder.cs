using System.IO;
using api.DTOs;
using Microsoft.AspNetCore.Http;

namespace api.tests.Builders;

public class HandleFileUploadDtoBuilder
{
    private string _fileName = "example.pdf";
    private string _fileHash = "valid-file-hash";
    private long _fileSize = 2048;
    private int _chunkIndex = 0;
    private int _totalChunks = 2;
    private IFormFile _chunkFile;
    private string _chunkHash = "valid-chunk-hash";

    public HandleFileUploadDtoBuilder()
    {
        var content = new byte[1024];
        var stream = new MemoryStream(content);
        _chunkFile = new FormFile(stream, 0, content.Length, "file", "chunk1");
    }

    public HandleFileUploadDtoBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public HandleFileUploadDtoBuilder WithFileHash(string fileHash)
    {
        _fileHash = fileHash;
        return this;
    }

    public HandleFileUploadDtoBuilder WithFileSize(long fileSize)
    {
        _fileSize = fileSize;
        return this;
    }

    public HandleFileUploadDtoBuilder WithChunkIndex(int index)
    {
        _chunkIndex = index;
        return this;
    }

    public HandleFileUploadDtoBuilder WithTotalChunks(int totalChunks)
    {
        _totalChunks = totalChunks;
        return this;
    }

    public HandleFileUploadDtoBuilder WithChunkFile(IFormFile chunkFile)
    {
        _chunkFile = chunkFile;
        return this;
    }

    public HandleFileUploadDtoBuilder WithChunkHash(string chunkHash)
    {
        _chunkHash = chunkHash;
        return this;
    }

    public HandleFileUploadDto Build()
    {
        return new HandleFileUploadDto
        {
            FileName = _fileName,
            FileHash = _fileHash,
            FileSize = _fileSize,
            ChunkIndex = _chunkIndex,
            TotalChunks = _totalChunks,
            ChunkFile = _chunkFile,
            ChunkHash = _chunkHash
        };
    }
}
