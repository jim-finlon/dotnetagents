// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Memory.Advanced;

/// <summary>
/// Episodic memory: store and recall episodes by similarity or time. FR-MEM-001.
/// </summary>
public interface IEpisodicMemory
{
    /// <summary>Store an episode (embedding computed by implementation if needed).</summary>
    Task StoreEpisodeAsync(Episode episode, CancellationToken cancellationToken = default);

    /// <summary>Recall episodes similar to the cue (e.g. by embedding similarity).</summary>
    Task<IReadOnlyList<Episode>> RecallSimilarAsync(string cue, int k = 5, CancellationToken cancellationToken = default);

    /// <summary>Recall episodes in a time range.</summary>
    Task<IReadOnlyList<Episode>> RecallTemporalAsync(DateTimeOffset start, DateTimeOffset endTime, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>Optional: consolidate old episodes (e.g. summarize, merge).</summary>
    Task ConsolidateAsync(CancellationToken cancellationToken = default);
}
