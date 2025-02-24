using api.Interfaces.Services;
using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController : ControllerBase {
    private readonly ILogger<FileController> _logger;
    private readonly IBlobService _blobService;

    public FileController(ILogger<FileController> logger, IBlobService blobService) {
        _logger = logger;
        _blobService = blobService;
    }

    [HttpPost("upload")]
    public IActionResult UploadFile([FromForm] FileUpload uploadedFile) {
        var response = new SuccessApiResponse<FileUpload>() {
            Data = uploadedFile
        };

        return Ok(response);
    }
}