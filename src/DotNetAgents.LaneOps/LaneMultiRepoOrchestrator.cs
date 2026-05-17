namespace DotNetAgents.LaneOps;

/// <summary>
/// Formal multi-repository lane merge contract for autonomous worktrees. Story a06b5786.
/// </summary>
/// <remarks>
/// The orchestrator is intentionally policy-first: it computes and executes a deterministic,
/// idempotent sequence through an injected command runner. Host-specific shells, SDLC completion,
/// and git remotes stay outside this class so the contract can be tested without mutating repos.
/// </remarks>
public sealed class LaneMultiRepoOrchestrator
{
    private readonly ILaneMultiRepoCommandRunner _runner;

    public LaneMultiRepoOrchestrator(ILaneMultiRepoCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public async Task<LaneMultiRepoMergeResult> MergeAsync(
        LaneMultiRepoMergePlan plan,
        LaneMultiRepoMergeState? state = null,
        CancellationToken cancellationToken = default)
    {
        Validate(plan);

        var working = state ?? LaneMultiRepoMergeState.Empty;
        var events = new List<LaneMultiRepoStepEvent>();
        var steps = BuildSteps(plan);

        foreach (var step in steps)
        {
            if (working.CompletedStepIds.Contains(step.Id))
            {
                events.Add(LaneMultiRepoStepEvent.Skipped(step, "step already completed in supplied state"));
                continue;
            }

            var commandResult = await _runner.ExecuteAsync(step, plan, working, cancellationToken).ConfigureAwait(false);
            events.Add(commandResult.Succeeded
                ? LaneMultiRepoStepEvent.Completed(step, commandResult.Message)
                : LaneMultiRepoStepEvent.Failed(step, commandResult.Message));

            if (!commandResult.Succeeded)
            {
                return LaneMultiRepoMergeResult.Failed(
                    working with { FailedStep = step },
                    step,
                    events,
                    BuildRecoverableActions(plan, step));
            }

            working = working.MarkCompleted(step);
        }

        return LaneMultiRepoMergeResult.Completed(working, events);
    }

    public static IReadOnlyList<LaneMultiRepoStep> BuildSteps(LaneMultiRepoMergePlan plan)
    {
        Validate(plan);

        var steps = new List<LaneMultiRepoStep>();
        foreach (var leg in plan.RepositoryLegs.Where(static leg => leg.Kind == LaneRepositoryKind.Submodule && leg.HasLaneChanges))
        {
            steps.Add(new LaneMultiRepoStep(MultiRepoLaneStepKind.SubmoduleRebase, leg.RepositoryId, $"Rebase {leg.RepositoryId} lane onto {leg.MainBranch}."));
            steps.Add(new LaneMultiRepoStep(MultiRepoLaneStepKind.SubmodulePush, leg.RepositoryId, $"Push {leg.RepositoryId} lane branch {leg.LaneBranch}."));
            steps.Add(new LaneMultiRepoStep(MultiRepoLaneStepKind.SubmoduleFastForwardMain, leg.RepositoryId, $"Fast-forward {leg.RepositoryId} {leg.MainBranch} from {leg.LaneBranch}."));
        }

        var hasSubmoduleChanges = plan.RepositoryLegs.Any(static leg => leg.Kind == LaneRepositoryKind.Submodule && leg.HasLaneChanges);
        var parentLeg = plan.RepositoryLegs.FirstOrDefault(static leg => leg.Kind == LaneRepositoryKind.Parent);
        var parentHasChanges = parentLeg?.HasLaneChanges == true || hasSubmoduleChanges;

        if (hasSubmoduleChanges)
            steps.Add(new LaneMultiRepoStep(MultiRepoLaneStepKind.ParentPointerBump, parentLeg?.RepositoryId ?? plan.ParentRepositoryId, "Commit parent pointers after submodule main refs are reachable."));

        if (parentHasChanges)
            steps.Add(new LaneMultiRepoStep(MultiRepoLaneStepKind.ParentFastForwardMain, parentLeg?.RepositoryId ?? plan.ParentRepositoryId, $"Fast-forward parent {plan.ParentMainBranch} from {plan.ParentLaneBranch}."));

        steps.Add(new LaneMultiRepoStep(MultiRepoLaneStepKind.Closeout, plan.ParentRepositoryId, RenderCloseoutDescription(plan.CloseoutMode)));
        steps.Add(new LaneMultiRepoStep(MultiRepoLaneStepKind.Cleanup, plan.ParentRepositoryId, "Remove temporary worktree and lane branches after merge evidence is durable."));
        return steps;
    }

    private static string RenderCloseoutDescription(LaneCloseoutMode mode) =>
        mode switch
        {
            LaneCloseoutMode.RecordStoryCloseoutAtomic => "Record SDLC completion atomically with merge completion.",
            LaneCloseoutMode.CreateExplicitFollowUpStory => "Create an explicit follow-up story instead of marking delivery Done.",
            _ => "Record completion state.",
        };

    private static IReadOnlyList<LaneRecoveryAction> BuildRecoverableActions(LaneMultiRepoMergePlan plan, LaneMultiRepoStep failedStep)
    {
        var actions = new List<LaneRecoveryAction>
        {
            new("inspect-mid-state", $"Inspect failed step {failedStep.Kind} for repository {failedStep.RepositoryId}.", MutatesState: false),
        };

        switch (failedStep.Kind)
        {
            case MultiRepoLaneStepKind.SubmodulePush:
                actions.Add(new("retry-submodule-push", "After credentials/network recovery, retry the submodule push. Prior completed steps are idempotent skips.", MutatesState: false));
                break;
            case MultiRepoLaneStepKind.ParentPointerBump:
                actions.Add(new("reset-parent-pointer-bump", "Reset unpushed parent pointer changes, verify submodule main refs are reachable, then replay from parent pointer bump.", MutatesState: true));
                break;
            case MultiRepoLaneStepKind.ParentFastForwardMain:
                actions.Add(new("rollback-parent-main", $"Reset parent {plan.ParentMainBranch} to its pre-merge ref if it moved locally but push failed.", MutatesState: true));
                actions.Add(new("record-follow-up", "If parent main changed remotely, create a follow-up story and attach the partial merge state instead of hiding the mid-failure.", MutatesState: false));
                break;
            case MultiRepoLaneStepKind.Closeout:
                actions.Add(new("record-completion-or-follow-up", "Retry record_story_completion, or create an explicit follow-up story when the completion gate is unsatisfied.", MutatesState: false));
                break;
            case MultiRepoLaneStepKind.Cleanup:
                actions.Add(new("retry-cleanup", "Retry cleanup only after merge and completion evidence are durable.", MutatesState: true));
                break;
            default:
                actions.Add(new("replay-from-failed-step", "Fix the underlying git state, then replay with the returned completed-step state.", MutatesState: false));
                break;
        }

        return actions;
    }

    private static void Validate(LaneMultiRepoMergePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.ParentRepositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.ParentLaneBranch);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.ParentMainBranch);
        if (plan.RepositoryLegs.Count == 0)
            throw new ArgumentException("At least one repository leg is required.", nameof(plan));
        if (plan.RepositoryLegs.Count(static leg => leg.Kind == LaneRepositoryKind.Parent) > 1)
            throw new ArgumentException("At most one parent repository leg is allowed.", nameof(plan));

        foreach (var leg in plan.RepositoryLegs)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(leg.RepositoryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(leg.LaneBranch);
            ArgumentException.ThrowIfNullOrWhiteSpace(leg.MainBranch);
        }
    }
}

public interface ILaneMultiRepoCommandRunner
{
    Task<LaneMultiRepoCommandResult> ExecuteAsync(
        LaneMultiRepoStep mergeStep,
        LaneMultiRepoMergePlan plan,
        LaneMultiRepoMergeState state,
        CancellationToken cancellationToken = default);
}

public sealed record LaneMultiRepoMergePlan(
    string ParentRepositoryId,
    string ParentLaneBranch,
    string ParentMainBranch,
    LaneCloseoutMode CloseoutMode,
    IReadOnlyList<LaneRepositoryLeg> RepositoryLegs);

public sealed record LaneRepositoryLeg(
    string RepositoryId,
    LaneRepositoryKind Kind,
    string LaneBranch,
    string MainBranch,
    bool HasLaneChanges);

public enum LaneRepositoryKind
{
    Parent = 0,
    Submodule = 1,
    ExternalRepository = 2,
}

public enum LaneCloseoutMode
{
    RecordStoryCloseoutAtomic = 0,
    CreateExplicitFollowUpStory = 1,
}

public enum MultiRepoLaneStepKind
{
    SubmoduleRebase = 0,
    SubmodulePush = 1,
    SubmoduleFastForwardMain = 2,
    ParentPointerBump = 3,
    ParentFastForwardMain = 4,
    Closeout = 5,
    Cleanup = 6,
}

public sealed record LaneMultiRepoStep(
    MultiRepoLaneStepKind Kind,
    string RepositoryId,
    string Description)
{
    public string Id => $"{RepositoryId}:{Kind}";
}

public sealed record LaneMultiRepoCommandResult(bool Succeeded, string Message)
{
    public static LaneMultiRepoCommandResult Success(string message = "ok") => new(true, message);
    public static LaneMultiRepoCommandResult Failure(string message) => new(false, message);
}

public sealed record LaneMultiRepoMergeState(
    IReadOnlySet<string> CompletedStepIds,
    LaneMultiRepoStep? LastCompletedStep,
    LaneMultiRepoStep? FailedStep)
{
    public static LaneMultiRepoMergeState Empty { get; } =
        new(new HashSet<string>(StringComparer.Ordinal), null, null);

    public LaneMultiRepoMergeState MarkCompleted(LaneMultiRepoStep step)
    {
        var completed = new HashSet<string>(CompletedStepIds, StringComparer.Ordinal)
        {
            step.Id,
        };
        return new LaneMultiRepoMergeState(completed, step, null);
    }
}

public sealed record LaneMultiRepoStepEvent(
    LaneMultiRepoStep Step,
    LaneMultiRepoStepOutcome Outcome,
    string Message)
{
    public static LaneMultiRepoStepEvent Completed(LaneMultiRepoStep step, string message) =>
        new(step, LaneMultiRepoStepOutcome.Completed, message);

    public static LaneMultiRepoStepEvent Skipped(LaneMultiRepoStep step, string message) =>
        new(step, LaneMultiRepoStepOutcome.Skipped, message);

    public static LaneMultiRepoStepEvent Failed(LaneMultiRepoStep step, string message) =>
        new(step, LaneMultiRepoStepOutcome.Failed, message);
}

public enum LaneMultiRepoStepOutcome
{
    Completed = 0,
    Skipped = 1,
    Failed = 2,
}

public sealed record LaneRecoveryAction(string Code, string Description, bool MutatesState);

public sealed record LaneMultiRepoMergeResult(
    bool Succeeded,
    LaneMultiRepoMergeState State,
    LaneMultiRepoStep? FailedStep,
    IReadOnlyList<LaneMultiRepoStepEvent> Events,
    IReadOnlyList<LaneRecoveryAction> RecoverableActions)
{
    public static LaneMultiRepoMergeResult Completed(
        LaneMultiRepoMergeState state,
        IReadOnlyList<LaneMultiRepoStepEvent> events) =>
        new(true, state, null, events, Array.Empty<LaneRecoveryAction>());

    public static LaneMultiRepoMergeResult Failed(
        LaneMultiRepoMergeState state,
        LaneMultiRepoStep failedStep,
        IReadOnlyList<LaneMultiRepoStepEvent> events,
        IReadOnlyList<LaneRecoveryAction> actions) =>
        new(false, state, failedStep, events, actions);
}
