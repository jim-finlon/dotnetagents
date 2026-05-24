// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.PublicSubstitutes.Session;
using DotNetAgents.SessionPersistence.PublicSubstitutes.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.SessionPersistence;

/// <summary>
/// DI registration for the Session Persistence HTTP client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add the AI Session Persistence HTTP client with explicit base URL and optional API key.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="baseAddress">Base URL of the API (e.g. http://localhost:5000).</param>
    /// <param name="apiKey">Optional API key for X-API-Key header.</param>
    public static IServiceCollection AddSessionPersistenceClient(
        this IServiceCollection services,
        string baseAddress,
        string? apiKey = null)
    {
        services.Configure<SessionPersistenceClientOptions>(o =>
        {
            o.BaseAddress = baseAddress;
            if (apiKey != null) o.ApiKey = apiKey;
        });
        services.AddHttpClient<ISessionPersistenceClient, SessionPersistenceClient>();
        return services;
    }

    /// <summary>
    /// Add the Session Persistence client and bind options from configuration
    /// (section <see cref="SessionPersistenceClientOptions.SectionName"/>: BaseAddress, ApiKey, RequestTimeout).
    /// </summary>
    public static IServiceCollection AddSessionPersistenceClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SessionPersistenceClientOptions>(configuration.GetSection(SessionPersistenceClientOptions.SectionName));
        services.AddHttpClient<ISessionPersistenceClient, SessionPersistenceClient>();
        return services;
    }

    /// <summary>Register the process-scoped public session store for examples and tests.</summary>
    public static IServiceCollection AddInMemorySessionStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IPublicSessionStore, InMemorySessionStore>();
        return services;
    }

    /// <summary>
    /// Register the local JSON-file public session store. The adapter is
    /// single-process and does not take inter-process file locks.
    /// </summary>
    public static IServiceCollection AddFileSnapshotSessionStore(
        this IServiceCollection services,
        string directoryPath)
    {
        services.TryAddSingleton<IPublicSessionStore>(_ => new FileSnapshotSessionStore(directoryPath));
        return services;
    }
}
