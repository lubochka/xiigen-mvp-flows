using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Xiigen.Fabrics.Scheduler;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.UserRegistration.Application;

public sealed class EmailVerificationService : MicroserviceBase<EmailVerificationService>
{
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;
    private readonly ISchedulerService _scheduler;
    private readonly IFreedomConfigService _freedom;

    public EmailVerificationService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ISchedulerService scheduler,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<EmailVerificationService> logger)
        : base(
            new ServiceDescriptor("T48", nameof(EmailVerificationService), UserRegistrationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _outbox = outbox;
        _scheduler = scheduler;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<EmailVerificationOutcome>> InitiateAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
        {
            return Failure<EmailVerificationOutcome>("VALIDATION_FAILED", "userId and email are required");
        }

        return await IssueTokenAsync(userId, email.Trim().ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<DataProcessResult<EmailVerificationOutcome>> VerifyAsync(
        string userId,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(rawToken))
        {
            return Failure<EmailVerificationOutcome>("VALIDATION_FAILED", "userId and token are required");
        }

        var tokenHash = HashToken(rawToken);
        var found = await _documents.SearchAsync(
            UserRegistrationConstants.VerificationTokens,
            new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["user_id"] = userId,
                ["token_hash"] = tokenHash
            },
            cancellationToken).ConfigureAwait(false);

        if (found.IsFailure || found.Data is null || found.Data.Count == 0)
        {
            return Failure<EmailVerificationOutcome>("INVALID_TOKEN", "Token not found");
        }

        var token = found.Data[0];
        var priorStatus = Convert.ToString(token.Body["status"], CultureInfo.InvariantCulture);
        var updated = token.Body.ToDictionary(pair => pair.Key, pair => pair.Value);
        updated["status"] = "used";
        updated["used_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        await _documents.UpsertAsync(
            UserRegistrationConstants.VerificationTokens,
            token.Id,
            updated,
            token.Version,
            cancellationToken).ConfigureAwait(false);

        if (priorStatus == "used")
        {
            return Failure<EmailVerificationOutcome>("USED_TOKEN", "Token already used");
        }

        if (priorStatus == "superseded")
        {
            return Failure<EmailVerificationOutcome>("INVALID_TOKEN", "Token was superseded");
        }

        var expiresAt = DateTimeOffset.Parse(Convert.ToString(token.Body["expires_at"], CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
        if (priorStatus == "expired" || expiresAt < DateTimeOffset.UtcNow)
        {
            return Failure<EmailVerificationOutcome>("EXPIRED_TOKEN", "Token expired");
        }

        var body = new Dictionary<string, object?>
        {
            ["id"] = userId,
            ["userId"] = userId,
            ["verified"] = true,
            ["verifiedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _outbox.AppendAsync(CreateEvent("xiigen.user-registration.email-verified.v1", userId, body), cancellationToken).ConfigureAwait(false);
        return Success(new EmailVerificationOutcome(200, body));
    }

    public async Task<DataProcessResult<EmailVerificationOutcome>> ResendAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
        {
            return Failure<EmailVerificationOutcome>("VALIDATION_FAILED", "userId and email are required");
        }

        var window = await GetNumberAsync("flow01_resend_rate_limit_minutes", 60, cancellationToken).ConfigureAwait(false);
        var recent = await _documents.SearchAsync(
            UserRegistrationConstants.ResendAttempts,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["user_id"] = userId },
            cancellationToken).ConfigureAwait(false);
        if (recent.IsSuccess && recent.Data?.Count > 0)
        {
            return Failure<EmailVerificationOutcome>(
                "RATE_LIMIT_EXCEEDED",
                "Resend verification is rate limited",
                new Dictionary<string, object?> { ["retryAfterMinutes"] = window });
        }

        var attemptId = $"resend-{Guid.NewGuid():N}";
        await _documents.UpsertAsync(
            UserRegistrationConstants.ResendAttempts,
            attemptId,
            new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["user_id"] = userId,
                ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await SupersedeActiveTokensAsync(userId, cancellationToken).ConfigureAwait(false);
        return await IssueTokenAsync(userId, email.Trim().ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataProcessResult<EmailVerificationOutcome>> IssueTokenAsync(
        string userId,
        string email,
        CancellationToken cancellationToken)
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = HashToken(rawToken);
        var tokenId = $"tok-{Guid.NewGuid():N}";
        var ttlSeconds = await GetNumberAsync("flow01_email_verification_ttl_seconds", 86_400, cancellationToken).ConfigureAwait(false);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds);

        var tokenDoc = new Dictionary<string, object?>
        {
            ["token_id"] = tokenId,
            ["user_id"] = userId,
            ["email"] = email,
            ["token_hash"] = tokenHash,
            ["status"] = "active",
            ["tenant_id"] = CurrentTenant.TenantId,
            ["knowledge_scope"] = "PRIVATE",
            ["expires_at"] = expiresAt.ToString("O", CultureInfo.InvariantCulture),
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var stored = await _documents.UpsertAsync(
            UserRegistrationConstants.VerificationTokens,
            tokenId,
            tokenDoc,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<EmailVerificationOutcome>(stored.Error?.Code ?? "STORE_FAILED", stored.Error?.Message ?? "Token store failed");
        }

        var eventBody = new Dictionary<string, object?>
        {
            ["id"] = tokenId,
            ["userId"] = userId,
            ["email"] = email,
            ["rawToken"] = rawToken,
            ["expiresAt"] = expiresAt.ToString("O", CultureInfo.InvariantCulture)
        };
        await _outbox.AppendAsync(CreateEvent("xiigen.user-registration.verification-email-requested.v1", tokenId, eventBody), cancellationToken).ConfigureAwait(false);
        await _scheduler.ScheduleDelayedAsync(
            "user-registration-token-expiry",
            TimeSpan.FromSeconds(ttlSeconds),
            new Dictionary<string, object?> { ["tokenId"] = tokenId, ["userId"] = userId },
            $"token-expiry-{tokenId}",
            cancellationToken).ConfigureAwait(false);

        return Success(new EmailVerificationOutcome(201, eventBody));
    }

    private async Task SupersedeActiveTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var active = await _documents.SearchAsync(
            UserRegistrationConstants.VerificationTokens,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["user_id"] = userId, ["status"] = "active" },
            cancellationToken).ConfigureAwait(false);
        if (active.Data is null)
        {
            return;
        }

        foreach (var token in active.Data)
        {
            var updated = token.Body.ToDictionary(pair => pair.Key, pair => pair.Value);
            updated["status"] = "superseded";
            updated["superseded_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            await _documents.UpsertAsync(UserRegistrationConstants.VerificationTokens, token.Id, updated, token.Version, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> GetNumberAsync(string key, int fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return value.Data?.Value is int integer ? integer : fallback;
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

    private static string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
