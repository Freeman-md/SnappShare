using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

public enum Status {
    Pending,
    Completed,
    Failed
}

public class FileEntry {

    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];

    [Required]
    [MaxLength(255)]
    public required string FileName { get; set; }

    [Required]
    [MaxLength(20)]
    public required string FileExtension { get; set; }

    [NotMapped]
    public string FullFileName => $"{FileName}.{FileExtension}";

    [Required]
    public int TotalChunks { get; set; }

    [NotMapped]
    public int UploadedChunks => Chunks?.Count ?? 0;

    public long FileSize { get; set; }

    [MaxLength(64)]
    public string? FileHash { get; set; } 

    [Required]
    [EnumDataType(typeof(Status))]
    public Status Status { get; set; } = Status.Pending;

    public bool IsLocked { get; set; } = false;

    [NotMapped]
    public bool IsLockExpired => LockedAt.HasValue && (DateTime.UtcNow - LockedAt.Value).TotalMinutes > 5;

    public DateTime CreatedAt { get; set; }  = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }  = DateTime.UtcNow;
    public DateTime? LockedAt { get; set; }
    public DateTime ExpiresAt { get; set; }  

    public ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();

}