namespace Xiigen.Flow.SaasMultiTenancy.Application;

public static class SaasMultiTenancyBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRule> Rules =
    [
        Rule("CF-15-1", "ATOMIC_TENANT_BOOTSTRAP", "Tenant provisioning uses a SETNX-style lock, seeds FREEDOM keys synchronously, activates the tenant, stores audit evidence, and then emits TenantProvisioned."),
        Rule("CF-15-2", "MACHINE_LOCKED_CONFIG", "Tenant configuration rejects machine-locked keys and writes mutable keys through optimistic concurrency."),
        Rule("CF-15-3", "QUOTA_AND_LIFECYCLE", "Quota counters materialize from tier definitions through one MULTI_EXEC batch, and tenant suspension never deletes tenant data."),
        Rule("CF-15-4", "TENANT_SCOPE_ISOLATION", "Tenant identity is resolved from tenant context or internal hashes, never from request body tenant scope fields."),
        Rule("CF-945", "P10_PRIMARY_CONCURRENCY", "Concurrent tenant provisioning for the same workspace allows one 201 winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-946", "P10_PRIMARY_IDEMPOTENCY", "Repeated tenant provisioning with one idempotency key replays the cached response and creates no duplicate tenant, FREEDOM, audit, quota, or event records."),
        Rule("CF-947", "P10_PRIMARY_VALIDATION", "Malformed tenant provisioning returns field-level validation errors and writes no partial tenant state."),
        Rule("CF-948", "P10_PRIMARY_TIMEOUT", "A downstream timeout returns 504 with retryAfterMs and retains an outbox recovery record without activating the tenant.")
    ];

    private static BfaRule Rule(string id, string type, string description) =>
        new(
            SaasMultiTenancyConstants.FlowId,
            id,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
