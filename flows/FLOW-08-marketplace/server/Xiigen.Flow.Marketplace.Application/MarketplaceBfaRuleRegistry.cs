namespace Xiigen.Flow.Marketplace.Application;

public static class MarketplaceBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-08-1", "BROKER_ACK_GATE", "Listing publication emits queue-visible marketplace events only after the listing/audit state is committed."),
        Rule("CF-08-2", "BUSINESS_KEY_IDEMPOTENCY", "Listing publication idempotency is stable for tenant, seller, listing, and idempotency key; retries do not duplicate side effects."),
        Rule("CF-08-3", "DATABASE_FABRIC_FOR_COUNTER", "Marketplace publication counters and feed generation state are stored in the document fabric, not process memory."),
        Rule("CF-08-4", "SCOPE_ISOLATION", "Listings, catalog rows, draft records, analytics, and audit rows are private per tenant except platform-admin/platform-support aggregate views."),
        Rule("CF-895", "CONCURRENCY_CONSTRAINT", "Optimistic concurrency on POST /api/dynamic/marketplace returns one 201 winner and one 409 CONCURRENT_UPDATE loser."),
        Rule("CF-896", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached publication response and creates no duplicate audit, listing, catalog, feed, analytics, or outbox rows."),
        Rule("CF-897", "VALIDATION_CONSTRAINT", "Empty or null primary marketplace fields return 400 VALIDATION_FAILED and do not write listing/catalog state."),
        Rule("CF-898", "DNA8_ORDERING", "Fabric timeout returns 504 TIMEOUT while retaining an outbox recovery event for queue retry."),
        Rule("CF-899", "CONCURRENCY_CONSTRAINT", "Duplicate concurrency coverage for retried marketplace mutations remains enforced for the same tenant-scoped listing key."),
        Rule("CF-900", "IDEMPOTENCY_CONSTRAINT", "Repeated idempotency coverage proves all three retries return the same cached marketplace response."),
        Rule("CF-901", "VALIDATION_CONSTRAINT", "Duplicate boundary coverage proves zero price is accepted as free while blank title/category remain validation failures."),
        Rule("CF-902", "DNA8_ORDERING", "Duplicate timeout coverage proves outbox recovery is retained and no event is emitted before state storage.")
    ];

    private static BfaRuleDefinition Rule(string id, string type, string description) => new(
        id,
        MarketplaceConstants.FlowId,
        type,
        description,
        "BUILD_FAILURE",
        "FLOW_SCOPED",
        "GLOBAL",
        "MASTER_TENANT_ID");
}
