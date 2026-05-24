// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace DotNetAgents.Runtime;

public sealed class InMemoryExecutionEnvironmentProvider :
    IExecutionEnvironmentProvider,
    ICommandExecutor,
    IArtifactCollector
{
    private readonly ConcurrentDictionary<string, ExecutionLease> _leases = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ArtifactReference> _artifacts = new(StringComparer.OrdinalIgnoreCase);
    private readonly IEnvironmentCleanupPolicy _cleanupPolicy;
    private readonly string _rootPath;

    public InMemoryExecutionEnvironmentProvider(
        string rootPath = "/tmp/dna-execution-leases",
        IEnvironmentCleanupPolicy? cleanupPolicy = null,
        ExecutionEnvironmentProviderMetadata? metadata = null)
    {
        _rootPath = rootPath.TrimEnd('/', '\\');
        _cleanupPolicy = cleanupPolicy ?? new PathScopedCleanupPolicy();
        Metadata = metadata ?? new ExecutionEnvironmentProviderMetadata(
            "in-memory-worktree",
            ExecutionEnvironmentKind.Fake,
            new HashSet<string>(["local-worktree", "command-capture", "artifact-capture"], StringComparer.OrdinalIgnoreCase),
            ExecutionBlastRadius.WorktreePath,
            ExecutionPersistenceMode.Ephemeral,
            ExecutionCredentialMode.None,
            ExecutionNetworkMode.Disabled,
            ExecutionCleanupGuarantee.VerifiedPathScoped,
            ExecutionApprovalRequirement.StoryClaimRequired);
    }

    public ExecutionEnvironmentProviderMetadata Metadata { get; }

    public Task<ExecutionLease> CreateLeaseAsync(
        ExecutionLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequest(request);

        var leaseId = Guid.NewGuid().ToString("n");
        var rootPath = string.IsNullOrWhiteSpace(request.RootPath)
            ? $"{_rootPath}/{leaseId}"
            : request.RootPath;
        var lease = new ExecutionLease
        {
            LeaseId = leaseId,
            ProviderName = Metadata.ProviderName,
            ActorId = request.ActorId,
            Purpose = request.Purpose,
            BaseCommit = request.BaseCommit,
            BranchName = request.BranchName,
            RootPath = rootPath,
            ExpiresAtUtc = request.TimeToLive is null ? null : DateTimeOffset.UtcNow.Add(request.TimeToLive.Value),
            AllowedOperations = request.AllowedOperations.Count == 0
                ? new HashSet<string>(["command", "artifact", "cleanup"], StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(request.AllowedOperations, StringComparer.OrdinalIgnoreCase),
            Metadata = request.Metadata.Count == 0
                ? ReadOnlyDictionary<string, string>.Empty
                : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase)),
            CapabilityTags = Metadata.CapabilityTags,
            BlastRadius = Metadata.BlastRadius,
            PersistenceMode = Metadata.PersistenceMode,
            CredentialMode = Metadata.CredentialMode,
            NetworkMode = Metadata.NetworkMode,
            CleanupGuarantee = Metadata.CleanupGuarantee,
            ApprovalRequirement = Metadata.ApprovalRequirement,
            CleanupCommand = $"cleanup {leaseId} --path {rootPath}"
        };

        if (!_leases.TryAdd(lease.LeaseId, lease))
        {
            throw new InvalidOperationException($"Execution lease '{lease.LeaseId}' already exists.");
        }

        return Task.FromResult(lease);
    }

    public Task<CommandExecutionResult> ExecuteAsync(
        CommandExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lease = GetLease(request.LeaseId);
        EnsureOperationAllowed(lease, "command");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Command);

        var commandId = Guid.NewGuid().ToString("n");
        var commandLine = RenderCommandLine(request.Command, request.Arguments ?? []);
        var failed = request.Command.Contains("fail", StringComparison.OrdinalIgnoreCase);
        var stdout = CreateArtifactRef(
            lease.LeaseId,
            $"commands/{commandId}/stdout",
            "text/plain",
            failed ? string.Empty : $"Command '{request.Command}' completed.");
        var stderr = CreateArtifactRef(
            lease.LeaseId,
            $"commands/{commandId}/stderr",
            "text/plain",
            failed ? $"Command '{request.Command}' failed." : string.Empty);

        _artifacts[stdout.Uri] = stdout;
        _artifacts[stderr.Uri] = stderr;

        return Task.FromResult(new CommandExecutionResult
        {
            CommandId = commandId,
            LeaseId = lease.LeaseId,
            CommandLine = commandLine,
            Status = failed ? CommandExecutionStatus.Failed : CommandExecutionStatus.Succeeded,
            ExitCode = failed ? 1 : 0,
            StandardOutputRef = stdout,
            StandardErrorRef = stderr,
            ArtifactRefs = [stdout, stderr],
            ErrorMessage = failed ? "Simulated command failure." : null
        });
    }

    public Task<ArtifactCollectionResult> CollectAsync(
        ArtifactCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lease = GetLease(request.LeaseId);
        EnsureOperationAllowed(lease, "artifact");
        if (string.IsNullOrWhiteSpace(request.RelativePath) ||
            Path.IsPathRooted(request.RelativePath) ||
            request.RelativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
        {
            throw new InvalidOperationException("Artifact collection path must be lease-relative and cannot traverse upward.");
        }

        var artifact = CreateArtifactRef(
            lease.LeaseId,
            $"artifacts/{request.RelativePath.Replace('\\', '/')}",
            request.MediaType,
            $"artifact:{request.RelativePath}");
        _artifacts[artifact.Uri] = artifact;
        return Task.FromResult(new ArtifactCollectionResult(lease.LeaseId, artifact));
    }

    public Task<ExecutionCleanupReceipt> CleanupAsync(
        ExecutionCleanupRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lease = GetLease(request.LeaseId);
        EnsureOperationAllowed(lease, "cleanup");
        var decision = _cleanupPolicy.Evaluate(lease, request.RequestedPath);
        if (!decision.Allowed)
        {
            return Task.FromResult(new ExecutionCleanupReceipt
            {
                LeaseId = lease.LeaseId,
                ProviderName = lease.ProviderName,
                RequestedPath = request.RequestedPath,
                RefusedPaths = decision.RefusedPaths ?? [request.RequestedPath],
                Succeeded = false,
                FailureReason = decision.Reason,
                CleanupCommand = lease.CleanupCommand
            });
        }

        if (lease.Metadata.TryGetValue("simulateCleanupFailure", out var fail) &&
            bool.TryParse(fail, out var shouldFail) &&
            shouldFail)
        {
            return Task.FromResult(new ExecutionCleanupReceipt
            {
                LeaseId = lease.LeaseId,
                ProviderName = lease.ProviderName,
                RequestedPath = request.RequestedPath,
                Succeeded = false,
                FailureReason = "Provider reported a simulated cleanup failure.",
                CleanupCommand = lease.CleanupCommand
            });
        }

        _leases.TryRemove(lease.LeaseId, out _);
        return Task.FromResult(new ExecutionCleanupReceipt
        {
            LeaseId = lease.LeaseId,
            ProviderName = lease.ProviderName,
            RequestedPath = request.RequestedPath,
            RemovedPaths = [lease.RootPath],
            Succeeded = true,
            CleanupCommand = lease.CleanupCommand
        });
    }

    private static void ValidateRequest(ExecutionLeaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaseCommit);
    }

    private ExecutionLease GetLease(string leaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);
        if (!_leases.TryGetValue(leaseId, out var lease))
        {
            throw new KeyNotFoundException($"Execution lease '{leaseId}' was not found.");
        }

        return lease;
    }

    private static void EnsureOperationAllowed(ExecutionLease lease, string operation)
    {
        if (!lease.AllowedOperations.Contains(operation))
        {
            throw new InvalidOperationException($"Lease '{lease.LeaseId}' does not allow '{operation}' operations.");
        }
    }

    private static ArtifactReference CreateArtifactRef(
        string leaseId,
        string relativePath,
        string mediaType,
        string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new ArtifactReference(
            $"memory://execution-leases/{leaseId}/{relativePath}",
            mediaType,
            hash,
            bytes.Length);
    }

    private static string RenderCommandLine(string command, IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return command;
        }

        return $"{command} {string.Join(' ', arguments.Select(QuoteArgument))}";
    }

    private static string QuoteArgument(string argument) =>
        argument.Contains(' ', StringComparison.Ordinal) ? $"\"{argument}\"" : argument;
}
