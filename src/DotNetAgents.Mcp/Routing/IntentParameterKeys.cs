namespace DotNetAgents.Mcp.Routing;

/// <summary>
/// Well-known keys for <see cref="Intent.Parameters"/> passed into MCP tool calls
/// (serialized to <see cref="Models.McpToolCallRequest.Arguments"/>).
/// </summary>
/// <remarks>
/// Voice hosts may add knowledge-memory keys on the same dictionary after recall:
/// <c>memoryPattern</c>, <c>memoryResolution</c>, <c>memoryScore</c>.
/// </remarks>
public static class IntentParameterKeys
{
    /// <summary>Optional long-term user memory (preferences, about-me) injected by the voice execute path when present.</summary>
    public const string JarvisUserMemory = "jarvisUserMemory";
}
