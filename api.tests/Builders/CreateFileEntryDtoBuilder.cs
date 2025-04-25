using api.DTOs;
using api.Enums;
using api.Models;

namespace api.tests.Builders;

public class CreateFileEntryDtoBuilder
{
    private string _fileName = "example.pdf";
    private string _fileHash = "valid-file-hash";
    private long _fileSize = 2048;
    private int _totalChunks = 2;
    private ExpiryDuration _expiresIn = ExpiryDuration.OneDay;

    public CreateFileEntryDtoBuilder() {
        
    }

    public CreateFileEntryDtoBuilder(FileEntry fileEntry)
    {
        WithFileName(fileEntry.FileName);
        WithFileHash(fileEntry.FileName);
        WithFileSize(fileEntry.FileSize);
        WithTotalChunks(fileEntry.TotalChunks);
        WithExpiryDuration(fileEntry.ExpiresIn);
    }

    public CreateFileEntryDtoBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public CreateFileEntryDtoBuilder WithFileHash(string fileHash)
    {
        _fileHash = fileHash;
        return this;
    }

    public CreateFileEntryDtoBuilder WithFileSize(long fileSize)
    {
        _fileSize = fileSize;
        return this;
    }

    public CreateFileEntryDtoBuilder WithTotalChunks(int totalChunks)
    {
        _totalChunks = totalChunks;
        return this;
    }

    public CreateFileEntryDtoBuilder WithExpiryDuration(ExpiryDuration expiresIn)
    {
        _expiresIn = expiresIn;
        return this;
    }

    public CreateFileEntryDto Build()
    {
        return new CreateFileEntryDto
        {
            FileName = _fileName,
            FileHash = _fileHash,
            FileSize = _fileSize,
            TotalChunks = _totalChunks,
            ExpiresIn = _expiresIn
        };
    }
}