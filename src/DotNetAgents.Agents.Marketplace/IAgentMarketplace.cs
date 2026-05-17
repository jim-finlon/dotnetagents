using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.Marketplace;

/// <summary>
/// Marketplace for discovering and sharing agents.
/// </summary>
public interface IAgentMarketplace
{
    /// <summary>
    /// Publishes an agent to the marketplace.
    /// </summary>
    /// <param name="listing">The agent listing to publish.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The published listing ID.</returns>
    Task<string> PublishAgentAsync(
        AgentListing listing,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for agents in the marketplace.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="filters">Optional search filters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>List of matching agent listings.</returns>
    Task<IReadOnlyList<AgentListing>> SearchAgentsAsync(
        string query,
        MarketplaceFilters? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an agent listing by ID.
    /// </summary>
    /// <param name="listingId">The listing ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The agent listing, or null if not found.</returns>
    Task<AgentListing?> GetListingAsync(
        string listingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers interest in an agent listing (for notifications).
    /// </summary>
    /// <param name="listingId">The listing ID.</param>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToAgentAsync(
        string listingId,
        string subscriberId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an agent listing in the marketplace.
/// </summary>
public class AgentListing
{
    /// <summary>
    /// Gets or sets the unique listing ID.
    /// </summary>
    public string ListingId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent ID.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the agent capabilities.
    /// </summary>
    public AgentCapabilities Capabilities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the publisher ID.
    /// </summary>
    public string PublisherId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the listing status.
    /// </summary>
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    /// <summary>
    /// Gets or sets the rating (0-5).
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Gets or sets the number of ratings.
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// Gets or sets the number of times this agent has been used.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Gets or sets the listing tags for search.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the listing metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets when the listing was published.
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets when the listing was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Status of an agent listing.
/// </summary>
public enum ListingStatus
{
    /// <summary>
    /// Listing is active and available.
    /// </summary>
    Active,

    /// <summary>
    /// Listing is pending approval.
    /// </summary>
    Pending,

    /// <summary>
    /// Listing is deprecated.
    /// </summary>
    Deprecated,

    /// <summary>
    /// Listing has been removed.
    /// </summary>
    Removed
}

/// <summary>
/// Filters for marketplace searches.
/// </summary>
public class MarketplaceFilters
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
    /// Gets or sets the required capabilities.
    /// </summary>
    public List<string> RequiredCapabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the publisher ID filter.
    /// </summary>
    public string? PublisherId { get; set; }

    /// <summary>
    /// Gets or sets the listing status filter.
    /// </summary>
    public ListingStatus? Status { get; set; }
}
