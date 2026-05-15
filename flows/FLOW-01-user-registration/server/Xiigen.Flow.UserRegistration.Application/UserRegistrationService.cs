using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Auth;
using Xiigen.Kernel;

namespace Xiigen.Flow.UserRegistration.Application;

public sealed class UserRegistrationService : MicroserviceBase<UserRegistrationService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightEmails = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IPasswordHasherService _passwordHasher;

    public UserRegistrationService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IPasswordHasherService passwordHasher,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<UserRegistrationService> logger)
        : base(
            new ServiceDescriptor("T47", nameof(UserRegistrationService), UserRegistrationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _passwordHasher = passwordHasher;
    }

    public async Task<DataProcessResult<UserRegistrationOutcome>> RegisterAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<UserRegistrationOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var email = NormalizeEmail(ReadString(request, "email"));
        var password = ReadString(request, "password") ?? ReadString(request, "credentials");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Failure<UserRegistrationOutcome>(
                "VALIDATION_FAILED",
                "Registration input validation failed",
                new Dictionary<string, object?>
                {
                    ["details"] = new[]
                    {
                        new Dictionary<string, object?> { ["field"] = string.IsNullOrWhiteSpace(email) ? "email" : "password", ["reason"] = "required" }
                    }
                });
        }

        var operationKey = $"{UserRegistrationConstants.FlowSlug}:register:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<UserRegistrationOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new UserRegistrationOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutData = new Dictionary<string, object?> { ["email"] = email, ["retryAfterMs"] = 5000 };
            var retained = await _outbox.AppendAsync(CreateEvent("xiigen.user-registration.registration-timeout.v1", email, timeoutData), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timeout", cancellationToken).ConfigureAwait(false);
            if (retained.IsFailure)
            {
                return Failure<UserRegistrationOutcome>("UPSTREAM_FAILURE", "Outbox append failed during timeout handling");
            }

            return Failure<UserRegistrationOutcome>(
                "TIMEOUT",
                "Downstream fabric timeout",
                new Dictionary<string, object?> { ["retryAfterMs"] = 5000 });
        }

        var inFlightKey = $"{CurrentTenant.TenantId}:{email}";
        if (!InFlightEmails.TryAdd(inFlightKey, 1))
        {
            return Failure<UserRegistrationOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent registration detected",
                new Dictionary<string, object?> { ["currentVersion"] = "pending", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            var existing = await _documents.SearchAsync(
                UserRegistrationConstants.UserRegistrations,
                new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["normalized_email"] = email },
                cancellationToken).ConfigureAwait(false);
            if (existing.IsSuccess && existing.Data?.Count > 0)
            {
                return Failure<UserRegistrationOutcome>("DUPLICATE_EMAIL", "Email already registered");
            }

            var hash = await _passwordHasher.HashAsync(password, cancellationToken).ConfigureAwait(false);
            if (hash.IsFailure || hash.Data is null)
            {
                return Failure<UserRegistrationOutcome>(
                    hash.Error?.Code ?? "HASH_FAILED",
                    hash.Error?.Message ?? "Password hashing failed");
            }

            var userId = $"usr-{Guid.NewGuid():N}";
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var body = new Dictionary<string, object?>
            {
                ["id"] = userId,
                ["userId"] = userId,
                ["email"] = email,
                ["normalized_email"] = email,
                ["credentials_hash"] = hash.Data.Hash,
                ["status"] = "PENDING_VERIFICATION",
                ["onboarding_materials"] = Array.Empty<string>(),
                ["tenant_id"] = CurrentTenant.TenantId,
                ["knowledge_scope"] = "PRIVATE",
                ["created_at"] = now
            };

            var stored = await _documents.UpsertAsync(
                UserRegistrationConstants.UserRegistrations,
                userId,
                body,
                expectedVersion: null,
                cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<UserRegistrationOutcome>(
                    stored.Error?.Code ?? "STORE_FAILED",
                    stored.Error?.Message ?? "Failed to store user registration");
            }

            var storedRecord = stored.Data!;
            var response = new Dictionary<string, object?>
            {
                ["id"] = userId,
                ["userId"] = userId,
                ["version"] = storedRecord.Version,
                ["accountStatus"] = "PENDING_VERIFICATION"
            };

            var outbox = await _outbox.AppendAsync(
                CreateEvent("xiigen.user-registration.registration-initiated.v1", userId, response),
                cancellationToken).ConfigureAwait(false);
            if (outbox.IsFailure)
            {
                return Failure<UserRegistrationOutcome>(
                    outbox.Error?.Code ?? "OUTBOX_FAILED",
                    outbox.Error?.Message ?? "Failed to append registration event");
            }

            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new UserRegistrationOutcome(201, response, true, true));
        }
        finally
        {
            InFlightEmails.TryRemove(inFlightKey, out _);
        }
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

    private static string? ReadString(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) && value is bool boolValue && boolValue;

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
