using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.SharableFlowsMarketplace.Application;

public sealed class SharableFlowsMarketplaceService : MicroserviceBase<SharableFlowsMarketplaceService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "publisherTenantId", "consumerTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public SharableFlowsMarketplaceService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<SharableFlowsMarketplaceService> logger)
        : base(new ServiceDescriptor("sharable-flows-marketplace-service", "Sharable Flows Marketplace", SharableFlowsMarketplaceConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<SharableFlowsMarketplaceOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = SharableFlowsMarketplaceConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1019-CF-1022" }, false));
    }

    public async Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> PublishPackageAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "packageSlug", "displayName", "semanticVersion", "license", "pricingModel", "logicManifest", "ragBlueprint");
        if (ReadInt(request, "artifactCount") <= 0) errors.Add(FieldError("artifactCount", "artifactCount must be greater than zero."));
        if (ReadString(request, "logicManifest").Contains("secret", StringComparison.OrdinalIgnoreCase)) errors.Add(FieldError("logicManifest", "logicManifest must not contain literal secrets."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("sharable-flows.package.timeout-retained", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var operation = OperationKey("publish", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var packageId = ScopedId($"package:{ReadString(request, "packageSlug")}");
        var existing = await GetTenantDocumentAsync(SharableFlowsMarketplaceConstants.PackageCollection, packageId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1019" }, false));
        }

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var body = new Dictionary<string, object?>
        {
            ["id"] = packageId,
            ["packageSlug"] = ReadString(request, "packageSlug"),
            ["displayName"] = ReadString(request, "displayName"),
            ["semanticVersion"] = ReadString(request, "semanticVersion"),
            ["license"] = ReadString(request, "license"),
            ["pricingModel"] = ReadString(request, "pricingModel"),
            ["artifactCount"] = ReadInt(request, "artifactCount"),
            ["contentAddress"] = $"sha256:{Hash(JsonSerializer.Serialize(request))}",
            ["immutability"] = "published-version-never-overwritten",
            ["logicPlaneOnly"] = true,
            ["tenantDataIncluded"] = false,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(SharableFlowsMarketplaceConstants.PackageCollection, packageId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<SharableFlowsMarketplaceOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("FLOW_TEMPLATE_PACKAGE_PUBLISHED", packageId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("sharable-flows.package.published", packageId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public async Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> InstallTemplateAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "packageSlug", "installMode", "dependencyPolicy", "connectorBindingRef");
        if (string.Equals(ReadString(request, "connectorBindingRef"), "literal-secret", StringComparison.OrdinalIgnoreCase)) errors.Add(FieldError("connectorBindingRef", "connectorBindingRef must be a secret reference, not a literal credential."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("install", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var installId = ScopedId($"install:{Hash(ReadString(request, "packageSlug") + ":" + ReadString(request, "installMode"))}");
        var body = new Dictionary<string, object?>
        {
            ["installId"] = installId,
            ["packageSlug"] = ReadString(request, "packageSlug"),
            ["installMode"] = ReadString(request, "installMode"),
            ["dependencyPolicy"] = ReadString(request, "dependencyPolicy"),
            ["connectorBindingRef"] = ReadString(request, "connectorBindingRef"),
            ["tenantScope"] = CurrentTenant.TenantId,
            ["copiedPlane"] = "logic-only",
            ["tenantDataCopied"] = false
        };
        var saved = await _documents.UpsertAsync(SharableFlowsMarketplaceConstants.InstallCollection, installId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<SharableFlowsMarketplaceOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("sharable-flows.template.installed", installId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> PublishRagBlueprintAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "blueprintSlug", "retrievalStrategy", "promptProfile", "schemaRef");
        if (ReadBool(request, "includeDataset") || ReadBool(request, "includeEmbeddings")) errors.Add(FieldError("blueprint", "RAG sharing is blueprint-only; datasets, embeddings, documents, and secrets stay tenant private."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("rag", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var blueprintId = ScopedId($"blueprint:{ReadString(request, "blueprintSlug")}");
        var body = new Dictionary<string, object?>
        {
            ["blueprintId"] = blueprintId,
            ["blueprintSlug"] = ReadString(request, "blueprintSlug"),
            ["retrievalStrategy"] = ReadString(request, "retrievalStrategy"),
            ["promptProfile"] = ReadString(request, "promptProfile"),
            ["schemaRef"] = ReadString(request, "schemaRef"),
            ["logicPlaneOnly"] = true,
            ["tenantScope"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(SharableFlowsMarketplaceConstants.BlueprintCollection, blueprintId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<SharableFlowsMarketplaceOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("sharable-flows.rag-blueprint.published", blueprintId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> RecordUsageAndSettlementAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "packageSlug", "meterEvent", "periodKey", "currency");
        if (ReadInt(request, "amountCents") <= 0) errors.Add(FieldError("amountCents", "amountCents must be an integer greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("settlement", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var settlementId = ScopedId($"settlement:{ReadString(request, "packageSlug")}:{ReadString(request, "periodKey")}");
        var body = new Dictionary<string, object?>
        {
            ["settlementId"] = settlementId,
            ["packageSlug"] = ReadString(request, "packageSlug"),
            ["meterEvent"] = ReadString(request, "meterEvent"),
            ["periodKey"] = ReadString(request, "periodKey"),
            ["amountCents"] = ReadInt(request, "amountCents"),
            ["currency"] = ReadString(request, "currency"),
            ["mathMode"] = "integer-cents-only",
            ["tenantScope"] = CurrentTenant.TenantId
        };
        var usageSaved = await _documents.UpsertAsync(SharableFlowsMarketplaceConstants.UsageCollection, ScopedId($"usage:{Guid.NewGuid():N}"), body, null, cancellationToken).ConfigureAwait(false);
        if (usageSaved.IsFailure) return Failure<SharableFlowsMarketplaceOutcome>(usageSaved.Error!.Code, usageSaved.Error.Message, usageSaved.Error.Details);
        var saved = await _documents.UpsertAsync(SharableFlowsMarketplaceConstants.SettlementCollection, settlementId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<SharableFlowsMarketplaceOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("sharable-flows.settlement.recorded", settlementId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> ProvisionSandboxAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "packageSlug", "sandboxProfile");
        if (ReadInt(request, "ttlHours") <= 0) errors.Add(FieldError("ttlHours", "ttlHours must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("sandbox", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var sandboxId = ScopedId($"sandbox:sandbox-{Hash(ReadString(request, "packageSlug") + ReadString(request, "sandboxProfile"))[..12]}");
        var body = new Dictionary<string, object?>
        {
            ["sandboxId"] = sandboxId,
            ["packageSlug"] = ReadString(request, "packageSlug"),
            ["sandboxProfile"] = ReadString(request, "sandboxProfile"),
            ["ttlHours"] = ReadInt(request, "ttlHours"),
            ["tenantPrefix"] = "sandbox-",
            ["tenantScope"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(SharableFlowsMarketplaceConstants.SandboxCollection, sandboxId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<SharableFlowsMarketplaceOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("sharable-flows.sandbox.provisioned", sandboxId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<SharableFlowsMarketplaceOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = SharableFlowsMarketplaceConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = SharableFlowsMarketplaceConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<SharableFlowsMarketplaceDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var packages = await _documents.SearchAsync(SharableFlowsMarketplaceConstants.PackageCollection, filter, cancellationToken).ConfigureAwait(false);
        var installs = await _documents.SearchAsync(SharableFlowsMarketplaceConstants.InstallCollection, filter, cancellationToken).ConfigureAwait(false);
        var blueprints = await _documents.SearchAsync(SharableFlowsMarketplaceConstants.BlueprintCollection, filter, cancellationToken).ConfigureAwait(false);
        var settlements = await _documents.SearchAsync(SharableFlowsMarketplaceConstants.SettlementCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new SharableFlowsMarketplaceDashboard(
            (packages.Data ?? []).Select(record => record.Body).ToArray(),
            (installs.Data ?? []).Select(record => record.Body).ToArray(),
            (blueprints.Data ?? []).Select(record => record.Body).ToArray(),
            (settlements.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["packageCount"] = packages.Data?.Count ?? 0, ["installCount"] = installs.Data?.Count ?? 0, ["blueprintCount"] = blueprints.Data?.Count ?? 0, ["settlementCount"] = settlements.Data?.Count ?? 0, ["requiredRuleCount"] = SharableFlowsMarketplaceConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<SharableFlowsMarketplaceOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> TimeoutAsync(string eventType, string idempotencyKey, CancellationToken cancellationToken)
    {
        await _outbox.AppendAsync(Envelope(eventType, ScopedId($"timeout:{Hash(idempotencyKey)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1022" }, false, true));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(SharableFlowsMarketplaceConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{SharableFlowsMarketplaceConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static SharableFlowsMarketplaceOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static SharableFlowsMarketplaceOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1021" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{SharableFlowsMarketplaceConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private SharableFlowsMarketplaceOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1020" }, false) : null;
    private SharableFlowsMarketplaceOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<SharableFlowsRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<SharableFlowsRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<SharableFlowsPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<SharableFlowsPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}