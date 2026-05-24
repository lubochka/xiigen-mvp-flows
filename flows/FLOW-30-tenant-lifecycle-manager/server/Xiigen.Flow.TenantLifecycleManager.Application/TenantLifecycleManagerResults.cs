namespace Xiigen.Flow.TenantLifecycleManager.Application;

public sealed record TenantLifecycleManagerOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record TenantLifecycleManagerDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Lifecycles,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> HealthChecks,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> OffboardingRuns,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record TenantLifecycleManagerBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record TenantLifecycleRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record TenantLifecyclePlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
