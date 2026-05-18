using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotNetAgents.Abstractions.PublicSubstitutes.Credentials;

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
}
