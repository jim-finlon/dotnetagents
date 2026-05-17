namespace DotNetAgents.Runtime;

public sealed class DefaultDelegationPolicy : IDelegationPolicy
{
    private static readonly string[] DeniedToolFragments =
    [
        "delegate",
        "credential",
        "secret",
        "memory_write",
        "write_memory",
        "shell_destructive",
        "filesystem_delete",
        "schedule",
        "cron",
        "deploy",
        "production"
    ];

    public DelegationPolicyDecision Evaluate(DelegatedAgentRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ParentSessionId))
            return DelegationPolicyDecision.Deny("Parent session id is required.");
        if (string.IsNullOrWhiteSpace(request.ParentActorId))
            return DelegationPolicyDecision.Deny("Parent actor id is required.");
        if (string.IsNullOrWhiteSpace(request.ChildActorId))
            return DelegationPolicyDecision.Deny("Child actor id is required.");
        if (string.IsNullOrWhiteSpace(request.Task))
            return DelegationPolicyDecision.Deny("Delegated task is required.");
        if (request.Timeout <= TimeSpan.Zero)
            return DelegationPolicyDecision.Deny("Delegated run timeout must be greater than zero.");
        if (request.MaxDepth < 1)
            return DelegationPolicyDecision.Deny("Delegated run max depth must be at least one.");
        if (request.CurrentDepth >= request.MaxDepth)
            return DelegationPolicyDecision.Deny("Delegated run max depth would be exceeded.");

        foreach (var toolName in request.AllowedTools.Concat(request.DeniedTools))
        {
            if (IsDefaultDenied(toolName))
                return DelegationPolicyDecision.Deny($"Tool '{toolName}' is denied by the default delegation policy.");
        }

        return DelegationPolicyDecision.Permit();
    }

    private static bool IsDefaultDenied(string toolName) =>
        DeniedToolFragments.Any(fragment => toolName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
