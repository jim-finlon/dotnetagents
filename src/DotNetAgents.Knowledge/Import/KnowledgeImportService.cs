// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNetAgents.Knowledge.Helpers;
using DotNetAgents.Knowledge.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Knowledge.Import;

/// <summary>
/// Imports knowledge items from markdown or JSON formats with deduplication and tech stack inference.
/// </summary>
public sealed class KnowledgeImportService : IKnowledgeImportService
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<KnowledgeImportService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeImportService"/> class.
    /// </summary>
    /// <param name="repository">The knowledge repository.</param>
    /// <param name="logger">Optional logger.</param>
    public KnowledgeImportService(
        IKnowledgeRepository repository,
        ILogger<KnowledgeImportService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<KnowledgeItem>> ParseMarkdownAsync(
        string markdownContent,
        CancellationToken cancellationToken = default)
    {
        var lessons = new List<KnowledgeItem>();
        var lines = markdownContent.Split('\n');
        string title = string.Empty;
        var category = KnowledgeCategory.Gotcha;
        var severity = KnowledgeSeverity.Info;
        var tags = new List<string>();
        var techStack = new List<string>();
        string? context = null;
        var description = string.Empty;
        string? solution = null;
        var currentSection = string.Empty;
        var sectionContent = new StringBuilder();
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;

        for (var i = 0; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = lines[i].TrimEnd();

            var lessonMatch = Regex.Match(line, @"^###\s+Lesson\s+(\d+):\s+(.+)$");
            if (lessonMatch.Success)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    FinalizeMarkdownLesson(ref description, sectionContent, currentSection, ref solution);
                    if (string.IsNullOrWhiteSpace(description))
                        description = title;
                    var item = BuildKnowledgeItem(title, description, context, solution, category, severity, tags, techStack, createdAt);
                    ExtractTechStackHints(item, ref techStack);
                    lessons.Add(item with { TechStack = techStack });
                }

                title = lessonMatch.Groups[2].Value.Trim();
                category = KnowledgeCategory.Gotcha;
                severity = KnowledgeSeverity.Info;
                tags = new List<string>();
                techStack = new List<string>();
                context = null;
                description = string.Empty;
                solution = null;
                currentSection = string.Empty;
                sectionContent.Clear();
                createdAt = DateTimeOffset.UtcNow;
                continue;
            }

            if (string.IsNullOrEmpty(title))
                continue;

            if (line.StartsWith("**Date**:", StringComparison.OrdinalIgnoreCase))
            {
                var dateStr = ExtractValue(line);
                if (DateTimeOffset.TryParse(dateStr, out var date))
                    createdAt = date;
            }
            else if (line.StartsWith("**Category**:", StringComparison.OrdinalIgnoreCase))
                category = ParseCategory(ExtractValue(line));
            else if (line.StartsWith("**Severity**:", StringComparison.OrdinalIgnoreCase))
                severity = ParseSeverity(ExtractValue(line));
            else if (line.StartsWith("**Tags**:", StringComparison.OrdinalIgnoreCase))
                tags = ParseTags(ExtractValue(line));
            else if (line.StartsWith("**Context**:", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "Context";
                sectionContent.Clear();
                var v = ExtractValue(line);
                if (!string.IsNullOrWhiteSpace(v))
                    sectionContent.AppendLine(v);
            }
            else if (line.StartsWith("**Problem**:", StringComparison.OrdinalIgnoreCase))
            {
                if (currentSection == "Context" && sectionContent.Length > 0)
                {
                    context = sectionContent.ToString().Trim();
                    sectionContent.Clear();
                }
                currentSection = "Problem";
                var v = ExtractValue(line);
                if (!string.IsNullOrWhiteSpace(v))
                    sectionContent.AppendLine(v);
            }
            else if (line.StartsWith("**Solution**:", StringComparison.OrdinalIgnoreCase))
            {
                if (currentSection == "Problem" && sectionContent.Length > 0)
                {
                    description = sectionContent.ToString().Trim();
                    sectionContent.Clear();
                }
                currentSection = "Solution";
                var v = ExtractValue(line);
                if (!string.IsNullOrWhiteSpace(v))
                    sectionContent.AppendLine(v);
            }
            else if (line.StartsWith("**Prevention**:", StringComparison.OrdinalIgnoreCase))
            {
                if (currentSection == "Solution" && sectionContent.Length > 0)
                {
                    solution = sectionContent.ToString().Trim();
                    sectionContent.Clear();
                }
                currentSection = "Prevention";
            }
            else if (line.StartsWith("---", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(line))
            {
                if (currentSection == "Context" && sectionContent.Length > 0)
                {
                    context = sectionContent.ToString().Trim();
                    sectionContent.Clear();
                }
                else if (currentSection == "Problem" && sectionContent.Length > 0)
                {
                    description = sectionContent.ToString().Trim();
                    sectionContent.Clear();
                }
                else if (currentSection == "Solution" && sectionContent.Length > 0)
                {
                    solution = sectionContent.ToString().Trim();
                    sectionContent.Clear();
                }
                currentSection = string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(line) && currentSection.Length > 0)
            {
                sectionContent.AppendLine(line);
            }
        }

        if (!string.IsNullOrEmpty(title))
        {
            FinalizeMarkdownLesson(ref description, sectionContent, currentSection, ref solution);
            if (string.IsNullOrWhiteSpace(description))
                description = title;
            var item = BuildKnowledgeItem(title, description, context, solution, category, severity, tags, techStack, createdAt);
            ExtractTechStackHints(item, ref techStack);
            lessons.Add(item with { TechStack = techStack });
        }

        return Task.FromResult(lessons);
    }

    /// <inheritdoc />
    public Task<List<KnowledgeItem>> ParseJsonAsync(
        string jsonContent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        JsonElement array;
        if (root.TryGetProperty("items", out var itemsProp))
            array = itemsProp;
        else if (root.TryGetProperty("lessons", out var lessonsProp))
            array = lessonsProp;
        else if (root.ValueKind == JsonValueKind.Array)
            array = root;
        else
            throw new InvalidOperationException("JSON must contain an 'items' or 'lessons' array, or be a root array.");

        var list = new List<KnowledgeItem>();
        foreach (var el in array.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var title = el.GetProperty("title").GetString() ?? string.Empty;
            var description = el.GetProperty("description").GetString() ?? string.Empty;
            var category = ParseCategory(el.TryGetProperty("category", out var cat) ? cat.GetString() : null);
            var severity = ParseSeverity(el.TryGetProperty("severity", out var sev) ? sev.GetString() : null);
            var tags = el.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array
                ? tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").Where(s => s.Length > 0).ToList()
                : new List<string>();
            var techStack = el.TryGetProperty("techStack", out var tsEl) && tsEl.ValueKind == JsonValueKind.Array
                ? tsEl.EnumerateArray().Select(t => t.GetString() ?? "").Where(s => s.Length > 0).ToList()
                : new List<string>();
            var context = el.TryGetProperty("context", out var ctx) ? ctx.GetString() : null;
            var solution = el.TryGetProperty("solution", out var sol) ? sol.GetString() : null;
            var createdAt = DateTimeOffset.UtcNow;
            if (el.TryGetProperty("createdAt", out var ca) && DateTimeOffset.TryParse(ca.GetString(), out var created))
                createdAt = created;

            var item = BuildKnowledgeItem(title, description, context, solution, category, severity, tags, techStack, createdAt);
            ExtractTechStackHints(item, ref techStack);
            list.Add(item with { TechStack = techStack });
        }

        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public async Task<KnowledgeImportResult> ImportAsync(
        List<KnowledgeItem> items,
        bool markAsGlobal = true,
        string? sourceSession = null,
        string? importSource = null,
        List<string>? defaultTechStack = null,
        CancellationToken cancellationToken = default)
    {
        var result = new KnowledgeImportResult();
        var importTime = DateTimeOffset.UtcNow;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var techStack = defaultTechStack != null && defaultTechStack.Count > 0 && item.TechStack.Count == 0
                    ? defaultTechStack
                    : item.TechStack.ToList();

                var toImport = markAsGlobal
                    ? item with { SessionId = null, SourceSession = sourceSession, ImportedAt = importTime, ImportSource = importSource, TechStack = techStack }
                    : item with { SourceSession = sourceSession, ImportedAt = importTime, ImportSource = importSource, TechStack = techStack };

                var existing = await _repository.FindDuplicateAsync(item.Title, item.Description, cancellationToken).ConfigureAwait(false);
                if (existing != null)
                {
                    var mergedTags = existing.Tags.Union(toImport.Tags, StringComparer.OrdinalIgnoreCase).ToList();
                    var mergedTech = existing.TechStack.Union(toImport.TechStack, StringComparer.OrdinalIgnoreCase).ToList();
                    var updated = existing with
                    {
                        Tags = mergedTags,
                        TechStack = mergedTech,
                        ReferenceCount = existing.ReferenceCount + 1,
                        LastReferencedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    await _repository.UpdateKnowledgeAsync(updated, cancellationToken).ConfigureAwait(false);
                    result.Skipped++;
                    result.KnowledgeIds.Add(existing.Id);
                }
                else
                {
                    var added = await _repository.AddKnowledgeAsync(toImport, cancellationToken).ConfigureAwait(false);
                    result.Imported++;
                    result.KnowledgeIds.Add(added.Id);
                }
            }
            catch (Exception ex)
            {
                result.Errors++;
                result.ErrorMessages.Add($"Failed to import '{item.Title}': {ex.Message}");
                _logger?.LogWarning(ex, "Failed to import knowledge item: {Title}", item.Title);
            }
        }

        _logger?.LogInformation(
            "Import complete: {Imported} imported, {Skipped} skipped, {Errors} errors",
            result.Imported,
            result.Skipped,
            result.Errors);

        return result;
    }

    #region Helpers

    private static void FinalizeMarkdownLesson(ref string description, StringBuilder sectionContent, string currentSection, ref string? solution)
    {
        if (currentSection == "Problem" && sectionContent.Length > 0 && string.IsNullOrWhiteSpace(description))
            description = sectionContent.ToString().Trim();
        if (currentSection == "Solution" && sectionContent.Length > 0)
            solution = sectionContent.ToString().Trim();
    }

    private static KnowledgeItem BuildKnowledgeItem(
        string title,
        string description,
        string? context,
        string? solution,
        KnowledgeCategory category,
        KnowledgeSeverity severity,
        List<string> tags,
        List<string> techStack,
        DateTimeOffset createdAt)
    {
        var contentHash = ContentHashHelper.CalculateContentHash(title, description);
        return new KnowledgeItem
        {
            Title = title,
            Description = description,
            Context = context,
            Solution = solution,
            Category = category,
            Severity = severity,
            Tags = tags,
            TechStack = techStack,
            ContentHash = contentHash,
            CreatedAt = createdAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string ExtractValue(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < line.Length - 1)
            return line[(colonIndex + 1)..].Trim().TrimStart('*').Trim();
        return string.Empty;
    }

    private static KnowledgeCategory ParseCategory(string? categoryStr)
    {
        if (string.IsNullOrWhiteSpace(categoryStr))
            return KnowledgeCategory.Gotcha;
        var n = categoryStr.Trim().ToLowerInvariant();
        return n switch
        {
            "toolfailure" or "tool failure" => KnowledgeCategory.ToolFailure,
            "approacherror" or "approach error" => KnowledgeCategory.ApproachError,
            "solution" => KnowledgeCategory.Solution,
            "bestpractice" or "best practice" => KnowledgeCategory.BestPractice,
            "gotcha" => KnowledgeCategory.Gotcha,
            "performance" => KnowledgeCategory.Performance,
            "security" => KnowledgeCategory.Security,
            "configuration" => KnowledgeCategory.Configuration,
            "error" => KnowledgeCategory.Error,
            _ => KnowledgeCategory.Gotcha
        };
    }

    private static KnowledgeSeverity ParseSeverity(string? severityStr)
    {
        if (string.IsNullOrWhiteSpace(severityStr))
            return KnowledgeSeverity.Info;
        var n = severityStr.Trim().ToLowerInvariant();
        return n switch
        {
            "critical" => KnowledgeSeverity.Critical,
            "error" => KnowledgeSeverity.Error,
            "warning" => KnowledgeSeverity.Warning,
            "info" or "informational" => KnowledgeSeverity.Info,
            _ => KnowledgeSeverity.Info
        };
    }

    private static List<string> ParseTags(string tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr))
            return new List<string>();
        var cleaned = tagsStr.Trim().TrimStart('[').TrimEnd(']');
        return cleaned.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private static void ExtractTechStackHints(KnowledgeItem item, ref List<string> techStack)
    {
        var content = $"{item.Title} {item.Description} {item.Context} {item.Solution}".ToLowerInvariant();
        var patterns = new Dictionary<string, string[]>
        {
            { "dotnet", new[] { "dotnet", ".net", "net10", "net9", "net8", "csharp", "c#" } },
            { "aspnet-core", new[] { "aspnet", "asp.net", "aspnetcore", "minimal api", "webapi" } },
            { "postgresql", new[] { "postgresql", "postgres", "npgsql" } },
            { "entity-framework", new[] { "entity framework", "ef core", "efcore" } },
            { "docker", new[] { "docker", "dockerfile", "docker-compose" } },
            { "mcp", new[] { "mcp", "model context protocol" } },
            { "serilog", new[] { "serilog" } }
        };

        foreach (var (tech, keywords) in patterns)
        {
            if (keywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)) &&
                !techStack.Contains(tech, StringComparer.OrdinalIgnoreCase))
            {
                techStack.Add(tech);
            }
        }
    }

    #endregion
}
