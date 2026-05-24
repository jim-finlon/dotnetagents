// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ModelRouting;

/// <summary>Selects a model or endpoint for a request. FR-MR-001.</summary>
public interface IModelRouter
{
    /// <summary>Returns the selected model/endpoint for the request.</summary>
    /// <param name="request">Request context (input, options, optional required capabilities).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Routing result with model id and optional endpoint.</returns>
    Task<RoutingResult> RouteAsync(RoutingRequest request, CancellationToken cancellationToken = default);
}
