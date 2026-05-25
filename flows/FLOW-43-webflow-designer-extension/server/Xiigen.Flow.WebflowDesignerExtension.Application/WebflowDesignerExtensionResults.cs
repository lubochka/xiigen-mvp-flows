using Xiigen.Kernel;

namespace Xiigen.Flow.WebflowDesignerExtension.Application;

public sealed record WebflowElement(
    string Id,
    string ElementType,
    string Name,
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyDictionary<string, string> NativeCss);

public sealed record WebflowPropertyRoute(string ElementId, string ElementType, IReadOnlyDictionary<string, string> Properties);
public sealed record WebflowValidationDiff(IReadOnlyDictionary<string, string> PipelineOnly, IReadOnlyDictionary<string, string> NativeOnly, IReadOnlyDictionary<string, WebflowValueDiff> ValueDiff);
public sealed record WebflowValueDiff(string Pipeline, string Native);
public sealed record WebflowAdapterReadResult(IReadOnlyList<WebflowPropertyRoute> Routes, IReadOnlyList<WebflowValidationDiff> Diffs, IReadOnlyList<WebflowElement> SourceElements);
public sealed record WebflowPackageReadback(string PackageName, string Version, bool IncludesStep1Invariants, bool IncludesFunctionalSpec, bool LocalRegistryReadback);
public sealed record WebflowDesignerExtensionOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);
public sealed record WebflowDesignerExtensionDashboard(IReadOnlyList<IReadOnlyDictionary<string, object?>> AdapterRuns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Publications, IReadOnlyDictionary<string, object?> Kpis);
public sealed record WebflowDesignerExtensionBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record WebflowDesignerExtensionRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record WebflowDesignerExtensionPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
