namespace Xiigen.Flow.AdaptiveRagDeepResearch.Application;

public static class AdaptiveRagDeepResearchBfaRuleRegistry
{
    public static readonly IReadOnlyList<AdaptiveRagDeepResearchBfaRule> Rules =
        AdaptiveRagDeepResearchConstants.RequiredBfaRules
            .Select(ruleId => new AdaptiveRagDeepResearchBfaRule(
                ruleId,
                AdaptiveRagDeepResearchConstants.FlowId,
                RuleType(ruleId),
                Description(ruleId),
                Severity(ruleId),
                int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture) >= 1007 ? "FLOW_SCOPED" : "CROSS_FLOW",
                "GLOBAL",
                "TENANT_CONTEXT"))
            .ToArray();

    private static string RuleType(string ruleId)
    {
        var number = int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture);
        return number switch
        {
            601 or 602 or 607 or 608 or 624 or 626 => "ROUTING_POLICY_GOVERNANCE",
            603 or 604 or 605 or 606 or 616 or 623 => "RAG_GRAPH_SCOPE_AND_INDEXING",
            609 or 610 or 614 or 615 or 619 or 620 => "PROMPT_AND_ASSET_PROMOTION",
            611 or 612 or 617 or 618 or 621 or 622 or 625 => "OBSERVABILITY_FEEDBACK_AND_BUDGET",
            >= 1007 and <= 1010 => "P10_EDGE_CASE_ENFORCEMENT",
            _ => "ADAPTIVE_RAG_BFA_RULE"
        };
    }

    private static string Severity(string ruleId)
    {
        var number = int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture);
        return number is 602 or 604 or 608 or 609 or 610 or 611 or 616 or 617 or 619 or 620 or 621 or 622 or 1007 or 1008 or 1009 or 1010
            ? "BUILD_FAILURE"
            : "WARN_CONTINUE";
    }

    private static string Description(string ruleId)
    {
        var number = int.Parse(ruleId[3..], System.Globalization.CultureInfo.InvariantCulture);
        return number switch
        {
            601 => "RAG routing strategy configuration remains compatible with FLOW-09 and FLOW-13 consumers.",
            602 => "Route gates cannot bypass BFA governance for adaptive RAG entry points.",
            603 => "Vector index version changes use rolling update metadata and keep a rollback target.",
            604 => "GraphRAG queries include tenant scope and reject cross-tenant injection.",
            605 => "Community rebuilds never run synchronously in the query path.",
            606 => "Hybrid fusion output shape stays compatible for existing consumers.",
            607 => "Bandit routing cannot override explicit FLOW-13 AI configuration.",
            608 => "Bandit policy updates are async queue work and never inline request mutation.",
            609 => "Prompt versions are immutable after activation.",
            610 => "Prompt promotion requires a quality gate before activation.",
            611 => "Feedback requires a valid trace id from an existing research run.",
            612 => "Hallucination checks use the multi-model dispatcher instead of a single hard-coded model.",
            613 => "Domain profiles are tenant-scoped.",
            614 => "Domain extraction prompts are versioned before activation.",
            615 => "RAG asset promotion is blocked when quality regression exceeds three percent.",
            616 => "Knowledge graph edits require the graph edit BFA gate.",
            617 => "Self-reflection may only reduce token use, never expand the context budget.",
            618 => "A/B allocation is deterministic by tenant, user, and experiment.",
            619 => "Control-plane assets are immutable and require new versions for changes.",
            620 => "Promotion ladder follows GENERATED to INJECTED to MINIMAL to CORE with no skipped step.",
            621 => "Observability uses ITraceSpanService or fabric interfaces, never direct OpenTelemetry SDK imports.",
            622 => "Budget caps surface BudgetExceeded instead of silently truncating context.",
            623 => "Graph traversal max hops and community level resolve from FREEDOM config, not hard-coded constants.",
            624 => "Bandit arm structure changes require an explicit migration plan.",
            625 => "Visual control-plane node types resolve from FREEDOM config instead of hard-coded UI lists.",
            626 => "Feedback aggregation waits for at least 100 runs or 24 hours before policy update.",
            1007 => "Concurrent POST /api/dynamic/adaptive-rag-deep-research uses optimistic concurrency and returns 409 to stale writers.",
            1008 => "X-Idempotency-Key replay returns cached 200 without duplicate state or outbox effects.",
            1009 => "Boundary validation returns 400 field details and writes no partial state.",
            1010 => "Fabric timeout returns 504 and retains outbox state for retry.",
            _ => $"Adaptive RAG Deep Research rule {ruleId}."
        };
    }
}
