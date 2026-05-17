using System.Text.RegularExpressions;

namespace DotNetAgents.Skills;

/// <summary>
/// Keyword/substring-match based <see cref="ISkillRetriever"/>. MVP retrieval strategy:
/// extract significant tokens from the task description; score each skill by the count of
/// task tokens appearing in the skill's name + description. Embedding-based retrieval
/// (the production target per the story description) lands as a follow-up that requires
/// the LLM gateway dependency.
/// </summary>
/// <remarks>
/// Scoring algorithm:
/// <list type="number">
///   <item>Tokenize task description; lowercase; strip stopwords + punctuation.</item>
///   <item>For each skill, lowercase its name + description.</item>
///   <item>Score = (count of unique task tokens that appear in skill text) / max(1, unique task token count).</item>
///   <item>Tie-break by skill id ascending for stability.</item>
/// </list>
/// This is intentionally simple — the contract is "give us top-K relevant" and a future
/// embedding retriever can swap in without changing callers.
/// </remarks>
public sealed class KeywordSkillRetriever : ISkillRetriever
{
    private static readonly HashSet<string> _stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "is", "are", "was", "were", "be", "been",
        "have", "has", "had", "do", "does", "did", "will", "would", "should", "could",
        "may", "might", "must", "can", "this", "that", "these", "those", "i", "you", "he",
        "she", "it", "we", "they", "what", "which", "who", "whom", "whose", "of", "at",
        "by", "for", "with", "about", "to", "from", "in", "out", "on", "off", "over",
        "under", "again", "further", "then", "once", "here", "there", "when", "where",
        "why", "how", "all", "any", "both", "each", "few", "more", "most", "other",
        "some", "such", "no", "not", "only", "own", "same", "so", "than", "too", "very",
        "s", "t", "just", "don", "now",
    };

    private static readonly Regex _tokenSplitter = new(@"[^\w]+", RegexOptions.Compiled);

    private readonly ISkillRegistry _registry;

    public KeywordSkillRetriever(ISkillRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public IReadOnlyList<SkillMatch> Match(string taskDescription, int topK = 3)
    {
        ArgumentNullException.ThrowIfNull(taskDescription);
        if (topK <= 0) return Array.Empty<SkillMatch>();

        var taskTokens = Tokenize(taskDescription);
        if (taskTokens.Count == 0) return Array.Empty<SkillMatch>();

        var skills = _registry.All();
        if (skills.Count == 0) return Array.Empty<SkillMatch>();

        var scored = new List<SkillMatch>(skills.Count);
        foreach (var skill in skills)
        {
            var skillText = (skill.Name + " " + skill.Description).ToLowerInvariant();
            var hits = taskTokens.Count(t => skillText.Contains(t, StringComparison.Ordinal));
            var score = (double)hits / taskTokens.Count;
            if (score > 0)
            {
                scored.Add(new SkillMatch(skill, score));
            }
        }

        return scored
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.Skill.Id, StringComparer.OrdinalIgnoreCase)
            .Take(topK)
            .ToArray();
    }

    private static HashSet<string> Tokenize(string text)
    {
        var tokens = _tokenSplitter
            .Split(text.ToLowerInvariant())
            .Where(t => t.Length >= 3 && !_stopwords.Contains(t));
        return new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);
    }
}
