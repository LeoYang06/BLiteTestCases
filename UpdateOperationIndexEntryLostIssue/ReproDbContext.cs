using BLite.Core;
using BLite.Core.Collections;
using BLite.Core.Metadata;

namespace IndexUpdateErrorIssue;

public partial class ReproDbContext : DocumentDbContext
{
    public DocumentCollection<string, LinkedFolder> LinkedFolders { get; set; } = null!;
    public DocumentCollection<Guid, Photo> Photos { get; set; } = null!;

    public ReproDbContext(string path) : base(path)
    {
        InitializeCollections();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Scenario 1 Index
        modelBuilder.Entity<LinkedFolder>().ToCollection("LinkedFolders").HasIndex(x => x.Id, unique: true).HasIndex(x => x.DeviceId);

        // Scenario 2 Index
        modelBuilder.Entity<Photo>().ToCollection("Photos").HasIndex(x => x.Id, unique: true).HasIndex(x => x.SourceId).HasIndex(x => x.DateTaken);
    }
}