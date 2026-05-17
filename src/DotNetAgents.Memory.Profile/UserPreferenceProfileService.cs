using System.Text.Json;
using DotNetAgents.Knowledge;
using DotNetAgents.Knowledge.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Memory.Profile;

public sealed record UserPreference(string Key, string Value, DateTimeOffset UpdatedAtUtc);
public sealed record UserPreferenceProfile(string UserId, IReadOnlyList<UserPreference> Preferences);

public interface IUserPreferenceProfileService
{
    Task<UserPreferenceProfile> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task UpsertPreferenceAsync(string userId, string key, string value, CancellationToken cancellationToken = default);
    Task<bool> DeletePreferenceAsync(string userId, string key, CancellationToken cancellationToken = default);
    Task<string> ExportProfileJsonAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class InMemoryUserPreferenceProfileService : IUserPreferenceProfileService
{
    private readonly Dictionary<string, Dictionary<string, UserPreference>> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public Task<UserPreferenceProfile> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        if (!_profiles.TryGetValue(userId, out var preferences))
        {
            return Task.FromResult(new UserPreferenceProfile(userId, []));
        }

        return Task.FromResult(new UserPreferenceProfile(userId, preferences.Values.OrderByDescending(x => x.UpdatedAtUtc).ToList()));
    }

    public Task UpsertPreferenceAsync(string userId, string key, string value, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Preference key must be provided.", nameof(key));
        }

        if (!_profiles.TryGetValue(userId, out var preferences))
        {
            preferences = new Dictionary<string, UserPreference>(StringComparer.OrdinalIgnoreCase);
            _profiles[userId] = preferences;
        }

        preferences[key.Trim()] = new UserPreference(key.Trim(), value, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task<bool> DeletePreferenceAsync(string userId, string key, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        if (string.IsNullOrWhiteSpace(key)) return Task.FromResult(false);
        if (!_profiles.TryGetValue(userId, out var preferences)) return Task.FromResult(false);
        return Task.FromResult(preferences.Remove(key.Trim()));
    }

    public async Task<string> ExportProfileJsonAsync(string userId, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        var profile = await GetProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        return System.Text.Json.JsonSerializer.Serialize(profile);
    }

    private static void Validate(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID must be provided.", nameof(userId));
        }
    }
}

public sealed class KnowledgeBackedPreferenceProfileService(IKnowledgeRepository knowledgeRepository)
    : IUserPreferenceProfileService
{
    private readonly IKnowledgeRepository _knowledgeRepository = knowledgeRepository;

    public async Task<UserPreferenceProfile> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        var sessionId = $"user:{userId}:preferences";
        var items = await _knowledgeRepository.GetKnowledgeBySessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var preferences = items
            .Where(i => i.Tags.Contains("preference", StringComparer.OrdinalIgnoreCase))
            .Select(i => new UserPreference(
                i.Metadata.TryGetValue("key", out var k) ? k : "unknown",
                i.Metadata.TryGetValue("value", out var v) ? v : string.Empty,
                i.CreatedAt))
            .OrderByDescending(p => p.UpdatedAtUtc)
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        return new UserPreferenceProfile(userId, preferences);
    }

    public Task UpsertPreferenceAsync(string userId, string key, string value, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Preference key must be provided.", nameof(key));
        }

        var item = new KnowledgeItem
        {
            SessionId = $"user:{userId}:preferences",
            Title = $"Preference: {key}",
            Description = $"User preference for {key}.",
            Category = KnowledgeCategory.BestPractice,
            Tags = ["preference", "user-profile"],
            Metadata = new Dictionary<string, string>
            {
                ["key"] = key.Trim(),
                ["value"] = value
            }
        };
        return _knowledgeRepository.AddKnowledgeAsync(item, cancellationToken);
    }

    public async Task<bool> DeletePreferenceAsync(string userId, string key, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        if (string.IsNullOrWhiteSpace(key)) return false;
        var sessionId = $"user:{userId}:preferences";
        var items = await _knowledgeRepository.GetKnowledgeBySessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var toDelete = items.Where(i => i.Metadata.TryGetValue("key", out var k) && string.Equals(k, key.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var item in toDelete)
            await _knowledgeRepository.DeleteKnowledgeAsync(item.Id, cancellationToken).ConfigureAwait(false);
        return toDelete.Count > 0;
    }

    public async Task<string> ExportProfileJsonAsync(string userId, CancellationToken cancellationToken = default)
    {
        Validate(userId);
        var profile = await GetProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(profile);
    }

    private static void Validate(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID must be provided.", nameof(userId));
        }
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAgentsMemoryProfile(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IUserPreferenceProfileService, InMemoryUserPreferenceProfileService>();
        return services;
    }

    public static IServiceCollection AddKnowledgeBackedPreferenceProfiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IUserPreferenceProfileService, KnowledgeBackedPreferenceProfileService>();
        return services;
    }
}
