namespace HasConversionSourceGeneratorIssue;

public class DevicePo
{
    public required string Id { get; init; }

    public required ulong SearchIndexId { get; init; }

    public required string Name { get; init; }

    public required string Identifier { get; init; }

    public DateTime AddedAt { get; set; }
}