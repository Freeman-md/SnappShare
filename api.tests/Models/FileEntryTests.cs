using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using api.Enums;
using api.Models;
using api.tests.Builders;
using Xunit;

namespace api.tests.Models;

public class FileEntryTests
{
    [Fact]
    public void TestValidFileEntry_ShouldPassValidation()
    {
        FileEntry fileEntry = new FileEntryBuilder().Build();

        ValidationContext validationContext = new(fileEntry);
        List<ValidationResult> validationResults = new();

        bool isValid = Validator.TryValidateObject(fileEntry, validationContext, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(null, "txt", 5, "hash", "http://file.com/test", FileEntryStatus.Pending)]     // Missing file name
    [InlineData("", "txt", 5, "hash", "http://file.com/test", FileEntryStatus.Pending)]       // Empty file name
    [InlineData("Test", null, 5, "hash", "http://file.com/test", FileEntryStatus.Pending)]    // Missing extension
    [InlineData("Test", "", 5, "hash", "http://file.com/test", FileEntryStatus.Pending)]      // Empty extension
    [InlineData("Test", "txt", -1, "hash", "http://file.com/test", FileEntryStatus.Pending)]  // Negative chunk count
    [InlineData("Test", "txt", 5, null, "http://file.com/test", FileEntryStatus.Pending)]     // Missing hash
    [InlineData("Test", "txt", 5, "", "http://file.com/test", FileEntryStatus.Pending)]       // Empty hash
    [InlineData("Test", "txt", 5, "hash", null, FileEntryStatus.Pending)]                     // Missing URL
    [InlineData("Test", "txt", 5, "hash", "", FileEntryStatus.Pending)]                       // Empty URL
    [InlineData("Test", "txt", 5, "hash", "http://file.com/test", (FileEntryStatus)99)]       // Invalid status
    public void TestInvalidFileEntry_ShouldFailValidation(
        string? fileName,
        string? fileExtension,
        int totalChunks,
        string? fileHash,
        string? fileUrl,
        FileEntryStatus status)
    {
        var fileEntry = new FileEntryBuilder()
                            .WithFileName(fileName ?? "")
                            .WithFileExtension(fileExtension ?? "")
                            .WithTotalChunks(totalChunks)
                            .WithFileHash(fileHash ?? "")
                            .WithFileUrl(fileUrl ?? "")
                            .WithStatus(status)
                            .WithExpiration(DateTime.UtcNow.AddHours(1))
                            .WithFileSize(1024)
                            .WithUpdatedAt(DateTime.UtcNow)
                            .WithCreatedAt(DateTime.UtcNow)
                            .WithId(Guid.NewGuid().ToString("N")[..12])
                            .WithLockState(false)
                            .Build();

        ValidationContext context = new(fileEntry);
        var results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(fileEntry, context, results, true);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }
}
