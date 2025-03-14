using System.Threading.Tasks;
using api.Data;
using api.Enums;
using api.Interfaces.Services;
using api.Models;
using Humanizer;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController : ControllerBase
{
    private readonly ILogger<FileController> _logger;
    private readonly IBlobService _blobService;
    private readonly IFileService _fileService;
    private readonly SnappshareContext _dbContext;

    private const string BlobContainerName = "snappshare";

    public FileController(ILogger<FileController> logger, IBlobService blobService, IFileService fileService, SnappshareContext dbContext)
    {
        _logger = logger;
        _blobService = blobService;
        _fileService = fileService;
        _dbContext = dbContext;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] FileUpload fileUpload)
    {

        try
        {
            FileUpload file = await _fileService.UploadFile(fileUpload);

            var response = new SuccessApiResponse<object>()
            {
                Data = new
                {
                    file.Id,
                    ExpiryDuration = $"Expires in {GetHumanReadableDuration((int)fileUpload.ExpiryDuration)}",
                    FileAccessUrl = $"{Request.Scheme}://{Request.Host}/file/{file.Id}"
                }
            };

            return Ok(response);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, ex.Message);

            return BadRequest(new ErrorApiResponse<object>(
                ExtractAzureErrorMessage(ex), 400, "Blob Storage Error"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(new ErrorApiResponse<object>(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFileDetails(string id)
    {
        try
        {
            var (file, expiryTime, isExpired) = await _fileService.GetFile(id);

            return Ok(new SuccessApiResponse<object>
            {
                Data = new
                {
                    ExpiresAt = expiryTime,
                    file.CreatedAt,
                    OriginalUrl = isExpired ? null : file.OriginalUrl
                }
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>
            {
                Message = "File Not Found",
                StatusCode = 404
            });
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
            "BlobAlreadyExists" => "The specified file already exists in the container.",
            "AuthorizationPermissionMismatch" => "You do not have the correct permissions to upload this file.",
            "ContainerNotFound" => "The specified container does not exist in Azure Storage.",
            _ => ex.Message
        };
    }

    private string GetHumanReadableDuration(int durationInMinutes)
    {
        return TimeSpan.FromMinutes(durationInMinutes).Humanize(minUnit: Humanizer.Localisation.TimeUnit.Minute);
    }

}