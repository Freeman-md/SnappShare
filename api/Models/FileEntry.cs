using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using api.Enums;

namespace api.Models;

public enum FileEntryStatus {
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

    [MaxLength(20)]
    public string? FileExtension { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int TotalChunks { get; set; }

    [NotMapped]
    public int UploadedChunks => Chunks?.Count ?? 0;

    public long FileSize { get; set; }

    [Required]
    [MaxLength(64)]
    public required string FileHash { get; set; } 

    [MaxLength(500)]
    public string? FileUrl { get; set; }

    [Required]
    [EnumDataType(typeof(FileEntryStatus))]
    public FileEntryStatus Status { get; set; } = FileEntryStatus.Pending;

    public bool IsLocked { get; set; } = false;

    [NotMapped] // TODO: Used to auto-unlock files locked for over 5 minutes (e.g. due to connection loss). When resuming upload, check if lock is still valid to avoid permanent lock.
    public bool IsLockExpired => LockedAt.HasValue && (DateTime.UtcNow - LockedAt.Value).TotalMinutes > 5;

    public DateTime CreatedAt { get; set; }  = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }  = DateTime.UtcNow;

    [NotMapped]
    public DateTime ExpiresAt => UpdatedAt.AddMinutes((int)ExpiresIn);

    public DateTime? LockedAt { get; set; }
    
    [Required(ErrorMessage = "Please indicate when the file is due to expire")]
    [EnumDataType(typeof(ExpiryDuration))]
    public ExpiryDuration ExpiresIn { get; set; }  

    public ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();

}