using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public class HandleFileUploadDto : ChunkDtoBase {
    [Required]
    [Range(1, long.MaxValue)]
    public long FileSize { get; set; }

    [Required]
    [EnumDataType(typeof(ExpiryDuration))]
    public ExpiryDuration ExpiresIn { get; set; }
}