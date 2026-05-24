using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.BlogCmsModules.Application;

public sealed class BlogCmsModulesService : MicroserviceBase<BlogCmsModulesService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private static readonly string[] ReservedRoutePrefixes = ["/api/", "/admin/", "/auth/"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public BlogCmsModulesService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<BlogCmsModulesService> logger)
        : base(new ServiceDescriptor("blog-cms-modules-service", "Blog/CMS Modules", BlogCmsModulesConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<BlogCmsModulesOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var rules = ReadRules(request);
        if (rules.Count == 0) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_REQUIRED" }, false));
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = BlogCmsModulesConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid, ["requiredCount"] = BlogCmsModulesConstants.RequiredBfaRules.Length }, false));
        }
        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["requiredCount"] = BlogCmsModulesConstants.RequiredBfaRules.Length, ["coverage"] = "CF-566-CF-600+CF-1003-CF-1006" }, false));
    }

    public async Task<DataProcessResult<BlogCmsModulesOutcome>> CreateContentAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "title", "body", "slug", "visibility", "authorUserId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout"))
        {
            await _outbox.AppendAsync(Envelope("blog.content.timeout-retained", ScopedId($"timeout:{Hash(idempotencyKey!)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantId"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
            return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true }, false, true));
        }
        if (ContainsUnsafeHtml(ReadString(request, "body"))) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "XSS_REJECTED", ["ruleId"] = "CF-577" }, false));
        var slug = NormalizeSlug(ReadString(request, "slug"));
        if (ReservedRoutePrefixes.Any(prefix => slug.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "ROUTE_PREFIX_RESERVED", ["ruleId"] = "CF-573" }, false));
        var op = OperationKey("content", idempotencyKey!);
        var replay = await ReserveAsync(op, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var contentId = ScopedId($"content:{Hash(slug)}");
        var existing = await GetTenantDocumentAsync(BlogCmsModulesConstants.ContentCollection, contentId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1003" }, false));
        }
        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var status = ReadBool(request, "publishNow") ? "published" : "draft";
        var body = new Dictionary<string, object?>
        {
            ["contentId"] = contentId,
            ["title"] = ReadString(request, "title"),
            ["body"] = ReadString(request, "body"),
            ["slug"] = slug,
            ["visibility"] = ReadString(request, "visibility"),
            ["authorUserId"] = ReadString(request, "authorUserId"),
            ["status"] = status,
            ["version"] = nextVersion,
            ["tenantId"] = CurrentTenant.TenantId,
            ["source_flow"] = BlogCmsModulesConstants.FlowId,
            ["searchIndexed"] = status == "published",
            ["indexName"] = $"blog-content-{CurrentTenant.TenantId}",
            ["storedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(BlogCmsModulesConstants.ContentCollection, contentId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<BlogCmsModulesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("CONTENT_STORED", contentId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope(status == "published" ? "blog.content.published" : "blog.content.draft-saved", contentId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(op, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public async Task<DataProcessResult<BlogCmsModulesOutcome>> AutosaveDraftAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "contentId", "revisionId", "body");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ContainsUnsafeHtml(ReadString(request, "body"))) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "XSS_REJECTED", ["ruleId"] = "CF-577" }, false));
        var draftId = ScopedId($"draft:{Hash(ReadString(request, "contentId") + ":" + ReadString(request, "revisionId"))}");
        var existing = await GetTenantDocumentAsync(BlogCmsModulesConstants.DraftCollection, draftId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return Success(Outcome(200, existing, false));
        var body = new Dictionary<string, object?> { ["draftId"] = draftId, ["contentId"] = ReadString(request, "contentId"), ["revisionId"] = ReadString(request, "revisionId"), ["body"] = ReadString(request, "body"), ["sessionKey"] = $"blog:session:{CurrentTenant.TenantId}:{ReadString(request, "revisionId")}:{Hash(ReadString(request, "contentId"))}", ["tenantId"] = CurrentTenant.TenantId, ["version"] = 1 };
        var saved = await _documents.UpsertAsync(BlogCmsModulesConstants.DraftCollection, draftId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<BlogCmsModulesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("blog.draft.revision.stored", draftId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("draft", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<BlogCmsModulesOutcome>> PublishContentAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "contentId", "approverPool");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (string.Equals(ReadString(request, "approverPool"), ReadString(request, "moderationPool"), StringComparison.OrdinalIgnoreCase)) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "REVIEWER_POOL_DEADLOCK", ["ruleId"] = "CF-592" }, false));
        var content = await GetTenantDocumentAsync(BlogCmsModulesConstants.ContentCollection, ReadString(request, "contentId"), cancellationToken).ConfigureAwait(false);
        if (content is null) return Success(Outcome(404, new Dictionary<string, object?> { ["code"] = "CONTENT_NOT_FOUND" }, false));
        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(content, "version"), expectedVersion, StringComparison.Ordinal)) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "PUBLISH_CONFLICT", ["currentVersion"] = ReadInt(content, "version", 1), ["ruleId"] = "CF-582" }, false));
        var updated = new Dictionary<string, object?>(content) { ["status"] = "published", ["searchIndexed"] = true, ["version"] = ReadInt(content, "version", 1) + 1, ["publishedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture), ["canonicalCacheInvalidated"] = ReadBool(request, "permalinkChanged") };
        if (ReadBool(request, "permalinkChanged")) updated["canonicalInvalidationEvent"] = "CANONICAL_INVALIDATE_ALL";
        var saved = await _documents.UpsertAsync(BlogCmsModulesConstants.ContentCollection, ReadString(request, "contentId"), updated, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<BlogCmsModulesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _documents.UpsertAsync(BlogCmsModulesConstants.PublishCollection, ScopedId($"publish:{Hash(ReadString(request, "contentId"))}"), new Dictionary<string, object?> { ["contentId"] = ReadString(request, "contentId"), ["status"] = "approved", ["tenantId"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("blog.content.published", ReadString(request, "contentId"), updated), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("publish", idempotencyKey!), updated, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, updated, true, true));
    }

    public async Task<DataProcessResult<BlogCmsModulesOutcome>> SubmitCommentAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "contentId", "commentHtml", "rateLimitKey");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadInt(request, "threadDepth", 0) > ReadInt(request, "maxDepth", 3)) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "THREAD_DEPTH_LIMIT", ["ruleId"] = "CF-596" }, false));
        if (ReadBool(request, "aiSpamCheckRequested") && !ReadBool(request, "aiBudgetPrechecked")) return Success(Outcome(402, new Dictionary<string, object?> { ["code"] = "AI_BUDGET_PRECHECK_REQUIRED", ["ruleId"] = "CF-570" }, false));
        if (ContainsUnsafeHtml(ReadString(request, "commentHtml"))) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "XSS_REJECTED", ["ruleId"] = "CF-577" }, false));
        var commentId = ScopedId($"comment:{Hash(ReadString(request, "contentId") + ":" + idempotencyKey)}");
        var body = new Dictionary<string, object?> { ["commentId"] = commentId, ["contentId"] = ReadString(request, "contentId"), ["commentHtml"] = ReadString(request, "commentHtml"), ["status"] = ReadBool(request, "requiresHumanReview") ? "human_review" : "approved", ["rateLimitMode"] = ReadString(request, "rateLimitKey").StartsWith("user:", StringComparison.Ordinal) ? "user" : "ip", ["tenantId"] = CurrentTenant.TenantId };
        var saved = await _documents.UpsertAsync(BlogCmsModulesConstants.CommentCollection, commentId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<BlogCmsModulesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("blog.comment.submitted", commentId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("comment", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<BlogCmsModulesOutcome>> RunStressTestsAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var submitted = ReadStressResults(request);
        var ids = submitted.Select(test => test.StressTestId).ToArray();
        var missingStressTests = BlogCmsModulesConstants.RequiredStressTests.Where(test => !ids.Contains(test, StringComparer.Ordinal)).ToArray();
        var failing = submitted.Where(test => !string.Equals(test.Status, "PASS", StringComparison.OrdinalIgnoreCase)).Select(test => test.StressTestId).ToArray();
        if (missingStressTests.Length > 0 || failing.Length > 0) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "STRESS_MATRIX_FAILED", ["missing"] = missingStressTests, ["failing"] = failing }, false));
        var runId = ScopedId($"stress:{Hash(idempotencyKey!)}");
        var body = new Dictionary<string, object?> { ["stressRunId"] = runId, ["status"] = "PASS", ["stressTestCount"] = submitted.Count, ["coverage"] = "ST-345-ST-368", ["tenantId"] = CurrentTenant.TenantId };
        var saved = await _documents.UpsertAsync(BlogCmsModulesConstants.StressCollection, runId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<BlogCmsModulesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("blog.cms.stress.completed", runId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("stress", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<BlogCmsModulesOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = BlogCmsModulesConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        var firstStepIsScope = steps.Count > 0 && string.Equals(steps[0].TaskTypeId, "T440", StringComparison.Ordinal);
        if (missing.Length > 0 || duplicate || !firstStepIsScope) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate, ["t440First"] = firstStepIsScope, ["ruleId"] = "CF-590" }, false));
        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["singleResponsibility"] = true, ["t440First"] = true, ["requiredTasks"] = BlogCmsModulesConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<BlogCmsModulesDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId };
        var content = await _documents.SearchAsync(BlogCmsModulesConstants.ContentCollection, filter, cancellationToken).ConfigureAwait(false);
        var drafts = await _documents.SearchAsync(BlogCmsModulesConstants.DraftCollection, filter, cancellationToken).ConfigureAwait(false);
        var comments = await _documents.SearchAsync(BlogCmsModulesConstants.CommentCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new BlogCmsModulesDashboard((content.Data ?? []).Select(record => record.Body).ToArray(), (drafts.Data ?? []).Select(record => record.Body).ToArray(), (comments.Data ?? []).Select(record => record.Body).ToArray(), new Dictionary<string, object?> { ["ruleCount"] = BlogCmsModulesConstants.RequiredBfaRules.Length, ["stressTestCount"] = BlogCmsModulesConstants.RequiredStressTests.Length, ["publishedCount"] = content.Data?.Count(record => string.Equals(ReadString(record.Body, "status"), "published", StringComparison.Ordinal)) ?? 0, ["commentCount"] = comments.Data?.Count ?? 0 }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantId"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<BlogCmsModulesOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(BlogCmsModulesConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantId"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{BlogCmsModulesConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static BlogCmsModulesOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static BlogCmsModulesOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{BlogCmsModulesConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private BlogCmsModulesOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED" }, false) : null;
    private BlogCmsModulesOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;
    private static bool ContainsUnsafeHtml(string value) => value.Contains("<script", StringComparison.OrdinalIgnoreCase) || value.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    private static string NormalizeSlug(string slug) => slug.StartsWith("/", StringComparison.Ordinal) ? slug : $"/blog/{slug.Trim('/')}";
    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<BlogCmsPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<BlogCmsPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<BlogCmsRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<BlogCmsRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<BlogCmsStressResult> ReadStressResults(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("stressResults", out var value) && value is not null ? JsonSerializer.Deserialize<BlogCmsStressResult[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
