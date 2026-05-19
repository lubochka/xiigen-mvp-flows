namespace Xiigen.Flow.ProfileEnrichment.Application;

public static class ProfileEnrichmentConstants
{
    public const string FlowId = "FLOW-02";
    public const string FlowSlug = "profile-enrichment";
    public const string DynamicEndpoint = "/api/dynamic/profile-enrichment";

    public const string BusinessProfiles = "xiigen-dotnet-business-profiles";
    public const string MatchingProfiles = "xiigen-dotnet-matching-profiles";
    public const string AnalyticsSegments = "xiigen-dotnet-analytics-segments";
    public const string LearningPrograms = "xiigen-dotnet-learning-programs";
    public const string BusinessMatches = "xiigen-dotnet-business-matches";
    public const string ConnectionSuggestions = "xiigen-dotnet-connection-suggestions";
    public const string PersonalizationFeeds = "xiigen-dotnet-personalization-feeds";
    public const string PersonalizationCompletions = "xiigen-dotnet-personalization-completions";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-02-1",
        "CF-02-2",
        "CF-02-3",
        "CF-02-4",
        "CF-854",
        "CF-855",
        "CF-856",
        "CF-857"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "business_partner",
        "freelancer",
        "event_organiser",
        "platform_admin"
    ];

    public const string EnrichmentDataReadyEvent = "xiigen.profile-enrichment.enrichment-data-ready.v1";
    public const string MatchingConvergedEvent = "xiigen.profile-enrichment.matching-converged.v1";
    public const string NeedsMoreDataEvent = "xiigen.profile-enrichment.needs-more-data.v1";
    public const string CompletedEvent = "xiigen.profile-enrichment.completed.v1";
    public const string PersonalizationCompletedMachineEvent = "PersonalizationCompleted";
}
