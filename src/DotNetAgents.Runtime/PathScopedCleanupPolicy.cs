namespace DotNetAgents.Runtime;

public sealed class PathScopedCleanupPolicy : IEnvironmentCleanupPolicy
{
    public CleanupPolicyDecision Evaluate(ExecutionLease lease, string requestedPath)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedPath);

        var expected = Normalize(lease.RootPath);
        var requested = Normalize(requestedPath);
        if (!string.Equals(expected, requested, StringComparison.Ordinal))
        {
            return new CleanupPolicyDecision(
                false,
                $"Requested cleanup path '{requestedPath}' does not match lease root '{lease.RootPath}'.",
                [requestedPath]);
        }

        return new CleanupPolicyDecision(true, "Requested path matches the lease root.");
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').TrimEnd('/');
}
