using api.Enums;

namespace api.Models.DTOs;

public enum UploadResponseDtoStatus
{
    COMPLETE,
    PARTIAL,
    SKIPPED,
    FAILED,
    SUCCESS,
    NEW
}

public class UploadResponseDto
{
    //TODO: refactor this DTO to take a file entry as a property (could be streamlined omitting certain properties - rather than adding properties one after the other). Then it will now have the status and the message.
    public UploadResponseDtoStatus Status { get; set; } = UploadResponseDtoStatus.NEW;

    public string? FileId { get; set; }
    public string? FileName { get; set; }

    public string? FileHash { get; set; }

    public long? FileSize { get; set; }

    public string? FileUrl { get; set; }

    public DateTime ExpiresAt { get; set; }

    public List<int>? UploadedChunks { get; set; }

    public int? UploadedChunk { get; set; }
    public int? TotalChunks { get; set; }

    public string? Message { get; set; }
}
