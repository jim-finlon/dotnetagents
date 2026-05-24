// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace DotNetAgents.Configuration;

/// <summary>
/// Fluent builder for configuring DotNetAgents.
/// </summary>
public class ConfigurationBuilder
{
    private readonly AgentConfiguration _configuration = new();

    /// <summary>
    /// Sets the default LLM provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder WithDefaultLLMProvider(string providerName)
    {
        _configuration.DefaultLLMProvider = providerName ?? throw new ArgumentNullException(nameof(providerName));
        return this;
    }

    /// <summary>
    /// Sets the default LLM model.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder WithDefaultLLMModel(string modelName)
    {
        _configuration.DefaultLLMModel = modelName ?? throw new ArgumentNullException(nameof(modelName));
        return this;
    }

    /// <summary>
    /// Adds an LLM provider configuration.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="configure">Action to configure the provider.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder AddLLMProvider(string providerName, Action<LLMProviderConfiguration> configure)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or whitespace.", nameof(providerName));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var providerConfig = new LLMProviderConfiguration();
        configure(providerConfig);
        _configuration.LLMProviders[providerName] = providerConfig;
        return this;
    }

    /// <summary>
    /// Adds a vector store configuration.
    /// </summary>
    /// <param name="storeName">The store name.</param>
    /// <param name="configure">Action to configure the store.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder AddVectorStore(string storeName, Action<VectorStoreConfiguration> configure)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new ArgumentException("Store name cannot be null or whitespace.", nameof(storeName));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var storeConfig = new VectorStoreConfiguration();
        configure(storeConfig);
        _configuration.VectorStores[storeName] = storeConfig;
        return this;
    }

    /// <summary>
    /// Adds an embedding model configuration.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    /// <param name="configure">Action to configure the model.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder AddEmbeddingModel(string modelName, Action<EmbeddingModelConfiguration> configure)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be null or whitespace.", nameof(modelName));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var modelConfig = new EmbeddingModelConfiguration();
        configure(modelConfig);
        _configuration.EmbeddingModels[modelName] = modelConfig;
        return this;
    }

    /// <summary>
    /// Configures execution options.
    /// </summary>
    /// <param name="configure">Action to configure execution options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder WithExecutionOptions(Action<ExecutionOptions> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        configure(_configuration.ExecutionOptions);
        return this;
    }

    /// <summary>
    /// Builds and validates the configuration.
    /// </summary>
    /// <returns>The validated configuration.</returns>
    public AgentConfiguration Build()
    {
        _configuration.Validate();
        return _configuration;
    }

    /// <summary>
    /// Builds the configuration without validation.
    /// </summary>
    /// <returns>The configuration.</returns>
    public AgentConfiguration BuildWithoutValidation()
    {
        return _configuration;
    }

    /// <summary>
    /// Loads configuration from Microsoft.Extensions.Configuration.
    /// </summary>
    /// <param name="configuration">The configuration source.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ConfigurationBuilder FromConfiguration(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection("DotNetAgents");
        if (section.Exists())
        {
            section.Bind(_configuration);
        }

        return this;
    }
}
