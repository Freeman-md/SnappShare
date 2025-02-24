using System.Threading.Tasks;
using api.Interfaces.Services;
using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController : ControllerBase
{
    private readonly ILogger<FileController> _logger;
    private readonly IBlobService _blobService;

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
            string fileUrl = await _blobService.UploadFileAsync(fileUpload.File!, "snappshare");

            var response = new SuccessApiResponse<object>()
            {
                Data = new
                {
                    FileUrl = fileUrl,
                    fileUpload.ExpiryDuration // this should be more descriptive - like "In 10 minutes"
                }
            };

            return Ok(response);
        }
        catch (System.Exception ex)
        {
            _logger.Log(LogLevel.Error, new EventId(), ex, ex.Message);
            return BadRequest(ex);
        }
    }
}