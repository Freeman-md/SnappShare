using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using api.Models;
using api.tests.Builders;
using Xunit;

namespace api.tests.Models;

public class ChunkTests
{
    [Fact]
    public void TestValidChunk_ShouldPassValidation()
    {
        // Arrange
        Chunk chunk = new ChunkBuilder().Build();
        ValidationContext context = new(chunk);
        List<ValidationResult> results = new();

        // Act
        bool isValid = Validator.TryValidateObject(chunk, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null, 0, 1024, "validhash", "http://snappshare.com/chunk/0")] // Missing FileId
    [InlineData("", 0, 1024, "validhash", "http://snappshare.com/chunk/0")]    // Empty FileId
    [InlineData("file123", -1, 1024, "validhash", "http://snappshare.com/chunk/0")] // Negative index
    [InlineData("file123", 0, 0, "validhash", "http://snappshare.com/chunk/0")]     // Zero chunk size
    [InlineData("file123", 0, 1024, null, "http://snappshare.com/chunk/0")]         // Null hash
    [InlineData("file123", 0, 1024, "", "http://snappshare.com/chunk/0")]           // Empty hash
    [InlineData("file123", 0, 1024, "validhash", null)]                             // Null URL
    [InlineData("file123", 0, 1024, "validhash", "")]                               // Empty URL
    public void TestInvalidChunk_ShouldFailValidation(string? fileId, int chunkIndex, long chunkSize, string? chunkHash, string? chunkUrl)
    {
        // Arrange
        var chunk = new ChunkBuilder()
                        .WithFileId(fileId ?? "")
                        .WithChunkIndex(chunkIndex)
                        .WithChunkSize(chunkSize)
                        .WithChunkHash(chunkHash ?? "")
                        .WithChunkUrl(chunkUrl ?? "")
                        .Build();

        ValidationContext context = new(chunk);
        List<ValidationResult> results = new();

        // Act
        bool isValid = Validator.TryValidateObject(chunk, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
    }
}
