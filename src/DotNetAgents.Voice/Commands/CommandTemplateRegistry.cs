using System.Collections.Concurrent;

namespace DotNetAgents.Voice.Commands;

/// <summary>
/// Default implementation of <see cref="ICommandTemplateRegistry"/>.
/// </summary>
public class CommandTemplateRegistry : ICommandTemplateRegistry
{
    private readonly ConcurrentDictionary<string, ICommandTemplate> _templates = new();

    /// <inheritdoc />
    public void RegisterTemplate(ICommandTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (string.IsNullOrWhiteSpace(template.Name))
        {
            throw new ArgumentException("Template name cannot be null or empty", nameof(template));
        }

        _templates[template.Name] = template;
    }

    /// <inheritdoc />
    public ICommandTemplate? GetTemplate(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
        }

        _templates.TryGetValue(templateName, out var template);
        return template;
    }

    /// <inheritdoc />
    public IReadOnlyList<ICommandTemplate> GetAllTemplates()
    {
        return _templates.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public string RenderTemplate(string templateName, Dictionary<string, object> values)
    {
        var template = GetTemplate(templateName);
        if (template == null)
        {
            throw new InvalidOperationException($"Template '{templateName}' not found");
        }

        return template.Render(values);
    }
}
