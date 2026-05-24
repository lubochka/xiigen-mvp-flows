namespace Xiigen.Flow.MetaFlowEngine.Application;

public static class MetaFlowEngineBfaRuleRegistry
{
    public static readonly IReadOnlyList<MetaFlowEngineBfaRule> Rules =
        MetaFlowEngineConstants.RequiredBfaRules
            .Select(ruleId => new MetaFlowEngineBfaRule(
                ruleId,
                MetaFlowEngineConstants.FlowId,
                RuleType(ruleId),
                Description(ruleId),
                Severity(ruleId),
                "CROSS_FLOW",
                "GLOBAL",
                "MASTER_TENANT_ID"))
            .ToArray();

    private static string RuleType(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number switch
        {
            >= 510 and <= 515 => "GAP_DETECTION_AND_PLAN_SAFETY",
            >= 516 and <= 522 => "CONTRACT_GENERATION_AND_COMPLETENESS",
            >= 523 and <= 530 => "GENESIS_VALIDATION_AND_SANDBOX_ISOLATION",
            >= 531 and <= 538 => "EVIDENCE_GITOPS_AND_REGISTRY_ASSIMILATION",
            >= 539 and <= 544 => "PROMOTION_AND_COMPLETION_GOVERNANCE",
            >= 989 and <= 995 => "P10_EDGE_CASE_ENFORCEMENT",
            _ => "META_FLOW_GOVERNANCE"
        };
    }

    private static string Severity(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number is 512 or 514 or 519 or 533 or 542 or >= 991 and <= 995 ? "WARN_CONTINUE" : "BUILD_FAILURE";
    }

    private static string Description(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number switch
        {
            510 => "Capability gap detection must check all active registries before emitting a gap.",
            511 => "Gap detection queries are tenant-scoped and cannot read another tenant capability registry.",
            516 => "Generated factory identifiers must not collide with the existing factory registry.",
            518 => "Generated task type identifiers must not collide with the existing task type catalog.",
            523 => "Generated code cannot import provider SDKs directly and must use fabric interfaces.",
            528 => "Sandbox fabric providers are isolated from production database, queue, and cache namespaces.",
            531 => "Evidence bundles must contain every required section before promotion evaluation.",
            536 => "CI stage must invoke FLOW-25 impact analysis before accepting generated flow changes.",
            539 => "Promotion criteria are machine-fixed and cannot be bypassed by configuration.",
            541 => "CORE promotion always requires human approval.",
            544 => "Original user flow execution is gated by FLOW-25 governance before production use.",
            989 => "Concurrent dynamic writes must use optimistic concurrency and report 409 for the loser.",
            990 => "Repeated writes with the same idempotency key must replay the cached response.",
            991 => "Boundary input validation returns field-level errors without partial writes.",
            992 => "Fabric timeout failures retain outbox records for retry.",
            993 => "Duplicate concurrent write coverage mirrors CF-989 for simulation-derived specs.",
            994 => "Duplicate idempotency coverage mirrors CF-990 for simulation-derived specs.",
            995 => "Duplicate boundary validation coverage mirrors CF-991 for simulation-derived specs.",
            _ => $"Meta Flow rule {ruleId} from the FLOW-26 conflict and stress-test corpus."
        };
    }
}

