namespace DotNetAgents.Abstractions.Tools;

/// <summary>
/// Registry for managing available tools.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Registers a tool in the registry.
    /// </summary>
    /// <param name="tool">The tool to register.</param>
    /// <exception cref="ArgumentException">Thrown when a tool with the same name is already registered.</exception>
    void Register(ITool tool);

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <returns>The tool if found, otherwise null.</returns>
    ITool? GetTool(string name);

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    /// <returns>A read-only list of all registered tools.</returns>
    IReadOnlyList<ITool> GetAllTools();

    /// <summary>
    /// Unregisters a tool from the registry.
    /// </summary>
    /// <param name="name">The name of the tool to unregister.</param>
    /// <returns>True if the tool was removed, false if it was not found.</returns>
    bool Unregister(string name);

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <returns>True if the tool is registered, otherwise false.</returns>
    bool IsRegistered(string name);
}
