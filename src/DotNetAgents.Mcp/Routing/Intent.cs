using DotNetAgents.Abstractions.Intent;

namespace DotNetAgents.Mcp.Routing;

/// <summary>MCP-routed dispatch intent (canonical model: <see cref="AgentDispatchIntent"/>).</summary>
public sealed record Intent : AgentDispatchIntent;
