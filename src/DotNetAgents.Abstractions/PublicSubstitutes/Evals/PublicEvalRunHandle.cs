namespace DotNetAgents.Abstractions.PublicSubstitutes.Evals;

/// <summary>Stable public eval-run identifier.</summary>
public readonly record struct PublicEvalRunHandle(string Value)
{
    public override string ToString() => Value;
}
