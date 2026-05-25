namespace Xiigen.Flow.PlatformAgent.Application;

public sealed record PlatformAgentOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained = false);
public sealed record PlatformAgentDashboard(IReadOnlyList<IReadOnlyDictionary<string, object?>> Sessions, IReadOnlyList<IReadOnlyDictionary<string, object?>> Actions, IReadOnlyList<IReadOnlyDictionary<string, object?>> Contributions, IReadOnlyDictionary<string, object?> Metrics);
public sealed record PlatformAgentBfaRule(string RuleId, string FlowId, string TaskTypeId, string Type, string Description, string ViolationSeverity, string KnowledgeScope, string RuleTenantScope);