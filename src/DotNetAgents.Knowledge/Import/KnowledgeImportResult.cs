// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Knowledge.Import;

/// <summary>
/// Result of importing knowledge items.
/// </summary>
public class KnowledgeImportResult
{
    /// <summary>
    /// Number of knowledge items successfully imported.
    /// </summary>
    public int Imported { get; set; }

    /// <summary>
    /// Number of knowledge items skipped (duplicates).
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Number of errors encountered.
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// List of error messages.
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// IDs of successfully imported or updated knowledge items.
    /// </summary>
    public List<Guid> KnowledgeIds { get; set; } = new();
}
