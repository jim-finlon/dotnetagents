// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Knowledge.Models;

namespace DotNetAgents.Knowledge.Export;

/// <summary>
/// Options for exporting knowledge items for AI fine-tuning.
/// </summary>
public class KnowledgeExportOptions
{
    /// <summary>
    /// Include global knowledge items (not tied to any session).
    /// </summary>
    public bool IncludeGlobal { get; set; } = true;

    /// <summary>
    /// Filter by tech stack (match against TechStack or Tags).
    /// </summary>
    public List<string>? TechStack { get; set; }

    /// <summary>
    /// Filter by category.
    /// </summary>
    public KnowledgeCategory? Category { get; set; }

    /// <summary>
    /// Minimum reference count (quality filter).
    /// </summary>
    public int? MinReferenceCount { get; set; }

    /// <summary>
    /// Minimum effective confidence from metadata (quality filter). Use when items have EffectiveConfidence in Metadata.
    /// </summary>
    public double? MinConfidence { get; set; }

    /// <summary>
    /// Maximum number of knowledge items to export.
    /// </summary>
    public int? MaxItems { get; set; }

    /// <summary>
    /// Custom system prompt for conversation formats.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Export strategy for transforming knowledge into messages.
    /// </summary>
    public KnowledgeExportStrategy Strategy { get; set; } = KnowledgeExportStrategy.QA;
}
