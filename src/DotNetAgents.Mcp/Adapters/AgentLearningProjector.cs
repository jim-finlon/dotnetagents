using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Adapters;

/// <summary>
/// Best-effort projector that forwards learning events to configured endpoints.
/// Projection failures are non-blocking by design.
/// </summary>
public sealed class AgentLearningProjector(
    IHttpClientFactory httpClientFactory,
    AgentLearningProjectionOptions options,
    ILogger<AgentLearningProjector> logger) : IAgentLearningProjector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AgentLearningProjectionResult> ProjectAsync(
        LearningEventV1 learningEvent,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return new AgentLearningProjectionResult(0, 0, []);
        }

        var enabledTargets = options.Targets
            .Where(target => target.Enabled && !string.IsNullOrWhiteSpace(target.Url))
            .ToList();
        if (enabledTargets.Count == 0)
        {
            return new AgentLearningProjectionResult(0, 0, []);
        }

        var failed = new List<string>();
        var success = 0;

        foreach (var target in enabledTargets)
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linked.CancelAfter(Math.Clamp(options.TimeoutMs, 100, 15000));

                var client = httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, target.Url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(target.ApiKeyHeader) && !string.IsNullOrWhiteSpace(target.ApiKey))
                {
                    request.Headers.Remove(target.ApiKeyHeader);
                    request.Headers.Add(target.ApiKeyHeader, target.ApiKey);
                }

                request.Content = new StringContent(
                    JsonSerializer.Serialize(learningEvent, JsonOptions),
                    Encoding.UTF8,
                    "application/json");
                using var response = await client.SendAsync(request, linked.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    failed.Add(target.Name);
                    logger.LogWarning(
                        "Learning event projection failed for {TargetName} with status {StatusCode}",
                        target.Name,
                        response.StatusCode);
                    continue;
                }

                success++;
            }
            catch (Exception ex)
            {
                failed.Add(target.Name);
                logger.LogWarning(ex, "Learning event projection failed for {TargetName}", target.Name);
            }
        }

        return new AgentLearningProjectionResult(enabledTargets.Count, success, failed);
    }
}
