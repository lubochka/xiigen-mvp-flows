namespace Xiigen.Flow.VisualFlowEngine.Application;

public static class VisualFlowEngineConstants
{
    public const string FlowId = "FLOW-18";
    public const string FlowSlug = "visual-flow-engine";
    public const string DynamicEndpoint = "/api/dynamic/visual-flow-engine";

    public const string FlowCanvases = "xiigen-flow18-flow-canvases";
    public const string CanvasAudits = "xiigen-flow18-canvas-audits";
    public const string PublicationAudits = "xiigen-flow18-publication-audits";
    public const string NodeTypes = "xiigen-flow18-node-types";
    public const string NodeCapabilities = "xiigen-flow18-node-capabilities";
    public const string NodeTypeLocks = "xiigen-flow18-node-type-locks";
    public const string NodeTypeAudits = "xiigen-flow18-node-type-audits";
    public const string InjectionLocks = "xiigen-flow18-injection-locks";
    public const string InjectionAudits = "xiigen-flow18-injection-audits";
    public const string InjectionResults = "xiigen-flow18-injection-results";
    public const string TestRuns = "xiigen-flow18-test-runs";
    public const string FreedomConfig = "xiigen-flow18-freedom-config";

    public const string FlowCanvasUpdatedEvent = "xiigen.flow18.canvas-updated.v1";
    public const string FlowImmutableRejectedEvent = "xiigen.flow18.flow-immutable-rejected.v1";
    public const string FlowCanvasUpdateFailedEvent = "xiigen.flow18.canvas-update-failed.v1";
    public const string CycleDetectedEvent = "xiigen.flow18.cycle-detected.v1";
    public const string TypeMismatchEvent = "xiigen.flow18.type-mismatch.v1";
    public const string FlowPublicationConflictEvent = "xiigen.flow18.publication-conflict.v1";
    public const string FlowPublishedEvent = "xiigen.flow18.flow-published.v1";
    public const string NodeTypeAlreadyExistsEvent = "xiigen.flow18.node-type-already-exists.v1";
    public const string NodeTypeRegisteredEvent = "xiigen.flow18.node-type-registered.v1";
    public const string NodeTypeRegistrationFailedEvent = "xiigen.flow18.node-type-registration-failed.v1";
    public const string InjectionConflictEvent = "xiigen.flow18.injection-conflict.v1";
    public const string CodeInjectedEvent = "xiigen.flow18.code-injected.v1";
    public const string InjectionFailedEvent = "xiigen.flow18.injection-failed.v1";
    public const string TimeoutRetainedEvent = "xiigen.flow18.timeout-retained.v1";

    public static readonly string[] RuleIds =
    [
        "CF-18-1",
        "CF-18-2",
        "CF-18-3",
        "CF-18-4",
        "CF-957",
        "CF-958",
        "CF-959",
        "CF-960"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T617",
        "T618",
        "T619",
        "T620"
    ];

    public static readonly string[] Roles =
    [
        "anonymous",
        "public_visitor",
        "tenant_user",
        "tenant_admin",
        "referral_user",
        "flow_author",
        "prompt_approver",
        "human_decision_maker",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "flow_author",
        "prompt_approver",
        "human_decision_maker",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] AuthorRoles =
    [
        "tenant_admin",
        "flow_author",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] PlatformOpsRoles =
    [
        "platform_admin",
        "platform_support",
        "prompt_approver",
        "human_decision_maker"
    ];

    public static readonly string[] NodeCategories =
    [
        "trigger",
        "action",
        "gate",
        "transform",
        "output"
    ];
}
