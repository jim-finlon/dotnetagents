using DotNetAgents.Knowledge.Models;

namespace DotNetAgents.Knowledge.Import;

/// <summary>
/// Service for importing knowledge items from markdown or JSON formats.
/// </summary>
public interface IKnowledgeImportService
{
    /// <summary>
    /// Parses markdown content (lessons-learned style) into knowledge items.
    /// </summary>
    /// <param name="markdownContent">Markdown content to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of parsed knowledge items.</returns>
    Task<List<KnowledgeItem>> ParseMarkdownAsync(
        string markdownContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses JSON content into knowledge items.
    /// Supports { "items": [...] }, { "lessons": [...] }, or a direct array.
    /// </summary>
    /// <param name="jsonContent">JSON content to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of parsed knowledge items.</returns>
    Task<List<KnowledgeItem>> ParseJsonAsync(
        string jsonContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports knowledge items into the repository with deduplication.
    /// </summary>
    /// <param name="items">Knowledge items to import.</param>
    /// <param name="markAsGlobal">Whether to mark items as global (SessionId = null).</param>
    /// <param name="sourceSession">Source session name for tracking.</param>
    /// <param name="importSource">Import source identifier (file path, URL, etc.).</param>
    /// <param name="defaultTechStack">Default tech stack to apply if an item has none.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import result with statistics.</returns>
    Task<KnowledgeImportResult> ImportAsync(
        List<KnowledgeItem> items,
        bool markAsGlobal = true,
        string? sourceSession = null,
        string? importSource = null,
        List<string>? defaultTechStack = null,
        CancellationToken cancellationToken = default);
}
