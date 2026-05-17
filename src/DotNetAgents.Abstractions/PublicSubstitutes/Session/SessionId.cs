namespace DotNetAgents.Abstractions.PublicSubstitutes.Session;

/// <summary>Public-safe session identifier used by public example agents.</summary>
public readonly record struct SessionId(string Value)
{
    public override string ToString() => Value;
}
