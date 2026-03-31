using BLite.Core;
using BLite.Core.Collections;
using BLite.Core.Metadata;

namespace HasConversionSourceGeneratorIssue;

public sealed partial class DeviceDbContext : DocumentDbContext
{
    public DeviceDbContext(string path) : base(path)
    {
        InitializeCollections();
    }

    public DocumentCollection<string, DevicePo> Devices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DevicePo>().ToCollection("Device").HasIndex(x => x.Id, unique: true).HasIndex(x => x.Identifier, unique: true).Property(x => x.SearchIndexId).HasConversion<UlongToInt64Converter>();
    }
}