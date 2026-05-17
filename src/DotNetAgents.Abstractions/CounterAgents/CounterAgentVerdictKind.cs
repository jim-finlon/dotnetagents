namespace DotNetAgents.Abstractions.CounterAgents;

/// <summary>
/// The kind of verdict a counter-agent can return for a proposed action.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><see cref="Approve"/>: counter-agent has no concerns; action proceeds.</description></item>
///   <item><description><see cref="Concern"/>: counter-agent has concerns the operator should see; action proceeds with concerns attached as metadata. Used for "I noticed something but it's not blocking."</description></item>
///   <item><description><see cref="Block"/>: counter-agent denies the action; middleware halts execution and surfaces reasons. Operator override path always available.</description></item>
/// </list>
/// </remarks>
public enum CounterAgentVerdictKind
{
    /// <summary>No concerns; action proceeds without annotation.</summary>
    Approve = 0,

    /// <summary>Concerns attached but action proceeds.</summary>
    Concern = 1,

    /// <summary>Action denied; halt execution.</summary>
    Block = 2,
}
