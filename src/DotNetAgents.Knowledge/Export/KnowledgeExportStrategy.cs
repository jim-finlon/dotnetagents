// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Knowledge.Export;

/// <summary>
/// Export strategy for transforming knowledge items into training data.
/// </summary>
public enum KnowledgeExportStrategy
{
    /// <summary>
    /// Question-Answer format: "How do I solve X?" -> Solution.
    /// </summary>
    QA,

    /// <summary>
    /// Error resolution format: Error message + Context -> Solution.
    /// </summary>
    ErrorResolution,

    /// <summary>
    /// Best practices format: Context -> Best practice.
    /// </summary>
    BestPractices,

    /// <summary>
    /// Comprehensive format: Include all knowledge fields in structured format.
    /// </summary>
    Comprehensive
}
