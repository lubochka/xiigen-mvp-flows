namespace Xiigen.Flow.FeatureRegistry.Application;

public sealed record FeatureRegistryOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record FeatureRegistryDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Features,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Signals,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Adapters,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Simulations,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record FeatureRegistryBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record FeatureRegistryRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record FeatureRegistryPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);

