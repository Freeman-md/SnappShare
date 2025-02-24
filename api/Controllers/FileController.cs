using System.Threading.Tasks;
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

    private const string BlobContainerName = "snappshare";

    public FileController(ILogger<FileController> logger, IBlobService blobService)
    {
        _logger = logger;
        _blobService = blobService;
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

            var response = new SuccessApiResponse<object>()
            {
                Data = new
                {
                ExpiryDuration = $"Expires in {GetHumanReadableDuration((int)fileUpload.ExpiryDuration)}",
                    FileUrl = sasUrl
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