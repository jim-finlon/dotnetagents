namespace DotNetAgents.Workflow.Session.Bootstrap;

/// <summary>
/// Bootstrap format options.
/// </summary>
public enum BootstrapFormat
{
    /// <summary>
    /// JSON format (default) - structured data.
    /// </summary>
    Json = 0,

    /// <summary>
    /// .cursorrules markdown format - for Cursor AI rules files.
    /// </summary>
    CursorRules = 1,

    /// <summary>
    /// agent.md markdown format - for general AI agent instructions.
    /// </summary>
    Agent = 2
}
