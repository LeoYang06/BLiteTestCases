namespace CrossCollectionCaseNotEnoughSpaceIssue;

// ====================================================================
// EN: Define two distinct entity types representing two different collections.
// ====================================================================

public class DocA
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] Payload { get; set; } = new byte[500];
}


public class DocB
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] Payload { get; set; } = new byte[500];
}