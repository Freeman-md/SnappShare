using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public class HandleFileUploadDto {
    [Required]
    public required string FileName { get; set; }

    [Required]
    public required string FileHash { get; set; }

    [Required]
    [Range(1, long.MaxValue)]
    public long FileSize { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int ChunkIndex { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int TotalChunks { get; set; } 

    [Required] 
    public required IFormFile ChunkFile { get; set; }

    [Required]
    public required string ChunkHash { get; set; } 

    [Required]
    [EnumDataType(typeof(ExpiryDuration))]
    public ExpiryDuration ExpiresIn { get; set; }
}