namespace DotNetAgents.Workflow.Checkpoints;

/// <summary>
/// Interface for serializing and deserializing workflow state.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IStateSerializer<TState> where TState : class
{
    /// <summary>
    /// Serializes a state object to a string.
    /// </summary>
    /// <param name="state">The state to serialize.</param>
    /// <returns>The serialized state as a string.</returns>
    string Serialize(TState state);

    /// <summary>
    /// Deserializes a string back to a state object.
    /// </summary>
    /// <param name="serializedState">The serialized state string.</param>
    /// <returns>The deserialized state object.</returns>
    TState Deserialize(string serializedState);
}
