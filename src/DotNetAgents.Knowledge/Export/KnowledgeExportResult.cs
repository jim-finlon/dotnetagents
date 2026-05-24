// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Knowledge.Models;

namespace DotNetAgents.Knowledge.Export;

/// <summary>
/// Result of a knowledge export operation.
/// </summary>
public class KnowledgeExportResult
{
    /// <summary>
    /// Formatted content ready for fine-tuning (JSONL format).
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Number of knowledge items exported.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Export format used.
    /// </summary>
    public KnowledgeExportFormat Format { get; set; }

    /// <summary>
    /// Export strategy used.
    /// </summary>
    public KnowledgeExportStrategy Strategy { get; set; }

    /// <summary>
    /// Metadata about exported items (IDs, reference counts, confidence, etc.).
    /// </summary>
    public List<KnowledgeItemExportMetadata> ItemMetadata { get; set; } = new();

    /// <summary>
    /// Export timestamp.
    /// </summary>
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Metadata about an exported knowledge item.
/// </summary>
public class KnowledgeItemExportMetadata
{
    /// <summary>
    /// Knowledge item ID.
    /// </summary>
    public Guid KnowledgeId { get; set; }

    /// <summary>
    /// Title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Category.
    /// </summary>
    public KnowledgeCategory Category { get; set; }

    /// <summary>
    /// Severity.
    /// </summary>
    public KnowledgeSeverity Severity { get; set; }

    /// <summary>
    /// Tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Tech stack.
    /// </summary>
    public List<string> TechStack { get; set; } = new();

    /// <summary>
    /// Reference count.
    /// </summary>
    public int ReferenceCount { get; set; }

    /// <summary>
    /// Effective confidence from metadata if present.
    /// </summary>
    public double? EffectiveConfidence { get; set; }

    /// <summary>
    /// When the item was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
