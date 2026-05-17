using DotNetAgents.Voice.IntentClassification;
using DotNetAgents.Voice.Orchestration;

namespace DotNetAgents.Voice.BehaviorTrees;

/// <summary>
/// Context object for command processing behavior tree operations.
/// </summary>
public class CommandProcessingContext
{
    /// <summary>
    /// Gets or sets the command state.
    /// </summary>
    public CommandState CommandState { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parsed intent.
    /// </summary>
    public Intent? Intent { get; set; }

    /// <summary>
    /// Gets or sets the processing strategy determined by the behavior tree.
    /// </summary>
    public CommandProcessingStrategy Strategy { get; set; } = CommandProcessingStrategy.Unknown;

    /// <summary>
    /// Gets or sets a value indicating whether clarification is needed.
    /// </summary>
    public bool NeedsClarification { get; set; }

    /// <summary>
    /// Gets or sets the clarification message (if needed).
    /// </summary>
    public string? ClarificationMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command should be executed directly.
    /// </summary>
    public bool ShouldExecuteDirectly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command requires a workflow.
    /// </summary>
    public bool RequiresWorkflow { get; set; }
}

/// <summary>
/// Represents the processing strategy for a command.
/// </summary>
public enum CommandProcessingStrategy
{
    /// <summary>
    /// Unknown strategy (not yet determined).
    /// </summary>
    Unknown,

    /// <summary>
    /// Simple command - execute directly.
    /// </summary>
    Simple,

    /// <summary>
    /// Multi-step command - execute workflow.
    /// </summary>
    MultiStep,

    /// <summary>
    /// Ambiguous command - request clarification.
    /// </summary>
    Ambiguous
}
