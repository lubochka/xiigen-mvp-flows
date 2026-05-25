namespace Xiigen.Flow.RagQualityFeedback.Application;

public static class RagQualityFeedbackConstants
{
    public const string FlowId = "FLOW-38";
    public const string FlowSlug = "rag-quality-feedback";
    public const string ImplementationAlias = "rag-quality-feedback";
    public const string DynamicEndpoint = "/api/dynamic/rag-quality-feedback";
    public const string PackageName = "Xiigen.Flow.RagQualityFeedback";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes = ["T594-CYCLE-OUTCOME-CLASSIFIER", "T595-RAG-QUALITY-UPDATER", "T596-DISTILLED-RULE-EXTRACTOR"];
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1054, 4).Select(value => $"CF-{value}").ToArray();

    public const string RetrievalOutcomeCollection = "xiigen-rag-retrieval-outcomes";
    public const string CapabilityGapCollection = "xiigen-capability-gap-signals";
    public const string CycleOutcomeCollection = "xiigen-cycle-outcomes";
    public const string QualityUpdateCollection = "xiigen-rag-quality-updates";
    public const string BatchRecordCollection = "xiigen-rag-quality-batch-records";
    public const string DistilledRulesCollection = "xiigen-distilled-rules";
    public const string AuditCollection = "xiigen-rag-quality-feedback-audit";

    public const decimal DeltaMagnitude = 0.05m;
    public const decimal ScoreFloor = 0.0m;
    public const decimal ScoreCeiling = 1.0m;
    public const decimal DistilledRuleMinConfidence = 0.85m;
}


