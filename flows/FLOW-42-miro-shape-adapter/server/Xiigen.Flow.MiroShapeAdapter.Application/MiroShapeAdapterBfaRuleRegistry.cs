namespace Xiigen.Flow.MiroShapeAdapter.Application;

public static class MiroShapeAdapterBfaRuleRegistry
{
    public static readonly IReadOnlyList<MiroShapeAdapterBfaRule> Rules =
    [
        new("CF-PORT-LIVE-01", MiroShapeAdapterConstants.FlowId, "PORTABILITY_LIVE_MODE", "Live external-adapter mode requires compiled package output, local registry readback, and adapter evidence before marketplace adoption.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-SCOPE-GLOBAL-01", MiroShapeAdapterConstants.FlowId, "GLOBAL_SCOPE_APPROVAL", "GLOBAL-scope external adapter publication requires platform approval evidence before tenant adoption.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-PRICING-01", MiroShapeAdapterConstants.FlowId, "PRICING_ENFORCEMENT", "MODULE adoption must preserve pricing metadata for per-use billing before live marketplace publication.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-M1-READ-01", MiroShapeAdapterConstants.FlowId, "MIRO_BOARD_MAPPING", "Miro cards, connectors, shapes, text, frames, and sticky notes map into shared architecture elements without storage side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-M1-CLASSIFY-01", MiroShapeAdapterConstants.FlowId, "MIRO_SHAPE_CLASSIFIER", "Shape classification must be deterministic, rule-based, and expose R1 through R10 with confidence scores.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-M1-SPATIAL-01", MiroShapeAdapterConstants.FlowId, "MIRO_SPATIAL_ANALYSIS", "Spatial analysis must use a 5dp containment tolerance and infer row, column, or absolute layout.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-M1-WRITE-01", MiroShapeAdapterConstants.FlowId, "MIRO_WRITEBACK", "Shared architecture output maps back into Miro-compatible payloads and writer failures are counted instead of aborting the batch.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-M1-PACKAGE-01", MiroShapeAdapterConstants.FlowId, "PACKAGE_EVIDENCE", "Published package includes STEP-1-INVARIANTS.md, FUNCTIONAL-SPEC.md, and local registry readback evidence.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-M1-SDK-BOUNDARY-01", MiroShapeAdapterConstants.FlowId, "SDK_BOUNDARY", "The pure mapping path has no module-level Miro SDK dependency and remains importable in tests without the live Miro API package.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
