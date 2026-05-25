namespace Xiigen.Flow.CanvaTextAdapter.Application;

public static class CanvaTextAdapterBfaRuleRegistry
{
    public static readonly IReadOnlyList<CanvaTextAdapterBfaRule> Rules =
    [
        new("CF-PORT-LIVE-01", CanvaTextAdapterConstants.FlowId, "PORTABILITY_LIVE_MODE", "Live external-adapter mode requires compiled package output, local registry readback, and adapter evidence before marketplace adoption.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-SCOPE-GLOBAL-01", CanvaTextAdapterConstants.FlowId, "GLOBAL_SCOPE_APPROVAL", "GLOBAL-scope external adapter publication requires platform approval evidence before tenant adoption.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-PRICING-01", CanvaTextAdapterConstants.FlowId, "PRICING_ENFORCEMENT", "MODULE adoption must preserve pricing metadata for per-use billing before live marketplace publication.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-C5-READ-01", CanvaTextAdapterConstants.FlowId, "CANVA_READ_MAPPING", "Canva content, style, alignment, color, width, and height map into the shared Figma-shaped text contract without storage or side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-C5-WRITE-01", CanvaTextAdapterConstants.FlowId, "CANVA_WRITE_MAPPING", "Shared style output maps back into Canva write payloads and writer failures are counted instead of aborting the batch.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-C5-PACKAGE-01", CanvaTextAdapterConstants.FlowId, "PACKAGE_EVIDENCE", "Published package includes STEP-1-INVARIANTS.md, FUNCTIONAL-SPEC.md, and local registry readback evidence.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-C5-SDK-BOUNDARY-01", CanvaTextAdapterConstants.FlowId, "SDK_BOUNDARY", "The pure mapping path has no module-level Canva SDK dependency and remains importable in tests without @canva/design.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
