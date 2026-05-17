using DotNetAgents.Abstractions.Intent;

namespace DotNetAgents.Voice.IntentClassification;

/// <summary>Voice-classified dispatch intent (canonical model: <see cref="AgentDispatchIntent"/>).</summary>
public sealed record Intent : AgentDispatchIntent;
