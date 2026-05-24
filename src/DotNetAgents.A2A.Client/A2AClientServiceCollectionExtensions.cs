// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.A2A.Client;

/// <summary>DI registration helpers for the A2A client. Story 49a210d7.</summary>
public static class A2AClientServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="IA2AClient"/> backed by a typed HttpClient. Operators can configure the
    /// HttpClient (timeout, default headers, handler chain) via the returned
    /// <see cref="IHttpClientBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Story a744ddc2 — the typed HttpClient pipeline includes
    /// <see cref="LaneTracePropagationHandler"/> by default so every outbound A2A request
    /// carries the W3C trace context plus the dna-lane-correlation header.
    /// </remarks>
    public static IHttpClientBuilder AddA2AClient(
        this IServiceCollection services,
        TimeSpan? agentCardTtl = null)
    {
        services.TryAddSingleton<IA2AClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient(nameof(A2AClient));
            return new A2AClient(
                http,
                sp.GetService<Microsoft.Extensions.Logging.ILogger<A2AClient>>(),
                agentCardTtl);
        });
        services.TryAddTransient<LaneTracePropagationHandler>();
        return services.AddHttpClient(nameof(A2AClient))
            .AddHttpMessageHandler<LaneTracePropagationHandler>();
    }
}
