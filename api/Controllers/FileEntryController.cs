using api.DTOs;
using api.Models;
using api.Models.DTOs;
using api.Tests.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("file-entry")]
[ApiController]
public class FileEntryController : ControllerBase
{
    private readonly ILogger<FileEntryController> _logger;
    private readonly IFileEntryService _fileEntryService;

    public FileEntryController(ILogger<FileEntryController> logger, IFileEntryService fileEntryService)
    {
        _logger = logger;
        _fileEntryService = fileEntryService;
    }

    /// <summary>
    /// Handles a chunked file upload. Creates the file entry if it's new, uploads a chunk, and finalizes the upload when complete.
    /// </summary>
    /// <param name="fileName">Original file name.</param>
    /// <param name="fileHash">Hash of the entire file (used for resumability).</param>
    /// <param name="fileSize">Size of the file in bytes.</param>
    /// <param name="chunkIndex">Index of the current chunk (0-based).</param>
    /// <param name="totalChunks">Total number of chunks the file has.</param>
    /// <param name="chunkFile">The file chunk being uploaded.</param>
    /// <param name="chunkHash">Hash of the chunk (used for deduplication).</param>
    /// <returns>Upload result indicating current status of upload (SUCCESS, SKIPPED, or COMPLETE).</returns>
    [HttpPost("handle-upload")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleFileUpload([FromForm] HandleFileUploadDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorApiResponse<object>("Invalid Request"));

        try
        {
            var result = await _fileEntryService.HandleFileUpload(
                                dto.FileName,
                                dto.FileHash,
                                dto.FileSize,
                                dto.ChunkIndex,
                                dto.TotalChunks,
                                dto.ChunkFile,
                                dto.ChunkHash,
                                dto.ExpiresIn
                            );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, ex.Message);
            return BadRequest(new ErrorApiResponse<object>(ex.Message));
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogWarning(ex, ex.Message);
            return BadRequest(new ErrorApiResponse<object>(ExtractAzureErrorMessage(ex)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(new ErrorApiResponse<object>(ex.Message));
        }
    }

    private string ExtractAzureErrorMessage(Azure.RequestFailedException ex)
    {
        return ex.ErrorCode switch
        {
            "BlobNotFound" => "The specified blob does not exist.",
            "AuthorizationPermissionMismatch" => "You don't have permission to upload this file.",
            "ContainerNotFound" => "The specified blob container does not exist.",
            _ => "An unexpected Azure storage error occurred."
        };
    }

}