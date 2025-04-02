using System;
using api.Controllers;
using api.Models;
using api.Models.DTOs;
using api.Services;
using api.tests.Builders;
using api.Tests.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace api.tests.Controllers;

public class FileEntryControllerTests
{
    private readonly Mock<ILogger<FileEntryController>> _logger;
    private readonly Mock<IFileEntryService> _fileEntryService;
    private readonly FileEntryController _fileEntryController;

    public FileEntryControllerTests()
    {
        _logger = new Mock<ILogger<FileEntryController>>();
        _fileEntryService = new Mock<IFileEntryService>();

        _fileEntryController = new FileEntryController(_logger.Object, _fileEntryService.Object);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnOk_WhenUploadIsSuccessful()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();
        var expectedResponse = new UploadResponseDto { Status = UploadResponseDtoStatus.SUCCESS };

        _fileEntryService.Setup(s => s.HandleFileUpload(
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ReturnsAsync(expectedResponse);

        var result = await _fileEntryController.HandleFileUpload(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(UploadResponseDtoStatus.SUCCESS, actualResponse.Status);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnCompleteStatus_WhenUploadFinalizes()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();
        var expectedResponse = new UploadResponseDto { Status = UploadResponseDtoStatus.COMPLETE };

        _fileEntryService.Setup(s => s.HandleFileUpload(
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ReturnsAsync(expectedResponse);

        var result = await _fileEntryController.HandleFileUpload(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(UploadResponseDtoStatus.COMPLETE, actualResponse.Status);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnSkippedStatus_WhenChunkWasAlreadyUploaded()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();
        var expectedResponse = new UploadResponseDto { Status = UploadResponseDtoStatus.SKIPPED };

        _fileEntryService.Setup(s => s.HandleFileUpload(
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ReturnsAsync(expectedResponse);

        var result = await _fileEntryController.HandleFileUpload(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(UploadResponseDtoStatus.SKIPPED, actualResponse.Status);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnBadRequest_WhenServiceThrowsArgumentException()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();

        _fileEntryService.Setup(s => s.HandleFileUpload(
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ThrowsAsync(new ArgumentException("Invalid parameters"));

        var result = await _fileEntryController.HandleFileUpload(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorApiResponse<object>>(badRequest.Value);
        Assert.Contains("Invalid", error.ErrorMessage);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnBadRequest_WhenUnhandledExceptionOccurs()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();

        _fileEntryService.Setup(s => s.HandleFileUpload(
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _fileEntryController.HandleFileUpload(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorApiResponse<object>>(badRequest.Value);
        Assert.Equal("Unexpected", error.ErrorMessage);
    }




}
