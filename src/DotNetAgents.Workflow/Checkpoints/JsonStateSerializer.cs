using System.Text.Json;

namespace DotNetAgents.Workflow.Checkpoints;

/// <summary>
/// JSON-based implementation of <see cref="IStateSerializer{TState}"/>.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class JsonStateSerializer<TState> : IStateSerializer<TState> where TState : class
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonStateSerializer{TState}"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options. If null, default options are used.</param>
    public JsonStateSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc/>
    public string Serialize(TState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        return JsonSerializer.Serialize(state, _options);
    }

    /// <inheritdoc/>
    public TState Deserialize(string serializedState)
    {
        if (string.IsNullOrWhiteSpace(serializedState))
            throw new ArgumentException("Serialized state cannot be null or whitespace.", nameof(serializedState));

        var result = JsonSerializer.Deserialize<TState>(serializedState, _options);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize state. Result is null.");
        }

        return result;
    }
}
