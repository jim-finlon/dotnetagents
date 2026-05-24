// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Governance.AgentPublishing;
using DotNetAgents.Governance.Workflows;

namespace DotNetAgents.Governance.AgentBuilder;

public sealed record NaturalLanguageAgentDraftRequest(
    string RequestId,
    string RequestedByActorId,
    string Description,
    DateTimeOffset RequestedAt);

public sealed record AgentDraftConnectorRequirement(
    string ToolOrConnectorId,
    string Reason,
    bool RequiresApproval);

public sealed record NaturalLanguageAgentDraft(
    string DraftId,
    string ProposedGoal,
    IReadOnlyList<string> ProposedInputs,
    WorkflowDefinitionContract Workflow,
    IReadOnlyList<AgentDraftConnectorRequirement> ConnectorRequirements,
    IReadOnlyList<string> OpenQuestions,
    string OutputShape,
    AgentPublishingState InitialPublishingState,
    bool CanPublishWithoutReview,
    IReadOnlyList<string> PublishBlockers);

public static class NaturalLanguageAgentDraftBuilder
{
    private static readonly string[] ActionTerms =
    [
        "send",
        "post",
        "publish",
        "create",
        "update",
        "delete",
        "notify",
        "ticket",
        "message"
    ];

    public static NaturalLanguageAgentDraft CreateDraft(NaturalLanguageAgentDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var description = request.Description.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(request));
        }

        var normalized = description.ToLowerInvariant();
        var requiresAction = ActionTerms.Any(normalized.Contains);
        var connectorRequirements = BuildConnectorRequirements(normalized);
        var openQuestions = BuildOpenQuestions(description, normalized, connectorRequirements, requiresAction);
        var workflow = BuildWorkflow(request.RequestId, requiresAction, connectorRequirements);

        var publishBlockers = new List<string>
        {
            "Human review receipt is required before shared or verified publishing."
        };

        if (connectorRequirements.Any(requirement => requirement.RequiresApproval))
        {
            publishBlockers.Add("Connector/action approvals must be resolved before runtime execution.");
        }

        if (openQuestions.Count > 0)
        {
            publishBlockers.Add("Open questions must be answered before publishing.");
        }

        return new NaturalLanguageAgentDraft(
            CreateDraftId(request.RequestId),
            ProposedGoal: description,
            ProposedInputs: BuildInputs(normalized),
            workflow,
            connectorRequirements,
            openQuestions,
            OutputShape: DetectOutputShape(normalized),
            InitialPublishingState: AgentPublishingState.PersonalDraft,
            CanPublishWithoutReview: false,
            publishBlockers);
    }

    private static WorkflowDefinitionContract BuildWorkflow(
        string requestId,
        bool requiresAction,
        IReadOnlyList<AgentDraftConnectorRequirement> connectorRequirements)
    {
        var steps = new List<WorkflowStepContract>
        {
            new(
                "understand_request",
                WorkflowStepKind.Reason,
                [WorkflowStepMemoryScopeKind.None],
                FallbackBehavior: "ask the requester to restate the agent goal"),
            new(
                "draft_plan",
                WorkflowStepKind.Respond,
                [WorkflowStepMemoryScopeKind.PreviousStep],
                FallbackBehavior: "return the safest partial draft with open questions")
        };

        if (requiresAction)
        {
            steps.Insert(1, new WorkflowStepContract(
                "connector_policy_gate",
                WorkflowStepKind.PolicyGate,
                [WorkflowStepMemoryScopeKind.PreviousStep],
                FallbackBehavior: "block action steps until connector approval is reviewed"));

            steps.Insert(2, new WorkflowStepContract(
                "proposed_action",
                WorkflowStepKind.Act,
                [WorkflowStepMemoryScopeKind.PreviousStep],
                ActionPolicy: new WorkflowStepActionPolicy(
                    connectorRequirements.Select(requirement => requirement.ToolOrConnectorId).DefaultIfEmpty("connector.action.review_required").ToList(),
                    RequiresApproval: true,
                    ["connector_approval_receipt"]),
                FallbackBehavior: "emit draft-only action warning without executing"));
        }

        return new WorkflowDefinitionContract(
            $"agent_builder_{SanitizeId(requestId)}",
            "natural-language-agent-request.v1",
            steps);
    }

    private static IReadOnlyList<AgentDraftConnectorRequirement> BuildConnectorRequirements(string normalized)
    {
        var requirements = new List<AgentDraftConnectorRequirement>();

        AddIfMentioned(requirements, normalized, "chat", "chat.messages.send");
        AddIfMentioned(requirements, normalized, "email", "email.messages.send");
        AddIfMentioned(requirements, normalized, "github", "github.issues.write");
        AddIfMentioned(requirements, normalized, "jira", "jira.issues.write");
        AddIfMentioned(requirements, normalized, "calendar", "calendar.events.write");

        if (requirements.Count == 0 && ActionTerms.Any(normalized.Contains))
        {
            requirements.Add(new(
                "connector.action.review_required",
                "The request asks for side effects but does not name a concrete connector.",
                RequiresApproval: true));
        }

        return requirements;
    }

    private static void AddIfMentioned(
        ICollection<AgentDraftConnectorRequirement> requirements,
        string normalized,
        string term,
        string toolOrConnectorId)
    {
        if (normalized.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            requirements.Add(new(
                toolOrConnectorId,
                $"Natural-language request mentions {term} action.",
                RequiresApproval: true));
        }
    }

    private static IReadOnlyList<string> BuildOpenQuestions(
        string description,
        string normalized,
        IReadOnlyList<AgentDraftConnectorRequirement> connectorRequirements,
        bool requiresAction)
    {
        var questions = new List<string>();

        if (!normalized.Contains("when") && !normalized.Contains("after") && !normalized.Contains("daily"))
        {
            questions.Add("What trigger should start this agent?");
        }

        if (!normalized.Contains("return") && !normalized.Contains("summar") && !normalized.Contains("report"))
        {
            questions.Add("What exact output should the agent produce?");
        }

        if (requiresAction && connectorRequirements.Count == 0)
        {
            questions.Add("Which connector and action should be requested for approval?");
        }

        if (description.Length < 24)
        {
            questions.Add("What business goal should this agent optimize for?");
        }

        return questions;
    }

    private static IReadOnlyList<string> BuildInputs(string normalized)
    {
        var inputs = new List<string> { "requester_intent" };

        if (normalized.Contains("email"))
        {
            inputs.Add("email_message");
        }

        if (normalized.Contains("customer") || normalized.Contains("contact"))
        {
            inputs.Add("contact_context");
        }

        return inputs;
    }

    private static string DetectOutputShape(string normalized)
    {
        if (normalized.Contains("summar"))
        {
            return "summary.v1";
        }

        if (normalized.Contains("ticket"))
        {
            return "ticket-draft.v1";
        }

        if (normalized.Contains("report"))
        {
            return "report.v1";
        }

        return "draft-response.v1";
    }

    private static string CreateDraftId(string requestId)
    {
        return $"draft-{SanitizeId(requestId)}";
    }

    private static string SanitizeId(string value)
    {
        var chars = value
            .Trim()
            .Select(static c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')
            .ToArray();

        var sanitized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "request" : sanitized;
    }
}
