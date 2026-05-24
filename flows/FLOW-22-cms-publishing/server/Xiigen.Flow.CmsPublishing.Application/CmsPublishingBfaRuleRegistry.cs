namespace Xiigen.Flow.CmsPublishing.Application;

public static class CmsPublishingBfaRuleRegistry
{
    public static IReadOnlyList<CmsPublishingBfaRule> Rules { get; } =
    [
        new("CF-22-1", CmsPublishingConstants.FlowId, "STATE_MACHINE", "OCC DRAFT to PUBLISHED state machine with version immutability.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-22-2", CmsPublishingConstants.FlowId, "APPROVAL_CONSTRAINT", "Sequential approval stages with role-based gates loaded from policy.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-22-3", CmsPublishingConstants.FlowId, "IDEMPOTENCY_CONSTRAINT", "SETNX scheduled-publish lock with durable timer retries.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-22-4", CmsPublishingConstants.FlowId, "DATA_PIPELINE", "Append-only analytics with PII exclusion.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-973", CmsPublishingConstants.FlowId, "CONCURRENCY_CONSTRAINT", "Optimistic concurrency on POST /api/dynamic/cms-publishing: winner 201, stale loser 409 CONCURRENT_UPDATE.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-974", CmsPublishingConstants.FlowId, "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns cached 200 and creates no duplicate side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-975", CmsPublishingConstants.FlowId, "VALIDATION_CONSTRAINT", "Boundary validation rejects empty/null/zero primary fields with VALIDATION_FAILED and no partial write.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-976", CmsPublishingConstants.FlowId, "DNA8_ORDERING", "Fabric timeout returns TIMEOUT while retaining outbox evidence for retry.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID")
    ];
}