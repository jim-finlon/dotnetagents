namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Types of input that can be requested from humans in workflows.
/// </summary>
public enum InputType
{
    /// <summary>
    /// Text input (string).
    /// </summary>
    Text,

    /// <summary>
    /// Numeric input (integer or decimal).
    /// </summary>
    Number,

    /// <summary>
    /// Boolean input (yes/no, true/false).
    /// </summary>
    Boolean,

    /// <summary>
    /// Date input.
    /// </summary>
    Date,

    /// <summary>
    /// Date and time input.
    /// </summary>
    DateTime,

    /// <summary>
    /// File upload input.
    /// </summary>
    File,

    /// <summary>
    /// Email address input.
    /// </summary>
    Email,

    /// <summary>
    /// URL input.
    /// </summary>
    Url,

    /// <summary>
    /// Multi-line text input.
    /// </summary>
    TextArea,

    /// <summary>
    /// JSON input.
    /// </summary>
    Json
}
