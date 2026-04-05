using System;
using BLite.Core;
using BLite.Core.Collections;
using BLite.Core.Metadata;

namespace PerformanceBenchmark;

public partial class BenchmarkBLiteDbContext : DocumentDbContext
{
    public BenchmarkBLiteDbContext(string path) : base(path)
    {
        InitializeCollections();
    }

    public DocumentCollection<Guid, BenchmarkPhotoPo> Photos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BenchmarkPhotoPo>()
           .ToCollection("Photos")
           .HasIndex(x => x.Id, unique: true)
           .HasIndex(x => x.FilePath, unique: true)
           .HasIndex(x => x.SourceId)
           .HasIndex(x => x.DateTaken)
           .HasIndex(x => new { x.DateTaken, x.Id });
    }
}