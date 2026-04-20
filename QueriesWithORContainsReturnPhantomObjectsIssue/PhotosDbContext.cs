using BLite.Core;
using BLite.Core.Collections;
using BLite.Core.Metadata;

namespace QueriesWithORContainsReturnPhantomObjectsIssue;

public partial class PhotosDbContext : DocumentDbContext
{
    public DocumentCollection<Guid, PhotoPo> Photos { get; set; } = null!;
    public DocumentCollection<Guid, PhotoMetadataPo> PhotoMetadata { get; set; } = null!;

    public PhotosDbContext(string path) : base(path)
    {
        InitializeCollections();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PhotoPo>().ToCollection("Photos").HasIndex(x => x.Id, unique: true).HasIndex(x => x.SourceId);

        modelBuilder.Entity<PhotoMetadataPo>().ToCollection("PhotoMetadata").HasIndex(x => x.Id, unique: true).HasIndex(x => x.SourceId);
    }
}