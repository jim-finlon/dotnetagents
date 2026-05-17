namespace DotNetAgents.Abstractions.Retrieval;

/// <summary>
/// Factory interface for creating vector store instances.
/// </summary>
public interface IVectorStoreFactory
{
    /// <summary>
    /// Creates a vector store instance for the specified store name.
    /// </summary>
    /// <param name="storeName">The name of the vector store (e.g., "Pinecone", "InMemory").</param>
    /// <returns>A vector store instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the store name is not supported.</exception>
    IVectorStore Create(string storeName);

    /// <summary>
    /// Creates a vector store instance from configuration.
    /// </summary>
    /// <param name="configurationKey">The configuration key to use.</param>
    /// <returns>A vector store instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the configuration key is not found.</exception>
    IVectorStore CreateFromConfiguration(string configurationKey);

    /// <summary>
    /// Gets a list of available vector store names.
    /// </summary>
    /// <returns>A list of store names.</returns>
    IReadOnlyList<string> GetAvailableStores();
}
