using System.Collections.Concurrent;

namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// Registry for dynamically registering intents and domains at runtime.
/// </summary>
public class IntentTaxonomyRegistry : IIntentTaxonomyRegistry
{
    private readonly ConcurrentDictionary<string, DomainDefinition> _domains = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IntentTaxonomyRegistry"/> class.
    /// </summary>
    public IntentTaxonomyRegistry()
    {
        // Register default intents from static IntentTaxonomy
        RegisterDefaultIntents();
    }

    /// <inheritdoc />
    public void RegisterIntent(
        string domain,
        string action,
        string? subType,
        string[] requiredParameters,
        string[]? optionalParameters = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be null or empty", nameof(domain));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        var domainDef = _domains.GetOrAdd(domain, d => new DomainDefinition(d));

        var intentDef = new IntentDefinition
        {
            Domain = domain,
            Action = action,
            SubType = subType,
            RequiredParameters = requiredParameters ?? Array.Empty<string>(),
            OptionalParameters = optionalParameters ?? Array.Empty<string>(),
            Description = description
        };

        // Register both formats for lookup
        domainDef.Intents[intentDef.FullName] = intentDef;
        if (!string.IsNullOrEmpty(subType))
        {
            domainDef.Intents[intentDef.FullNameUnderscore] = intentDef;
        }
    }

    /// <inheritdoc />
    public void RegisterDomain(
        string domain,
        string? description = null,
        string? targetService = null,
        Dictionary<string, string>? actionDescriptions = null)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be null or empty", nameof(domain));

        var domainDef = _domains.GetOrAdd(domain, d => new DomainDefinition(d));
        domainDef.Description = description;
        domainDef.TargetService = targetService;

        if (actionDescriptions != null)
        {
            foreach (var (action, actionDesc) in actionDescriptions)
            {
                domainDef.ActionDescriptions[action] = actionDesc;
            }
        }
    }

    /// <inheritdoc />
    public IntentTaxonomy GetTaxonomy()
    {
        return new IntentTaxonomy(_domains.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRequiredParameters(string domain, string action, string? subType = null)
    {
        var intentDef = GetIntentDefinition(domain, action, subType);
        return intentDef?.RequiredParameters ?? Array.Empty<string>();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetOptionalParameters(string domain, string action, string? subType = null)
    {
        var intentDef = GetIntentDefinition(domain, action, subType);
        return intentDef?.OptionalParameters ?? Array.Empty<string>();
    }

    /// <inheritdoc />
    public string? GetTargetService(string domain)
    {
        if (_domains.TryGetValue(domain, out var domainDef))
        {
            return domainDef.TargetService;
        }

        // Fallback to static taxonomy
        return IntentTaxonomy.DomainToService.GetValueOrDefault(domain);
    }

    private IntentDefinition? GetIntentDefinition(string domain, string action, string? subType)
    {
        if (!_domains.TryGetValue(domain, out var domainDef))
        {
            return null;
        }

        // Try both formats
        var intentKey = string.IsNullOrEmpty(subType)
            ? $"{domain}.{action}"
            : $"{domain}.{action}.{subType}";

        if (domainDef.Intents.TryGetValue(intentKey, out var intentDef))
        {
            return intentDef;
        }

        // Try underscore format
        var intentKeyUnderscore = string.IsNullOrEmpty(subType)
            ? $"{domain}.{action}"
            : $"{domain}.{action}_{subType}";

        domainDef.Intents.TryGetValue(intentKeyUnderscore, out intentDef);
        return intentDef;
    }

    private void RegisterDefaultIntents()
    {
        // Register default domains
        foreach (var domain in IntentTaxonomy.DomainNames)
        {
            var targetService = IntentTaxonomy.DomainToService.GetValueOrDefault(domain);
            RegisterDomain(domain, targetService: targetService);
        }

        // Register default intents
        foreach (var (intentKey, requiredParams) in IntentTaxonomy.RequiredParameters)
        {
            var (domain, action, subType) = ParseIntentKey(intentKey);
            var optionalParams = IntentTaxonomy.OptionalParameters.GetValueOrDefault(intentKey, Array.Empty<string>());
            RegisterIntent(domain, action, subType, requiredParams.ToArray(), optionalParams.ToArray());
        }
    }

    private static (string Domain, string Action, string? SubType) ParseIntentKey(string intentKey)
    {
        var parts = intentKey.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException($"Invalid intent key format: {intentKey}", nameof(intentKey));
        }

        var domain = parts[0];
        var actionPart = parts[1];

        // Check if action contains underscore (e.g., "create_personal")
        var actionSubParts = actionPart.Split('_', 2, StringSplitOptions.RemoveEmptyEntries);
        if (actionSubParts.Length == 2)
        {
            return (domain, actionSubParts[0], actionSubParts[1]);
        }

        // Check if there's a third part (domain.action.subtype)
        if (parts.Length >= 3)
        {
            return (domain, actionPart, parts[2]);
        }

        return (domain, actionPart, null);
    }
}
