namespace Xiigen.Flow.BlogCmsModules.Application;

public sealed record BlogCmsModulesOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record BlogCmsModulesDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Content,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Drafts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Comments,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record BlogCmsModulesBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record BlogCmsRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record BlogCmsStressResult(string StressTestId, string Status, string Validates);
public sealed record BlogCmsPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
