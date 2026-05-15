namespace Xiigen.Flow.UserRegistration.Application;

public sealed record UserRegistrationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool Stored,
    bool OutboxRecorded);

public sealed record EmailVerificationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body);

public sealed record OnboardingOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool Completed);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
