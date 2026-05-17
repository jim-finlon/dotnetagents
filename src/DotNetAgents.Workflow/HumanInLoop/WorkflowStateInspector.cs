namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Provides functionality to inspect workflow state.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class WorkflowStateInspector<TState> where TState : class
{
    /// <summary>
    /// Gets a snapshot of the workflow state.
    /// </summary>
    /// <param name="state">The workflow state to inspect.</param>
    /// <returns>A state snapshot containing metadata about the state.</returns>
    public StateSnapshot<TState> GetSnapshot(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new StateSnapshot<TState>
        {
            State = state,
            CapturedAt = DateTimeOffset.UtcNow,
            StateType = typeof(TState).Name,
            Properties = GetStateProperties(state)
        };
    }

    /// <summary>
    /// Gets a summary of the workflow state.
    /// </summary>
    /// <param name="state">The workflow state to summarize.</param>
    /// <returns>A string summary of the state.</returns>
    public string GetSummary(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var properties = GetStateProperties(state);
        var summary = $"State Type: {typeof(TState).Name}\n";
        summary += $"Properties: {properties.Count}\n";

        foreach (var prop in properties.Take(10))
        {
            var value = prop.Value?.ToString() ?? "null";
            if (value.Length > 100)
            {
                value = value.Substring(0, 97) + "...";
            }

            summary += $"  {prop.Key}: {value}\n";
        }

        if (properties.Count > 10)
        {
            summary += $"  ... and {properties.Count - 10} more properties\n";
        }

        return summary;
    }

    private static Dictionary<string, object?> GetStateProperties(TState state)
    {
        var properties = new Dictionary<string, object?>();
        var type = typeof(TState);

        foreach (var prop in type.GetProperties())
        {
            try
            {
                var value = prop.GetValue(state);
                properties[prop.Name] = value;
            }
            catch
            {
                // Skip properties that can't be read
            }
        }

        return properties;
    }

    /// <summary>
    /// Represents a snapshot of workflow state.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    public class StateSnapshot<TState> where TState : class
    {
        /// <summary>
        /// Gets the workflow state.
        /// </summary>
        public TState State { get; init; } = null!;

        /// <summary>
        /// Gets the timestamp when the snapshot was captured.
        /// </summary>
        public DateTimeOffset CapturedAt { get; init; }

        /// <summary>
        /// Gets the type name of the state.
        /// </summary>
        public string StateType { get; init; } = string.Empty;

        /// <summary>
        /// Gets the properties of the state.
        /// </summary>
        public Dictionary<string, object?> Properties { get; init; } = new();
    }
}
