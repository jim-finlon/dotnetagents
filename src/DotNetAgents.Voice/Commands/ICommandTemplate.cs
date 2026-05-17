namespace DotNetAgents.Voice.Commands;

/// <summary>
/// Interface for command templates that can render parameterized commands.
/// </summary>
public interface ICommandTemplate
{
    /// <summary>
    /// Gets the name of the template.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the template.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the template string with placeholders.
    /// </summary>
    string Template { get; }

    /// <summary>
    /// Gets the dictionary of parameter names to their types/descriptions.
    /// </summary>
    Dictionary<string, string> Parameters { get; }

    /// <summary>
    /// Renders the template with the provided values.
    /// </summary>
    /// <param name="values">The parameter values to substitute.</param>
    /// <returns>The rendered command string.</returns>
    string Render(Dictionary<string, object> values);
}
