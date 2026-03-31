using System.ComponentModel.DataAnnotations.Schema;

namespace FirstOrDefaultAyncIssue;

[Table("linked_folders")]
public class LinkedFolderPo(string id) : PoBase<string>(id)
{
    public required string DeviceId { get; init; }

    public required string Name { get; init; }

    public required string Path { get; init; }

    public int FileCount { get; set; }

    public long FileSize { get; set; }

    public bool IsManaged { get; set; }

    public bool IsVaultSource { get; set; }

    public DateTime AddedAt { get; set; }
}