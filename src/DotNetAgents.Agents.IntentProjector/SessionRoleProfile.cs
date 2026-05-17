namespace DotNetAgents.Agents.IntentProjector;

/// <summary>
/// Story 2215c3b4 — role-specific session configuration profile. Each role is a
/// named subset of intent tags + a default projection kind, so an operator (or
/// returning agent) can restart into a UI-heavy, API-heavy, docs-heavy, or other
/// specialized operating mode without manually composing IncludeTags or picking
/// a projection kind.
/// </summary>
/// <remarks>
/// The role profile reads from the intent-projector layer (story fc90a807); it
/// never duplicates policy authority. Profiles do NOT bind to specific consumer
/// ids — that binding is caller-supplied so the same role works across any
/// IntentDocument that has a compatible consumer registered. Tags drive block
/// selection: <see cref="IncludeTags"/> is forwarded to
/// <see cref="IntentProjectionRequest.IncludeTags"/>. Reference blocks remain
/// available unless <see cref="DropReferenceBlocks"/> is true.
/// </remarks>
public sealed record SessionRoleProfile(
    string Id,
    string DisplayName,
    string Summary,
    IReadOnlyList<string> IncludeTags,
    IntentProjectionKind DefaultProjectionKind,
    bool DropReferenceBlocks = false);

/// <summary>
/// Story 2215c3b4 — well-known role profiles shipped as the initial catalog.
/// New roles ship via additive entries; renaming or removing a role is a
/// breaking change to the operator workflow.
/// </summary>
public static class SessionRoleCatalog
{
    public const string UiHeavyRoleId = "session-role.ui-heavy";
    public const string ApiHeavyRoleId = "session-role.api-heavy";
    public const string DataHeavyRoleId = "session-role.data-heavy";
    public const string DocsOnlyRoleId = "session-role.docs-only";
    public const string OpsResponderRoleId = "session-role.ops-responder";

    private static readonly Dictionary<string, SessionRoleProfile> ById = new(StringComparer.OrdinalIgnoreCase)
    {
        [UiHeavyRoleId] = new(
            Id: UiHeavyRoleId,
            DisplayName: "UI-heavy session",
            Summary: "Frontend/Blazor/UX-focused operating mode. Selects blocks tagged ui/frontend/blazor/ux/accessibility/operator-shell; projects as a model prompt by default.",
            IncludeTags: new[] { "ui", "frontend", "blazor", "ux", "accessibility", "operator-shell" },
            DefaultProjectionKind: IntentProjectionKind.ModelPrompt),

        [ApiHeavyRoleId] = new(
            Id: ApiHeavyRoleId,
            DisplayName: "API-heavy session",
            Summary: "Backend/API/MCP-focused operating mode. Selects blocks tagged backend/api/mcp/service-design/dotnet/rest; projects as a model prompt by default.",
            IncludeTags: new[] { "backend", "api", "mcp", "service-design", "dotnet", "rest" },
            DefaultProjectionKind: IntentProjectionKind.ModelPrompt),

        [DataHeavyRoleId] = new(
            Id: DataHeavyRoleId,
            DisplayName: "Data-heavy session",
            Summary: "Database/schema/migrations-focused operating mode. Selects blocks tagged data/database/schema/migration/ef-core/sqlite/postgres; projects as a model prompt by default.",
            IncludeTags: new[] { "data", "database", "schema", "migration", "ef-core", "sqlite", "postgres" },
            DefaultProjectionKind: IntentProjectionKind.ModelPrompt),

        [DocsOnlyRoleId] = new(
            Id: DocsOnlyRoleId,
            DisplayName: "Docs-only session",
            Summary: "Documentation-strategy session. Excludes reference blocks to reduce noise; projects as an AGENTS.md surface for a docs-focused agent.",
            IncludeTags: new[] { "docs", "documentation", "runbook", "operator-onboarding" },
            DefaultProjectionKind: IntentProjectionKind.AgentsMarkdown,
            DropReferenceBlocks: true),

        [OpsResponderRoleId] = new(
            Id: OpsResponderRoleId,
            DisplayName: "Ops responder session",
            Summary: "Event/operations-focused operating mode. Selects blocks tagged infrastructure, observability, deploy, security, event, runbook; projects as a tool prompt for fast handoff.",
            IncludeTags: new[] { "infra", "observability", "deploy", "security", "event", "runbook" },
            DefaultProjectionKind: IntentProjectionKind.ToolPrompt)
    };

    public static IReadOnlyCollection<SessionRoleProfile> All => ById.Values.ToArray();

    public static SessionRoleProfile? TryGet(string roleId) =>
        ById.TryGetValue(roleId, out var profile) ? profile : null;

    public static SessionRoleProfile Get(string roleId) =>
        TryGet(roleId) ?? throw new IntentProjectionException(
            $"Unknown session role id '{roleId}'. Known roles: {string.Join(", ", ById.Keys)}.");
}
