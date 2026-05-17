using System.Diagnostics;

namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Shared <see cref="ActivitySource"/> for DotNetAgents GenAI spans. Backends listening for
/// <see cref="Name"/> get the full set of agent/LLM/tool/framework/routing-decision spans
/// emitted with OTEL GenAI semantic-convention attribute names.
/// </summary>
public static class GenAiActivitySource
{
    /// <summary>The ActivitySource name. Backends subscribe to this string to capture DotNetAgents GenAI spans.</summary>
    public const string Name = "DotNetAgents.GenAi";

    /// <summary>Framework version stamped on each emitted span via <see cref="GenAiAttributeNames.FrameworkVersion"/>.</summary>
    public static string FrameworkVersion { get; set; } = typeof(GenAiActivitySource).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    /// <summary>The ActivitySource instance. Use this directly for full control or use the helpers in <see cref="GenAiActivities"/>.</summary>
    public static readonly ActivitySource ActivitySource = new(Name, FrameworkVersion);
}
