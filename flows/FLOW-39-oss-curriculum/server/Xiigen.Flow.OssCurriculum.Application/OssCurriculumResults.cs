using Xiigen.Kernel;

namespace Xiigen.Flow.OssCurriculum.Application;

public sealed record OssCurriculumOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record OssCurriculumDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> CurriculumTiers,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ShadowRuns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> LearningSignals,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ProgressChecks,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record OssCurriculumBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record OssCurriculumRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record OssCurriculumPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
public sealed record QueueDispatchRecord(string MessageType, CloudEventEnvelope Event, DateTimeOffset DispatchedAt);

public interface IOssCurriculumQueueBroker
{
    Task<DataProcessResult<QueueDispatchRecord>> PublishAsync(CloudEventEnvelope envelope, CancellationToken cancellationToken = default);
    Task<DataProcessResult<IReadOnlyList<QueueDispatchRecord>>> ListAsync(CancellationToken cancellationToken = default);
}
