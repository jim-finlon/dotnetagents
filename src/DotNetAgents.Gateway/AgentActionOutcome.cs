namespace DotNetAgents.Gateway;

/// <summary>
/// Outcome receipt that links an LLM model invocation to the downstream action / artifact /
/// review / test / deployment / user feedback / production result that followed. Story 8bd0eb13
/// (P0 outcome scoring). Pairs with model-use telemetry (story b8a250ea — captures the model
/// decision; this one captures the result so model routing can optimize for quality + cost +
/// latency by domain).
/// </summary>
/// <remarks>
/// One outcome receipt = one (invocation × consequence) pair. Multiple receipts can attach to
/// the same <see cref="ModelInvocationId"/> when several downstream signals arrive over time
/// (completion + review + production telemetry from the same invocation). Producers SHOULD use
/// <see cref="OutcomeKind.Unknown"/> when the consequence hasn't materialized yet rather than
/// guessing — the AC requires unknown / delayed outcomes be represented explicitly.
/// </remarks>
/// <param name="OutcomeId">UUID stamped at receipt creation.</param>
/// <param name="ModelInvocationId">Stable id of the LLM invocation that produced the action being scored. Joined against the model-use telemetry pipeline.</param>
/// <param name="Kind">Kind of downstream signal carried in this receipt.</param>
/// <param name="LinkedArtifactType">Artifact category the receipt scores: work-item-completion, code-diff, review-verdict, test-run, deployment-run, user-acceptance, generated-document, ui-screenshot-eval, tool-execution.</param>
/// <param name="LinkedArtifactId">Identifier of the linked artifact (free-form — story id, PR ref, test run guid, etc.).</param>
/// <param name="AgentId">Stable agent id that made the LLM call (e.g. "agent-gamma", "agent-alpha", "autonomous-loop").</param>
/// <param name="DomainTag">Task domain bucket (e.g. "implementation", "review", "deploy"). Drives aggregated-quality lookups by domain.</param>
/// <param name="ModelId">Model id from the gateway catalog (e.g. "qwen3-32b", "gpt-4o").</param>
/// <param name="GatewayId">Gateway hosting <see cref="ModelId"/> (e.g. "local-gateway", "openai").</param>
/// <param name="WasLocalRoute">True when the routing decision picked a local model; false when it escalated external. Surfaced separately from <see cref="EscalationReason"/> so dashboards can aggregate on local-vs-external without parsing the reason string.</param>
/// <param name="EscalationReason">When <see cref="WasLocalRoute"/> is false, the structured reason the router escalated (e.g. "HighCognitionRequired", "LocalQualityBelowThreshold", "OperatorPolicy"). Null on local routes.</param>
/// <param name="Scores">Per-dimension scores for this outcome.</param>
/// <param name="DomainScores">Domain-specific score fields keyed by free-form domain key (e.g. "browser-vision-overlap", "code-style-conformance"). Empty when no domain scoring applies.</param>
/// <param name="EscalationJustificationQuality">When the route was an escalation, an operator-graded score (0.0–1.0) of how well the escalation reason was supported by the input + output. Null when not graded.</param>
/// <param name="ReworkCount">How many follow-up iterations / fixes were required after this invocation. 0 = converged on first try.</param>
/// <param name="OperatorSatisfaction">Optional operator-supplied 0.0–1.0 satisfaction rating; null when not collected.</param>
/// <param name="SafetyEvents">Discrete safety events (PII leak, secret exposure, hallucinated reference) detected post-hoc; empty when none.</param>
/// <param name="ObservedAtUtc">When the consequence materialized (completion time, test completion, etc.).</param>
/// <param name="ProducedAtUtc">When this receipt was recorded.</param>
/// <param name="Notes">Free-form operator note; useful when dashboards surface a low score and the operator wants context.</param>
public sealed record AgentActionOutcome(
    Guid OutcomeId,
    Guid ModelInvocationId,
    OutcomeKind Kind,
    OutcomeArtifactType LinkedArtifactType,
    string LinkedArtifactId,
    string AgentId,
    string DomainTag,
    string ModelId,
    string GatewayId,
    bool WasLocalRoute,
    string? EscalationReason,
    OutcomeScores Scores,
    IReadOnlyDictionary<string, double> DomainScores,
    double? EscalationJustificationQuality,
    int ReworkCount,
    double? OperatorSatisfaction,
    IReadOnlyList<string> SafetyEvents,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset ProducedAtUtc,
    string? Notes);

/// <summary>
/// Per-dimension scores for one outcome. Null fields = not applicable / not measured for this
/// outcome kind. Range is 0.0 (worst) to 1.0 (best) where the dimension is a score; latency/cost
/// are absolute values and have units in their field names.
/// </summary>
/// <param name="Correctness">0.0–1.0 — was the produced artifact factually + structurally correct?</param>
/// <param name="AcceptanceCriteriaSatisfaction">0.0–1.0 — fraction of the AC bullets the output satisfied.</param>
/// <param name="ReviewVerdict">Structured review outcome: "Approved" / "ChangesRequested" / "Blocked" / null when no review ran.</param>
/// <param name="TestPassRatio">Tests-passed / tests-total when a test run is linked; null otherwise.</param>
/// <param name="LatencyMs">Wall-clock latency of the invocation (or end-to-end of the action).</param>
/// <param name="CostUsd">Frontier USD spent on this invocation. 0 for local-route invocations unless the operator priced them.</param>
public sealed record OutcomeScores(
    double? Correctness,
    double? AcceptanceCriteriaSatisfaction,
    string? ReviewVerdict,
    double? TestPassRatio,
    long? LatencyMs,
    decimal? CostUsd);

public enum OutcomeKind
{
    /// <summary>The downstream signal arrived and is recorded.</summary>
    Recorded = 0,

    /// <summary>The action was attempted but the consequence is still pending (e.g. CI in flight, operator hasn't reviewed yet). Receipts in this state SHOULD be replaced or amended when the real outcome arrives.</summary>
    Pending = 1,

    /// <summary>The consequence is unknown — no signal arrived within the operator's tracking window. Distinguished from <see cref="Recorded"/> with low scores so dashboards don't conflate "no data" with "bad result".</summary>
    Unknown = 2,
}

public enum OutcomeArtifactType
{
    StoryCloseout = 0,
    CodeDiff = 1,
    ReviewVerdict = 2,
    TestRun = 3,
    DeploymentRun = 4,
    UserAcceptance = 5,
    GeneratedDocument = 6,
    UiScreenshotEval = 7,
    ToolExecution = 8,
}
