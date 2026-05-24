// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Configuration;

/// <summary>
/// Main configuration class for DotNetAgents library.
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Gets or sets the default LLM provider name.
    /// </summary>
    public string DefaultLLMProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default LLM model name.
    /// </summary>
    public string DefaultLLMModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default embedding model name.
    /// </summary>
    public string DefaultEmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default vector store name.
    /// </summary>
    public string DefaultVectorStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets LLM-specific configuration options.
    /// </summary>
    public Dictionary<string, LLMProviderConfiguration> LLMProviders { get; set; } = new();

    /// <summary>
    /// Gets or sets vector store-specific configuration options.
    /// </summary>
    public Dictionary<string, VectorStoreConfiguration> VectorStores { get; set; } = new();

    /// <summary>
    /// Gets or sets embedding model-specific configuration options.
    /// </summary>
    public Dictionary<string, EmbeddingModelConfiguration> EmbeddingModels { get; set; } = new();

    /// <summary>
    /// Gets or sets global execution options.
    /// </summary>
    public ExecutionOptions ExecutionOptions { get; set; } = new();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="AgentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DefaultLLMProvider))
        {
            errors.Add("DefaultLLMProvider is required.");
        }

        if (string.IsNullOrWhiteSpace(DefaultLLMModel))
        {
            errors.Add("DefaultLLMModel is required.");
        }

        if (!LLMProviders.ContainsKey(DefaultLLMProvider))
        {
            errors.Add($"DefaultLLMProvider '{DefaultLLMProvider}' is not configured in LLMProviders.");
        }

        if (errors.Count > 0)
        {
            throw new AgentException(
                $"Configuration validation failed: {string.Join(" ", errors)}",
                ErrorCategory.ConfigurationError);
        }
    }
}

/// <summary>
/// Configuration for an LLM provider.
/// </summary>
public class LLMProviderConfiguration
{
    /// <summary>
    /// Gets or sets the provider type (e.g., "OpenAI", "Azure", "Anthropic").
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API endpoint URL.
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the API key secret name (for use with ISecretsProvider).
    /// </summary>
    public string? ApiKeySecretName { get; set; }

    /// <summary>
    /// Gets or sets additional provider-specific configuration.
    /// </summary>
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();
}

/// <summary>
/// Configuration for a vector store.
/// </summary>
public class VectorStoreConfiguration
{
    /// <summary>
    /// Gets or sets the vector store type (e.g., "Pinecone", "Weaviate", "InMemory").
    /// </summary>
    public string StoreType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string or endpoint URL.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets additional store-specific configuration.
    /// </summary>
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();
}

/// <summary>
/// Configuration for an embedding model.
/// </summary>
public class EmbeddingModelConfiguration
{
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding dimension.
    /// </summary>
    public int Dimension { get; set; }

    /// <summary>
    /// Gets or sets additional model-specific configuration.
    /// </summary>
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();
}

/// <summary>
/// Global execution options.
/// </summary>
public class ExecutionOptions
{
    /// <summary>
    /// Gets or sets the default timeout for operations.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of retries for failed operations.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable request/response logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable cost tracking.
    /// </summary>
    public bool EnableCostTracking { get; set; } = true;
}
