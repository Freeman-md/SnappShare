using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController : ControllerBase {
    private readonly ILogger<FileController> _logger;

    public FileController(ILogger<FileController> logger) {
        _logger = logger;
    }

    [HttpPost("upload")]
    public IActionResult UploadFile([FromForm] FileUpload uploadedFile) {
        var response = new SuccessApiResponse<FileUpload>() {
            Data = uploadedFile
        };

        return Ok(response);
    }
}