namespace Xiigen.Flow.SharableFlowsMarketplace.Application;

public sealed record SharableFlowsMarketplaceOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record SharableFlowsMarketplaceDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Packages,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Installs,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Blueprints,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Settlements,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record SharableFlowsMarketplaceBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record SharableFlowsRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record SharableFlowsPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);