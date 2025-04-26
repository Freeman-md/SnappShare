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
    /// Creates New File Entry before upload process
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Newly created file entry</returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(FileEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFileEntry([FromForm] CreateFileEntryDto dto) {
        if (!ModelState.IsValid) {
            return BadRequest(new ErrorApiResponse<object>("Invalid Request"));
        }

        try
        {
            var result = await _fileEntryService.CreateFileEntry(
                dto.FileName,
                dto.FileHash,
                dto.FileSize,
                dto.TotalChunks,
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

    /// <summary>
    /// Handles a chunked file upload. Creates the file entry if it's new, uploads a chunk, and finalizes the upload when complete.
    /// </summary>
    /// <param name="dto"></param>
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

    /// <summary>
    /// Retrieves the status and metadata of an uploaded file.
    /// </summary>
    /// <param name="fileId">The unique identifier of the file.</param>
    /// <returns>Upload response containing file status, chunk progress, and URL if available.</returns>
    [HttpGet("{fileId}")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileEntry([FromRoute] string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return BadRequest(new ErrorApiResponse<object>("File ID must be provided."));

        try
        {
            var response = await _fileEntryService.GetFileEntry(fileId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, ex.Message);
            return BadRequest(new ErrorApiResponse<object>(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, ex.Message);
            return NotFound(new ErrorApiResponse<object>(ex.Message));
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