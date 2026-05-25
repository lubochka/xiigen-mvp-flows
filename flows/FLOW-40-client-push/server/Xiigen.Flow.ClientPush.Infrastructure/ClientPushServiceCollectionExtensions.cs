using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.ClientPush.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.ClientPush.Infrastructure;

public static class ClientPushServiceCollectionExtensions
{
    public static IServiceCollection AddClientPushFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<InMemoryClientPushConnectionPool>();
        services.AddSingleton<IClientPushConnectionPool>(sp => sp.GetRequiredService<InMemoryClientPushConnectionPool>());
        services.AddSingleton<ClientPushService>();
        return services;
    }
}

public sealed class InMemoryClientPushConnectionPool : IClientPushConnectionPool
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly Dictionary<string, Dictionary<string, ActiveConnection>> _pool = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public InMemoryClientPushConnectionPool(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public DataProcessResult<ClientPushConnectionInfo> RegisterConnection(string correlationId, IReadOnlyDictionary<string, object?> activeFlowState)
    {
        if (string.IsNullOrWhiteSpace(correlationId)) return DataProcessResult<ClientPushConnectionInfo>.Failure("MISSING_CORRELATION_ID", "correlationId is required.");
        if (activeFlowState.Count == 0) return DataProcessResult<ClientPushConnectionInfo>.Failure("MISSING_ACTIVE_FLOW_STATE", "activeFlowState is required before SSE registration.");

        var tenantScopeId = CurrentTenantId();
        var now = DateTimeOffset.UtcNow;
        var connection = new ActiveConnection(tenantScopeId, correlationId, now, now, [], false);
        lock (_gate)
        {
            if (!_pool.TryGetValue(tenantScopeId, out var tenantPool))
            {
                tenantPool = new Dictionary<string, ActiveConnection>(StringComparer.Ordinal);
                _pool[tenantScopeId] = tenantPool;
            }

            tenantPool[correlationId] = connection;
        }

        return DataProcessResult<ClientPushConnectionInfo>.Success(connection.ToInfo());
    }

    public DataProcessResult<bool> PushEvent(string correlationId, ClientPushSseEvent sseEvent)
    {
        var tenantScopeId = CurrentTenantId();
        lock (_gate)
        {
            if (!_pool.TryGetValue(tenantScopeId, out var tenantPool) || !tenantPool.TryGetValue(correlationId, out var connection) || connection.Closed)
            {
                return DataProcessResult<bool>.Success(false);
            }

            connection.Events.Add(sseEvent);
            connection.LastAcknowledgedAt = DateTimeOffset.UtcNow;
            return DataProcessResult<bool>.Success(true);
        }
    }

    public DataProcessResult<bool> CloseConnection(string correlationId)
    {
        var tenantScopeId = CurrentTenantId();
        lock (_gate)
        {
            if (!_pool.TryGetValue(tenantScopeId, out var tenantPool) || !tenantPool.TryGetValue(correlationId, out var connection))
            {
                return DataProcessResult<bool>.Success(false);
            }

            connection.Closed = true;
            tenantPool.Remove(correlationId);
            if (tenantPool.Count == 0) _pool.Remove(tenantScopeId);
            return DataProcessResult<bool>.Success(true);
        }
    }

    public DataProcessResult<IReadOnlyList<ClientPushConnectionInfo>> GetActiveConnections()
    {
        var tenantScopeId = CurrentTenantId();
        lock (_gate)
        {
            if (!_pool.TryGetValue(tenantScopeId, out var tenantPool))
            {
                return DataProcessResult<IReadOnlyList<ClientPushConnectionInfo>>.Success([]);
            }

            return DataProcessResult<IReadOnlyList<ClientPushConnectionInfo>>.Success(tenantPool.Values.Where(connection => !connection.Closed).Select(connection => connection.ToInfo()).ToArray());
        }
    }

    public DataProcessResult<bool> HasCorrelationOutsideCurrentTenant(string correlationId)
    {
        var tenantScopeId = CurrentTenantId();
        lock (_gate)
        {
            var found = _pool.Any(entry => !string.Equals(entry.Key, tenantScopeId, StringComparison.Ordinal) && entry.Value.ContainsKey(correlationId));
            return DataProcessResult<bool>.Success(found);
        }
    }

    public IReadOnlyList<ClientPushSseEvent> EventsForCurrentTenant(string correlationId)
    {
        var tenantScopeId = CurrentTenantId();
        lock (_gate)
        {
            return _pool.TryGetValue(tenantScopeId, out var tenantPool) && tenantPool.TryGetValue(correlationId, out var connection) ? connection.Events.ToArray() : [];
        }
    }

    public void AgeConnectionForCurrentTenant(string correlationId, TimeSpan age)
    {
        var tenantScopeId = CurrentTenantId();
        lock (_gate)
        {
            if (_pool.TryGetValue(tenantScopeId, out var tenantPool) && tenantPool.TryGetValue(correlationId, out var connection))
            {
                connection.LastAcknowledgedAt = DateTimeOffset.UtcNow - age;
            }
        }
    }

    private string CurrentTenantId() => _tenantContextAccessor.Current?.TenantId ?? "__missing_tenant_context__";

    private sealed class ActiveConnection
    {
        public ActiveConnection(string tenantScope, string correlationId, DateTimeOffset connectedAt, DateTimeOffset lastAcknowledgedAt, List<ClientPushSseEvent> events, bool closed)
        {
            TenantId = tenantScope;
            CorrelationId = correlationId;
            ConnectedAt = connectedAt;
            LastAcknowledgedAt = lastAcknowledgedAt;
            Events = events;
            Closed = closed;
        }

        public string TenantId { get; }
        public string CorrelationId { get; }
        public DateTimeOffset ConnectedAt { get; }
        public DateTimeOffset LastAcknowledgedAt { get; set; }
        public List<ClientPushSseEvent> Events { get; }
        public bool Closed { get; set; }

        public ClientPushConnectionInfo ToInfo() => new(TenantId, CorrelationId, ConnectedAt, LastAcknowledgedAt);
    }
}
