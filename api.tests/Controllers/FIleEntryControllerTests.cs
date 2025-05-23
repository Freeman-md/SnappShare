using System;
using api.Controllers;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Models.DTOs;
using api.Services;
using api.tests.Builders;
using api.Tests.Interfaces.Services;
using Microsoft.AspNetCore.Http;
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
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash, dto.ExpiresIn))
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
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash, dto.ExpiresIn))
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
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash, dto.ExpiresIn))
            .ReturnsAsync(expectedResponse);

        var result = await _fileEntryController.HandleFileUpload(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(UploadResponseDtoStatus.SKIPPED, actualResponse.Status);
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnBadRequest_WhenUnhandledExceptionOccurs()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();

        _fileEntryService.Setup(s => s.HandleFileUpload(
            dto.FileName, dto.FileHash, dto.FileSize, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash, dto.ExpiresIn))
            .ThrowsAsync(new Exception("Unexpected"));

        await Assert.ThrowsAnyAsync<Exception>(async () => await _fileEntryController.HandleFileUpload(dto));
    }

    [Fact]
    public async Task HandleFileUpload_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();

        _fileEntryController.ModelState.AddModelError("FileName", "File name is required");

        var result = await _fileEntryController.HandleFileUpload(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorApiResponse<object>>(badRequest.Value);
        Assert.Equal("Invalid Request", errorResponse.ErrorMessage);

        _fileEntryService.Verify(s => s.HandleFileUpload(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<IFormFile>(),
            It.IsAny<string>(),
            It.IsAny<ExpiryDuration>()
        ), Times.Never);
    }

    [Fact]
    public async Task UploadFileEntryChunk_ShouldReturnOk_WhenUploadIsSuccessful()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();
        var expectedResponse = new UploadResponseDto { Status = UploadResponseDtoStatus.SUCCESS };

        _fileEntryService.Setup(s => s.UploadFileEntryChunk(
            It.IsAny<string>(), dto.FileName, dto.FileHash, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ReturnsAsync(expectedResponse);

        var result = await _fileEntryController.UploadFileEntryChunk("file123", dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(UploadResponseDtoStatus.SUCCESS, actualResponse.Status);
    }

    [Fact]
    public async Task UploadFileEntryChunk_ShouldReturnSkippedStatus_WhenChunkAlreadyUploaded()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();
        var expectedResponse = new UploadResponseDto { Status = UploadResponseDtoStatus.SKIPPED };

        _fileEntryService.Setup(s => s.UploadFileEntryChunk(
            It.IsAny<string>(), dto.FileName, dto.FileHash, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ReturnsAsync(expectedResponse);

        var result = await _fileEntryController.UploadFileEntryChunk("file123", dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(UploadResponseDtoStatus.SKIPPED, actualResponse.Status);
    }

    [Fact]
    public async Task UploadFileEntryChunk_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();
        _fileEntryController.ModelState.AddModelError("FileHash", "Required");

        var result = await _fileEntryController.UploadFileEntryChunk("file123", dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorApiResponse<object>>(badRequest.Value);
        Assert.Equal("Invalid Request", errorResponse.ErrorMessage);

        _fileEntryService.Verify(s => s.UploadFileEntryChunk(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<IFormFile>(),
            It.IsAny<string>()),
        Times.Never);
    }

    [Fact]
    public async Task UploadFileEntryChunk_ShouldReturnBadRequest_WhenUnexpectedExceptionOccurs()
    {
        var dto = new HandleFileUploadDtoBuilder().Build();

        _fileEntryService.Setup(s => s.UploadFileEntryChunk(
            It.IsAny<string>(), dto.FileName, dto.FileHash, dto.ChunkIndex, dto.TotalChunks, dto.ChunkFile, dto.ChunkHash))
            .ThrowsAsync(new Exception("Unexpected Error"));

        await Assert.ThrowsAnyAsync<Exception>(async () => await _fileEntryController.UploadFileEntryChunk("file123", dto));
    }

    //TODO: Now the controller just rethrows the error from services or repo's but if possible, set up something whereby the exception handler handles this exception and returns the expected response.


    [Fact]
    public async Task CreateFileEntry_ReturnsOk_WhenFileIsCreated()
    {
        FileEntry fileEntry = new FileEntryBuilder().Build();
        CreateFileEntryDto fileEntryDto = new CreateFileEntryDtoBuilder(fileEntry).Build();

        _fileEntryService
                .Setup(service => service.CreateFileEntry(fileEntryDto.FileName, fileEntryDto.FileHash, fileEntryDto.FileSize, fileEntryDto.TotalChunks, fileEntryDto.ExpiresIn))
                .ReturnsAsync(fileEntry);


        var result = await _fileEntryController.CreateFileEntry(fileEntryDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FileEntry>(okResult.Value);
        Assert.Equal(fileEntry.FileName, fileEntryDto.FileName);
        _fileEntryService.Verify(
            service => service.CreateFileEntry(fileEntryDto.FileName, fileEntryDto.FileHash, fileEntryDto.FileSize, fileEntryDto.TotalChunks, fileEntryDto.ExpiresIn),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateFileEntry_ThrowsException_WhenUnexpectedExceptionOccurs()
    {
        CreateFileEntryDto fileEntryDto = new CreateFileEntryDtoBuilder().Build();

        _fileEntryService
                .Setup(service => service.CreateFileEntry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<ExpiryDuration>()))
            .ThrowsAsync(new Exception("Something went wrong"));


        await Assert.ThrowsAnyAsync<Exception>(async () => await _fileEntryController.CreateFileEntry(fileEntryDto));
    }

    [Fact]
    public async Task GetFileEntry_ReturnsOk_WhenFileIsFound()
    {
        var fileId = "abc123";
        var expected = new UploadResponseDto { FileId = fileId, Status = UploadResponseDtoStatus.COMPLETE };

        _fileEntryService
            .Setup(s => s.GetFileEntry(fileId))
            .ReturnsAsync(expected);

        var result = await _fileEntryController.GetFileEntry(fileId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(fileId, response.FileId);
    }

    [Fact]
    public async Task GetFileEntry_ReturnsBadRequest_WhenFileIdIsEmpty()
    {
        var result = await _fileEntryController.GetFileEntry("");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorApiResponse<object>>(badRequest.Value);

        _fileEntryService.Verify(s => s.GetFileEntry(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetFileEntry_ThrowsException_WhenUnexpectedExceptionOccurs()
    {
        var fileId = "abc123";

        _fileEntryService
            .Setup(s => s.GetFileEntry(fileId))
            .ThrowsAsync(new Exception("Something went wrong"));

        await Assert.ThrowsAnyAsync<Exception>(async () => await _fileEntryController.GetFileEntry(fileId));
    }

    [Fact]
    public async Task FinalizeFileEntry_ReturnsOk_WhenFileIsComplete()
    {
        FileEntry fileEntry = new FileEntryBuilder().Build();
        var expected = new UploadResponseDto { FileId = fileEntry.Id, Status = UploadResponseDtoStatus.COMPLETE };

        _fileEntryService
            .Setup(s => s.FinalizeUpload(fileEntry.Id))
            .ReturnsAsync(expected);

        var result = await _fileEntryController.FinalizeFileEntry(fileEntry.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UploadResponseDto>(okResult.Value);
        Assert.Equal(fileEntry.Id, response.FileId);
    }

    [Fact]
    public async Task FinalizeFileEntry_ReturnsBadRequest_WhenFileIdIsEmpty()
    {
        var result = await _fileEntryController.FinalizeFileEntry("");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorApiResponse<object>>(badRequest.Value);

        _fileEntryService.Verify(s => s.FinalizeUpload(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FinalizeFileEntry_ThrowsException_WhenUnexpectedExceptionOccurs()
    {
        var fileId = "abc123";

        _fileEntryService
            .Setup(s => s.FinalizeUpload(fileId))
            .ThrowsAsync(new Exception("Something went wrong"));

            
        await Assert.ThrowsAnyAsync<Exception>(async () => await _fileEntryController.FinalizeFileEntry(fileId));
    }

}
