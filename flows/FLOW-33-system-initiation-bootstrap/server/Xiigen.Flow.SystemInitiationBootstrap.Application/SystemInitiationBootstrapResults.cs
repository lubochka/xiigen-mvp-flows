namespace Xiigen.Flow.SystemInitiationBootstrap.Application;

public sealed record SystemInitiationBootstrapOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record SystemInitiationBootstrapDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> BootstrapRuns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Registries,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> CapabilityGaps,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> SmokeRuns,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record SystemInitiationBootstrapBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record SystemBootstrapRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record SystemBootstrapPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
