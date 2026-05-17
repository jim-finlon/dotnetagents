namespace DotNetAgents.Governance.Identity;

/// <summary>
/// Well-known scope names shared across DNA services. Services may add service-specific
/// scopes (e.g. "sdlc.story.close") — these are the cross-cutting ones.
/// </summary>
public static class InvokerScopes
{
    public const string PlatformAdmin = "platform.admin";
    public const string AgentAuthor = "agent.author";
    public const string AgentPublish = "agent.publish";
    public const string AgentInvoke = "agent.invoke";
    public const string ConnectorGrant = "connector.grant";
}
