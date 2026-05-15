using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.UserRegistration.Application;

public sealed class OnboardingDeliveryService : MicroserviceBase<OnboardingDeliveryService>
{
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public OnboardingDeliveryService(
        IDocumentStore documents,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<OnboardingDeliveryService> logger)
        : base(
            new ServiceDescriptor("T49", nameof(OnboardingDeliveryService), UserRegistrationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _outbox = outbox;
        _freedom = freedom;
    }

    public Task<DataProcessResult<OnboardingOutcome>> DeliverWorkspaceAsync(string userId, CancellationToken cancellationToken = default) =>
        StoreDeliveryAsync(userId, "workspace_setup", null, cancellationToken);

    public Task<DataProcessResult<OnboardingOutcome>> DeliverTutorialAsync(string userId, CancellationToken cancellationToken = default) =>
        StoreDeliveryAsync(userId, "flow_tutorial", null, cancellationToken);

    public async Task<DataProcessResult<OnboardingOutcome>> DeliverInvitationAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var inviter = await GetStringAsync("flow01_invitation_inviter_name", "The Team", cancellationToken).ConfigureAwait(false);
        var community = await GetStringAsync("flow01_invitation_community_name", "Community", cancellationToken).ConfigureAwait(false);
        return await StoreDeliveryAsync(
            userId,
            "community_invitation",
            new Dictionary<string, object?> { ["inviterName"] = inviter, ["communityName"] = community },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DataProcessResult<OnboardingOutcome>> CompleteAsync(
        string userId,
        string email,
        int existingOnboardingMaterialsCount = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
        {
            return Failure<OnboardingOutcome>("VALIDATION_FAILED", "userId and email are required");
        }

        if (existingOnboardingMaterialsCount >= 3)
        {
            return Success(new OnboardingOutcome(200, new Dictionary<string, object?> { ["completed"] = true, ["ssoBypass"] = true }, true));
        }

        var records = await _documents.SearchAsync(
            UserRegistrationConstants.OnboardingDeliveries,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["user_id"] = userId },
            cancellationToken).ConfigureAwait(false);
        if (records.IsFailure || records.Data is null)
        {
            return Failure<OnboardingOutcome>("DELIVERY_QUERY_FAILED", "Could not read onboarding delivery records");
        }

        var presentTypes = records.Data
            .Select(record => Convert.ToString(record.Body["material_type"], CultureInfo.InvariantCulture))
            .Where(value => value is not null)
            .ToHashSet(StringComparer.Ordinal);
        if (!UserRegistrationConstants.OnboardingMaterialTypes.All(type => presentTypes.Contains(type)))
        {
            return Failure<OnboardingOutcome>("DELIVERY_INCOMPLETE", "Not all onboarding materials are present");
        }

        var completionId = $"onb-{Guid.NewGuid():N}";
        var completedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var body = new Dictionary<string, object?>
        {
            ["id"] = completionId,
            ["userId"] = userId,
            ["email"] = email,
            ["tenantId"] = CurrentTenant.TenantId,
            ["onboardingCompletedAt"] = completedAt
        };

        var stored = await _documents.UpsertAsync(
            UserRegistrationConstants.OnboardingDeliveries,
            completionId,
            body,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<OnboardingOutcome>(stored.Error?.Code ?? "STORE_FAILED", stored.Error?.Message ?? "Completion store failed");
        }

        await _outbox.AppendAsync(CreateEvent("xiigen.user-registration.onboarding-completed.v1", userId, body), cancellationToken).ConfigureAwait(false);
        return Success(new OnboardingOutcome(200, body, true));
    }

    private async Task<DataProcessResult<OnboardingOutcome>> StoreDeliveryAsync(
        string userId,
        string materialType,
        IReadOnlyDictionary<string, object?>? socialParams,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure<OnboardingOutcome>("VALIDATION_FAILED", "userId is required");
        }

        var deliveryId = $"del-{materialType}-{Guid.NewGuid():N}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = deliveryId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["material_type"] = materialType,
            ["status"] = "delivered",
            ["knowledge_scope"] = "PRIVATE",
            ["delivered_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        if (socialParams is not null)
        {
            body["social_params"] = socialParams;
        }

        var stored = await _documents.UpsertAsync(UserRegistrationConstants.OnboardingDeliveries, deliveryId, body, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            var failed = body.ToDictionary(pair => pair.Key, pair => pair.Value);
            failed["status"] = "failed";
            failed["failed_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            await _documents.UpsertAsync(UserRegistrationConstants.OnboardingDeliveries, deliveryId, failed, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Failure<OnboardingOutcome>(stored.Error?.Code ?? "DELIVERY_FAILED", stored.Error?.Message ?? "Delivery failed");
        }

        return Success(new OnboardingOutcome(201, body, false));
    }

    private async Task<string> GetStringAsync(string key, string fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return Convert.ToString(value.Data?.Value, CultureInfo.InvariantCulture) ?? fallback;
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            "xiigen://flow/user-registration",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = UserRegistrationConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });
}
