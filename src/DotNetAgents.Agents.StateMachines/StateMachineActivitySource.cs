using System.Diagnostics;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Activity source for state machine tracing.
/// </summary>
public static class StateMachineActivitySource
{
    /// <summary>
    /// The activity source name for state machine operations.
    /// </summary>
    public const string SourceName = "DotNetAgents.Agents.StateMachines";

    /// <summary>
    /// The activity source instance.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName);

    /// <summary>
    /// Activity name for state transitions.
    /// </summary>
    public const string TransitionActivityName = "StateMachine.Transition";

    /// <summary>
    /// Activity name for state entry.
    /// </summary>
    public const string EntryActivityName = "StateMachine.StateEntry";

    /// <summary>
    /// Activity name for state exit.
    /// </summary>
    public const string ExitActivityName = "StateMachine.StateExit";
}
