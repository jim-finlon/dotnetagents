namespace DotNetAgents.Agents.IntentProjector;

public enum IntentBlockRole
{
    System,
    Developer,
    User,
    Tool,
    Policy,
    Reference
}

public enum IntentContextScope
{
    Global,
    Workspace,
    Project,
    Story,
    ToolSurface,
    Runtime
}

public enum IntentSecurityClassification
{
    Public,
    Internal,
    Confidential,
    SecretReferenceOnly
}

public enum IntentConsumerKind
{
    HostedModel,
    LocalModel,
    AgentTool,
    HumanOperatedTool,
    ConfigSurface
}

public enum IntentProjectionKind
{
    AgentsMarkdown,
    RuleMarkdown,
    ConfigJson,
    ModelPrompt,
    ToolPrompt
}

public sealed record IntentBlock(
    string Id,
    string Title,
    IntentBlockRole Role,
    IntentContextScope Scope,
    int Precedence,
    IntentSecurityClassification Security,
    string Body,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? SourceRefs = null,
    IReadOnlyList<string>? CredentialRefs = null);

public sealed record IntentConsumerProfile(
    string Id,
    string DisplayName,
    IntentConsumerKind Kind,
    IReadOnlyList<IntentProjectionKind> SupportedProjectionKinds,
    bool SupportsLargeContext,
    bool RequiresOfflineSafeOutput = false);

public sealed record IntentDocument(
    string Id,
    string Title,
    string Version,
    string Summary,
    IReadOnlyList<IntentBlock> Blocks,
    IReadOnlyList<IntentConsumerProfile> Consumers,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record IntentProjectionRequest(
    IntentProjectionKind Kind,
    string ConsumerId,
    string? TargetRoot = null,
    IReadOnlyList<string>? IncludeTags = null,
    bool IncludeReferenceBlocks = true);

public sealed record IntentProjectionArtifact(
    string RelativePath,
    string ContentType,
    string Content);

public sealed record IntentProjectionReceipt(
    string DocumentId,
    string ConsumerId,
    IntentProjectionKind Kind,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<IntentProjectionArtifact> Artifacts,
    IReadOnlyList<string> ValidationMessages);

public sealed class IntentProjectionException(string message) : InvalidOperationException(message);
