// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Knowledge.Export;

/// <summary>
/// Service for exporting knowledge items in various formats for AI fine-tuning.
/// </summary>
public interface IKnowledgeExportService
{
    /// <summary>
    /// Exports knowledge items in the specified format for AI fine-tuning.
    /// </summary>
    /// <param name="format">The export format (OpenAI JSONL, Anthropic JSONL, Instruction-Response, ChatML).</param>
    /// <param name="options">Export options (filters, limits, strategy).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Export result with formatted content and metadata.</returns>
    Task<KnowledgeExportResult> ExportAsync(
        KnowledgeExportFormat format,
        KnowledgeExportOptions options,
        CancellationToken cancellationToken = default);
}
