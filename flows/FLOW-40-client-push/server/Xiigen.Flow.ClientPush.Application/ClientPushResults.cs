using Xiigen.Kernel;

namespace Xiigen.Flow.ClientPush.Application;

public sealed record ClientPushOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record ClientPushDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Connections,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Deliveries,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> KeepaliveRuns,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record ClientPushBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record ClientPushRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record ClientPushPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);

public sealed record ClientPushSseEvent(string Event, IReadOnlyDictionary<string, object?> Data, string? Id);
public sealed record ClientPushConnectionInfo(string TenantId, string CorrelationId, DateTimeOffset ConnectedAt, DateTimeOffset LastAcknowledgedAt);
public sealed record ClientPushDeliveryRecord(string MessageType, CloudEventEnvelope Event, DateTimeOffset DispatchedAt);

public interface IClientPushConnectionPool
{
    DataProcessResult<ClientPushConnectionInfo> RegisterConnection(string correlationId, IReadOnlyDictionary<string, object?> activeFlowState);
    DataProcessResult<bool> PushEvent(string correlationId, ClientPushSseEvent sseEvent);
    DataProcessResult<bool> CloseConnection(string correlationId);
    DataProcessResult<IReadOnlyList<ClientPushConnectionInfo>> GetActiveConnections();
    DataProcessResult<bool> HasCorrelationOutsideCurrentTenant(string correlationId);
}
