namespace DotNetAgents.Ecosystem;

/// <summary>
/// Marketplace for discovering and sharing integrations.
/// </summary>
public interface IIntegrationMarketplace
{
    /// <summary>
    /// Publishes an integration to the marketplace.
    /// </summary>
    /// <param name="integration">The integration to publish.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The published integration ID.</returns>
    Task<string> PublishAsync(
        IntegrationListing integration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for integrations.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="filters">Optional search filters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>List of matching integrations.</returns>
    Task<IReadOnlyList<IntegrationListing>> SearchAsync(
        string query,
        IntegrationFilters? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an integration by ID.
    /// </summary>
    /// <param name="integrationId">The integration ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The integration, or null if not found.</returns>
    Task<IntegrationListing?> GetAsync(
        string integrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integrations by category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>List of integrations in the category.</returns>
    Task<IReadOnlyList<IntegrationListing>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Integration listing in the marketplace.
/// </summary>
public class IntegrationListing
{
    /// <summary>
    /// Gets or sets the integration ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integration description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integration category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the publisher ID.
    /// </summary>
    public string PublisherId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integration type.
    /// </summary>
    public IntegrationType Type { get; set; }

    /// <summary>
    /// Gets or sets the integration status.
    /// </summary>
    public IntegrationStatus Status { get; set; } = IntegrationStatus.Active;

    /// <summary>
    /// Gets or sets the rating (0-5).
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Gets or sets the number of ratings.
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// Gets or sets the number of downloads.
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// Gets or sets the integration tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the repository URL.
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Gets or sets the documentation URL.
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Gets or sets when the integration was published.
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets when the integration was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Types of integrations.
/// </summary>
public enum IntegrationType
{
    /// <summary>
    /// Plugin integration.
    /// </summary>
    Plugin,

    /// <summary>
    /// Tool integration.
    /// </summary>
    Tool,

    /// <summary>
    /// Provider integration.
    /// </summary>
    Provider,

    /// <summary>
    /// Workflow template.
    /// </summary>
    WorkflowTemplate,

    /// <summary>
    /// Custom integration.
    /// </summary>
    Custom
}

/// <summary>
/// Status of an integration.
/// </summary>
public enum IntegrationStatus
{
    /// <summary>
    /// Integration is active and available.
    /// </summary>
    Active,

    /// <summary>
    /// Integration is pending approval.
    /// </summary>
    Pending,

    /// <summary>
    /// Integration is deprecated.
    /// </summary>
    Deprecated,

    /// <summary>
    /// Integration has been removed.
    /// </summary>
    Removed
}

/// <summary>
/// Filters for integration searches.
/// </summary>
public class IntegrationFilters
{
    /// <summary>
    /// Gets or sets the minimum rating.
    /// </summary>
    public double? MinRating { get; set; }

    /// <summary>
    /// Gets or sets the required tags.
    /// </summary>
    public List<string> RequiredTags { get; set; } = new();

    /// <summary>
    /// Gets or sets the integration type filter.
    /// </summary>
    public IntegrationType? Type { get; set; }

    /// <summary>
    /// Gets or sets the publisher ID filter.
    /// </summary>
    public string? PublisherId { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public IntegrationStatus? Status { get; set; }
}
