// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace DotNetAgents.LaneOps;

/// <summary>
/// Story 97a623fd. Wraps the autonomous-lane recovery shell helper with typed parsing
/// plus operator-auditable records for every verify/apply attempt.
/// </summary>
public sealed class RecoveryHarness : IRecoveryHarness
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IRecoveryProcessRunner _processRunner;
    private readonly IRecoveryHarnessAuditSink _auditSink;
    private readonly TimeProvider _clock;

    public RecoveryHarness(
        IRecoveryProcessRunner? processRunner = null,
        IRecoveryHarnessAuditSink? auditSink = null,
        TimeProvider? clock = null)
    {
        _processRunner = processRunner ?? new BashRecoveryProcessRunner();
        _auditSink = auditSink ?? NullRecoveryHarnessAuditSink.Instance;
        _clock = clock ?? TimeProvider.System;
    }

    public Task<RecoveryAttemptResult> VerifyAsync(RecoveryHarnessRequest request, CancellationToken cancellationToken = default) =>
        RunAttemptAsync(request, RecoveryAttemptMode.Verify, cancellationToken);

    public async Task<RecoveryApplyResult> ApplyAsync(RecoveryHarnessRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var verifyAttempt = await RunAttemptAsync(request, RecoveryAttemptMode.Verify, cancellationToken).ConfigureAwait(false);
        if (!verifyAttempt.Succeeded)
            return new RecoveryApplyResult(verifyAttempt, null);

        var applyAttempt = await RunAttemptAsync(request, RecoveryAttemptMode.Apply, cancellationToken).ConfigureAwait(false);
        return new RecoveryApplyResult(verifyAttempt, applyAttempt);
    }

    private async Task<RecoveryAttemptResult> RunAttemptAsync(
        RecoveryHarnessRequest request,
        RecoveryAttemptMode mode,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var shell = ResolveShell(request.BashExecutable);
        var scriptPath = ResolveScriptPath(request.ScriptPath);
        var arguments = BuildArguments(request, shell, scriptPath, mode);
        var environment = BuildEnvironment(request);
        var processResult = await _processRunner.RunAsync(shell, arguments, environment, request.MainCheckoutPath, cancellationToken).ConfigureAwait(false);
        var report = TryParseReport(processResult.StandardOutput);

        var auditRecord = new RecoveryHarnessAuditRecord(
            request.StoryId,
            request.LaneId,
            request.ActorId,
            mode == RecoveryAttemptMode.Verify ? "verify" : "apply",
            report?.RefusalReason ?? ExtractFallbackRefusal(processResult),
            SerializeVerifyState(report),
            processResult.ExitCode,
            _clock.GetUtcNow());

        await _auditSink.RecordAsync(auditRecord, cancellationToken).ConfigureAwait(false);

        return new RecoveryAttemptResult(
            mode,
            report,
            processResult.ExitCode,
            processResult.StandardOutput,
            processResult.StandardError);
    }

    private static IReadOnlyDictionary<string, string?> BuildEnvironment(RecoveryHarnessRequest request)
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["SDLC_MCP_AUTH_HEADER"] = request.SdlcMcpAuthHeader,
            ["PMA_MCP_AUTH_HEADER"] = request.PmaMcpAuthHeader,
            ["WORKFLOW_SERVICE_API_KEY"] = request.SdlcApiKey,
        };

        return environment;
    }

    private static void Validate(RecoveryHarnessRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorktreePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MainCheckoutPath);
    }

    private static string ResolveScriptPath(string? scriptPath)
    {
        if (!string.IsNullOrWhiteSpace(scriptPath))
            return scriptPath;

        var path = FindWorkspaceScriptPath();
        return path ?? throw new InvalidOperationException("Could not locate scripts/recover-dna-agent-worktree.sh. Supply ScriptPath explicitly.");
    }

    private static string ResolveShell(string? requestedShell)
    {
        if (!string.IsNullOrWhiteSpace(requestedShell))
            return requestedShell;

        if (!OperatingSystem.IsWindows())
            return "bash";

        var candidates = new[]
        {
            @"C:\Program Files\Git\bin\bash.exe",
            @"C:\Program Files\Git\usr\bin\bash.exe",
            @"C:\Program Files (x86)\Git\bin\bash.exe",
        };

        return candidates.FirstOrDefault(File.Exists) ?? "bash";
    }

    private static string? FindWorkspaceScriptPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "scripts", "recover-dna-agent-worktree.sh");
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        return null;
    }

    private static List<string> BuildArguments(RecoveryHarnessRequest request, string shell, string scriptPath, RecoveryAttemptMode mode)
    {
        var args = new List<string>
        {
            NormalizePathForShell(scriptPath, shell),
            "--json",
            "--worktree-path",
            NormalizePathForShell(request.WorktreePath, shell),
            "--main-checkout",
            NormalizePathForShell(request.MainCheckoutPath, shell)
        };
        if (!string.IsNullOrWhiteSpace(request.Branch))
            args.AddRange(["--branch", request.Branch]);
        if (!string.IsNullOrWhiteSpace(request.StoryId))
            args.AddRange(["--story-id", request.StoryId]);
        if (!string.IsNullOrWhiteSpace(request.MergedRef))
            args.AddRange(["--merged-ref", request.MergedRef]);
        if (!string.IsNullOrWhiteSpace(request.SdlcMcpUrl))
            args.AddRange(["--sdlc-mcp-url", request.SdlcMcpUrl]);
        if (!string.IsNullOrWhiteSpace(request.PmaMcpUrl))
            args.AddRange(["--pma-mcp-url", request.PmaMcpUrl]);
        if (request.McpTimeoutSec is { } timeout)
            args.AddRange(["--mcp-timeout-sec", timeout.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
        if (request.AllowDirty)
            args.Add("--allow-dirty");
        if (request.SkipMergedCheck)
            args.Add("--skip-merged-check");
        if (request.SkipFetch)
            args.Add("--skip-fetch");
        if (mode == RecoveryAttemptMode.Apply)
            args.Add("--apply");

        if (request.WorktreesRoots is { Count: > 0 })
        {
            foreach (var root in request.WorktreesRoots)
            {
                if (!string.IsNullOrWhiteSpace(root))
                    args.AddRange(["--worktrees-root", NormalizePathForShell(root, shell)]);
            }
        }

        return args;
    }

    private static string NormalizePathForShell(string value, string shell)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(value))
            return value;

        if (!shell.Contains("bash", StringComparison.OrdinalIgnoreCase))
            return value;

        var normalized = value.Replace('\\', '/');
        if (normalized.Length >= 3 && char.IsLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/')
            return $"/{char.ToLowerInvariant(normalized[0])}{normalized[2..]}";

        return normalized;
    }

    private static RecoveryScriptReport? TryParseReport(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
            return null;

        var trimmed = stdout.Trim();
        var firstBrace = trimmed.IndexOf('{');
        if (firstBrace > 0)
            trimmed = trimmed[firstBrace..];

        try
        {
            return JsonSerializer.Deserialize<RecoveryScriptReport>(trimmed, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string SerializeVerifyState(RecoveryScriptReport? report)
    {
        if (report?.Verify is null)
            return "{}";

        return JsonSerializer.Serialize(report.Verify, JsonOptions);
    }

    private static string ExtractFallbackRefusal(RecoveryProcessResult processResult)
    {
        if (!string.IsNullOrWhiteSpace(processResult.StandardError))
            return processResult.StandardError.Trim();
        if (!string.IsNullOrWhiteSpace(processResult.StandardOutput))
            return processResult.StandardOutput.Trim();
        return $"process exited with code {processResult.ExitCode}";
    }
}
