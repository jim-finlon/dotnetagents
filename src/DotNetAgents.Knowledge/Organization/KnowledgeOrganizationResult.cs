namespace DotNetAgents.Knowledge.Organization;

/// <summary>
/// Result of organizing knowledge items.
/// </summary>
public class KnowledgeOrganizationResult
{
    /// <summary>
    /// Number of duplicate groups merged.
    /// </summary>
    public int Merged { get; set; }

    /// <summary>
    /// Number of knowledge items deleted (merged into primary).
    /// </summary>
    public int Deleted { get; set; }

    /// <summary>
    /// Number of knowledge items updated (tags/tech stack merged or organized).
    /// </summary>
    public int Updated { get; set; }

    /// <summary>
    /// Number of items that were organized by category/tags/tech stack.
    /// </summary>
    public int Organized { get; set; }

    /// <summary>
    /// List of merge operations performed.
    /// </summary>
    public List<KnowledgeMergeOperation> Merges { get; set; } = new();

    /// <summary>
    /// IDs of deleted knowledge items.
    /// </summary>
    public List<Guid> DeletedKnowledgeIds { get; set; } = new();

    /// <summary>
    /// Whether this was a dry run (no changes persisted).
    /// </summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// Represents a merge operation.
/// </summary>
public class KnowledgeMergeOperation
{
    /// <summary>
    /// ID of the knowledge item that was kept (primary).
    /// </summary>
    public Guid PrimaryKnowledgeId { get; set; }

    /// <summary>
    /// IDs of knowledge items that were merged into the primary.
    /// </summary>
    public List<Guid> MergedKnowledgeIds { get; set; } = new();

    /// <summary>
    /// Reason for the merge.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
