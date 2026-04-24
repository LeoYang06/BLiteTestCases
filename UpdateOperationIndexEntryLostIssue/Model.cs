namespace IndexUpdateErrorIssue;

// ====================================================================
// Entities mimicking the production scenario.
// ====================================================================
public class LinkedFolder
{
    public string Id { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string Name { get; set; } = "";
}

public class Photo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceId { get; set; } = "";
    public DateTime DateTaken { get; set; }
}