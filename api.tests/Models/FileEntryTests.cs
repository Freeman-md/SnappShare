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
    [InlineData(null, "txt", 5, "hash", "http://file.com/test", FileEntryStatus.Pending, ExpiryDuration.OneDay)]     // Missing file name
    [InlineData("", "txt", 5, "hash", "http://file.com/test", FileEntryStatus.Pending, ExpiryDuration.OneDay)]       // Empty file name
    [InlineData("Test", "txt", -1, "hash", "http://file.com/test", FileEntryStatus.Pending, ExpiryDuration.OneDay)]  // Negative chunk count
    [InlineData("Test", "txt", 5, null, "http://file.com/test", FileEntryStatus.Pending, ExpiryDuration.OneDay)]     // Missing hash
    [InlineData("Test", "txt", 5, "", "http://file.com/test", FileEntryStatus.Pending, ExpiryDuration.OneDay)]       // Empty hash
    [InlineData("Test", "txt", 5, "hash", "http://file.com/test", (FileEntryStatus)99, ExpiryDuration.OneDay)]       // Invalid status
    [InlineData("Test", "txt", 5, "hash", "http://file.com/test", FileEntryStatus.Pending, (ExpiryDuration)(-1))]       // Invalid expiresIn (Expiry Duration)
    public void TestInvalidFileEntry_ShouldFailValidation(
        string? fileName,
        string? fileExtension,
        int totalChunks,
        string? fileHash,
        string? fileUrl,
        FileEntryStatus status,
        ExpiryDuration expiresIn
        )
    {
        var fileEntry = new FileEntryBuilder()
                            .WithFileName(fileName ?? "")
                            .WithFileExtension(fileExtension ?? "")
                            .WithTotalChunks(totalChunks)
                            .WithFileHash(fileHash ?? "")
                            .WithFileUrl(fileUrl ?? "")
                            .WithStatus(status)
                            .WithExpiration(ExpiryDuration.FiveMinutes)
                            .WithFileSize(1024)
                            .WithUpdatedAt(DateTime.UtcNow)
                            .WithCreatedAt(DateTime.UtcNow)
                            .WithId(Guid.NewGuid().ToString("N")[..12])
                            .WithLockState(false)
                            .WithExpiration(expiresIn)
                            .Build();

        ValidationContext context = new(fileEntry);
        var results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(fileEntry, context, results, true);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }
}
