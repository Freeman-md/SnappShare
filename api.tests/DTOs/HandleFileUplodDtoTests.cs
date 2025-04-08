using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using api.DTOs;
using api.Enums;
using api.tests.Builders;
using Xunit;

namespace api.tests.DTOs;

public class HandleFileUploadDtoTests
{
    [Fact]
    public void HandleFileUploadDto_ShouldPassValidation_WhenAllFieldsAreValid()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("", "hash", 123, 0, 5, "chunkHash")] // Empty file name
    [InlineData("file", "", 123, 0, 5, "chunkHash")] // Empty file hash
    [InlineData("file", "hash", 0, 0, 5, "chunkHash")] // Zero file size
    [InlineData("file", "hash", 123, -1, 5, "chunkHash")] // Negative chunk index
    [InlineData("file", "hash", 123, 0, 0, "chunkHash")] // Zero total chunks
    [InlineData("file", "hash", 123, 0, 5, "")] // Empty chunk hash
    public void HandleFileUploadDto_ShouldFailValidation_WhenRequiredFieldsAreInvalid(
        string fileName,
        string fileHash,
        long fileSize,
        int chunkIndex,
        int totalChunks,
        string chunkHash)
    {
        var dto = new HandleFileUploadDtoBuilder()
            .WithFileName(fileName)
            .WithFileHash(fileHash)
            .WithFileSize(fileSize)
            .WithChunkIndex(chunkIndex)
            .WithTotalChunks(totalChunks)
            .WithChunkHash(chunkHash)
            .Build();

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void HandleFileUploadDto_ShouldFailValidation_WhenChunkFileIsNull()
    {
        var dto = new HandleFileUploadDtoBuilder()
                    .WithChunkFile(null!)
                    .Build();

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("required"));
    }
}
