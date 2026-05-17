using System.Text.Json;
using DotNetAgents.A2A;
using DotNetAgents.Mcp.Models;
using DotNetAgents.Mcp.Server;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.A2A.Server;

/// <summary>
/// A2A facade over an existing MCP tool provider. Skills map 1:1 to allowed MCP tools so
/// services can publish an agent-side wire without re-implementing their business logic.
/// </summary>
public class McpToolBackedA2AAgent : IA2AAgent
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _serviceName;
    private readonly string _agentName;
    private readonly string _description;
    private readonly string _version;
    private readonly HashSet<string> _allowedTools;

    public McpToolBackedA2AAgent(
        IServiceScopeFactory scopeFactory,
        string serviceName,
        string agentName,
        string description,
        IEnumerable<string> allowedTools,
        string version = "1.0")
    {
        _scopeFactory = scopeFactory;
        _serviceName = serviceName;
        _agentName = agentName;
        _description = description;
        _version = version;
        _allowedTools = new HashSet<string>(allowedTools.Where(name => !string.IsNullOrWhiteSpace(name)), StringComparer.Ordinal);
    }

    public AgentCard GetAgentCard()
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IMcpToolProvider>();
        var tools = provider.GetToolsAsync(_serviceName).GetAwaiter().GetResult();
        return new AgentCard
        {
            Name = _agentName,
            Description = _description,
            Version = _version,
            SupportedModes = ["task", "stream"],
            Skills = tools
                .Where(tool => _allowedTools.Contains(tool.Name))
                .OrderBy(tool => tool.Name, StringComparer.Ordinal)
                .Select(ToSkill)
                .ToList()
        };
    }

    public async Task<A2AResponse> HandleTaskAsync(A2ATask task, CancellationToken cancellationToken = default)
    {
        if (!_allowedTools.Contains(task.Skill))
        {
            return new A2AResponse
            {
                TaskId = task.Id,
                Success = false,
                Error = $"Unsupported A2A skill '{task.Skill}'."
            };
        }

        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IMcpToolProvider>();
        var response = await provider.CallToolAsync(task.Skill, ToDictionary(task.Input), cancellationToken).ConfigureAwait(false);
        return new A2AResponse
        {
            TaskId = task.Id,
            Success = response.Success,
            Output = response,
            Error = response.Error
        };
    }

    public async IAsyncEnumerable<A2AEvent> StreamTaskAsync(
        A2ATask task,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new A2AEvent { TaskId = task.Id, EventType = "started", Payload = new { task.Skill } };
        var response = await HandleTaskAsync(task, cancellationToken).ConfigureAwait(false);
        yield return new A2AEvent { TaskId = task.Id, EventType = response.Success ? "completed" : "error", Payload = response };
    }

    private static Skill ToSkill(McpToolDefinition tool) => new()
    {
        Name = tool.Name,
        Description = tool.Description,
        InputSchema = tool.InputSchema
    };

    private static IReadOnlyDictionary<string, object> ToDictionary(object? input)
    {
        if (input is IReadOnlyDictionary<string, object> readOnly)
        {
            return readOnly;
        }

        if (input is Dictionary<string, object> dictionary)
        {
            return dictionary;
        }

        if (input is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            return jsonElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => (object)property.Value, StringComparer.Ordinal);
        }

        return new Dictionary<string, object>(StringComparer.Ordinal);
    }
}
