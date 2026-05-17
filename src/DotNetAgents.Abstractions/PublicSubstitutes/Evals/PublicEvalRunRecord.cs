namespace DotNetAgents.Abstractions.PublicSubstitutes.Evals;

/// <summary>Public eval-run projection with flattened case results.</summary>
public sealed record PublicEvalRunRecord(
    PublicEvalRunHandle Handle,
    PublicEvalRunRequest Request,
    IReadOnlyList<PublicEvalCaseResult> Cases,
    PublicEvalRunSummary? Summary,
    bool Completed);
