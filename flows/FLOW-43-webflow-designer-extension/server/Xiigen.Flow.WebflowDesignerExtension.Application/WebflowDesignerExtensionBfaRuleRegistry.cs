namespace Xiigen.Flow.WebflowDesignerExtension.Application;

public static class WebflowDesignerExtensionBfaRuleRegistry
{
    public static readonly IReadOnlyList<WebflowDesignerExtensionBfaRule> Rules =
    [
        new("CF-800_CSS_SHORTHANDS", WebflowDesignerExtensionConstants.FlowId, "CSS_SHORTHAND_EXPANSION", "Border, font, padding, margin, and box-shadow shorthand values expand into discrete CSS properties without mutating shared style pipeline files.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-801_ELEMENT_CLASSIFICATION", WebflowDesignerExtensionConstants.FlowId, "ELEMENT_PROPERTY_ROUTING", "All supported Webflow Designer element categories route to non-empty pipeline-compatible property maps.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-802_NO_DEPRECATED_WEBFLOW_API", WebflowDesignerExtensionConstants.FlowId, "SDK_BOUNDARY", "Pure adapter code must not call deprecated Webflow runtime APIs or require a live Designer runtime during tests.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-803_VALIDATION_DIFF", WebflowDesignerExtensionConstants.FlowId, "VALIDATION_DIFF", "Pipeline CSS output must be comparable against native Webflow CSS with pipeline-only, native-only, and value-diff buckets.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-W1-PACKAGE-01", WebflowDesignerExtensionConstants.FlowId, "PACKAGE_EVIDENCE", "Published package includes STEP-1-INVARIANTS.md, FUNCTIONAL-SPEC.md, and local registry readback evidence.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-W1-LIVE-AUTH-01", WebflowDesignerExtensionConstants.FlowId, "LIVE_AUTH", "Protected Webflow adapter endpoints require valid token, canonical role, and cross-tenant token rejection before mutation.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-W1-SCOPE-01", WebflowDesignerExtensionConstants.FlowId, "TENANT_SCOPE", "Run and publication evidence records are scoped from TenantContext, never from a free tenant id parameter.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
