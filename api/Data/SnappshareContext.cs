using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class SnappshareContext : DbContext {
    public DbSet<FileUpload> FileUploads { get; set; }

    public SnappshareContext(DbContextOptions<SnappshareContext> options) : base(options) {}
}