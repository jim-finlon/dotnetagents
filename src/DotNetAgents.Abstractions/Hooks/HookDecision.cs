namespace DotNetAgents.Abstractions.Hooks;

/// <summary>
/// The verdict a hook returns at a lifecycle checkpoint. Discriminated record:
/// <see cref="AllowDecision"/> proceeds; <see cref="BlockDecision"/> halts the lifecycle event;
/// <see cref="RedactDecision"/> proceeds with replaced content. Hook chains short-circuit on
/// the first non-Allow verdict.
/// </summary>
public abstract record HookDecision
{
    private HookDecision() { }

    /// <summary>Allow the lifecycle event to proceed unchanged.</summary>
    public sealed record AllowDecision : HookDecision;

    /// <summary>
    /// Block the lifecycle event. Reasons are surfaced in evidence; the agent loop receives
    /// the block as either a thrown exception (PreToolUse, PreLlmCall) or a synthesized
    /// "blocked" result depending on the checkpoint.
    /// </summary>
    public sealed record BlockDecision(IReadOnlyList<string> Reasons, string? HookId = null) : HookDecision;

    /// <summary>
    /// Allow the lifecycle event to proceed but with replaced content (the redacted payload
    /// is what downstream code sees). Used at PostToolUse / PostLlmCall to scrub outputs;
    /// at PreToolUse / PreLlmCall to rewrite inputs.
    /// </summary>
    public sealed record RedactDecision(object? ReplacementContent, IReadOnlyList<string> Reasons, string? HookId = null) : HookDecision;

    /// <summary>Convenience: a singleton Allow decision.</summary>
    public static AllowDecision Allow { get; } = new();

    /// <summary>Convenience factory for Block decisions.</summary>
    public static BlockDecision BlockedBecause(IEnumerable<string> reasons, string? hookId = null) =>
        new(reasons.ToArray(), hookId);

    /// <summary>Convenience factory for single-reason Block.</summary>
    public static BlockDecision BlockedBecause(string reason, string? hookId = null) =>
        new(new[] { reason }, hookId);

    /// <summary>Convenience factory for Redact decisions.</summary>
    public static RedactDecision RedactedTo(
        object? replacementContent,
        IEnumerable<string> reasons,
        string? hookId = null) =>
        new(replacementContent, reasons.ToArray(), hookId);
}
