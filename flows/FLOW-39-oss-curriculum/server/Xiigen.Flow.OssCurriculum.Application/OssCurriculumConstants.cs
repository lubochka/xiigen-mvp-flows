namespace Xiigen.Flow.OssCurriculum.Application;

public static class OssCurriculumConstants
{
    public const string FlowId = "FLOW-39";
    public const string FlowSlug = "oss-curriculum";
    public const string ImplementationAlias = "oss-curriculum";
    public const string DynamicEndpoint = "/api/dynamic/oss-curriculum";
    public const string PackageName = "Xiigen.Flow.OssCurriculum";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T597-CURRICULUM-TIER-ASSIGNER",
        "T598-SHADOW-RUN-ORCHESTRATOR",
        "T599-LEARNING-SIGNAL-COLLECTOR",
        "T600-CURRICULUM-PROGRESS-TRACKER"
    ];

    public static readonly string[] RequiredBfaRules = ["CF-803", "CF-804", "CF-1058", "CF-1059", "CF-1060", "CF-1061"];
    public static readonly string[] P10BfaRules = ["CF-1058", "CF-1059", "CF-1060", "CF-1061"];
    public static readonly string[] LearningSignalKeys = ["grade_trend", "rag_context_size", "graph_context_size"];
    public static readonly string[] DefaultOssModels = ["llama3:8b", "codellama:13b", "deepseek-coder:6.7b"];

    public static readonly IReadOnlyDictionary<string, int> ArchetypeTierMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["ROUTING"] = 1,
        ["DATA_PIPELINE"] = 2,
        ["PROCESSING"] = 3,
        ["ORCHESTRATION"] = 4,
        ["SCHEDULED"] = 5
    };

    public const string CurriculumTierCollection = "xiigen-curriculum-tiers";
    public const string ShadowRunCollection = "xiigen-shadow-runs";
    public const string LearningSignalCollection = "xiigen-learning-signals";
    public const string ProgressCollection = "xiigen-curriculum-progress";
    public const string AuditCollection = "xiigen-oss-curriculum-audit";

    public const int PlateauWindow = 3;
    public const int DefaultMinCycles = 3;
}
