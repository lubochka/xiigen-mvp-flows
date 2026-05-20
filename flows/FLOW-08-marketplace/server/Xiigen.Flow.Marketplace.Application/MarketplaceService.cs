using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.Marketplace.Application;

public sealed class MarketplaceService : MicroserviceBase<MarketplaceService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightListings = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public MarketplaceService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<MarketplaceService> logger)
        : base(
            new ServiceDescriptor("FLOW-08", nameof(MarketplaceService), MarketplaceConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<MarketplaceOutcome>> PublishListingAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<MarketplaceOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required", new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var listingId = (ReadString(request, "listingId") ?? $"listing-{Guid.NewGuid():N}").Trim();
        var sellerId = (ReadString(request, "sellerId") ?? string.Empty).Trim();
        var operationKey = $"{MarketplaceConstants.FlowSlug}:publish:{CurrentTenant.TenantId}:{listingId}:{idempotencyKey}";

        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<MarketplaceOutcome>(reservation.Error?.Code ?? "IDEMPOTENCY_FAILED", reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new MarketplaceOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutPayload = new Dictionary<string, object?>
            {
                ["listingId"] = listingId,
                ["sellerId"] = sellerId,
                ["code"] = "TIMEOUT",
                ["retryAfterMs"] = 5000
            };
            await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.TimeoutRetainedEvent, listingId, timeoutPayload), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timed out", cancellationToken).ConfigureAwait(false);
            return Success(new MarketplaceOutcome(504, timeoutPayload, false, true));
        }

        var concurrencyKey = $"{CurrentTenant.TenantId}:{listingId}";
        if (!InFlightListings.TryAdd(concurrencyKey, 0))
        {
            return Failure<MarketplaceOutcome>("CONCURRENT_UPDATE", "Concurrent marketplace listing mutation detected", new Dictionary<string, object?> { ["currentVersion"] = ifMatch ?? "unknown", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(80, cancellationToken).ConfigureAwait(false);
            }

            var audit = await WriteAuditFirstAsync(listingId, request, cancellationToken).ConfigureAwait(false);
            if (audit.IsFailure)
            {
                return Failure<MarketplaceOutcome>(audit.Error!.Code, audit.Error.Message, audit.Error.Details);
            }

            var validation = ValidateListing(request);
            if (validation is not null)
            {
                return validation;
            }

            var price = ReadDecimal(request, "price")!.Value;
            var cap = await ResolvePriceCapAsync(request, cancellationToken).ConfigureAwait(false);
            if (cap.HasValue && price > cap.Value)
            {
                return Failure<MarketplaceOutcome>("PRICE_EXCEEDS_CAP", "Listing price is above the tenant marketplace cap", new Dictionary<string, object?> { ["field"] = "price", ["maxPrice"] = cap.Value });
            }

            var moderation = ResolveModeration(request);
            await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.ModerationInlineEvent, listingId, new Dictionary<string, object?> { ["listingId"] = listingId, ["result"] = moderation }), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.PriceCheckInlineEvent, listingId, new Dictionary<string, object?> { ["listingId"] = listingId, ["price"] = price }), cancellationToken).ConfigureAwait(false);

            if (moderation == "REJECT")
            {
                var draft = CreateListingBody(listingId, request, "DRAFT", price);
                draft["draftReason"] = "moderation_rejected";
                var storedDraft = await _documents.UpsertAsync(MarketplaceConstants.Drafts, listingId, draft, ifMatch, cancellationToken).ConfigureAwait(false);
                if (storedDraft.IsFailure)
                {
                    return Failure<MarketplaceOutcome>(storedDraft.Error?.Code ?? "STORE_FAILED", storedDraft.Error?.Message ?? "Failed to store marketplace draft");
                }

                await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.ListingDraftedEvent, listingId, CountOnlyPayload(listingId, "DRAFT", 1)), cancellationToken).ConfigureAwait(false);
                var body = new Dictionary<string, object?>(draft)
                {
                    ["id"] = listingId,
                    ["version"] = storedDraft.Data?.Version ?? "v1",
                    ["status"] = "DRAFT"
                };
                await _idempotency.MarkSucceededAsync(operationKey, body, cancellationToken).ConfigureAwait(false);
                return Success(new MarketplaceOutcome(201, body, true, true));
            }

            if (moderation == "UNCERTAIN")
            {
                await _documents.UpsertAsync(MarketplaceConstants.ModerationReviews, listingId, CreateListingBody(listingId, request, "REVIEW_REQUIRED", price), null, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.HumanReviewRequestedEvent, listingId, CountOnlyPayload(listingId, "REVIEW_REQUIRED", 1)), cancellationToken).ConfigureAwait(false);
            }

            var listing = CreateListingBody(listingId, request, moderation == "UNCERTAIN" ? "REVIEW_REQUIRED" : "ACTIVE", price);
            var stored = await _documents.UpsertAsync(MarketplaceConstants.Listings, listingId, listing, ifMatch, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<MarketplaceOutcome>(stored.Error?.Code ?? "STORE_FAILED", stored.Error?.Message ?? "Failed to store marketplace listing");
            }

            var catalog = new Dictionary<string, object?>(listing)
            {
                ["searchKey"] = $"{ReadString(request, "category")}-{ReadString(request, "title")}".ToLowerInvariant(),
                ["version"] = stored.Data?.Version ?? "v1"
            };
            await _documents.UpsertAsync(MarketplaceConstants.Catalog, listingId, catalog, null, cancellationToken).ConfigureAwait(false);
            var feed = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["listing_count"] = 1,
                ["status"] = listing["status"],
                ["generated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            await _documents.UpsertAsync(MarketplaceConstants.FeedBatches, $"feed-{listingId}", feed, null, cancellationToken).ConfigureAwait(false);
            await _documents.UpsertAsync(MarketplaceConstants.Analytics, $"analytics-{listingId}", CreateAnalyticsBody(listingId, request), null, cancellationToken).ConfigureAwait(false);

            await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.ListingPublishedEvent, listingId, CountOnlyPayload(listingId, Convert.ToString(listing["status"])!, 1)), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.ListingIndexedEvent, listingId, new Dictionary<string, object?> { ["count"] = 1 }), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.FeedGeneratedEvent, listingId, new Dictionary<string, object?> { ["count"] = 1 }), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.AnalyticsRecordedEvent, listingId, new Dictionary<string, object?> { ["listingCount"] = 1 }), cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?>(listing)
            {
                ["id"] = listingId,
                ["version"] = stored.Data?.Version ?? "v1",
                ["catalogIndexed"] = true,
                ["feedGenerated"] = true
            };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new MarketplaceOutcome(201, response, true, true));
        }
        finally
        {
            InFlightListings.TryRemove(concurrencyKey, out _);
        }
    }

    public async Task<DataProcessResult<MarketplaceCatalog>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await _documents.SearchAsync(MarketplaceConstants.Catalog, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        if (catalog.IsFailure)
        {
            return Failure<MarketplaceCatalog>(catalog.Error?.Code ?? "CATALOG_READ_FAILED", catalog.Error?.Message ?? "Could not read marketplace catalog");
        }

        var rows = (catalog.Data ?? []).Select(row => (IReadOnlyDictionary<string, object?>)row.Body).ToList();
        return Success(new MarketplaceCatalog(rows, rows.Count, CountByStatus(rows)));
    }

    public async Task<DataProcessResult<MarketplaceAnalytics>> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var analytics = await _documents.SearchAsync(MarketplaceConstants.Analytics, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        if (analytics.IsFailure)
        {
            return Failure<MarketplaceAnalytics>(analytics.Error?.Code ?? "ANALYTICS_READ_FAILED", analytics.Error?.Message ?? "Could not read marketplace analytics");
        }

        var rows = (analytics.Data ?? []).Select(row => row.Body).ToList();
        var views = rows.Sum(row => Convert.ToInt32(row.GetValueOrDefault("views") ?? 0, CultureInfo.InvariantCulture));
        var inquiries = rows.Sum(row => Convert.ToInt32(row.GetValueOrDefault("inquiries") ?? 0, CultureInfo.InvariantCulture));
        var conversion = inquiries / (double)(views == 0 ? 1 : views);
        return Success(new MarketplaceAnalytics(rows.Count, views, inquiries, conversion, CountByStatus(rows), rows.Any(row => row.ContainsKey("viewerIds") || row.ContainsKey("viewer_ids"))));
    }

    private async Task<DataProcessResult<bool>> WriteAuditFirstAsync(string listingId, IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken)
    {
        var auditBody = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["listing_id"] = listingId,
            ["seller_id"] = ReadString(request, "sellerId"),
            ["action"] = "SAVE_REQUESTED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var audit = await _documents.UpsertAsync(MarketplaceConstants.PublicationAudits, $"audit-{listingId}", auditBody, null, cancellationToken).ConfigureAwait(false);
        if (audit.IsFailure)
        {
            return Failure<bool>(audit.Error?.Code ?? "AUDIT_FAILED", audit.Error?.Message ?? "Publication audit write failed");
        }

        await _outbox.AppendAsync(CreateEvent(MarketplaceConstants.SaveRequestedEvent, listingId, new Dictionary<string, object?> { ["listingId"] = listingId }), cancellationToken).ConfigureAwait(false);
        return Success(true);
    }

    private DataProcessResult<MarketplaceOutcome>? ValidateListing(IReadOnlyDictionary<string, object?> request)
    {
        var details = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "sellerId"))) details.Add(Field("sellerId", "seller is required"));
        if (string.IsNullOrWhiteSpace(ReadString(request, "title"))) details.Add(Field("title", "title is required"));
        if (string.IsNullOrWhiteSpace(ReadString(request, "category"))) details.Add(Field("category", "category is required"));
        var price = ReadDecimal(request, "price");
        if (!price.HasValue) details.Add(Field("price", "price is required"));
        if (price.HasValue && price.Value < 0) details.Add(Field("price", "negative price is not allowed"));

        if (details.Count == 0)
        {
            return null;
        }

        return Success(new MarketplaceOutcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false, false));
    }

    private async Task<decimal?> ResolvePriceCapAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken)
    {
        var requestCap = ReadDecimal(request, "maxPriceCap");
        if (requestCap.HasValue)
        {
            return requestCap.Value;
        }

        var freedom = await _freedom.GetAsync($"flow08_marketplace_max_price_cap.{CurrentTenant.TenantId}", cancellationToken).ConfigureAwait(false);
        return freedom.IsSuccess && freedom.Data?.Value is not null && decimal.TryParse(Convert.ToString(freedom.Data.Value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var cap)
            ? cap
            : null;
    }

    private Dictionary<string, object?> CreateListingBody(string listingId, IReadOnlyDictionary<string, object?> request, string status, decimal price) => new()
    {
        ["tenant_id"] = CurrentTenant.TenantId,
        ["listing_id"] = listingId,
        ["seller_id"] = ReadString(request, "sellerId")!.Trim(),
        ["title"] = ReadString(request, "title")!.Trim(),
        ["category"] = ReadString(request, "category")!.Trim(),
        ["description"] = ReadString(request, "description") ?? string.Empty,
        ["price"] = price,
        ["currency"] = ReadString(request, "currency") ?? "USD",
        ["inventory"] = ReadInt(request, "inventory") ?? 0,
        ["status"] = status,
        ["published_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
    };

    private Dictionary<string, object?> CreateAnalyticsBody(string listingId, IReadOnlyDictionary<string, object?> request)
    {
        var views = ReadInt(request, "views") ?? 0;
        var inquiries = ReadInt(request, "inquiries") ?? 0;
        return new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["listing_id"] = listingId,
            ["listing_count"] = 1,
            ["views"] = views,
            ["inquiries"] = inquiries,
            ["conversion_rate"] = inquiries / (double)(views == 0 ? 1 : views),
            ["status"] = "RECORDED"
        };
    }

    private static string ResolveModeration(IReadOnlyDictionary<string, object?> request)
    {
        var signal = (ReadString(request, "moderationSignal") ?? ReadString(request, "description") ?? string.Empty).ToUpperInvariant();
        if (signal.Contains("REJECT", StringComparison.Ordinal) || signal.Contains("BANNED", StringComparison.Ordinal)) return "REJECT";
        if (signal.Contains("UNCERTAIN", StringComparison.Ordinal) || signal.Contains("REVIEW", StringComparison.Ordinal)) return "UNCERTAIN";
        return "PASS";
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{MarketplaceConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(data),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = MarketplaceConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private static Dictionary<string, object?> CountOnlyPayload(string listingId, string status, int count) => new()
    {
        ["listingId"] = listingId,
        ["status"] = status,
        ["count"] = count
    };

    private static IReadOnlyDictionary<string, int> CountByStatus(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var status = Convert.ToString(row.GetValueOrDefault("status"), CultureInfo.InvariantCulture) ?? "UNKNOWN";
            counts[status] = counts.GetValueOrDefault(status) + 1;
        }

        return counts;
    }

    private static Dictionary<string, object?> Field(string field, string reason) => new() { ["field"] = field, ["reason"] = reason };

    private static string? ReadString(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var result) && result;

    private static decimal? ReadDecimal(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static int? ReadInt(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
}

