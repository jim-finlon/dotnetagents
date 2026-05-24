// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotNetAgents.Abstractions.PublicSubstitutes.Credentials;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Credentials.Client;

/// <summary>
/// Public credential-reference wiring for local development and open-core hosts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the public local credential substitute that resolves
    /// <see cref="CredentialReference"/> values from process environment variables.
    /// </summary>
    public static IServiceCollection AddEnvironmentVariableCredentialResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<ICredentialReferenceResolver, EnvironmentVariableCredentialReferenceResolver>();
        return services;
    }

    /// <summary>
    /// Register a rotation-aware credential accessor over the currently registered
    /// <see cref="ICredentialReferenceResolver"/>.
    /// </summary>
    public static IServiceCollection AddRotationAwareCredentialAccessor(
        this IServiceCollection services,
        Action<RotationAwareCredentialAccessorOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IRotationAwareCredentialAccessor>(sp =>
        {
            var inner = sp.GetRequiredService<ICredentialReferenceResolver>();
            var options = sp.GetService<IOptions<RotationAwareCredentialAccessorOptions>>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new RotationAwareCredentialReferenceResolver(inner, options, timeProvider);
        });

        return services;
    }
}
