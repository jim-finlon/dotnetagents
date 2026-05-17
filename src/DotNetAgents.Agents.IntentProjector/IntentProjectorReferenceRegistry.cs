namespace DotNetAgents.Agents.IntentProjector;

public static class IntentProjectorReferenceRegistry
{
    public static IntentDocument CreateDefault()
        => new(
            "dna-agent-intent",
            "DNA Agent Intent Projection",
            "1.0",
            "Canonical reusable intent for generated agent guidance, rule files, model prompts, and tool prompts.",
            Blocks:
            [
                new(
                    "core4",
                    "Core 4 Bootstrap",
                    IntentBlockRole.Policy,
                    IntentContextScope.Workspace,
                    10,
                    IntentSecurityClassification.Internal,
                    "Verify ai-session-persistence, planning_tools, credentials, and knowledge-memory before substantive delivery.",
                    Tags: ["governance", "core4"],
                    SourceRefs: ["AGENTS.md#Core 4 + Session Persistence execution gate"]),
                new(
                    "worktree",
                    "Worktree Delivery Discipline",
                    IntentBlockRole.Developer,
                    IntentContextScope.Project,
                    20,
                    IntentSecurityClassification.Internal,
                    "Use a dedicated DNA worktree for implementation, validate there, merge safely from the canonical checkout, then remove only the lane worktree created for the story.",
                    Tags: ["governance", "delivery"],
                    SourceRefs: ["docs/DNA-WORKTREE-COLLABORATION-RUNBOOK.md"]),
                new(
                    "credentials",
                    "Credential Custody",
                    IntentBlockRole.System,
                    IntentContextScope.ToolSurface,
                    30,
                    IntentSecurityClassification.SecretReferenceOnly,
                    "",
                    Tags: ["security", "credentials"],
                    SourceRefs: ["AGENTS.md#CredentialsAgent-first auth recovery"],
                    CredentialRefs: ["credential-ref/session/api-key", "credential-ref/workflow/api-key", "credential-ref/knowledge/api-key"]),
                new(
                    "local-open-model",
                    "Local and Open Model Compatibility",
                    IntentBlockRole.Reference,
                    IntentContextScope.Runtime,
                    80,
                    IntentSecurityClassification.Internal,
                    "Projection outputs preserve compact metadata and credential references so local runners can consume the same intent without hosted-provider assumptions.",
                    Tags: ["model-routing", "local-llm"],
                    SourceRefs: ["docs/DNA-CONTEXT-TOKEN-EFFICIENCY-RESEARCH.md"])
            ],
            Consumers:
            [
                new(
                    "codex-cli",
                    "Codex CLI",
                    IntentConsumerKind.HumanOperatedTool,
                    [IntentProjectionKind.AgentsMarkdown, IntentProjectionKind.ModelPrompt, IntentProjectionKind.ToolPrompt, IntentProjectionKind.ConfigJson],
                    SupportsLargeContext: true),
                new(
                    "cursor-rules",
                    "Cursor rules",
                    IntentConsumerKind.ConfigSurface,
                    [IntentProjectionKind.RuleMarkdown, IntentProjectionKind.ConfigJson],
                    SupportsLargeContext: false),
                new(
                    "local-open-runner",
                    "Local/open-source model runner",
                    IntentConsumerKind.LocalModel,
                    [IntentProjectionKind.ModelPrompt, IntentProjectionKind.ConfigJson],
                    SupportsLargeContext: false,
                    RequiresOfflineSafeOutput: true)
            ]);
}
