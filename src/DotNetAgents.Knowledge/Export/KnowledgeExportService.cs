// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;
using DotNetAgents.Knowledge.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Knowledge.Export;

/// <summary>
/// Exports knowledge items in various formats for AI fine-tuning.
/// </summary>
public sealed class KnowledgeExportService : IKnowledgeExportService
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<KnowledgeExportService>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string DefaultSystemPrompt =
        "You are an expert software development assistant specializing in .NET, ASP.NET Core, and modern web development practices.";

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeExportService"/> class.
    /// </summary>
    /// <param name="repository">The knowledge repository.</param>
    /// <param name="logger">Optional logger.</param>
    public KnowledgeExportService(
        IKnowledgeRepository repository,
        ILogger<KnowledgeExportService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<KnowledgeExportResult> ExportAsync(
        KnowledgeExportFormat format,
        KnowledgeExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var items = await GetFilteredItemsAsync(options, cancellationToken).ConfigureAwait(false);

        if (options.MaxItems.HasValue && items.Count > options.MaxItems.Value)
        {
            items = items
                .OrderByDescending(EffectiveConfidenceOrZero)
                .ThenByDescending(k => k.ReferenceCount)
                .ThenByDescending(SeverityOrder)
                .Take(options.MaxItems.Value)
                .ToList();
        }

        var content = format switch
        {
            KnowledgeExportFormat.OpenAiJsonl => GenerateOpenAiJsonl(items, options),
            KnowledgeExportFormat.AnthropicJsonl => GenerateAnthropicJsonl(items, options),
            KnowledgeExportFormat.InstructionResponse => GenerateInstructionResponse(items, options),
            KnowledgeExportFormat.ChatML => GenerateChatML(items, options),
            _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
        };

        var metadata = items.Select(k => new KnowledgeItemExportMetadata
        {
            KnowledgeId = k.Id,
            Title = k.Title,
            Category = k.Category,
            Severity = k.Severity,
            Tags = k.Tags.ToList(),
            TechStack = k.TechStack.ToList(),
            ReferenceCount = k.ReferenceCount,
            EffectiveConfidence = GetEffectiveConfidence(k),
            CreatedAt = k.CreatedAt
        }).ToList();

        _logger?.LogInformation(
            "Exported {Count} knowledge items in {Format} format",
            items.Count,
            format);

        return new KnowledgeExportResult
        {
            Content = content,
            ItemCount = items.Count,
            Format = format,
            Strategy = options.Strategy,
            ItemMetadata = metadata,
            ExportedAt = DateTimeOffset.UtcNow
        };
    }

    #region Format Generators

    private string GenerateOpenAiJsonl(IReadOnlyList<KnowledgeItem> items, KnowledgeExportOptions options)
    {
        var sb = new StringBuilder();
        var systemPrompt = options.SystemPrompt ?? DefaultSystemPrompt;

        foreach (var item in items)
        {
            var (userMessage, assistantMessage) = BuildMessages(item, options.Strategy);
            var jsonObject = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage },
                    new { role = "assistant", content = assistantMessage }
                }
            };
            sb.AppendLine(JsonSerializer.Serialize(jsonObject, _jsonOptions));
        }

        return sb.ToString().TrimEnd();
    }

    private string GenerateAnthropicJsonl(IReadOnlyList<KnowledgeItem> items, KnowledgeExportOptions options)
    {
        var sb = new StringBuilder();
        var systemPrompt = options.SystemPrompt ?? DefaultSystemPrompt;

        foreach (var item in items)
        {
            var (userMessage, assistantMessage) = BuildMessages(item, options.Strategy);
            var jsonObject = new
            {
                messages = new[]
                {
                    new { role = "user", content = userMessage },
                    new { role = "assistant", content = assistantMessage }
                },
                system = systemPrompt
            };
            sb.AppendLine(JsonSerializer.Serialize(jsonObject, _jsonOptions));
        }

        return sb.ToString().TrimEnd();
    }

    private string GenerateInstructionResponse(IReadOnlyList<KnowledgeItem> items, KnowledgeExportOptions options)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            var (prompt, completion) = BuildInstructionResponse(item, options.Strategy);
            var jsonObject = new { prompt, completion };
            sb.AppendLine(JsonSerializer.Serialize(jsonObject, _jsonOptions));
        }

        return sb.ToString().TrimEnd();
    }

    private string GenerateChatML(IReadOnlyList<KnowledgeItem> items, KnowledgeExportOptions options)
    {
        var sb = new StringBuilder();
        var systemPrompt = options.SystemPrompt ?? DefaultSystemPrompt;

        foreach (var item in items)
        {
            var (userMessage, assistantMessage) = BuildMessages(item, options.Strategy);
            var jsonObject = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage },
                    new { role = "assistant", content = assistantMessage }
                }
            };
            sb.AppendLine(JsonSerializer.Serialize(jsonObject, _jsonOptions));
        }

        return sb.ToString().TrimEnd();
    }

    #endregion

    #region Message Builders

    private static (string UserMessage, string AssistantMessage) BuildMessages(
        KnowledgeItem item,
        KnowledgeExportStrategy strategy)
    {
        return strategy switch
        {
            KnowledgeExportStrategy.QA => BuildQAMessages(item),
            KnowledgeExportStrategy.ErrorResolution => BuildErrorResolutionMessages(item),
            KnowledgeExportStrategy.BestPractices => BuildBestPracticesMessages(item),
            KnowledgeExportStrategy.Comprehensive => BuildComprehensiveMessages(item),
            _ => BuildQAMessages(item)
        };
    }

    private static (string Prompt, string Completion) BuildInstructionResponse(
        KnowledgeItem item,
        KnowledgeExportStrategy strategy)
    {
        return strategy switch
        {
            KnowledgeExportStrategy.QA => BuildQAInstructionResponse(item),
            KnowledgeExportStrategy.ErrorResolution => BuildErrorResolutionInstructionResponse(item),
            KnowledgeExportStrategy.BestPractices => BuildBestPracticesInstructionResponse(item),
            KnowledgeExportStrategy.Comprehensive => BuildComprehensiveInstructionResponse(item),
            _ => BuildQAInstructionResponse(item)
        };
    }

    private static (string, string) BuildQAMessages(KnowledgeItem item)
    {
        var techStack = item.TechStack.Count > 0
            ? $" (Tech Stack: {string.Join(", ", item.TechStack)})"
            : "";
        var context = !string.IsNullOrWhiteSpace(item.Context)
            ? $"\n\nContext: {item.Context}"
            : "";
        var problem = !string.IsNullOrWhiteSpace(item.ErrorMessage)
            ? $"\n\nError: {item.ErrorMessage}"
            : "";
        var userMessage = $"I'm working on a {item.Category} issue{techStack}.{context}{problem}\n\n{item.Description}\n\nHow should I solve this?";
        var solution = !string.IsNullOrWhiteSpace(item.Solution)
            ? item.Solution
            : "Based on the description provided, here's the recommended approach: " + item.Description;
        var tags = item.Tags.Count > 0
            ? $"\n\nTags: {string.Join(", ", item.Tags)}"
            : "";
        var assistantMessage = $"{solution}{tags}";
        return (userMessage, assistantMessage);
    }

    private static (string, string) BuildErrorResolutionMessages(KnowledgeItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ErrorMessage))
            return BuildQAMessages(item);

        var context = !string.IsNullOrWhiteSpace(item.Context)
            ? $"Context: {item.Context}\n\n"
            : "";
        var userMessage = $"{context}Error: {item.ErrorMessage}\n\n{item.Description}";
        var solution = !string.IsNullOrWhiteSpace(item.Solution)
            ? item.Solution
            : "The issue can be resolved by following best practices for this scenario.";
        return (userMessage, $"Solution: {solution}");
    }

    private static (string, string) BuildBestPracticesMessages(KnowledgeItem item)
    {
        var techStack = item.TechStack.Count > 0
            ? $" for {string.Join(", ", item.TechStack)}"
            : "";
        var userMessage = $"What are the best practices{techStack} for handling {item.Category} scenarios like this?\n\n{item.Description}";
        var solution = !string.IsNullOrWhiteSpace(item.Solution) ? item.Solution : item.Description;
        return (userMessage, $"Best Practice: {solution}");
    }

    private static (string, string) BuildComprehensiveMessages(KnowledgeItem item)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Lesson: {item.Title}");
        sb.AppendLine($"Category: {item.Category}");
        sb.AppendLine($"Severity: {item.Severity}");
        if (item.TechStack.Count > 0)
            sb.AppendLine($"Tech Stack: {string.Join(", ", item.TechStack)}");
        if (!string.IsNullOrWhiteSpace(item.Context))
            sb.AppendLine($"\nContext: {item.Context}");
        sb.AppendLine($"\nDescription: {item.Description}");
        if (!string.IsNullOrWhiteSpace(item.ErrorMessage))
            sb.AppendLine($"\nError: {item.ErrorMessage}");
        var userMessage = sb.ToString();
        var assistantMessage = !string.IsNullOrWhiteSpace(item.Solution)
            ? $"Solution: {item.Solution}"
            : "This lesson documents an important learning from development experience.";
        return (userMessage, assistantMessage);
    }

    private static (string, string) BuildQAInstructionResponse(KnowledgeItem item)
    {
        var (u, a) = BuildQAMessages(item);
        return (u, a);
    }

    private static (string, string) BuildErrorResolutionInstructionResponse(KnowledgeItem item)
    {
        var (u, a) = BuildErrorResolutionMessages(item);
        return (u, a);
    }

    private static (string, string) BuildBestPracticesInstructionResponse(KnowledgeItem item)
    {
        var (u, a) = BuildBestPracticesMessages(item);
        return (u, a);
    }

    private static (string, string) BuildComprehensiveInstructionResponse(KnowledgeItem item)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Lesson: {item.Title}");
        sb.AppendLine($"Category: {item.Category} | Severity: {item.Severity}");
        if (item.TechStack.Count > 0)
            sb.AppendLine($"Tech Stack: {string.Join(", ", item.TechStack)}");
        if (item.Tags.Count > 0)
            sb.AppendLine($"Tags: {string.Join(", ", item.Tags)}");
        if (!string.IsNullOrWhiteSpace(item.Context))
            sb.AppendLine($"\nContext: {item.Context}");
        sb.AppendLine($"\nProblem: {item.Description}");
        if (!string.IsNullOrWhiteSpace(item.ErrorMessage))
            sb.AppendLine($"\nError: {item.ErrorMessage}");
        var prompt = sb.ToString().TrimEnd();
        var completion = !string.IsNullOrWhiteSpace(item.Solution)
            ? $"Solution: {item.Solution}"
            : "This lesson documents important learning from development experience.";
        return (prompt, completion);
    }

    #endregion

    #region Helpers

    private async Task<List<KnowledgeItem>> GetFilteredItemsAsync(
        KnowledgeExportOptions options,
        CancellationToken cancellationToken)
    {
        List<KnowledgeItem> items;

        if (options.TechStack != null && options.TechStack.Count > 0)
        {
            var relevant = await _repository.GetRelevantKnowledgeAsync(
                options.TechStack,
                options.TechStack,
                10000,
                cancellationToken).ConfigureAwait(false);
            items = relevant.ToList();
        }
        else
        {
            var query = new KnowledgeQuery
            {
                SessionId = null,
                IncludeGlobal = options.IncludeGlobal,
                Category = options.Category,
                Page = 1,
                PageSize = 10000,
                SortBy = "ReferenceCount",
                SortDescending = true
            };
            var result = await _repository.QueryKnowledgeAsync(query, cancellationToken).ConfigureAwait(false);
            items = result.Items.ToList();
        }

        if (options.MinReferenceCount.HasValue)
            items = items.Where(k => k.ReferenceCount >= options.MinReferenceCount.Value).ToList();

        if (options.MinConfidence.HasValue)
        {
            items = items.Where(k =>
            {
                var conf = GetEffectiveConfidence(k);
                return conf.HasValue && conf.Value >= options.MinConfidence.Value;
            }).ToList();
        }

        items = items
            .OrderByDescending(EffectiveConfidenceOrZero)
            .ThenByDescending(k => k.ReferenceCount)
            .ThenByDescending(SeverityOrder)
            .ToList();

        return items;
    }

    private static double EffectiveConfidenceOrZero(KnowledgeItem k)
    {
        return GetEffectiveConfidence(k) ?? 0.0;
    }

    private static double? GetEffectiveConfidence(KnowledgeItem k)
    {
        if (k.Metadata.TryGetValue("EffectiveConfidence", out var ecStr) &&
            double.TryParse(ecStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ec))
            return ec;
        if (k.Metadata.TryGetValue("Confidence", out var cStr) &&
            double.TryParse(cStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var c))
            return c;
        return null;
    }

    private static int SeverityOrder(KnowledgeItem k)
    {
        return k.Severity switch
        {
            KnowledgeSeverity.Critical => 4,
            KnowledgeSeverity.Error => 3,
            KnowledgeSeverity.Warning => 2,
            _ => 1
        };
    }

    #endregion
}
