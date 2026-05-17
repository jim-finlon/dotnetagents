namespace DotNetAgents.Abstractions.PublicSubstitutes.Evals;

/// <summary>Query shape for public eval-run lookup.</summary>
public sealed record PublicEvalRunQuery(
    string? SuiteName = null,
    bool? Completed = null,
    int? Limit = null);
