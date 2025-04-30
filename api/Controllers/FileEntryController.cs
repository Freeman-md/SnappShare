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
    /// Creates a new file entry with upload metadata before starting the chunked upload process.
    /// </summary>
    /// <param name="dto">Details of the file to be uploaded, including size, hash, and expiry settings.</param>
    /// <returns>The created file entry metadata.</returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(FileEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFileEntry([FromBody] CreateFileEntryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorApiResponse<object>("Invalid Request"));
        }

        var result = await _fileEntryService.CreateFileEntry(
                dto.FileName,
                dto.FileHash,
                dto.FileSize,
                dto.TotalChunks,
                dto.ExpiresIn
            );

        return Ok(result);
    }

    /// <summary>
    /// Handles a chunked file upload. Automatically creates a file entry if new, uploads the chunk, and finalizes the upload when complete.
    /// </summary>
    /// <param name="dto">Information about the file chunk being uploaded.</param>
    /// <returns>Upload result indicating the current status (SUCCESS, SKIPPED, or COMPLETE).</returns>

    [HttpPost("handle-upload")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleFileUpload([FromForm] HandleFileUploadDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorApiResponse<object>("Invalid Request"));

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

    /// <summary>
    /// Retrieves the upload status, chunk progress, and secure download URL for a given file ID.
    /// Returns 400 if the file ID is invalid, or 404 if the file does not exist.
    /// </summary>
    /// <param name="fileId">Unique identifier of the file entry.</param>
    /// <returns>Upload response containing file status, progress, and metadata.</returns>
    [HttpGet("{fileId}")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileEntry([FromRoute] string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return BadRequest(new ErrorApiResponse<object>("File ID must be provided."));

        var response = await _fileEntryService.GetFileEntry(fileId);

        return Ok(response);
    }

    /// <summary>
    /// Uploads an individual chunk for an existing file entry and updates upload progress.
    /// </summary>
    /// <param name="fileId">Unique identifier of the file entry.</param>
    /// <param name="dto">Chunk details including index, hash, and file metadata.</param>
    /// <returns>Upload response indicating the result of the chunk upload.</returns>
    [HttpPost("{fileId}/upload")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFileEntryChunk([FromRoute] string fileId, [FromForm] ChunkDtoBase dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorApiResponse<object>("Invalid Request"));

        var result = await _fileEntryService.UploadFileEntryChunk(
                                fileId,
                                dto.FileName,
                                dto.FileHash,
                                dto.ChunkIndex,
                                dto.TotalChunks,
                                dto.ChunkFile,
                                dto.ChunkHash
                            );

        return Ok(result);
    }

    /// <summary>
    /// Finalizes the chunked upload for a file, commits the uploaded blocks, generates a secure download URL, and schedules automatic file deletion.
    /// </summary>
    /// <param name="fileId">Unique identifier of the file entry.</param>
    /// <returns>Upload result confirming successful finalization or indicating issues.</returns>
    [HttpPost("{fileId}/finalize")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FinalizeFileEntry([FromRoute] string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return BadRequest(new ErrorApiResponse<object>("File ID must be provided"));
        }

        UploadResponseDto result = await _fileEntryService.FinalizeUpload(fileId);

        return Ok(result);
    }

}