// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Session;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DotNetAgents.Workflow.Session.Bootstrap;

/// <summary>
/// Default implementation of <see cref="IBootstrapGenerator"/>.
/// </summary>
public class BootstrapGenerator : IBootstrapGenerator
{
    private readonly ILogger<BootstrapGenerator>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapGenerator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public BootstrapGenerator(ILogger<BootstrapGenerator>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<BootstrapPayload> GenerateAsync(
        BootstrapData data,
        BootstrapFormat format = BootstrapFormat.Json,
        CancellationToken cancellationToken = default)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger?.LogDebug(
                "Generating bootstrap payload. SessionId: {SessionId}, Format: {Format}",
                data.SessionId,
                format);

            var formattedContent = format switch
            {
                BootstrapFormat.Json => GenerateJsonFormat(data),
                BootstrapFormat.CursorRules => GenerateCursorRulesFormat(data),
                BootstrapFormat.Agent => GenerateAgentFormat(data),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
            };

            var payload = new BootstrapPayload
            {
                SessionId = data.SessionId,
                WorkflowRunId = data.WorkflowRunId,
                ResumePoint = data.ResumePoint,
                Format = format,
                FormattedContent = formattedContent,
                GeneratedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["sessionName"] = data.SessionName ?? string.Empty,
                    ["hasTasks"] = data.TaskSummary != null,
                    ["hasKnowledge"] = data.KnowledgeItems != null && data.KnowledgeItems.Count > 0,
                    ["hasMilestones"] = data.Milestones.Count > 0,
                    ["hasSnapshot"] = data.LastSnapshot != null,
                    ["hasContext"] = data.SessionContext != null,
                    ["hasProjectRules"] = data.ProjectRules != null
                }
            };

            _logger?.LogInformation(
                "Bootstrap payload generated. SessionId: {SessionId}, Format: {Format}, Size: {Size} bytes",
                data.SessionId,
                format,
                formattedContent.Length);

            return Task.FromResult(payload);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate bootstrap payload. SessionId: {SessionId}", data.SessionId);
            throw new AgentException(
                $"Failed to generate bootstrap payload: {ex.Message}",
                ErrorCategory.WorkflowError,
                ex);
        }
    }

    private static string GenerateJsonFormat(BootstrapData data)
    {
        var payload = new
        {
            session = new
            {
                id = data.SessionId,
                name = data.SessionName,
                description = data.SessionDescription,
                workflowRunId = data.WorkflowRunId,
                resumePoint = data.ResumePoint
            },
            taskSummary = data.TaskSummary != null ? new
            {
                total = data.TaskSummary.Total,
                pending = data.TaskSummary.Pending,
                inProgress = data.TaskSummary.InProgress,
                completed = data.TaskSummary.Completed,
                blocked = data.TaskSummary.Blocked,
                completionPercentage = data.TaskSummary.CompletionPercentage,
                tasks = data.TaskSummary.Tasks.Select(t => new
                {
                    id = t.Id,
                    content = t.Content,
                    status = t.Status,
                    priority = t.Priority,
                    order = t.Order,
                    tags = t.Tags,
                    completedAt = t.CompletedAt
                }).ToList()
            } : null,
            knowledgeItems = data.KnowledgeItems?.Select(k => new
            {
                id = k.Id,
                title = k.Title,
                description = k.Description,
                category = k.Category,
                severity = k.Severity,
                solution = k.Solution,
                tags = k.Tags,
                referenceCount = k.ReferenceCount
            }).ToList(),
            milestones = data.Milestones.Select(m => new
            {
                id = m.Id,
                name = m.Name,
                description = m.Description,
                status = m.Status.ToString(),
                order = m.Order,
                completedAt = m.CompletedAt
            }).ToList(),
            lastSnapshot = data.LastSnapshot != null ? new
            {
                id = data.LastSnapshot.Id,
                snapshotNumber = data.LastSnapshot.SnapshotNumber,
                createdAt = data.LastSnapshot.CreatedAt,
                trigger = data.LastSnapshot.Trigger.ToString(),
                resumePoint = data.LastSnapshot.ResumePoint
            } : null,
            sessionContext = data.SessionContext != null ? new
            {
                recentFiles = data.SessionContext.RecentFiles,
                lastModifiedFile = data.SessionContext.LastModifiedFile,
                lastCommitMessage = data.SessionContext.LastCommitMessage,
                lastCommitHash = data.SessionContext.LastCommitHash,
                keyDecisions = data.SessionContext.KeyDecisions,
                openQuestions = data.SessionContext.OpenQuestions,
                assumptions = data.SessionContext.Assumptions,
                recentCommands = data.SessionContext.RecentCommands,
                recentErrors = data.SessionContext.RecentErrors,
                workingDirectory = data.SessionContext.WorkingDirectory,
                activeBranch = data.SessionContext.ActiveBranch
            } : null,
            projectRules = data.ProjectRules != null ? new
            {
                id = data.ProjectRules.Id,
                rulesContent = data.ProjectRules.RulesContent,
                formatType = data.ProjectRules.FormatType.ToString(),
                categories = data.ProjectRules.Categories
            } : null,
            metadata = data.Metadata,
            generatedAt = DateTimeOffset.UtcNow
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    private static string GenerateCursorRulesFormat(BootstrapData data)
    {
        var sb = new StringBuilder();

        // Header
        var sessionName = data.SessionName ?? data.SessionId;
        sb.AppendLine($"# {sessionName} - Session Context");
        sb.AppendLine();
        sb.AppendLine($"**Session ID**: {data.SessionId}");
        if (!string.IsNullOrWhiteSpace(data.WorkflowRunId))
            sb.AppendLine($"**Workflow Run ID**: {data.WorkflowRunId}");
        sb.AppendLine($"**Last Updated**: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Current Resume Point
        sb.AppendLine("## 📍 Current Status");
        sb.AppendLine();
        sb.AppendLine(data.ResumePoint);
        sb.AppendLine();

        // Task Summary
        if (data.TaskSummary != null)
        {
            sb.AppendLine("## 📊 Task Progress");
            sb.AppendLine();
            sb.AppendLine($"- **Total**: {data.TaskSummary.Total} tasks");
            sb.AppendLine($"- **Completed**: {data.TaskSummary.Completed} ({data.TaskSummary.CompletionPercentage:F1}%)");
            sb.AppendLine($"- **In Progress**: {data.TaskSummary.InProgress}");
            sb.AppendLine($"- **Pending**: {data.TaskSummary.Pending}");
            if (data.TaskSummary.Blocked > 0)
                sb.AppendLine($"- **Blocked**: {data.TaskSummary.Blocked} ⚠️");
            sb.AppendLine();

            // Tasks List
            if (data.TaskSummary.Tasks.Count > 0)
            {
                sb.AppendLine("## ✅ Tasks");
                sb.AppendLine();
                foreach (var task in data.TaskSummary.Tasks)
                {
                    var checkbox = task.Status == "Completed" ? "[x]" : "[ ]";
                    var statusIcon = task.Status switch
                    {
                        "InProgress" => "🔄",
                        "Blocked" => "⛔",
                        "Completed" => "✅",
                        _ => ""
                    };

                    sb.Append($"- {checkbox} {task.Content}");
                    if (!string.IsNullOrEmpty(statusIcon))
                        sb.Append($" {statusIcon}");
                    if (task.Priority == "Critical")
                        sb.Append(" (🔴 CRITICAL)");
                    else if (task.Priority == "High")
                        sb.Append(" (🟠 HIGH)");
                    sb.AppendLine();

                    if (task.Tags.Count > 0)
                    {
                        sb.AppendLine($"  - Tags: {string.Join(", ", task.Tags)}");
                    }
                }
                sb.AppendLine();
            }
        }

        // Knowledge Items
        if (data.KnowledgeItems != null && data.KnowledgeItems.Count > 0)
        {
            sb.AppendLine("## 📚 Lessons Learned");
            sb.AppendLine();
            foreach (var knowledge in data.KnowledgeItems.Take(10))
            {
                var severityIcon = knowledge.Severity switch
                {
                    "Critical" => "🔴",
                    "Error" => "❌",
                    "Warning" => "⚠️",
                    _ => "ℹ️"
                };

                sb.AppendLine($"### {severityIcon} {knowledge.Title}");
                sb.AppendLine($"**Category**: {knowledge.Category} | **Severity**: {knowledge.Severity}");
                if (knowledge.Tags.Count > 0)
                    sb.AppendLine($"**Tags**: {string.Join(", ", knowledge.Tags)}");
                sb.AppendLine();
                sb.AppendLine(knowledge.Description);

                if (!string.IsNullOrWhiteSpace(knowledge.Solution))
                {
                    sb.AppendLine();
                    sb.AppendLine($"**Solution**: {knowledge.Solution}");
                }
                sb.AppendLine();
            }
        }

        // Project Rules (when format is CursorRules or Both)
        if (data.ProjectRules != null &&
            (data.ProjectRules.FormatType == RulesFormat.Both || data.ProjectRules.FormatType == RulesFormat.CursorRules) &&
            !string.IsNullOrWhiteSpace(data.ProjectRules.RulesContent))
        {
            sb.AppendLine("## 📋 Project Rules");
            sb.AppendLine();
            sb.AppendLine(data.ProjectRules.RulesContent);
            sb.AppendLine();
        }

        // Milestones
        if (data.Milestones.Count > 0)
        {
            sb.AppendLine("## 🎯 Milestones");
            sb.AppendLine();
            foreach (var milestone in data.Milestones)
            {
                var statusIcon = milestone.Status == MilestoneStatus.Completed ? "✅" :
                                milestone.Status == MilestoneStatus.InProgress ? "🔄" : "⏸️";
                sb.AppendLine($"- {statusIcon} **{milestone.Name}** - {milestone.Description}");
                if (milestone.CompletedAt.HasValue)
                    sb.AppendLine($"  - Completed: {milestone.CompletedAt:yyyy-MM-dd}");
            }
            sb.AppendLine();
        }

        // Session Context
        if (data.SessionContext != null)
        {
            sb.AppendLine("## 💻 Code Context");
            sb.AppendLine();

            if (data.SessionContext.RecentFiles.Count > 0)
            {
                sb.AppendLine("### Recent Files");
                foreach (var file in data.SessionContext.RecentFiles.Take(10))
                {
                    sb.AppendLine($"- `{file}`");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(data.SessionContext.LastModifiedFile))
            {
                sb.AppendLine($"**Last Modified**: `{data.SessionContext.LastModifiedFile}`");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(data.SessionContext.LastCommitMessage))
            {
                sb.AppendLine($"**Last Commit**: {data.SessionContext.LastCommitMessage}");
                if (!string.IsNullOrWhiteSpace(data.SessionContext.LastCommitHash))
                {
                    var hash = data.SessionContext.LastCommitHash.Length > 8
                        ? data.SessionContext.LastCommitHash.Substring(0, 8)
                        : data.SessionContext.LastCommitHash;
                    sb.AppendLine($"  - Hash: `{hash}`");
                }
                sb.AppendLine();
            }

            if (data.SessionContext.KeyDecisions.Count > 0)
            {
                sb.AppendLine("### Key Decisions");
                foreach (var decision in data.SessionContext.KeyDecisions)
                {
                    sb.AppendLine($"- {decision}");
                }
                sb.AppendLine();
            }

            if (data.SessionContext.OpenQuestions.Count > 0)
            {
                sb.AppendLine("### Open Questions");
                foreach (var question in data.SessionContext.OpenQuestions)
                {
                    sb.AppendLine($"- ❓ {question}");
                }
                sb.AppendLine();
            }

            if (data.SessionContext.Assumptions.Count > 0)
            {
                sb.AppendLine("### Assumptions");
                foreach (var assumption in data.SessionContext.Assumptions)
                {
                    sb.AppendLine($"- **{assumption.Key}**: {assumption.Value}");
                }
                sb.AppendLine();
            }

            if (data.SessionContext.RecentErrors.Count > 0)
            {
                sb.AppendLine("### Recent Errors");
                foreach (var error in data.SessionContext.RecentErrors.Take(5))
                {
                    sb.AppendLine($"- ⚠️ {error}");
                }
                sb.AppendLine();
            }
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Generated by DotNetAgents at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }

    private static string GenerateAgentFormat(BootstrapData data)
    {
        var sb = new StringBuilder();

        // Header
        var sessionName = data.SessionName ?? data.SessionId;
        sb.AppendLine($"# Agent Instructions: {sessionName}");
        sb.AppendLine();

        sb.AppendLine("## Project Overview");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(data.SessionDescription))
            sb.AppendLine($"**Description**: {data.SessionDescription}");
        if (data.TaskSummary != null)
            sb.AppendLine($"**Progress**: {data.TaskSummary.CompletionPercentage:F1}% complete ({data.TaskSummary.Completed}/{data.TaskSummary.Total} tasks)");
        sb.AppendLine();

        // Resume Point
        sb.AppendLine("## Where You Left Off");
        sb.AppendLine();
        sb.AppendLine(data.ResumePoint);
        sb.AppendLine();

        // Current Tasks
        if (data.TaskSummary != null && data.TaskSummary.Tasks.Count > 0)
        {
            var inProgress = data.TaskSummary.Tasks.Where(t => t.Status == "InProgress").ToList();
            var pending = data.TaskSummary.Tasks.Where(t => t.Status == "Pending").Take(5).ToList();

            if (inProgress.Count > 0 || pending.Count > 0)
            {
                sb.AppendLine("## What to Do Next");
                sb.AppendLine();

                if (inProgress.Count > 0)
                {
                    sb.AppendLine("### Currently In Progress:");
                    foreach (var task in inProgress.Take(3))
                    {
                        sb.AppendLine($"- **{task.Content}** (Priority: {task.Priority})");
                    }
                    sb.AppendLine();
                }

                if (pending.Count > 0)
                {
                    sb.AppendLine("### Up Next:");
                    foreach (var task in pending)
                    {
                        sb.AppendLine($"- {task.Content} (Priority: {task.Priority})");
                    }
                    sb.AppendLine();
                }
            }
        }

        // Project Rules (when format is Agent or Both)
        if (data.ProjectRules != null &&
            (data.ProjectRules.FormatType == RulesFormat.Both || data.ProjectRules.FormatType == RulesFormat.Agent) &&
            !string.IsNullOrWhiteSpace(data.ProjectRules.RulesContent))
        {
            sb.AppendLine("## Project Rules");
            sb.AppendLine();
            sb.AppendLine(data.ProjectRules.RulesContent);
            sb.AppendLine();
        }

        // Important Lessons
        if (data.KnowledgeItems != null && data.KnowledgeItems.Count > 0)
        {
            sb.AppendLine("## Important Lessons");
            sb.AppendLine();
            sb.AppendLine("Things learned during this session that you should know:");
            sb.AppendLine();

            foreach (var knowledge in data.KnowledgeItems.Take(5))
            {
                sb.AppendLine($"### {knowledge.Title}");
                sb.AppendLine($"**Category**: {knowledge.Category} | **Severity**: {knowledge.Severity}");
                sb.AppendLine();
                sb.AppendLine(knowledge.Description);

                if (!string.IsNullOrWhiteSpace(knowledge.Solution))
                {
                    sb.AppendLine();
                    sb.AppendLine($"**Solution**: {knowledge.Solution}");
                }
                sb.AppendLine();
            }
        }

        // Milestones
        if (data.Milestones.Count > 0)
        {
            sb.AppendLine("## Project Milestones");
            sb.AppendLine();
            foreach (var milestone in data.Milestones)
            {
                var status = milestone.Status == MilestoneStatus.Completed ? "✅ Complete" :
                            milestone.Status == MilestoneStatus.InProgress ? "🔄 In Progress" :
                            "⏸️ Pending";
                sb.AppendLine($"- **{milestone.Name}**: {status}");
                sb.AppendLine($"  {milestone.Description}");
            }
            sb.AppendLine();
        }

        // Code Context
        if (data.SessionContext != null)
        {
            sb.AppendLine("## Code Context");
            sb.AppendLine();

            if (data.SessionContext.RecentFiles.Count > 0)
            {
                sb.AppendLine("**Recent Files Worked On:**");
                foreach (var file in data.SessionContext.RecentFiles.Take(10))
                {
                    sb.AppendLine($"- {file}");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(data.SessionContext.LastModifiedFile))
            {
                sb.AppendLine($"**Last Modified File**: {data.SessionContext.LastModifiedFile}");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(data.SessionContext.LastCommitMessage))
            {
                sb.AppendLine($"**Last Commit**: {data.SessionContext.LastCommitMessage}");
                sb.AppendLine();
            }

            if (data.SessionContext.KeyDecisions.Count > 0)
            {
                sb.AppendLine("**Key Decisions Made:**");
                foreach (var decision in data.SessionContext.KeyDecisions)
                {
                    sb.AppendLine($"- {decision}");
                }
                sb.AppendLine();
            }

            if (data.SessionContext.OpenQuestions.Count > 0)
            {
                sb.AppendLine("**Open Questions:**");
                foreach (var question in data.SessionContext.OpenQuestions)
                {
                    sb.AppendLine($"- {question}");
                }
                sb.AppendLine();
            }
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine($"*Session ID: {data.SessionId}*");
        sb.AppendLine($"*Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }
}
