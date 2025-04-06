namespace api.Models.DTOs;

public enum UploadResponseDtoStatus {
    COMPLETE,
    PARTIAL,
    SKIPPED,
    FAILED,
    SUCCESS,
    NEW
}

public class UploadResponseDto
{
    public UploadResponseDtoStatus Status { get; set; } = UploadResponseDtoStatus.NEW;
    
    public string? FileId { get; set; }
    
    public string? FileUrl { get; set; }

    public List<int>? UploadedChunks { get; set; }

    public int? UploadedChunk { get; set; }
    public int? TotalChunks { get; set; }
    
    public string? Message { get; set; }
}
