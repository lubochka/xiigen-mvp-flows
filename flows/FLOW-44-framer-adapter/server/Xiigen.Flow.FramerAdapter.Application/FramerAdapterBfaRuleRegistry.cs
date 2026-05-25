namespace Xiigen.Flow.FramerAdapter.Application;

public static class FramerAdapterBfaRuleRegistry
{
    public static readonly IReadOnlyList<FramerAdapterBfaRule> Rules =
    [
        new("CF-804_TEXT_NODE_FILTER", FramerAdapterConstants.FlowId, "TEXT_NODE_FILTER", "Only Framer text nodes are read into the shared text model from a mixed selection.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-805_TEXT_STYLE_MAPPING", FramerAdapterConstants.FlowId, "TEXT_STYLE_MAPPING", "Font weight, color paint, size, line-height, and letter-spacing mappings are deterministic.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-806_PLUGIN_DATA_STORAGE", FramerAdapterConstants.FlowId, "PLUGIN_DATA_STORAGE", "Adapter state persistence uses getPluginData and setPluginData semantics with no browser storage fallback.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-FR1-NO_FIGMA_RUNTIME", FramerAdapterConstants.FlowId, "SDK_BOUNDARY", "Pure adapter code must not call Figma runtime APIs and must remain unit-testable without a live Framer runtime.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-FR1-PACKAGE-01", FramerAdapterConstants.FlowId, "PACKAGE_EVIDENCE", "Published package includes STEP-1-INVARIANTS.md, FUNCTIONAL-SPEC.md, and local registry readback evidence.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-FR1-LIVE-AUTH-01", FramerAdapterConstants.FlowId, "LIVE_AUTH", "Protected Framer adapter endpoints require a valid token, canonical role, and cross-tenant token rejection before mutation.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-FR1-SCOPE-01", FramerAdapterConstants.FlowId, "TENANT_SCOPE", "Run and publication evidence records are scoped from TenantContext, never from a free tenant id parameter.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
