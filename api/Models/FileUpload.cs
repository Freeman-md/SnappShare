using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using api.Enums;
using api.Validators;

namespace api.Models;

[FileOrNoteRequired]
public class FileUpload
{

    [JsonPropertyName("file")]
    [AllowedFileExtensions(ErrorMessage = "Invalid file type. Allowed types: .jpg, .png, .pdf, .txt")]
    [MaxFileSize(5)]
    public IFormFile? File { get; set; }

    [JsonPropertyName("note")]
    [StringLength(200, MinimumLength = 20)]
    public string? Note { get; set; }

    [JsonPropertyName("ExpiryDuration")]
    [Required(ErrorMessage = "Expiry duration is required")]
    public ExpiryDuration ExpiryDuration { get; set; }

}