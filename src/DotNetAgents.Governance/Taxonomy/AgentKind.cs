namespace DotNetAgents.Governance.Taxonomy;

/// <summary>
/// First-class distinction between the two agent orchestration shapes.
/// Runtime dispatch differs: conversational agents hold state across turns; task-based
/// agents ingest a request, produce a result, and are torn down.
/// </summary>
public enum AgentKind
{
    /// <summary>Long-lived sessions, multi-turn context, user-driven (JARVIS, tutors, editorial chat).</summary>
    Conversational = 0,

    /// <summary>Single-shot, input → output (code-review agents, test runners, summarizers).</summary>
    TaskBased = 1
}
