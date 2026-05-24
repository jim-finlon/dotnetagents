// SPDX-License-Identifier: Apache-2.0

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Enhanced workflow state inspector with visual viewer, history tracking, and modification support.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class EnhancedWorkflowStateInspector<TState> : WorkflowStateInspector<TState> where TState : class
{
    private readonly List<StateHistoryEntry<TState>> _history = new();
    private readonly ILogger<EnhancedWorkflowStateInspector<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedWorkflowStateInspector{TState}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public EnhancedWorkflowStateInspector(ILogger<EnhancedWorkflowStateInspector<TState>>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the history of state snapshots.
    /// </summary>
    public IReadOnlyList<StateHistoryEntry<TState>> History => _history.AsReadOnly();

    /// <summary>
    /// Captures a snapshot of the current state and adds it to history.
    /// </summary>
    /// <param name="state">The workflow state to capture.</param>
    /// <param name="context">Optional context describing when/why this snapshot was taken.</param>
    /// <returns>The state snapshot.</returns>
    public StateSnapshot<TState> CaptureSnapshot(TState state, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var snapshot = GetSnapshot(state);
        var entry = new StateHistoryEntry<TState>
        {
            Snapshot = snapshot,
            Context = context,
            Index = _history.Count
        };

        _history.Add(entry);
        _logger?.LogDebug(
            "Captured state snapshot #{Index} for state type '{StateType}'. Context: {Context}",
            entry.Index,
            snapshot.StateType,
            context ?? "No context");

        return snapshot;
    }

    /// <summary>
    /// Gets a visual representation of the state as JSON.
    /// </summary>
    /// <param name="state">The workflow state to visualize.</param>
    /// <param name="prettyPrint">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the state.</returns>
    public string GetVisualJson(TState state, bool prettyPrint = true)
    {
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = prettyPrint,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(state, options);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to serialize state to JSON. Falling back to summary.");
            return GetSummary(state);
        }
    }

    /// <summary>
    /// Gets a formatted visual representation of the state with property values.
    /// </summary>
    /// <param name="state">The workflow state to visualize.</param>
    /// <returns>A formatted string representation.</returns>
    public string GetVisualRepresentation(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var snapshot = GetSnapshot(state);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"═══════════════════════════════════════════════════════════");
        sb.AppendLine($"Workflow State: {snapshot.StateType}");
        sb.AppendLine($"Captured At: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"═══════════════════════════════════════════════════════════");
        sb.AppendLine();

        foreach (var (key, value) in snapshot.Properties.OrderBy(p => p.Key))
        {
            var valueStr = FormatValue(value);
            sb.AppendLine($"{key,-30} : {valueStr}");
        }

        sb.AppendLine($"═══════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    /// <summary>
    /// Modifies a property value in the state.
    /// </summary>
    /// <param name="state">The workflow state to modify.</param>
    /// <param name="propertyName">The name of the property to modify.</param>
    /// <param name="value">The new value.</param>
    /// <param name="captureHistory">Whether to capture a snapshot after modification.</param>
    /// <returns>True if the property was successfully modified; otherwise, false.</returns>
    public bool ModifyProperty(TState state, string propertyName, object? value, bool captureHistory = true)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var type = typeof(TState);
        var prop = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (prop == null || !prop.CanWrite)
        {
            _logger?.LogWarning("Property '{PropertyName}' not found or is read-only on type '{TypeName}'.", propertyName, type.Name);
            return false;
        }

        try
        {
            // Convert value to property type if needed
            object? convertedValue = value;
            if (value != null && prop.PropertyType != value.GetType())
            {
                var nullableType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                convertedValue = Convert.ChangeType(value, nullableType);
            }

            var oldValue = prop.GetValue(state);
            prop.SetValue(state, convertedValue);

            _logger?.LogInformation(
                "Modified property '{PropertyName}' from '{OldValue}' to '{NewValue}'.",
                propertyName,
                oldValue ?? "null",
                convertedValue ?? "null");

            if (captureHistory)
            {
                CaptureSnapshot(state, $"Property '{propertyName}' modified");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to modify property '{PropertyName}'.", propertyName);
            return false;
        }
    }

    /// <summary>
    /// Rolls back the state to a previous snapshot.
    /// </summary>
    /// <param name="state">The current state to roll back.</param>
    /// <param name="snapshotIndex">The index of the snapshot to roll back to. If null, rolls back to the previous snapshot.</param>
    /// <returns>True if rollback was successful; otherwise, false.</returns>
    public bool RollbackToSnapshot(TState state, int? snapshotIndex = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_history.Count == 0)
        {
            _logger?.LogWarning("No history available for rollback.");
            return false;
        }

        var targetIndex = snapshotIndex ?? (_history.Count - 2);
        if (targetIndex < 0 || targetIndex >= _history.Count)
        {
            _logger?.LogWarning("Invalid snapshot index {Index}. History count: {Count}.", targetIndex, _history.Count);
            return false;
        }

        var targetSnapshot = _history[targetIndex].Snapshot;

        try
        {
            // Copy properties from snapshot to current state
            var type = typeof(TState);
            foreach (var prop in type.GetProperties())
            {
                if (prop.CanWrite && targetSnapshot.Properties.TryGetValue(prop.Name, out var value))
                {
                    prop.SetValue(state, value);
                }
            }

            _logger?.LogInformation("Rolled back state to snapshot #{Index}.", targetIndex);
            CaptureSnapshot(state, $"Rolled back to snapshot #{targetIndex}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to rollback state to snapshot #{Index}.", targetIndex);
            return false;
        }
    }

    /// <summary>
    /// Clears the history.
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
        _logger?.LogInformation("State history cleared.");
    }

    /// <summary>
    /// Gets a diff between two state snapshots.
    /// </summary>
    /// <param name="snapshot1">The first snapshot.</param>
    /// <param name="snapshot2">The second snapshot.</param>
    /// <returns>A dictionary of changed properties with their old and new values.</returns>
    public Dictionary<string, PropertyChange> GetDiff(StateSnapshot<TState> snapshot1, StateSnapshot<TState> snapshot2)
    {
        ArgumentNullException.ThrowIfNull(snapshot1);
        ArgumentNullException.ThrowIfNull(snapshot2);

        var diff = new Dictionary<string, PropertyChange>();

        var allKeys = snapshot1.Properties.Keys.Union(snapshot2.Properties.Keys);

        foreach (var key in allKeys)
        {
            var value1 = snapshot1.Properties.TryGetValue(key, out var v1) ? v1 : null;
            var value2 = snapshot2.Properties.TryGetValue(key, out var v2) ? v2 : null;

            if (!Equals(value1, value2))
            {
                diff[key] = new PropertyChange
                {
                    PropertyName = key,
                    OldValue = value1,
                    NewValue = value2
                };
            }
        }

        return diff;
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string str)
        {
            if (str.Length > 100)
                return $"\"{str.Substring(0, 97)}...\" ({str.Length} chars)";
            return $"\"{str}\"";
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var items = enumerable.Cast<object>().Take(5).ToList();
            var count = enumerable.Cast<object>().Count();
            var itemsStr = string.Join(", ", items.Select(i => FormatValue(i)));
            return count > 5 ? $"[{itemsStr}, ...] ({count} items)" : $"[{itemsStr}]";
        }

        return value.ToString() ?? "null";
    }

    /// <summary>
    /// Represents a history entry for state snapshots.
    /// </summary>
    /// <typeparam name="THistoryState">The type of the workflow state.</typeparam>
    public class StateHistoryEntry<THistoryState> where THistoryState : class
    {
        /// <summary>
        /// Gets the state snapshot.
        /// </summary>
        public StateSnapshot<THistoryState> Snapshot { get; init; } = null!;

        /// <summary>
        /// Gets the optional context describing when/why this snapshot was taken.
        /// </summary>
        public string? Context { get; init; }

        /// <summary>
        /// Gets the index of this entry in the history.
        /// </summary>
        public int Index { get; init; }
    }

    /// <summary>
    /// Represents a change to a property between two states.
    /// </summary>
    public class PropertyChange
    {
        /// <summary>
        /// Gets the name of the property that changed.
        /// </summary>
        public string PropertyName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the old value.
        /// </summary>
        public object? OldValue { get; init; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public object? NewValue { get; init; }
    }
}
