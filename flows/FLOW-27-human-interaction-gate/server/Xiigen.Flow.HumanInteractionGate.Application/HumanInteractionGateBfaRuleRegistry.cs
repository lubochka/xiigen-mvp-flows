namespace Xiigen.Flow.HumanInteractionGate.Application;

public static class HumanInteractionGateBfaRuleRegistry
{
    public static readonly IReadOnlyList<HumanInteractionGateBfaRule> Rules =
        HumanInteractionGateConstants.RequiredBfaRules
            .Select(ruleId => new HumanInteractionGateBfaRule(
                ruleId,
                HumanInteractionGateConstants.FlowId,
                RuleType(ruleId),
                Description(ruleId),
                Severity(ruleId),
                ruleId is "CF-996" or "CF-997" or "CF-998" or "CF-999" or "CF-1000" or "CF-1001" or "CF-1002" ? "FLOW_SCOPED" : "CROSS_FLOW",
                "GLOBAL",
                "MASTER_TENANT_ID"))
            .ToArray();

    private static string RuleType(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number switch
        {
            545 or 552 or 557 => "TENANT_ISOLATION",
            546 or 547 or 548 or 551 or 556 or 558 or 561 or 565 => "HUMAN_TASK_STATE_MACHINE",
            549 or 559 or 560 => "SNAPSHOT_AND_AUDIT_INTEGRITY",
            550 or 554 or 562 or 563 or 564 => "DEPENDENCY_SCHEDULER",
            553 => "NOTIFICATION_DEDUPE",
            >= 996 and <= 1002 => "P10_EDGE_CASE_ENFORCEMENT",
            _ => "HUMAN_INTERACTION_GATE"
        };
    }

    private static string Severity(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number is 545 or 550 or 552 or 555 or 557 or 562 or 563 or 999 ? "BUILD_FAILURE" : "WARN_CONTINUE";
    }

    private static string Description(string ruleId)
    {
        var number = int.Parse(ruleId.AsSpan(3));
        return number switch
        {
            545 => "UserTask documents are tenant-scoped; cross-tenant task access returns 404.",
            546 => "Only one waiting_for_user task can exist per tenant, run, and node.",
            547 => "Quorum answers use atomic counters and close the gate once.",
            548 => "Expired claims auto-release and stale answer submissions are rejected.",
            549 => "Node snapshots are invalidated when source node state is newer.",
            550 => "Blocking HumanInteractionGate nodes block all transitive dependents until answered or expired.",
            551 => "Terminal run state transitions are idempotent no-ops once terminal.",
            552 => "Group membership and role resolution queries always include tenant scope.",
            553 => "Notifications are deduplicated per task, channel, and recipient.",
            554 => "allSettled joins ignore optional waiting/skipped branches without blocking required paths.",
            555 => "Completion cannot mark READY or SUCCEEDED while blocking UserTasks remain open.",
            556 => "Claim release affects only the claim; it never closes the UserTask.",
            557 => "NodeSnapshot documents and queries are tenant-isolated.",
            558 => "Expired UserTasks reject late answers and cannot phantom-resume branches.",
            559 => "SubFlow progress rolls up from the child RunSnapshot, not child node counts.",
            560 => "Every task reassignment writes an immutable audit event.",
            561 => "Quorum and allOf partial answers are append-only and preserved.",
            562 => "DAG cycles are detected before run start and fail with a cycle path.",
            563 => "Required terminal nodes must all succeed before READY declaration.",
            564 => "SubFlow HumanInteractionGate tasks do not propagate as parent-run open requirements.",
            565 => "allOf gates require every active target answer or explicit admin resolution.",
            996 => "Optimistic concurrency returns one 201 winner and one 409 loser.",
            997 => "Idempotent retries replay the cached response without duplicate effects.",
            998 => "Boundary validation returns field-level 400 without partial write.",
            999 => "Fabric timeouts retain outbox state and return retryable timeout status.",
            1000 => "Second optimistic concurrency coverage from P10 duplicate row.",
            1001 => "Second idempotency coverage from P10 duplicate row.",
            1002 => "Second boundary validation coverage from P10 duplicate row.",
            _ => $"Human Interaction Gate rule {ruleId}."
        };
    }
}
