namespace Xiigen.Flow.UserRegistration.Application;

public static class UserRegistrationConstants
{
    public const string FlowId = "FLOW-01";
    public const string FlowSlug = "user-registration";
    public const string DynamicEndpoint = "/api/dynamic/user-registration";
    public const string UserRegistrations = "user-registrations";
    public const string VerificationTokens = "user-registration-verification-tokens";
    public const string ResendAttempts = "user-registration-resend-attempts";
    public const string OnboardingDeliveries = "user-registration-onboarding-deliveries";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-01-1",
        "CF-01-2",
        "CF-01-3",
        "CF-01-4",
        "CF-850",
        "CF-851",
        "CF-852",
        "CF-853"
    };

    public static readonly IReadOnlyList<string> OnboardingMaterialTypes =
    [
        "workspace_setup",
        "flow_tutorial",
        "community_invitation"
    ];
}
