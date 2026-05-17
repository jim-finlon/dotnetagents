namespace DotNetAgents.LaneOps;

/// <summary>
/// First-class runner taxonomy per AUTONOMOUS-AGENT-OPERATING-MODEL-PLAN.md §4. Story b6909293.
/// Additive-only — never reorder; downstream lease records persist by name.
/// </summary>
public enum RunnerClass
{
    /// <summary>Unspecified placeholder. Routing refuses to match against this value.</summary>
    Unspecified = 0,

    /// <summary>
    /// Operator-supervised local execution on the workstation. Only admissible when the
    /// operator explicitly allows local execution (autonomy tier supervised_local).
    /// </summary>
    LocalLightweight = 1,

    /// <summary>Disposable VM-backed worker for compile/test/integration work.</summary>
    CodingVm = 2,

    /// <summary>
    /// Tightly isolated for remote-privileged, security-relevant, or infrastructure-touching
    /// work. Required for any workload class flagged remote_privileged.
    /// </summary>
    PrivilegedLab = 3,

    /// <summary>Page-automation work (Playwright/Selenium against external surfaces).</summary>
    BrowserRunner = 4,

    /// <summary>Narrative / specification-only work; cannot run code.</summary>
    DocsSpecRunner = 5,

    /// <summary>Disposable K3s pod-backed coding worker for explicitly pod-eligible workloads.</summary>
    K3sCodingWorker = 6,
}

/// <summary>
/// Closed vocabulary of workload classes the routing policy understands. Mirrors the
/// strings used by AsyncCodingWorkRequestRecord.WorkloadClass in PMA.
/// </summary>
public static class WorkloadClasses
{
    public const string ContractSpec = "contract_spec";
    public const string RepoRefactor = "repo_refactor";
    public const string IntegrationChange = "integration_change";
    public const string RemotePrivileged = "remote_privileged";
    public const string ToolingExperiment = "tooling_experiment";
    public const string DocsSpec = "docs_spec";
    public const string BrowserAutomation = "browser_automation";
    public const string SecurityWork = "security_work";
}

/// <summary>
/// Closed vocabulary of autonomy tiers the routing policy understands.
/// </summary>
public static class AutonomyTiers
{
    /// <summary>Workstation-supervised; operator approves each lane action.</summary>
    public const string SupervisedLocal = "supervised_local";

    /// <summary>Default async tier; supervisor approves checkpoints, runner executes between.</summary>
    public const string GovernedAsync = "governed_async";

    /// <summary>Stricter async tier reserved for high-blast or compliance-relevant work.</summary>
    public const string StrictAsync = "strict_async";
}
