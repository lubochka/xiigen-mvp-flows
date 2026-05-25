namespace Xiigen.Flow.HistoryBootstrap.Application;

public static class HistoryBootstrapConstants
{
    public const string FlowId = "FLOW-45";
    public const string FlowSlug = "history-bootstrap";
    public const string ImplementationAlias = "history-bootstrap";
    public const string DynamicEndpoint = "/api/dynamic/history-bootstrap";
    public const string PackageName = "@xiigen/history-bootstrap";
    public const string PackageVersion = "1.0.0";

    public const string ArchitecturePhilosophyCollection = "xiigen-architecture-philosophy";
    public const string PlanningDecisionsCollection = "xiigen-planning-decisions";
    public const string RagPatternsCollection = "xiigen-rag-patterns";
    public const string PhilosophySummariesCollection = "xiigen-philosophy-summaries";
    public const string BootstrapCompletionsCollection = "xiigen-bootstrap-completions";
    public const string BootstrapIdempotencyCollection = "xiigen-history-bootstrap-idempotency";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T601-BootstrapFromDocuments",
        "T602-ArchPhilosophyRetriever",
        "T603-PhilosophyPatternSummarizer",
        "T604-HistoryBootstrapOrchestrator"
    ];

    public static readonly string[] RequiredBfaRules =
    [
        "CF-803_BOOTSTRAP_BEFORE_FLOW_EXECUTION",
        "CF-804_ARCH_PATTERN_GLOBAL_SCOPE",
        "CF-HB1-DESIGN_REASONING_TO_PLANNING",
        "CF-HB2-ARCH_PATTERN_TO_RAG",
        "CF-HB3-SUMMARY_STORE_BEFORE_QUEUE",
        "CF-HB4-IDEMPOTENT_BOOTSTRAP_RUN",
        "CF-HB5-TENANT_SCOPED_DASHBOARD"
    ];

    public static readonly string[] AllowedSeedSources =
    [
        "history-seeds/arch-philosophy.json",
        "history-seeds/history-bootstrap-design-corpus.json",
        "docs/xiigenDesign/ENGINE_ARCHITECTURE_MERGED.md",
        "fixtures/design-reasoning/history-bootstrap-design-decisions.json"
    ];
}
