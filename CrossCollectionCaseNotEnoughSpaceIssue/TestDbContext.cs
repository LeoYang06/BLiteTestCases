using BLite.Core;
using BLite.Core.Collections;
using BLite.Core.Metadata;

namespace CrossCollectionCaseNotEnoughSpaceIssue;

public partial class TestDbContext : DocumentDbContext
{
    public DocumentCollection<Guid, DocA> ColA { get; set; } = null!;
    public DocumentCollection<Guid, DocB> ColB { get; set; } = null!;

    public TestDbContext(string path) : base(path)
    {
        InitializeCollections();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocA>().ToCollection("ColA").HasIndex(x => x.Id, unique: true);
        modelBuilder.Entity<DocB>().ToCollection("ColB").HasIndex(x => x.Id, unique: true);
    }
}