// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.PublicSubstitutes.Lab;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Core.PublicSubstitutes.Lab;

/// <summary>DI helpers for public lab environment descriptor substitutes.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultLabEnvironmentDescriptor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ILabEnvironmentDescriptor, DefaultLabEnvironmentDescriptor>();
        return services;
    }
}
