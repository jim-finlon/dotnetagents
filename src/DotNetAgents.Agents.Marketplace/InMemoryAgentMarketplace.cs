using DotNetAgents.Agents.Registry;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Marketplace;

/// <summary>
/// In-memory implementation of agent marketplace.
/// </summary>
public class InMemoryAgentMarketplace : IAgentMarketplace
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<InMemoryAgentMarketplace>? _logger;
    private readonly Dictionary<string, AgentListing> _listings = new();
    private readonly Dictionary<string, HashSet<string>> _subscribers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentMarketplace"/> class.
    /// </summary>
    /// <param name="agentRegistry">The agent registry.</param>
    /// <param name="logger">Optional logger instance.</param>
    public InMemoryAgentMarketplace(
        IAgentRegistry agentRegistry,
        ILogger<InMemoryAgentMarketplace>? logger = null)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> PublishAgentAsync(
        AgentListing listing,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listing);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (string.IsNullOrEmpty(listing.ListingId))
            {
                listing.ListingId = Guid.NewGuid().ToString();
            }

            listing.PublishedAt = DateTimeOffset.UtcNow;
            listing.UpdatedAt = DateTimeOffset.UtcNow;

            if (listing.Status == ListingStatus.Active)
            {
                listing.Status = ListingStatus.Pending; // Require approval in production
            }

            _listings[listing.ListingId] = listing;

            _logger?.LogInformation(
                "Published agent {AgentId} to marketplace as listing {ListingId}",
                listing.AgentId,
                listing.ListingId);
        }

        return Task.FromResult(listing.ListingId);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentListing>> SearchAgentsAsync(
        string query,
        MarketplaceFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var results = _listings.Values
                .Where(listing => listing.Status == ListingStatus.Active)
                .Where(listing => MatchesQuery(listing, query))
                .Where(listing => MatchesFilters(listing, filters))
                .OrderByDescending(listing => listing.Rating)
                .ThenByDescending(listing => listing.UsageCount)
                .ToList();

            return Task.FromResult<IReadOnlyList<AgentListing>>(results);
        }
    }

    /// <inheritdoc />
    public Task<AgentListing?> GetListingAsync(
        string listingId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(listingId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _listings.TryGetValue(listingId, out var listing);
            return Task.FromResult<AgentListing?>(listing);
        }
    }

    /// <inheritdoc />
    public Task SubscribeToAgentAsync(
        string listingId,
        string subscriberId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(listingId);
        ArgumentException.ThrowIfNullOrEmpty(subscriberId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(listingId, out var subscribers))
            {
                subscribers = new HashSet<string>();
                _subscribers[listingId] = subscribers;
            }

            subscribers.Add(subscriberId);

            _logger?.LogInformation(
                "Subscriber {SubscriberId} subscribed to agent listing {ListingId}",
                subscriberId,
                listingId);
        }

        return Task.CompletedTask;
    }

    private static bool MatchesQuery(AgentListing listing, string query)
    {
        var queryLower = query.ToLowerInvariant();
        return listing.Name.ToLowerInvariant().Contains(queryLower) ||
               listing.Description.ToLowerInvariant().Contains(queryLower) ||
               listing.Tags.Any(tag => tag.ToLowerInvariant().Contains(queryLower)) ||
               listing.Capabilities.SupportedTools.Any(tool => tool.ToLowerInvariant().Contains(queryLower));
    }

    private static bool MatchesFilters(AgentListing listing, MarketplaceFilters? filters)
    {
        if (filters == null)
            return true;

        if (filters.MinRating.HasValue && listing.Rating < filters.MinRating.Value)
            return false;

        if (filters.RequiredTags.Count > 0)
        {
            var hasAllTags = filters.RequiredTags.All(tag => listing.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
            if (!hasAllTags)
                return false;
        }

        if (filters.RequiredCapabilities.Count > 0)
        {
            var hasAllCapabilities = filters.RequiredCapabilities.All(cap =>
                listing.Capabilities.SupportedTools.Contains(cap, StringComparer.OrdinalIgnoreCase) ||
                listing.Capabilities.SupportedIntents.Contains(cap, StringComparer.OrdinalIgnoreCase));
            if (!hasAllCapabilities)
                return false;
        }

        if (!string.IsNullOrEmpty(filters.PublisherId) && listing.PublisherId != filters.PublisherId)
            return false;

        if (filters.Status.HasValue && listing.Status != filters.Status.Value)
            return false;

        return true;
    }
}
