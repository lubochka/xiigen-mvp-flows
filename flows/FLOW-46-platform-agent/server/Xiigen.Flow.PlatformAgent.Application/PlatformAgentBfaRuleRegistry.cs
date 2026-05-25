namespace Xiigen.Flow.PlatformAgent.Application;

public static class PlatformAgentBfaRuleRegistry
{
    public static readonly IReadOnlyList<PlatformAgentBfaRule> Rules =
    [
        Rule("CF-839", "T651", "TENANT_ISOLATION", "TenantScopeGateway writes xiigen-agent-actions audit before the bounded cross-tenant context switch."),
        Rule("CF-840", "T653", "COST_GATE", "SuperJudge returns DEFER_TO_AF9 with zero LLM cost when platformPatternsMatched is zero."),
        Rule("CF-841", "T655", "PRIVACY", "PatternContributor sanitizer failure writes SANITIZATION_FAILED and does not write global RAG patterns."),
        Rule("CF-1082", "T650", "CONCURRENCY_CONSTRAINT", "Optimistic concurrency on platform-agent mutation returns one winner and one 409 loser."),
        Rule("CF-1083", "T650", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns cached session without duplicate side effects."),
        Rule("CF-1084", "T650", "ORDERING_CONSTRAINT", "Boundary validation rejects empty intent before partial writes."),
        Rule("CF-1085", "T654", "DNA8_ORDERING", "Fabric timeout retains outbox row for deduplicative retry."),
        Rule("CF-1086", "T654", "CONCURRENCY_CONSTRAINT", "Action publishing mutation honors optimistic concurrency."),
        Rule("CF-1087", "T654", "IDEMPOTENCY_CONSTRAINT", "Action replay does not duplicate proposed actions."),
        Rule("CF-1088", "T654", "ORDERING_CONSTRAINT", "Action boundary validation rejects missing target for tenant actions."),
        Rule("CF-1089", "T654", "DNA8_ORDERING", "Action state is written before AgentActionProposed CloudEvent."),
        Rule("CF-1090", "T655", "CONCURRENCY_CONSTRAINT", "Contribution mutation honors optimistic concurrency."),
        Rule("CF-1091", "T655", "IDEMPOTENCY_CONSTRAINT", "Contribution replay does not duplicate shared patterns."),
        Rule("CF-1092", "T655", "ORDERING_CONSTRAINT", "Contribution boundary validation rejects empty primary fields."),
        Rule("CF-1093", "T655", "DNA8_ORDERING", "Contribution state is retained before retryable publication."),
        Rule("CF-1094", "T656", "CONCURRENCY_CONSTRAINT", "Agent chat client handles concurrent action confirmation safely."),
        Rule("CF-1095", "T656", "IDEMPOTENCY_CONSTRAINT", "Agent chat client action confirmation is idempotent.")
    ];

    private static PlatformAgentBfaRule Rule(string id, string task, string type, string description) =>
        new(id, PlatformAgentConstants.FlowId, task, type, description, "BUILD_FAILURE", "GLOBAL", "TENANT_CONTEXT");
}