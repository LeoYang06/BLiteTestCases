using System.ComponentModel.DataAnnotations.Schema;

namespace FirstOrDefaultAyncIssue;

[Table("devices")]
public class DevicePo(string id) : PoBase<string>(id)
{
    public override string Id { get; init; }

    public required string Name { get; init; }

    public DeviceType Type { get; set; }

    public required string Identifier { get; init; }

    public DateTime AddedAt { get; set; }

    public DateTime LastSeen { get; set; }

    public required List<LinkedFolderPo> LinkedFolders { get; set; } = [];
}