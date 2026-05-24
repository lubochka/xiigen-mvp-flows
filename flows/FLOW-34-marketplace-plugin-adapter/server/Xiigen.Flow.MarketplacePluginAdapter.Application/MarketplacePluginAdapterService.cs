using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.MarketplacePluginAdapter.Application;

public sealed class MarketplacePluginAdapterService : MicroserviceBase<MarketplacePluginAdapterService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "adapterTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public MarketplacePluginAdapterService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<MarketplacePluginAdapterService> logger)
        : base(new ServiceDescriptor("marketplace-plugin-adapter-service", "Marketplace Plugin Adapter", MarketplacePluginAdapterConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<MarketplacePluginAdapterOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = MarketplacePluginAdapterConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1027-CF-1030" }, false));
    }

    public async Task<DataProcessResult<MarketplacePluginAdapterOutcome>> StartAdapterAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "platformId", "adapterSlug", "adapterMode", "pluginFamily", "apiMapRef");
        if (ReadInt(request, "targetPlatformCount") <= 0) errors.Add(FieldError("targetPlatformCount", "targetPlatformCount must be greater than zero."));
        if (ReadInt(request, "adapterSurfaceCount") <= 0) errors.Add(FieldError("adapterSurfaceCount", "adapterSurfaceCount must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("marketplace-plugin-adapter.timeout-retained", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var operation = OperationKey("adapter", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var bootstrapId = ScopedId($"adapter:{ReadString(request, "platformId")}:{ReadString(request, "adapterSlug")}");
        var existing = await GetTenantDocumentAsync(MarketplacePluginAdapterConstants.BootstrapCollection, bootstrapId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1027" }, false));
        }

        var missingFactories = ReadStringArray(request, "validationFindings");
        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var body = new Dictionary<string, object?>
        {
            ["id"] = bootstrapId,
            ["platformId"] = ReadString(request, "platformId"),
            ["adapterSlug"] = ReadString(request, "adapterSlug"),
            ["adapterMode"] = ReadString(request, "adapterMode"),
            ["pluginFamily"] = ReadString(request, "pluginFamily"),
            ["apiMapRef"] = ReadString(request, "apiMapRef"),
            ["targetPlatformCount"] = ReadInt(request, "targetPlatformCount"),
            ["adapterSurfaceCount"] = ReadInt(request, "adapterSurfaceCount"),
            ["validationFindings"] = missingFactories,
            ["adapterStatus"] = missingFactories.Count == 0 ? "packaged" : "validation_gaps_detected",
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(MarketplacePluginAdapterConstants.BootstrapCollection, bootstrapId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<MarketplacePluginAdapterOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        await _documents.UpsertAsync(MarketplacePluginAdapterConstants.RegistryCollection, ScopedId($"registry:{Hash(ReadString(request, "adapterSlug"))}"), new Dictionary<string, object?> { ["platformId"] = ReadString(request, "platformId"), ["adapterSlug"] = ReadString(request, "adapterSlug"), ["pluginFamily"] = ReadString(request, "pluginFamily"), ["apiMapRef"] = ReadString(request, "apiMapRef"), ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        if (missingFactories.Count > 0)
        {
            await _documents.UpsertAsync(MarketplacePluginAdapterConstants.GapCollection, ScopedId($"gap:{Hash(string.Join('|', missingFactories))}"), new Dictionary<string, object?> { ["validationFindings"] = missingFactories, ["eventType"] = "AdapterValidationCompleted", ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(Envelope("marketplace-plugin-adapter.validation-gaps-detected", bootstrapId, body), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _documents.UpsertAsync(MarketplacePluginAdapterConstants.SmokeCollection, ScopedId($"package:{ReadString(request, "adapterSlug")}"), new Dictionary<string, object?> { ["adapterSlug"] = ReadString(request, "adapterSlug"), ["result"] = "published-to-local-marketplace", ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(Envelope("marketplace-plugin-adapter.packaged", bootstrapId, body), cancellationToken).ConfigureAwait(false);
        }

        await AuditAsync("MARKETPLACE_PLUGIN_ADAPTER_PACKAGED", bootstrapId, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public DataProcessResult<MarketplacePluginAdapterOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = MarketplacePluginAdapterConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = MarketplacePluginAdapterConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<MarketplacePluginAdapterDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var runs = await _documents.SearchAsync(MarketplacePluginAdapterConstants.BootstrapCollection, filter, cancellationToken).ConfigureAwait(false);
        var registries = await _documents.SearchAsync(MarketplacePluginAdapterConstants.RegistryCollection, filter, cancellationToken).ConfigureAwait(false);
        var gaps = await _documents.SearchAsync(MarketplacePluginAdapterConstants.GapCollection, filter, cancellationToken).ConfigureAwait(false);
        var smoke = await _documents.SearchAsync(MarketplacePluginAdapterConstants.SmokeCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new MarketplacePluginAdapterDashboard(
            (runs.Data ?? []).Select(record => record.Body).ToArray(),
            (registries.Data ?? []).Select(record => record.Body).ToArray(),
            (gaps.Data ?? []).Select(record => record.Body).ToArray(),
            (smoke.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["adapterRunCount"] = runs.Data?.Count ?? 0, ["registryCount"] = registries.Data?.Count ?? 0, ["validationGapCount"] = gaps.Data?.Count ?? 0, ["packageCount"] = smoke.Data?.Count ?? 0, ["requiredRuleCount"] = MarketplacePluginAdapterConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<MarketplacePluginAdapterOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<MarketplacePluginAdapterOutcome>> TimeoutAsync(string eventType, string idempotencyKey, CancellationToken cancellationToken)
    {
        await _outbox.AppendAsync(Envelope(eventType, ScopedId($"timeout:{Hash(idempotencyKey)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1030" }, false, true));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(MarketplacePluginAdapterConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{MarketplacePluginAdapterConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static MarketplacePluginAdapterOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static MarketplacePluginAdapterOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1029" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{MarketplacePluginAdapterConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private MarketplacePluginAdapterOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1028" }, false) : null;
    private MarketplacePluginAdapterOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<SystemBootstrapRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<SystemBootstrapRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<SystemBootstrapPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<SystemBootstrapPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
