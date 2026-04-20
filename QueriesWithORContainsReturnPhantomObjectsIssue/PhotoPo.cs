namespace QueriesWithORContainsReturnPhantomObjectsIssue;

// ==========================================
// Entities Definition: Simulate the two entities in the project, sharing the same fields: Id and SourceId
// ==========================================

public class PhotoPo
{
    public Guid Id { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
}

public class PhotoMetadataPo
{
    public Guid Id { get; set; }
    public Guid PhotoId { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string ExifData { get; set; } = string.Empty;
}