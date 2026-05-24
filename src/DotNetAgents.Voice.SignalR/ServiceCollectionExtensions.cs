// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Voice.Notifications;
using DotNetAgents.Voice.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Voice.SignalR;

/// <summary>
/// Extension methods for registering SignalR command notification services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR command notification services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandNotifications(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ICommandNotificationService, SignalRCommandNotificationService>();

        return services;
    }

}
