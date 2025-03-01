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
    private readonly SnappshareContext _dbContext;

    private const string BlobContainerName = "snappshare";

    public FileController(ILogger<FileController> logger, IBlobService blobService, SnappshareContext dbContext)
    {
        _logger = logger;
        _blobService = blobService;
        _dbContext = dbContext;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] FileUpload fileUpload)
    {

        try
        {
            string fileName = fileUpload.File!.FileName;
            DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddMinutes((double)fileUpload.ExpiryDuration);

            (_, string uniqueFileName) = await _blobService.UploadFileAsync(fileUpload.File!, BlobContainerName, expiryTime);

            string sasUrl = await _blobService.GenerateSasTokenAsync(uniqueFileName, BlobContainerName, expiryTime);

            var newFile = new FileUpload
            {
                Id = Guid.NewGuid().ToString()[..6],
                OriginalUrl = sasUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiryDuration = fileUpload.ExpiryDuration,
            };

            _dbContext.FileUploads.Add(newFile);
            await _dbContext.SaveChangesAsync();

            var response = new SuccessApiResponse<object>()
            {
                Data = new
                {
                    newFile.Id,
                    ExpiryDuration = $"Expires in {GetHumanReadableDuration((int)fileUpload.ExpiryDuration)}",
                    FileAccessUrl = $"{Request.Scheme}://{Request.Host}/file/{newFile.Id}"
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
        var file = await _dbContext.FileUploads.FindAsync(id);

        if (file == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Message = "File Not Found",
                StatusCode = 404
            });
        }

        // Calculate expiry time
        DateTimeOffset expiryTime = file.CreatedAt.AddMinutes((double)file.ExpiryDuration);
        bool isExpired = DateTimeOffset.UtcNow >= expiryTime;

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