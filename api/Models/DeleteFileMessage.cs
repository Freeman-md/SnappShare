namespace api.Models;

public class DeleteFileMessage
{
    public string FileId { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string ContainerName { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
}
