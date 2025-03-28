using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text;

namespace api.Models;

public class Chunk {

    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];

    [Required]
    public required string FileId { get; set; }

    [ForeignKey("FileId")]
    public FileEntry? FileEntry { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int ChunkIndex { get; set; }

    [NotMapped]
    public string BlockId => Convert.ToBase64String(Encoding.UTF8.GetBytes(ChunkIndex.ToString("D6")));

    [Required]
    [Range(1, int.MaxValue)]
    public long ChunkSize { get; set; }

    [Required]
    [MaxLength(64)]
    public required string ChunkHash { get; set; }

    [Required]
    [MaxLength(500)]
    public required string ChunkUrl { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;


}