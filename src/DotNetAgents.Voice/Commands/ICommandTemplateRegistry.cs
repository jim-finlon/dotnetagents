namespace DotNetAgents.Voice.Commands;

/// <summary>
/// Interface for registering and retrieving command templates.
/// </summary>
public interface ICommandTemplateRegistry
{
    /// <summary>
    /// Registers a command template.
    /// </summary>
    /// <param name="template">The template to register.</param>
    void RegisterTemplate(ICommandTemplate template);

    /// <summary>
    /// Gets a template by name.
    /// </summary>
    /// <param name="templateName">The template name.</param>
    /// <returns>The template, or null if not found.</returns>
    ICommandTemplate? GetTemplate(string templateName);

    /// <summary>
    /// Gets all registered templates.
    /// </summary>
    /// <returns>The list of all templates.</returns>
    IReadOnlyList<ICommandTemplate> GetAllTemplates();

    /// <summary>
    /// Renders a template by name with the provided values.
    /// </summary>
    /// <param name="templateName">The template name.</param>
    /// <param name="values">The parameter values.</param>
    /// <returns>The rendered command string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found.</exception>
    string RenderTemplate(string templateName, Dictionary<string, object> values);
}
