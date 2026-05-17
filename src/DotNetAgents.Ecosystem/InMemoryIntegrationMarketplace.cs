using Microsoft.Extensions.Logging;

namespace DotNetAgents.Ecosystem;

/// <summary>
/// In-memory implementation of integration marketplace.
/// </summary>
public class InMemoryIntegrationMarketplace : IIntegrationMarketplace
{
    private readonly Dictionary<string, IntegrationListing> _integrations = new();
    private readonly Dictionary<string, List<string>> _categoryIndex = new();
    private readonly ILogger<InMemoryIntegrationMarketplace>? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryIntegrationMarketplace"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public InMemoryIntegrationMarketplace(ILogger<InMemoryIntegrationMarketplace>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string> PublishAsync(
        IntegrationListing integration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integration);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (string.IsNullOrEmpty(integration.Id))
            {
                integration.Id = Guid.NewGuid().ToString();
            }

            integration.PublishedAt = DateTimeOffset.UtcNow;
            integration.UpdatedAt = DateTimeOffset.UtcNow;

            if (integration.Status == IntegrationStatus.Active)
            {
                integration.Status = IntegrationStatus.Pending; // Require approval in production
            }

            _integrations[integration.Id] = integration;

            // Index by category
            if (!string.IsNullOrEmpty(integration.Category))
            {
                if (!_categoryIndex.TryGetValue(integration.Category, out var integrations))
                {
                    integrations = new List<string>();
                    _categoryIndex[integration.Category] = integrations;
                }
                integrations.Add(integration.Id);
            }

            _logger?.LogInformation(
                "Published integration {IntegrationId} ({IntegrationName})",
                integration.Id,
                integration.Name);
        }

        return Task.FromResult(integration.Id);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationListing>> SearchAsync(
        string query,
        IntegrationFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var results = _integrations.Values
                .Where(integration => integration.Status == IntegrationStatus.Active)
                .Where(integration => MatchesQuery(integration, query))
                .Where(integration => MatchesFilters(integration, filters))
                .OrderByDescending(integration => integration.Rating)
                .ThenByDescending(integration => integration.DownloadCount)
                .ToList();

            return Task.FromResult<IReadOnlyList<IntegrationListing>>(results);
        }
    }

    /// <inheritdoc />
    public Task<IntegrationListing?> GetAsync(
        string integrationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(integrationId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _integrations.TryGetValue(integrationId, out var integration);
            return Task.FromResult<IntegrationListing?>(integration);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationListing>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(category);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_categoryIndex.TryGetValue(category, out var integrationIds))
            {
                var integrations = integrationIds
                    .Select(id => _integrations.TryGetValue(id, out var integration) ? integration : null)
                    .Where(i => i != null && i.Status == IntegrationStatus.Active)
                    .Cast<IntegrationListing>()
                    .ToList();

                return Task.FromResult<IReadOnlyList<IntegrationListing>>(integrations);
            }
        }

        return Task.FromResult<IReadOnlyList<IntegrationListing>>(new List<IntegrationListing>());
    }

    private static bool MatchesQuery(IntegrationListing integration, string query)
    {
        var queryLower = query.ToUpperInvariant();
        return integration.Name.ToUpperInvariant().Contains(queryLower) ||
               integration.Description.ToUpperInvariant().Contains(queryLower) ||
               integration.Tags.Any(tag => tag.ToUpperInvariant().Contains(queryLower));
    }

    private static bool MatchesFilters(IntegrationListing integration, IntegrationFilters? filters)
    {
        if (filters == null)
            return true;

        if (filters.MinRating.HasValue && integration.Rating < filters.MinRating.Value)
            return false;

        if (filters.RequiredTags.Count > 0)
        {
            var hasAllTags = filters.RequiredTags.All(tag => integration.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
            if (!hasAllTags)
                return false;
        }

        if (filters.Type.HasValue && integration.Type != filters.Type.Value)
            return false;

        if (!string.IsNullOrEmpty(filters.PublisherId) && integration.PublisherId != filters.PublisherId)
            return false;

        if (filters.Status.HasValue && integration.Status != filters.Status.Value)
            return false;

        return true;
    }
}
