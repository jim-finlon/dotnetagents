// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record BootstrapPayload(
    ProjectInfo? Project,
    string ResumePoint,
    IReadOnlyList<TaskInfo> Tasks,
    TaskStatistics? TaskStats,
    IReadOnlyList<LessonInfo>? RelevantLessons,
    IReadOnlyList<MilestoneInfo>? Milestones,
    SnapshotInfo? LastSnapshot,
    SessionContextInfo? SessionContext,
    ProjectRulesInfo? ProjectRules,
    DateTime GeneratedAt
);

public record ProjectInfo(Guid Id, string Name, string? Description, string Status, DateTime CreatedAt, DateTime UpdatedAt, Dictionary<string, string>? Metadata);

public record TaskInfo(Guid Id, string Content, string Status, string Priority, int Order, IReadOnlyList<Guid>? DependsOn, IReadOnlyList<string>? Tags, string? Notes);

public record TaskStatistics(int Total, int Pending, int InProgress, int Completed, int Blocked, double CompletionPercentage);

public record LessonInfo(Guid Id, string Title, string Description, string Category, string? Severity, IReadOnlyList<string>? Tags, int ReferenceCount, DateTime CreatedAt);

public record MilestoneInfo(Guid Id, string Name, string Description, string Status, int Order);

public record SnapshotInfo(Guid Id, int SnapshotNumber, DateTime CreatedAt, string Trigger);

public record SessionContextInfo(
    IReadOnlyList<string>? RecentFiles,
    string? LastModifiedFile,
    string? LastCommitMessage,
    string? LastCommitHash,
    IReadOnlyList<string>? KeyDecisions,
    IReadOnlyList<string>? OpenQuestions,
    Dictionary<string, string>? Assumptions,
    IReadOnlyList<string>? RecentCommands,
    IReadOnlyList<string>? RecentErrors,
    string? WorkingDirectory,
    string? ActiveBranch
);

public record ProjectRulesInfo(string? RulesContent, string? FormatType, IReadOnlyList<string>? Categories);
