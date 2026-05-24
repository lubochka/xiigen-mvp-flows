namespace Xiigen.Flow.BfaCrossFlowGovernance.Application;

public static class BfaCrossFlowGovernanceBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaCrossFlowGovernanceBfaRule> Rules =
        BfaCrossFlowGovernanceConstants.RequiredBfaRules
            .Select(ruleId => new BfaCrossFlowGovernanceBfaRule(
                ruleId,
                BfaCrossFlowGovernanceConstants.FlowId,
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
            >= 473 and <= 476 => "CHANGE_INTAKE_AND_DEPENDENCY_SCOPE",
            >= 477 and <= 482 => "STATIC_AND_SEMANTIC_CONFLICT_DETECTION",
            >= 483 and <= 488 => "SEVERITY_AND_STATE_MACHINE",
            >= 489 and <= 496 => "HUMAN_DECISION_AND_RESOLUTION",
            >= 497 and <= 509 => "AUDIT_ISOLATION_AND_CROSS_FLOW_GATE",
            >= 510 and <= 544 => "SELF_BUILD_AND_EXTENSION_GOVERNANCE",
            _ => "BFA_GOVERNANCE"
        };
    }

    private static string Severity(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number is 475 or 485 or 490 ? "WARN_CONTINUE" : "BUILD_FAILURE";
    }

    private static string Description(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number switch
        {
            473 => "Proposed change type must be one of the approved flow definition surfaces.",
            474 => "Proposed change identity and content hash must deduplicate replayed submissions.",
            476 => "Dependency queries must be tenant-scoped before any cross-flow lookup.",
            477 => "Required field removal or endpoint removal is a breaking conflict.",
            480 => "Semantic impact analysis cannot run before static conflict detection completes.",
            483 => "Critical composite severity must block publish and enter arbitration.",
            487 => "Every arbitration state transition persists before emitting downstream events.",
            489 => "Force-proceed options are hidden unless the actor has override authority.",
            493 => "Duplicate decision submission must be idempotent and never duplicate side effects.",
            497 => "Closing an arbitration session requires an immutable audit record.",
            501 => "Cross-tenant guard output is aggregate-only and cannot leak tenant-specific data.",
            504 => "Minor-safety linked flow changes cannot be force-proceeded.",
            507 => "Generated code cannot bypass BFA governance for AI adapter changes.",
            510 => "Capability gap detection must check all active registries before emitting a gap.",
            536 => "CI stage must invoke FLOW-25 impact analysis before accepting generated flow changes.",
            544 => "Original user flow execution is gated by FLOW-25 governance before production use.",
            _ => $"BFA governance rule {ruleId} from the FLOW-25 conflict and stress-test corpus."
        };
    }
}
