using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.SaasMultiTenancy.Application;

public sealed class SaasMultiTenancyService : MicroserviceBase<SaasMultiTenancyService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly IReadOnlyDictionary<string, object?> DefaultTierDefinition = new Dictionary<string, object?>
    {
        ["subscriptionTier"] = "growth",
        ["masterTenantId"] = SaasMultiTenancyConstants.MasterTenantId,
        ["quotas"] = new Dictionary<string, object?>
        {
            ["users"] = 100,
            ["storageGb"] = 50,
            ["monthlyEvents"] = 10000
        }
    };

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _provisionLocks = new(StringComparer.Ordinal);

    public SaasMultiTenancyService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<SaasMultiTenancyService> logger)
        : base(
            new ServiceDescriptor("saas-multi-tenancy-service", "SaaS Multi-Tenancy", SaasMultiTenancyConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<SaasMultiTenancyOutcome>> ProvisionTenantAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateProvisionRequest(request);
        if (validation.Count > 0)
        {
            return Success(new SaasMultiTenancyOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var slug = NormalizeSlug(ReadString(request, "tenantSlug")!);
        var operatorId = ReadString(request, "operatorId")!.Trim();
        var operationKey = $"{SaasMultiTenancyConstants.FlowSlug}:provision:{CurrentTenant.TenantId}:{operatorId}:{slug}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<SaasMultiTenancyOutcome>(reservation.Error!.Code, reservation.Error.Message, reservation.Error.Details);
        }

        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new SaasMultiTenancyOutcome(200, reservation.Data.CachedResponse, true, false));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = new Dictionary<string, object?>
            {
                ["code"] = "TIMEOUT",
                ["message"] = "Tenant provisioning timed out before activation.",
                ["retryAfterMs"] = 5000,
                ["tenantSlug"] = slug
            };
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TimeoutRetainedEvent, slug, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Tenant provisioning timed out", cancellationToken).ConfigureAwait(false);
            return Success(new SaasMultiTenancyOutcome(504, timeoutBody, false, true));
        }

        var gate = _provisionLocks.GetOrAdd($"{CurrentTenant.TenantId}:provision:{slug}", _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            return Failure<SaasMultiTenancyOutcome>("CONCURRENT_UPDATE", "Concurrent tenant provisioning detected.");
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var tenantDocumentId = TenantDocumentId(slug);
            var current = await _documents.GetAsync(SaasMultiTenancyConstants.Tenants, tenantDocumentId, cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
            {
                return Failure<SaasMultiTenancyOutcome>(current.Error!.Code, current.Error.Message, current.Error.Details);
            }

            if (current.Data is not null && !string.Equals(ifMatch, current.Data.Version, StringComparison.Ordinal))
            {
                return Failure<SaasMultiTenancyOutcome>(
                    "CONCURRENT_UPDATE",
                    "Tenant provisioning version does not match.",
                    new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
            }

            var tierDefinition = await ResolveTierDefinitionAsync(ReadString(request, "subscriptionTier")!, cancellationToken).ConfigureAwait(false);
            if (tierDefinition.IsFailure)
            {
                return Failure<SaasMultiTenancyOutcome>(tierDefinition.Error!.Code, tierDefinition.Error.Message, tierDefinition.Error.Details);
            }

            var trustedMasterTenantId = ReadString(tierDefinition.Data!, "masterTenantId") ?? SaasMultiTenancyConstants.MasterTenantId;
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var tenantId = ComputeTenantId(slug, operatorId);
            var lockRecord = await _documents.UpsertAsync(
                SaasMultiTenancyConstants.ProvisionLocks,
                $"provision:{operatorId}:{slug}",
                WithTenant(new Dictionary<string, object?>
                {
                    ["operator_id"] = operatorId,
                    ["tenant_slug"] = slug,
                    ["reserved_at"] = now
                }),
                null,
                cancellationToken).ConfigureAwait(false);
            if (lockRecord.IsFailure)
            {
                return Failure<SaasMultiTenancyOutcome>(lockRecord.Error!.Code, lockRecord.Error.Message, lockRecord.Error.Details);
            }

            var provisioningState = WithTenant(new Dictionary<string, object?>
            {
                ["tenant_id"] = tenantId,
                ["tenant_slug"] = slug,
                ["workspace_name"] = ReadString(request, "workspaceName")!.Trim(),
                ["status"] = "PROVISIONING",
                ["subscription_tier"] = ReadString(request, "subscriptionTier")!.Trim(),
                ["billing_contact"] = ReadString(request, "billingContact")!.Trim(),
                ["operator_id"] = operatorId,
                ["master_tenant_id"] = trustedMasterTenantId,
                ["provisioned_at"] = now
            });
            var storedProvisioning = await _documents.UpsertAsync(SaasMultiTenancyConstants.Tenants, tenantDocumentId, provisioningState, ifMatch, cancellationToken).ConfigureAwait(false);
            if (storedProvisioning.IsFailure)
            {
                return Failure<SaasMultiTenancyOutcome>(storedProvisioning.Error!.Code, storedProvisioning.Error.Message, storedProvisioning.Error.Details);
            }

            await SeedFreedomKeysAsync(tenantId, trustedMasterTenantId, ReadString(request, "subscriptionTier")!, now, cancellationToken).ConfigureAwait(false);

            var activeState = new Dictionary<string, object?>(provisioningState)
            {
                ["status"] = "ACTIVE",
                ["activated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            var storedActive = await _documents.UpsertAsync(SaasMultiTenancyConstants.Tenants, tenantDocumentId, activeState, storedProvisioning.Data!.Version, cancellationToken).ConfigureAwait(false);
            if (storedActive.IsFailure)
            {
                return Failure<SaasMultiTenancyOutcome>(storedActive.Error!.Code, storedActive.Error.Message, storedActive.Error.Details);
            }

            await StoreAuditAsync(SaasMultiTenancyConstants.ProvisionAudits, $"provision-{tenantId}", "TENANT_PROVISIONED", activeState, cancellationToken).ConfigureAwait(false);
            var quotaResult = await MaterializeQuotasCoreAsync(tenantId, ReadString(request, "subscriptionTier")!, cancellationToken).ConfigureAwait(false);
            if (quotaResult.IsFailure)
            {
                return Failure<SaasMultiTenancyOutcome>(quotaResult.Error!.Code, quotaResult.Error.Message, quotaResult.Error.Details);
            }

            var response = new Dictionary<string, object?>
            {
                ["id"] = tenantId,
                ["tenantSlug"] = slug,
                ["status"] = "ACTIVE",
                ["version"] = storedActive.Data!.Version,
                ["subscriptionTier"] = ReadString(request, "subscriptionTier"),
                ["masterTenantIdSource"] = "trusted-tier-definition",
                ["quotaBatchMode"] = "MULTI_EXEC"
            };
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TenantProvisionedEvent, tenantId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new SaasMultiTenancyOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<SaasMultiTenancyOutcome>> UpdateConfigurationAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var key = ReadString(request, "key");
        if (string.IsNullOrWhiteSpace(key))
        {
            return Success(new SaasMultiTenancyOutcome(400, ValidationBody([FieldError("key", "Value is required.")]), false, false));
        }

        if (SaasMultiTenancyConstants.MachineLockedKeys.Contains(key, StringComparer.Ordinal))
        {
            var lockedBody = ErrorBody("MACHINE_LOCKED_KEY", "This workspace setting is controlled by the platform and cannot be changed here.");
            await StoreAuditAsync(SaasMultiTenancyConstants.ConfigAudits, $"immutable-{CurrentTenant.TenantId}-{key}-{Guid.NewGuid():N}", "CONFIG_KEY_IMMUTABLE", lockedBody, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.ConfigKeyImmutableEvent, key, lockedBody), cancellationToken).ConfigureAwait(false);
            return Success(new SaasMultiTenancyOutcome(409, lockedBody, true, true));
        }

        if (key.StartsWith("quota_", StringComparison.Ordinal) && ReadInt(request, "value") < 0)
        {
            return Success(new SaasMultiTenancyOutcome(400, ValidationBody([FieldError("value", "Quota values must be zero or greater.")]), false, false));
        }

        var documentId = $"config:{CurrentTenant.TenantId}:{key}";
        var existing = await _documents.GetAsync(SaasMultiTenancyConstants.FreedomConfig, documentId, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return Failure<SaasMultiTenancyOutcome>(existing.Error!.Code, existing.Error.Message, existing.Error.Details);
        }

        if (existing.Data is not null && !string.Equals(ifMatch, existing.Data.Version, StringComparison.Ordinal))
        {
            return Failure<SaasMultiTenancyOutcome>(
                "CONCURRENT_UPDATE",
                "Configuration version does not match.",
                new Dictionary<string, object?> { ["currentVersion"] = existing.Data.Version });
        }

        var body = WithTenant(new Dictionary<string, object?>
        {
            ["key"] = key,
            ["value"] = request.TryGetValue("value", out var value) ? value : null,
            ["updated_by"] = ReadString(request, "operatorId") ?? "tenant-operator",
            ["updated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["scope"] = "tenant-freedom"
        });
        var stored = await _documents.UpsertAsync(SaasMultiTenancyConstants.FreedomConfig, documentId, body, ifMatch, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<SaasMultiTenancyOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        await StoreAuditAsync(SaasMultiTenancyConstants.ConfigAudits, $"config-{CurrentTenant.TenantId}-{key}-{Guid.NewGuid():N}", "TENANT_CONFIGURATION_UPDATED", body, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["key"] = key,
            ["version"] = stored.Data!.Version,
            ["status"] = "UPDATED"
        };
        await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TenantConfigurationUpdatedEvent, key, response), cancellationToken).ConfigureAwait(false);
        return Success(new SaasMultiTenancyOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<SaasMultiTenancyOutcome>> MaterializeQuotasAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var tier = ReadString(request, "subscriptionTier") ?? "growth";
        return await MaterializeQuotasCoreAsync(CurrentTenant.TenantId, tier, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DataProcessResult<SaasMultiTenancyOutcome>> ChangeLifecycleAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var action = ReadString(request, "action")?.Trim().ToUpperInvariant();
        var currentStatus = ReadString(request, "currentStatus")?.Trim().ToUpperInvariant() ?? "ACTIVE";
        var targetStatus = action switch
        {
            "SUSPEND" => "SUSPENDED",
            "TERMINATE" => "CANCELLED",
            "REACTIVATE" => "ACTIVE",
            _ => string.Empty
        };
        var validation = ValidateTransition(currentStatus, targetStatus);
        if (validation is not null)
        {
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TenantLifecycleRejectedEvent, CurrentTenant.TenantId, validation.Body), cancellationToken).ConfigureAwait(false);
            return Success(validation);
        }

        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["status"] = targetStatus,
            ["lifecycle_action"] = action,
            ["changed_at"] = now,
            ["reason"] = ReadString(request, "reason") ?? "operator-requested"
        });
        await _documents.UpsertAsync(SaasMultiTenancyConstants.Tenants, TenantDocumentId(CurrentTenant.TenantId), body, null, cancellationToken).ConfigureAwait(false);
        await StoreAuditAsync(SaasMultiTenancyConstants.LifecycleAudits, $"lifecycle-{CurrentTenant.TenantId}-{Guid.NewGuid():N}", $"TENANT_{targetStatus}", body, cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?>
        {
            ["tenant"] = CurrentTenant.TenantId,
            ["status"] = targetStatus,
            ["changedAt"] = now
        };
        if (action == "SUSPEND")
        {
            response["cascadeToSubscriptions"] = true;
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TenantSuspendedEvent, CurrentTenant.TenantId, response), cancellationToken).ConfigureAwait(false);
        }
        else if (action == "TERMINATE")
        {
            response["tombstoneRef"] = Hash($"{CurrentTenant.TenantId}:terminate:{now}");
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TenantTerminatedEvent, CurrentTenant.TenantId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.DataPurgeRequestedEvent, CurrentTenant.TenantId, new Dictionary<string, object?>
            {
                ["tenant"] = CurrentTenant.TenantId,
                ["tombstoneRef"] = response["tombstoneRef"],
                ["purgeScope"] = "ALL_TENANT_DATA"
            }), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TenantReactivatedEvent, CurrentTenant.TenantId, response), cancellationToken).ConfigureAwait(false);
        }

        return Success(new SaasMultiTenancyOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<SaasMultiTenancyDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await SearchTenantAsync(SaasMultiTenancyConstants.Tenants, cancellationToken).ConfigureAwait(false);
        var config = await SearchTenantAsync(SaasMultiTenancyConstants.FreedomConfig, cancellationToken).ConfigureAwait(false);
        var quotas = await SearchTenantAsync(SaasMultiTenancyConstants.QuotaCounters, cancellationToken).ConfigureAwait(false);
        var lifecycle = await SearchTenantAsync(SaasMultiTenancyConstants.LifecycleAudits, cancellationToken).ConfigureAwait(false);
        if (tenants.IsFailure || config.IsFailure || quotas.IsFailure || lifecycle.IsFailure)
        {
            var error = tenants.Error ?? config.Error ?? quotas.Error ?? lifecycle.Error!;
            return Failure<SaasMultiTenancyDashboard>(error.Code, error.Message, error.Details);
        }

        var tenantData = tenants.Data ?? [];
        var configData = config.Data ?? [];
        var quotaData = quotas.Data ?? [];
        var lifecycleData = lifecycle.Data ?? [];
        return Success(new SaasMultiTenancyDashboard(
            tenantData.Select(record => record.Body).ToArray(),
            configData.Select(record => record.Body).ToArray(),
            quotaData.Select(record => record.Body).ToArray(),
            lifecycleData.Select(record => record.Body).ToArray(),
            new Dictionary<string, object?>
            {
                ["activeTenants"] = tenantData.Count(record => string.Equals(ReadString(record.Body, "status"), "ACTIVE", StringComparison.Ordinal)),
                ["configKeys"] = configData.Count,
                ["quotaBatches"] = quotaData.Count,
                ["lifecycleEvents"] = lifecycleData.Count
            }));
    }

    private async Task<DataProcessResult<SaasMultiTenancyOutcome>> MaterializeQuotasCoreAsync(string tenant, string tier, CancellationToken cancellationToken)
    {
        var tierDefinition = await ResolveTierDefinitionAsync(tier, cancellationToken).ConfigureAwait(false);
        if (tierDefinition.IsFailure)
        {
            return Failure<SaasMultiTenancyOutcome>(tierDefinition.Error!.Code, tierDefinition.Error.Message, tierDefinition.Error.Details);
        }

        var quotas = ReadObject(tierDefinition.Data!, "quotas") ?? new Dictionary<string, object?>();
        var commands = quotas.Select(pair => new Dictionary<string, object?>
        {
            ["command"] = "SET",
            ["key"] = $"quota:{tenant}:{pair.Key}",
            ["value"] = pair.Value
        }).ToArray();
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["tenant"] = tenant,
            ["subscription_tier"] = tier,
            ["batch_mode"] = "MULTI_EXEC",
            ["commands"] = commands,
            ["materialized_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        });
        var stored = await _documents.UpsertAsync(SaasMultiTenancyConstants.QuotaCounters, $"quotas:{tenant}:{tier}", body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<SaasMultiTenancyOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        var response = new Dictionary<string, object?>
        {
            ["tenant"] = tenant,
            ["subscriptionTier"] = tier,
            ["batchMode"] = "MULTI_EXEC",
            ["commandCount"] = commands.Length,
            ["version"] = stored.Data!.Version
        };
        await _outbox.AppendAsync(CreateEvent(SaasMultiTenancyConstants.TenantQuotaMaterializedEvent, tenant, response), cancellationToken).ConfigureAwait(false);
        return Success(new SaasMultiTenancyOutcome(200, response, true, true));
    }

    private async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> ResolveTierDefinitionAsync(string tier, CancellationToken cancellationToken)
    {
        var found = await _documents.GetAsync(SaasMultiTenancyConstants.TierDefinitions, $"tier:{tier}", cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return Failure<IReadOnlyDictionary<string, object?>>(found.Error!.Code, found.Error.Message, found.Error.Details);
        }

        return Success(found.Data?.Body ?? DefaultTierDefinition);
    }

    private async Task SeedFreedomKeysAsync(string createdTenant, string trustedMaster, string tier, string now, CancellationToken cancellationToken)
    {
        var keys = new Dictionary<string, object?>
        {
            ["tenantId"] = createdTenant,
            ["masterTenantId"] = trustedMaster,
            ["provisionedAt"] = now,
            ["subscriptionTier"] = tier
        };
        foreach (var pair in keys)
        {
            await _documents.UpsertAsync(
                SaasMultiTenancyConstants.FreedomConfig,
                $"machine:{createdTenant}:{pair.Key}",
                WithTenant(new Dictionary<string, object?>
                {
                    ["tenant"] = createdTenant,
                    ["key"] = pair.Key,
                    ["value"] = pair.Value,
                    ["locked"] = true,
                    ["source"] = "tenant-provisioning"
                }),
                null,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task StoreAuditAsync(
        string collection,
        string id,
        string auditType,
        IReadOnlyDictionary<string, object?> body,
        CancellationToken cancellationToken)
    {
        var audit = WithTenant(new Dictionary<string, object?>
        {
            ["audit_type"] = auditType,
            ["correlation_id"] = CurrentCorrelationId,
            ["subject"] = id,
            ["body_hash"] = JsonSerializer.Serialize(body).GetHashCode(StringComparison.Ordinal),
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        });
        await _documents.UpsertAsync(collection, id, audit, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataProcessResult<IReadOnlyList<DocumentRecord>>> SearchTenantAsync(string collection, CancellationToken cancellationToken) =>
        await _documents.SearchAsync(collection, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);

    private IReadOnlyDictionary<string, object?> WithTenant(IReadOnlyDictionary<string, object?> body)
    {
        var copy = new Dictionary<string, object?>(body)
        {
            ["tenant_id"] = CurrentTenant.TenantId
        };
        return copy;
    }

    private SaasMultiTenancyOutcome? ValidateTransition(string currentStatus, string targetStatus)
    {
        if (currentStatus == "PAUSED")
        {
            return new SaasMultiTenancyOutcome(409, ErrorBody("INVALID_TRANSITION", "Paused billing state cannot be used as a tenant lifecycle state."), false, true);
        }

        var allowed = currentStatus switch
        {
            "TRIAL" => new[] { "ACTIVE", "CANCELLED" },
            "ACTIVE" => new[] { "SUSPENDED", "CANCELLED" },
            "SUSPENDED" => new[] { "ACTIVE", "CANCELLED" },
            _ => []
        };
        return allowed.Contains(targetStatus, StringComparer.Ordinal)
            ? null
            : new SaasMultiTenancyOutcome(409, ErrorBody("INVALID_TRANSITION", $"Transition {currentStatus} to {targetStatus} is not allowed."), false, true);
    }

    private static List<Dictionary<string, object?>> ValidateProvisionRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<Dictionary<string, object?>>();
        AddRequiredString(errors, request, "tenantSlug");
        AddRequiredString(errors, request, "workspaceName");
        AddRequiredString(errors, request, "subscriptionTier");
        AddRequiredString(errors, request, "billingContact");
        AddRequiredString(errors, request, "operatorId");
        return errors;
    }

    private static SaasMultiTenancyOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new SaasMultiTenancyOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static SaasMultiTenancyOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var forbidden = request.Keys.FirstOrDefault(key =>
            string.Equals(key, "tenantId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "tenant_id", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "masterTenantId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "master_tenant_id", StringComparison.OrdinalIgnoreCase));
        return forbidden is null
            ? null
            : new SaasMultiTenancyOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", $"{forbidden} is resolved from trusted platform context, never request body."), false, false);
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            SaasMultiTenancyConstants.FlowId,
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId,
                ["xiigenflowid"] = SaasMultiTenancyConstants.FlowId
            });

    private static IReadOnlyDictionary<string, object?> ValidationBody(IEnumerable<Dictionary<string, object?>> details) =>
        new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details.ToArray() };

    private static IReadOnlyDictionary<string, object?> ErrorBody(string code, string message) =>
        new Dictionary<string, object?> { ["code"] = code, ["message"] = message };

    private static Dictionary<string, object?> FieldError(string field, string reason) =>
        new() { ["field"] = field, ["reason"] = reason };

    private static void AddRequiredString(List<Dictionary<string, object?>> errors, IReadOnlyDictionary<string, object?> request, string field)
    {
        if (string.IsNullOrWhiteSpace(ReadString(request, field)))
        {
            errors.Add(FieldError(field, "Value is required."));
        }
    }

    private static string TenantDocumentId(string slug) => $"tenant:{NormalizeSlug(slug)}";

    private static string ComputeTenantId(string slug, string operatorId) => $"tenant-{Hash($"{NormalizeSlug(slug)}:{operatorId}")[..16]}";

    private static string NormalizeSlug(string value) =>
        string.Join("-", value.Trim().ToLowerInvariant().Split([' ', '_', '.'], StringSplitOptions.RemoveEmptyEntries));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string? ReadString(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static int ReadInt(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => Convert.ToInt32(longValue, CultureInfo.InvariantCulture),
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && value is bool boolValue && boolValue;

    private static IReadOnlyDictionary<string, object?>? ReadObject(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) ? value as IReadOnlyDictionary<string, object?> : null;
}
