using System;
using System.ComponentModel.DataAnnotations;
using api.Enums;
using api.Models;
using api.tests.Builders;
using Microsoft.AspNetCore.Http;

namespace api.tests.Models;

public class FileUploadTests
{
    [Fact]
    public void TestValidFileUpload_ShouldPassValidation()
    {
        FileUpload fileUpload = new FileUploadBuilder()
                                        .WithFile(new FormFile(Stream.Null, 0, 1024, "file", "file.txt"))
                                        .WithExpiryDuration(ExpiryDuration.FiveMinutes)
                                        .Build();
        ValidationContext validationContext = new ValidationContext(fileUpload);
        List<ValidationResult> validationResults = new();

        bool isValid = Validator.TryValidateObject(fileUpload, validationContext, validationResults);

        Assert.True(isValid);
        Assert.NotNull(validationResults);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(1024 * 1024 * 10, "validFile.png", "This note is a valid one with more than 20 characters", ExpiryDuration.FiveMinutes)]
    [InlineData(1024 * 1024 * 3, null, null, ExpiryDuration.FiveMinutes)]
    [InlineData(1024 * 1024 * 3, null, "Valid Note", ExpiryDuration.FiveMinutes)]
    [InlineData(1024 * 1024 * 3, "", "Valid Note", ExpiryDuration.FiveMinutes)]
    [InlineData(1024 * 1024 * 3, "invalidFileExtension.xlsx", "This note is a valid one with more than 20 characters", ExpiryDuration.FiveMinutes)]
    [InlineData(1024 * 1024 * 3, "validFile.png", "Short Note", ExpiryDuration.FiveMinutes)]
    [InlineData(1024 * 1024 * 3, "validFile.png", "******************************************************************************************************************************************************************************************************************************************************", ExpiryDuration.FiveMinutes)]
    [InlineData(1024 * 1024 * 3, "validFile.png", "This note is a valid one with more than 20 characters", (ExpiryDuration)(-1))]
    public void TestMissingProperties_ShouldFailValidation(long fileSize, string? fileName, string? note, ExpiryDuration? expiryDuration)
    {
        FileUpload fileUpload = new FileUploadBuilder()
            .WithFile(fileName != null ? new FormFile(Stream.Null, 0, fileSize, "file", fileName) : null!)
            .WithNote(note!)
            .WithExpiryDuration(expiryDuration ?? default)
            .Build();

        ValidationContext validationContext = new ValidationContext(fileUpload);
        List<ValidationResult> validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(fileUpload, validationContext, validationResults, true);

        Assert.False(isValid);
        Assert.NotNull(validationResults);
        Assert.NotEmpty(validationResults);
    }
}
