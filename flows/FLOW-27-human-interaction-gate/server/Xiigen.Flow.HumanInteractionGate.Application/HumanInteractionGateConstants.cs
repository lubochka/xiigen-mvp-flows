namespace Xiigen.Flow.HumanInteractionGate.Application;

public static class HumanInteractionGateConstants
{
    public const string FlowId = "FLOW-27";
    public const string FlowSlug = "human-interaction-gate";
    public const string ImplementationAlias = "human-approval-gate";
    public const string DynamicEndpoint = "/api/dynamic/human-interaction-gate";
    public const string PackageName = "Xiigen.Flow.HumanInteractionGate";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "tenant_user",
        "tenant_admin",
        "platform_admin",
        "platform_support",
        "human_decision_maker"
    ];

    public static readonly string[] TaskViewerRoles = ["tenant_user", "tenant_admin", "platform_admin", "platform_support", "human_decision_maker"];
    public static readonly string[] ApproverRoles = ["tenant_admin", "platform_admin", "human_decision_maker"];
    public static readonly string[] SupportRoles = ["platform_support", "tenant_admin", "platform_admin", "human_decision_maker"];
    public static readonly string[] AdminRoles = ["tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T413", "T414", "T415", "T416", "T417", "T418", "T419", "T420", "T421", "T422"
    ];

    public static readonly string[] RequiredStressTests =
        Enumerable.Range(325, 21).Select(value => $"ST-{value}").ToArray();

    public static readonly string[] RequiredBfaRules =
        Enumerable.Range(545, 21).Select(value => $"CF-{value}")
            .Concat(Enumerable.Range(996, 7).Select(value => $"CF-{value}"))
            .ToArray();

    public const string RulePackCollection = "xiigen-human-interaction-gate-rule-packs";
    public const string TaskCollection = "xiigen-human-interaction-gate-user-tasks";
    public const string ClaimCollection = "xiigen-human-interaction-gate-claims";
    public const string SnapshotCollection = "xiigen-human-interaction-gate-node-snapshots";
    public const string CompletionCollection = "xiigen-human-interaction-gate-completion-gates";
    public const string StressCollection = "xiigen-human-interaction-gate-stress";
    public const string AuditCollection = "xiigen-human-interaction-gate-audit";
}
