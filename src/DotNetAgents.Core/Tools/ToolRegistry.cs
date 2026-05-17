using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Core.Tools;

/// <summary>
/// Default implementation of <see cref="IToolRegistry"/>.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void Register(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (_tools.ContainsKey(tool.Name))
        {
            throw new ArgumentException($"A tool with the name '{tool.Name}' is already registered.", nameof(tool));
        }

        _tools[tool.Name] = tool;
    }

    /// <inheritdoc/>
    public ITool? GetTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace.", nameof(name));

        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITool> GetAllTools() => _tools.Values.ToList().AsReadOnly();

    /// <inheritdoc/>
    public bool Unregister(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace.", nameof(name));

        return _tools.Remove(name);
    }

    /// <inheritdoc/>
    public bool IsRegistered(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _tools.ContainsKey(name);
    }
}
