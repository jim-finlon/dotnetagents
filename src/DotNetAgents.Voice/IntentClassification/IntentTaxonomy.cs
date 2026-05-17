namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// Defines the taxonomy of supported intents and their configurations.
/// This class provides static access to the default taxonomy, but dynamic registration
/// is supported through <see cref="IIntentTaxonomyRegistry"/>.
/// </summary>
public class IntentTaxonomy
{
    /// <summary>
    /// Gets the dictionary of domain definitions.
    /// </summary>
    public IReadOnlyDictionary<string, DomainDefinition> Domains { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntentTaxonomy"/> class.
    /// </summary>
    /// <param name="domains">The dictionary of domain definitions.</param>
    public IntentTaxonomy(IReadOnlyDictionary<string, DomainDefinition> domains)
    {
        Domains = domains ?? throw new ArgumentNullException(nameof(domains));
    }

    /// <summary>
    /// Gets the list of supported domain names (for backward compatibility).
    /// </summary>
    [Obsolete("Use IntentTaxonomy.Default.Domains instead")]
    public static readonly IReadOnlyList<string> DomainNames = new[]
    {
        "notes",
        "tasks",
        "calendar",
        "business",
        "media",
        "research"
    };

    /// <summary>
    /// Gets the list of supported actions.
    /// </summary>
    public static readonly IReadOnlyList<string> Actions = new[]
    {
        "create",
        "list",
        "update",
        "delete",
        "query",
        "analyze",
        "generate",
        "schedule",
        "reminder"
    };

    /// <summary>
    /// Gets the mapping of domains to their target MCP services (for backward compatibility).
    /// </summary>
    [Obsolete("Use IntentTaxonomy.Default.Domains[domain].TargetService instead")]
    public static readonly IReadOnlyDictionary<string, string> DomainToService = new Dictionary<string, string>
    {
        ["notes"] = "session_persistence",
        ["tasks"] = "business_manager",
        ["calendar"] = "time_management",
        ["business"] = "business_manager",
        ["media"] = "privateer_gen",
        ["research"] = "session_persistence"
    };

    /// <summary>
    /// Gets the required parameters for each intent type (for backward compatibility).
    /// </summary>
    [Obsolete("Use IIntentTaxonomyRegistry.GetRequiredParameters instead")]
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RequiredParameters = new Dictionary<string, IReadOnlyList<string>>
    {
        // Notes
        ["notes.create"] = new[] { "content" },
        ["notes.list"] = Array.Empty<string>(),
        ["notes.delete"] = new[] { "note_id" },

        // Tasks
        ["tasks.create_personal"] = new[] { "title" },
        ["tasks.create_team"] = new[] { "title", "assignee" },
        ["tasks.list"] = Array.Empty<string>(),
        ["tasks.complete"] = new[] { "task_id" },

        // Calendar
        ["calendar.create_event"] = new[] { "title", "date", "time" },
        ["calendar.create_reminder"] = new[] { "title", "date", "time" },
        ["calendar.query"] = new[] { "date_range" },

        // Business
        ["business.create_invoice"] = new[] { "client", "amount" },
        ["business.list_invoices"] = Array.Empty<string>(),
        ["business.create_project"] = new[] { "name" },

        // Media
        ["media.generate"] = new[] { "type", "prompt" },

        // Research
        ["research.topic"] = new[] { "topic" },
        ["research.and_save"] = new[] { "topic" }
    };

    /// <summary>
    /// Gets the optional parameters for each intent type (for backward compatibility).
    /// </summary>
    [Obsolete("Use IIntentTaxonomyRegistry.GetOptionalParameters instead")]
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> OptionalParameters = new Dictionary<string, IReadOnlyList<string>>
    {
        ["notes.create"] = new[] { "title", "category", "tags" },
        ["tasks.create_personal"] = new[] { "description", "due_date", "priority" },
        ["tasks.create_team"] = new[] { "description", "due_date", "priority" },
        ["calendar.create_event"] = new[] { "attendees", "location", "description" },
        ["business.create_invoice"] = new[] { "description", "due_date", "items" },
        ["media.generate"] = new[] { "style", "duration", "format" }
    };

    /// <summary>
    /// Gets the static default taxonomy for backward compatibility.
    /// Must be declared after <see cref="DomainNames"/> / parameter dictionaries so static initialization runs in order.
    /// </summary>
    public static IntentTaxonomy Default { get; } = CreateDefault();

    private static IntentTaxonomy CreateDefault()
    {
        var registry = new IntentTaxonomyRegistry();
        return registry.GetTaxonomy();
    }
}
