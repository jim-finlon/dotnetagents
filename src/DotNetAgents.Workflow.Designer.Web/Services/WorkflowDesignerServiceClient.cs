// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Workflow.Designer;
using System.Text;
using System.Text.Json;

namespace DotNetAgents.Workflow.Designer.Web.Services;

/// <summary>
/// Client implementation of IWorkflowDesignerService for web frontend.
/// </summary>
public class WorkflowDesignerServiceClient : IWorkflowDesignerService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowDesignerServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public WorkflowDesignerServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = _httpClient.BaseAddress?.ToString() ?? "https://localhost:8080";
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowDefinitionDto>> ListWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/workflows", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<WorkflowDefinitionDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<WorkflowDefinitionDto>();
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionDto?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        var response = await _httpClient.GetAsync($"{_baseUrl}/api/workflows/{workflowId}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<WorkflowDefinitionDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionDto> SaveWorkflowAsync(WorkflowDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var json = JsonSerializer.Serialize(definition);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        if (string.IsNullOrEmpty(definition.Name))
        {
            response = await _httpClient.PostAsync($"{_baseUrl}/api/workflows", content, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            response = await _httpClient.PutAsync($"{_baseUrl}/api/workflows/{definition.Name}", content, cancellationToken).ConfigureAwait(false);
        }

        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<WorkflowDefinitionDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? definition;
    }

    /// <inheritdoc />
    public async Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var json = JsonSerializer.Serialize(definition);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/workflows/validate", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<WorkflowValidationResult>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new WorkflowValidationResult { IsValid = false, Errors = new List<string> { "Validation failed" } };
    }

    /// <inheritdoc />
    public async Task<string> ExecuteWorkflowAsync(string workflowId, object? initialState = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        var request = new { initialState = initialState };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/workflows/{workflowId}/execute", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);
        return result?["executionId"] ?? Guid.NewGuid().ToString();
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionDto?> GetExecutionStatusAsync(string executionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(executionId);

        var response = await _httpClient.GetAsync($"{_baseUrl}/api/executions/{executionId}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<WorkflowExecutionDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
