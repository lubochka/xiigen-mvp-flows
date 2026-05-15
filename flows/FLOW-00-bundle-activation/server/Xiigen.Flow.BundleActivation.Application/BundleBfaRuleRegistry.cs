namespace Xiigen.Flow.BundleActivation.Application;

public sealed record BundleBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantScope);

public static class BundleBfaRuleRegistry
{
    public static readonly IReadOnlyList<BundleBfaRule> Rules =
    [
        new("CF-842", "FLOW-00", "CONCURRENCY_CONSTRAINT", "Optimistic concurrency on POST /api/dynamic/bundle-activation returns one 201 winner and one 409 loser.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-843", "FLOW-00", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns cached response without duplicate side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-844", "FLOW-00", "ORDERING_CONSTRAINT", "Boundary validation rejects empty, null, or zero primary input with VALIDATION_FAILED and no partial write.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-845", "FLOW-00", "DNA8_ORDERING", "Fabric timeout returns TIMEOUT while retaining outbox record for retry.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-846", "FLOW-00", "CONCURRENCY_CONSTRAINT", "Duplicate concurrency row from P9 is preserved as P10/P11 coverage.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-847", "FLOW-00", "IDEMPOTENCY_CONSTRAINT", "Duplicate idempotency row from P9 is preserved as P10/P11 coverage.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-848", "FLOW-00", "ORDERING_CONSTRAINT", "Duplicate boundary validation row from P9 is preserved as P10/P11 coverage.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-849", "FLOW-00", "DNA8_ORDERING", "Duplicate timeout/outbox row from P9 is preserved as P10/P11 coverage.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID")
    ];
}
