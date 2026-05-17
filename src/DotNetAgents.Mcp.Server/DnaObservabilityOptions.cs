namespace DotNetAgents.Mcp.Server;

public sealed class DnaObservabilityOptions
{
    public string? DnaRepositoryRoot { get; set; }
    public string? OutboxRoot { get; set; }
    public string? DefaultEnvironment { get; set; }
    public string? DefaultActorType { get; set; }
    public string? DefaultActorId { get; set; }
    public string? DefaultActorDisplayName { get; set; }
    public string? DefaultWorktreePath { get; set; }
    public string? DefaultBranch { get; set; }
    public string? DefaultCommitSha { get; set; }
}
