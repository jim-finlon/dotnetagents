using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for interacting with the GitHub API (issues, pull requests, repositories).
/// </summary>
public class GitHubTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private static readonly JsonElement _inputSchema;

    static GitHubTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""action"": {
                    ""type"": ""string"",
                    ""description"": ""Action to perform: 'create_issue', 'get_issue', 'create_pr', 'get_pr', 'list_repos', 'get_repo'"",
                    ""enum"": [""create_issue"", ""get_issue"", ""create_pr"", ""get_pr"", ""list_repos"", ""get_repo""]
                },
                ""owner"": {
                    ""type"": ""string"",
                    ""description"": ""Repository owner (username or organization)""
                },
                ""repo"": {
                    ""type"": ""string"",
                    ""description"": ""Repository name""
                },
                ""title"": {
                    ""type"": ""string"",
                    ""description"": ""Issue or PR title""
                },
                ""body"": {
                    ""type"": ""string"",
                    ""description"": ""Issue or PR body/description""
                },
                ""issue_number"": {
                    ""type"": ""integer"",
                    ""description"": ""Issue or PR number""
                },
                ""head"": {
                    ""type"": ""string"",
                    ""description"": ""PR head branch name""
                },
                ""base"": {
                    ""type"": ""string"",
                    ""description"": ""PR base branch name. Default: main""
                },
                ""labels"": {
                    ""type"": ""array"",
                    ""description"": ""Labels to add to issue or PR"",
                    ""items"": {""type"": ""string""}
                }
            },
            ""required"": [""action""]
        }");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubTool"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making requests.</param>
    /// <param name="token">The GitHub personal access token.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or token is null.</exception>
    public GitHubTool(HttpClient httpClient, string token)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _token = token ?? throw new ArgumentNullException(nameof(token));

        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DotNetAgents");
    }

    /// <inheritdoc/>
    public string Name => "github_api";

    /// <inheritdoc/>
    public string Description => "Interacts with the GitHub API to manage issues, pull requests, and repositories.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input is not IDictionary<string, object> inputDict)
        {
            return ToolResult.Failure("Input must be a dictionary.");
        }

        try
        {
            if (!inputDict.TryGetValue("action", out var actionObj) || actionObj == null)
            {
                return ToolResult.Failure("Action is required.");
            }

            var action = actionObj.ToString() ?? string.Empty;

            return action switch
            {
                "create_issue" => await CreateIssueAsync(inputDict, cancellationToken).ConfigureAwait(false),
                "get_issue" => await GetIssueAsync(inputDict, cancellationToken).ConfigureAwait(false),
                "create_pr" => await CreatePullRequestAsync(inputDict, cancellationToken).ConfigureAwait(false),
                "get_pr" => await GetPullRequestAsync(inputDict, cancellationToken).ConfigureAwait(false),
                "list_repos" => await ListRepositoriesAsync(inputDict, cancellationToken).ConfigureAwait(false),
                "get_repo" => await GetRepositoryAsync(inputDict, cancellationToken).ConfigureAwait(false),
                _ => ToolResult.Failure($"Unknown action: {action}")
            };
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"Failed to execute GitHub action: {ex.Message}");
        }
    }

    private async Task<ToolResult> CreateIssueAsync(
        IDictionary<string, object> input,
        CancellationToken cancellationToken)
    {
        if (!input.TryGetValue("owner", out var ownerObj) || ownerObj == null ||
            !input.TryGetValue("repo", out var repoObj) || repoObj == null ||
            !input.TryGetValue("title", out var titleObj) || titleObj == null)
        {
            return ToolResult.Failure("Owner, repo, and title are required for creating an issue.");
        }

        var owner = ownerObj.ToString() ?? string.Empty;
        var repo = repoObj.ToString() ?? string.Empty;
        var title = titleObj.ToString() ?? string.Empty;
        var body = input.TryGetValue("body", out var bodyObj) ? bodyObj.ToString() ?? string.Empty : string.Empty;

        var payload = new Dictionary<string, object>
        {
            ["title"] = title,
            ["body"] = body
        };

        if (input.TryGetValue("labels", out var labelsObj) && labelsObj is IEnumerable<object> labels)
        {
            payload["labels"] = labels.Select(l => l.ToString() ?? string.Empty).ToArray();
        }

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"repos/{owner}/{repo}/issues",
            content,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ToolResult.Failure($"GitHub API error: {response.StatusCode}. {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            $"Issue created successfully: #{result?["number"]}",
            result ?? new Dictionary<string, object>());
    }

    private async Task<ToolResult> GetIssueAsync(
        IDictionary<string, object> input,
        CancellationToken cancellationToken)
    {
        if (!input.TryGetValue("owner", out var ownerObj) || ownerObj == null ||
            !input.TryGetValue("repo", out var repoObj) || repoObj == null ||
            !input.TryGetValue("issue_number", out var issueNumberObj) || issueNumberObj == null)
        {
            return ToolResult.Failure("Owner, repo, and issue_number are required for getting an issue.");
        }

        var owner = ownerObj.ToString() ?? string.Empty;
        var repo = repoObj.ToString() ?? string.Empty;
        var issueNumber = issueNumberObj.ToString() ?? string.Empty;

        var response = await _httpClient.GetAsync(
            $"repos/{owner}/{repo}/issues/{issueNumber}",
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ToolResult.Failure($"GitHub API error: {response.StatusCode}. {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            $"Retrieved issue #{issueNumber}",
            result ?? new Dictionary<string, object>());
    }

    private async Task<ToolResult> CreatePullRequestAsync(
        IDictionary<string, object> input,
        CancellationToken cancellationToken)
    {
        if (!input.TryGetValue("owner", out var ownerObj) || ownerObj == null ||
            !input.TryGetValue("repo", out var repoObj) || repoObj == null ||
            !input.TryGetValue("title", out var titleObj) || titleObj == null ||
            !input.TryGetValue("head", out var headObj) || headObj == null)
        {
            return ToolResult.Failure("Owner, repo, title, and head are required for creating a PR.");
        }

        var owner = ownerObj.ToString() ?? string.Empty;
        var repo = repoObj.ToString() ?? string.Empty;
        var title = titleObj.ToString() ?? string.Empty;
        var head = headObj.ToString() ?? string.Empty;
        var @base = input.TryGetValue("base", out var baseObj) ? baseObj.ToString() ?? "main" : "main";
        var body = input.TryGetValue("body", out var bodyObj) ? bodyObj.ToString() ?? string.Empty : string.Empty;

        var payload = new Dictionary<string, object>
        {
            ["title"] = title,
            ["body"] = body,
            ["head"] = head,
            ["base"] = @base
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"repos/{owner}/{repo}/pulls",
            content,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ToolResult.Failure($"GitHub API error: {response.StatusCode}. {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            $"Pull request created successfully: #{result?["number"]}",
            result ?? new Dictionary<string, object>());
    }

    private async Task<ToolResult> GetPullRequestAsync(
        IDictionary<string, object> input,
        CancellationToken cancellationToken)
    {
        if (!input.TryGetValue("owner", out var ownerObj) || ownerObj == null ||
            !input.TryGetValue("repo", out var repoObj) || repoObj == null ||
            !input.TryGetValue("issue_number", out var prNumberObj) || prNumberObj == null)
        {
            return ToolResult.Failure("Owner, repo, and issue_number (PR number) are required for getting a PR.");
        }

        var owner = ownerObj.ToString() ?? string.Empty;
        var repo = repoObj.ToString() ?? string.Empty;
        var prNumber = prNumberObj.ToString() ?? string.Empty;

        var response = await _httpClient.GetAsync(
            $"repos/{owner}/{repo}/pulls/{prNumber}",
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ToolResult.Failure($"GitHub API error: {response.StatusCode}. {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            $"Retrieved PR #{prNumber}",
            result ?? new Dictionary<string, object>());
    }

    private async Task<ToolResult> ListRepositoriesAsync(
        IDictionary<string, object> input,
        CancellationToken cancellationToken)
    {
        var owner = input.TryGetValue("owner", out var ownerObj) ? ownerObj.ToString() ?? string.Empty : string.Empty;
        var endpoint = string.IsNullOrWhiteSpace(owner)
            ? "user/repos"
            : $"users/{owner}/repos";

        var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ToolResult.Failure($"GitHub API error: {response.StatusCode}. {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            $"Retrieved {result?.Count ?? 0} repositories",
            new Dictionary<string, object> { ["repositories"] = result ?? new List<Dictionary<string, object>>() });
    }

    private async Task<ToolResult> GetRepositoryAsync(
        IDictionary<string, object> input,
        CancellationToken cancellationToken)
    {
        if (!input.TryGetValue("owner", out var ownerObj) || ownerObj == null ||
            !input.TryGetValue("repo", out var repoObj) || repoObj == null)
        {
            return ToolResult.Failure("Owner and repo are required for getting a repository.");
        }

        var owner = ownerObj.ToString() ?? string.Empty;
        var repo = repoObj.ToString() ?? string.Empty;

        var response = await _httpClient.GetAsync(
            $"repos/{owner}/{repo}",
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ToolResult.Failure($"GitHub API error: {response.StatusCode}. {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToolResult.Success(
            $"Retrieved repository {owner}/{repo}",
            result ?? new Dictionary<string, object>());
    }
}
