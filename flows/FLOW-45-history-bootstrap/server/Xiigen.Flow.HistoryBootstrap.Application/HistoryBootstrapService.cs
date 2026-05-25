using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.HistoryBootstrap.Application;

public sealed class HistoryBootstrapService : MicroserviceBase<HistoryBootstrapService>
{
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public HistoryBootstrapService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<HistoryBootstrapService> logger)
        : base(new ServiceDescriptor("history-bootstrap-service", "History Bootstrap", HistoryBootstrapConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public DataProcessResult<bool> ValidateSeedSource(string source)
    {
        var allowed = HistoryBootstrapConstants.AllowedSeedSources.Contains(source, StringComparer.Ordinal);
        return Success(allowed);
    }

    public DataProcessResult<IReadOnlyList<HistoryPattern>> ParseArchitectureSeed(IReadOnlyDictionary<string, object?> request)
    {
        var source = ReadString(request, "source", "history-seeds/arch-philosophy.json");
        if (!ValidateSeedSource(source).Data) return Failure<IReadOnlyList<HistoryPattern>>("SEED_SOURCE_NOT_ALLOWED", "Seed source is outside the approved architecture-history corpus.");

        var records = ReadRecords(request, "patterns");
        if (records.Count == 0) records = DefaultPatterns();

        var parsed = records.Select((record, index) =>
        {
            var type = NormalizePatternType(ReadString(record, "patternType", index % 2 == 0 ? "ARCH_PATTERN" : "DESIGN_REASONING"));
            var id = ReadString(record, "patternId", $"HB-{index + 1:000}");
            return new HistoryPattern(
                id,
                type,
                "GLOBAL",
                "FLOW_SCOPED",
                ReadString(record, "title", $"History pattern {index + 1}"),
                source,
                new Dictionary<string, object?>
                {
                    ["flowId"] = ReadString(record, "flowId", HistoryBootstrapConstants.FlowId),
                    ["taskTypeId"] = ReadString(record, "taskTypeId", HistoryBootstrapConstants.RequiredTaskTypes[Math.Min(index, HistoryBootstrapConstants.RequiredTaskTypes.Length - 1)]),
                    ["problem"] = ReadString(record, "problem", "Historical architecture choice"),
                    ["solution"] = ReadString(record, "solution", "Bootstrap from approved source document"),
                    ["tradeoff"] = ReadString(record, "tradeoff", "Global knowledge only, tenant data excluded")
                });
        }).ToArray();

        return Success<IReadOnlyList<HistoryPattern>>(parsed);
    }

    public async Task<DataProcessResult<HistoryBootstrapOutcome>> SeedDocumentsAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var parsed = ParseArchitectureSeed(request);
        if (parsed.IsFailure) return Success(Outcome(400, ErrorBody("SEED_PARSE_FAILED", "Seed parse failed."), false));

        var failed = 0;
        var planning = 0;
        var rag = 0;
        foreach (var pattern in parsed.Data ?? [])
        {
            var target = TargetCollection(pattern.PatternType);
            if (target == HistoryBootstrapConstants.PlanningDecisionsCollection) planning++;
            if (target == HistoryBootstrapConstants.RagPatternsCollection || target == HistoryBootstrapConstants.ArchitecturePhilosophyCollection) rag++;
            var body = PatternDocument(pattern);
            var result = await _documents.UpsertAsync(target, pattern.PatternId, body, null, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure) failed++;
        }

        var data = new Dictionary<string, object?>
        {
            ["patternsSeeded"] = (parsed.Data?.Count ?? 0) - failed,
            ["planningDecisionsSeeded"] = planning,
            ["ragPatternsSeeded"] = rag,
            ["patternsFailed"] = failed,
            ["source"] = ReadString(request, "source", "history-seeds/arch-philosophy.json")
        };
        return Success(Outcome(201, data, true));
    }

    public async Task<DataProcessResult<ArchPhilosophyRetrievalResult>> RetrieveArchitecturePatternsAsync(string? patternType = null, CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["knowledgeScope"] = "GLOBAL" };
        if (!string.IsNullOrWhiteSpace(patternType)) filter["patternType"] = NormalizePatternType(patternType);
        var arch = await _documents.SearchAsync(HistoryBootstrapConstants.ArchitecturePhilosophyCollection, filter, cancellationToken).ConfigureAwait(false);
        var rag = await _documents.SearchAsync(HistoryBootstrapConstants.RagPatternsCollection, filter, cancellationToken).ConfigureAwait(false);
        if (arch.IsFailure || rag.IsFailure) return Failure<ArchPhilosophyRetrievalResult>("ARCH_PHILOSOPHY_RETRIEVAL_FAILED", "Architecture history retrieval failed.");
        var patterns = (arch.Data ?? []).Concat(rag.Data ?? []).Select(record => record.Body).ToArray();
        return Success(new ArchPhilosophyRetrievalResult(patterns, patterns.Length));
    }

    public async Task<DataProcessResult<SummarizeResult>> SummarizePatternsAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var runId = ReadString(request, "summarizationRunId");
        if (string.IsNullOrWhiteSpace(runId)) return Failure<SummarizeResult>("MISSING_SUMMARIZATION_RUN_ID", "summarizationRunId is required.");

        var existing = await _documents.SearchAsync(HistoryBootstrapConstants.BootstrapIdempotencyCollection, new Dictionary<string, object?> { ["summarizationRunId"] = runId }, cancellationToken).ConfigureAwait(false);
        if ((existing.Data?.Count ?? 0) > 0)
        {
            var cached = JsonSerializer.Deserialize<SummarizeResult>(JsonSerializer.Serialize(existing.Data![0].Body["summarizeResult"]))!;
            return Success(cached);
        }

        var patterns = ReadRecords(request, "patterns");
        var groups = patterns.GroupBy(record => ReadString(record, "patternType", "UNKNOWN"), StringComparer.Ordinal).ToArray();
        var summaries = groups.Select(group => new PhilosophySummary(group.Key, group.Count(), group.Select(item => ReadString(item, "patternId")).Where(id => !string.IsNullOrWhiteSpace(id)).ToArray())).ToArray();
        foreach (var summary in summaries)
        {
            await _documents.UpsertAsync(HistoryBootstrapConstants.PhilosophySummariesCollection, $"{runId}-{summary.PatternType}", new Dictionary<string, object?>
            {
                ["summarizationRunId"] = runId,
                ["tenantScope"] = CurrentTenant.TenantId,
                ["patternType"] = summary.PatternType,
                ["patternCount"] = summary.PatternCount,
                ["patternIds"] = summary.PatternIds,
                ["knowledgeScope"] = "GLOBAL",
                ["summarizedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            }, null, cancellationToken).ConfigureAwait(false);
        }

        var result = new SummarizeResult(runId, summaries.Length, patterns.Count, summaries);
        await _documents.UpsertAsync(HistoryBootstrapConstants.BootstrapIdempotencyCollection, $"summary:{runId}", new Dictionary<string, object?> { ["summarizationRunId"] = runId, ["summarizeResult"] = result, ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.history-bootstrap.summary-ready", runId, new Dictionary<string, object?> { ["summarizationRunId"] = runId, ["groupsCreated"] = summaries.Length }), cancellationToken).ConfigureAwait(false);
        return Success(result);
    }

    public async Task<DataProcessResult<HistoryBootstrapOutcome>> RunBootstrapAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var runId = ReadString(request, "bootstrapRunId");
        if (string.IsNullOrWhiteSpace(runId)) return Success(Outcome(400, ErrorBody("MISSING_BOOTSTRAP_RUN_ID", "bootstrapRunId is required."), false));

        var existing = await _documents.SearchAsync(HistoryBootstrapConstants.BootstrapIdempotencyCollection, new Dictionary<string, object?> { ["bootstrapRunId"] = runId }, cancellationToken).ConfigureAwait(false);
        if ((existing.Data?.Count ?? 0) > 0) return Success(Outcome(200, (IReadOnlyDictionary<string, object?>)existing.Data![0].Body["orchestrationResult"]!, true));

        var seed = await SeedDocumentsAsync(request, cancellationToken).ConfigureAwait(false);
        var retrieve = await RetrieveArchitecturePatternsAsync(null, cancellationToken).ConfigureAwait(false);
        var summarize = retrieve.IsSuccess
            ? await SummarizePatternsAsync(new Dictionary<string, object?> { ["summarizationRunId"] = $"{runId}-summary", ["patterns"] = retrieve.Data!.Patterns }, cancellationToken).ConfigureAwait(false)
            : Failure<SummarizeResult>("SUMMARY_SKIPPED", "Retrieval failed.");

        var seedCount = ToInt(seed.Data?.Body, "patternsSeeded");
        var retrievedCount = retrieve.Data?.TotalReturned ?? 0;
        var summarizedGroups = summarize.Data?.GroupsCreated ?? 0;
        var status = seedCount > 0 && retrievedCount > 0 && summarizedGroups > 0 ? "COMPLETE" : seedCount > 0 ? "PARTIAL" : "FAILED";
        var orchestration = new Dictionary<string, object?>
        {
            ["bootstrapRunId"] = runId,
            ["seedCount"] = seedCount,
            ["retrievedCount"] = retrievedCount,
            ["summarizedGroups"] = summarizedGroups,
            ["completionStatus"] = status,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["completedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _documents.UpsertAsync(HistoryBootstrapConstants.BootstrapCompletionsCollection, ScopedId($"completion:{runId}"), orchestration, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(HistoryBootstrapConstants.BootstrapIdempotencyCollection, $"run:{runId}", new Dictionary<string, object?> { ["bootstrapRunId"] = runId, ["tenantScope"] = CurrentTenant.TenantId, ["orchestrationResult"] = orchestration }, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.history-bootstrap.completed", runId, orchestration), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(status == "COMPLETE" ? 201 : 202, orchestration, true, true));
    }

    public DataProcessResult<HistoryBootstrapOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRecords(request, "rules");
        var ids = rules.Select(rule => ReadString(rule, "ruleId")).ToArray();
        var missing = HistoryBootstrapConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.Where(id => !string.IsNullOrWhiteSpace(id)).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(ReadString(rule, "severity")) || string.IsNullOrWhiteSpace(ReadString(rule, "factory")) || string.IsNullOrWhiteSpace(ReadString(rule, "trigger")) || string.IsNullOrWhiteSpace(ReadString(rule, "scope"))).Select(rule => ReadString(rule, "ruleId")).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "FLOW-45 history bootstrap invariants" }, false));
    }

    public DataProcessResult<HistoryBootstrapOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadRecords(request, "planSteps");
        var taskIds = steps.Select(step => ReadString(step, "taskTypeId")).ToHashSet(StringComparer.Ordinal);
        var missing = HistoryBootstrapConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => ReadString(step, "taskTypeId"), StringComparer.Ordinal).Any(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = HistoryBootstrapConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<HistoryBootstrapDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var completions = await _documents.SearchAsync(HistoryBootstrapConstants.BootstrapCompletionsCollection, filter, cancellationToken).ConfigureAwait(false);
        var summaries = await _documents.SearchAsync(HistoryBootstrapConstants.PhilosophySummariesCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new HistoryBootstrapDashboard(
            (completions.Data ?? []).Select(record => record.Body).ToArray(),
            (summaries.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["completionCount"] = completions.Data?.Count ?? 0, ["summaryCount"] = summaries.Data?.Count ?? 0, ["requiredRuleCount"] = HistoryBootstrapConstants.RequiredBfaRules.Length }));
    }

    private string TargetCollection(string patternType) => patternType switch
    {
        "DESIGN_REASONING" => HistoryBootstrapConstants.PlanningDecisionsCollection,
        "ARCH_PATTERN" or "RAG_PATTERN" or "RAG" => HistoryBootstrapConstants.RagPatternsCollection,
        _ => HistoryBootstrapConstants.ArchitecturePhilosophyCollection
    };

    private IReadOnlyDictionary<string, object?> PatternDocument(HistoryPattern pattern) => new Dictionary<string, object?>
    {
        ["patternId"] = pattern.PatternId,
        ["patternType"] = pattern.PatternType,
        ["knowledgeScope"] = "GLOBAL",
        ["connectionType"] = "FLOW_SCOPED",
        ["tenantScope"] = CurrentTenant.TenantId,
        ["title"] = pattern.Title,
        ["source"] = pattern.Source,
        ["metadata"] = pattern.Metadata,
        ["seededAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
    };

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{HistoryBootstrapConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static HistoryBootstrapOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static IReadOnlyDictionary<string, object?> ErrorBody(string code, string message) => new Dictionary<string, object?> { ["code"] = code, ["message"] = message };
    private static string NormalizePatternType(string value) => value.Equals("RAG", StringComparison.OrdinalIgnoreCase) ? "RAG_PATTERN" : value.ToUpperInvariant();
    private static int ToInt(IReadOnlyDictionary<string, object?>? body, string key) => body is not null && body.TryGetValue(key, out var value) ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : 0;

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> DefaultPatterns() =>
    [
        new Dictionary<string, object?> { ["patternId"] = "HB-001", ["patternType"] = "ARCH_PATTERN", ["title"] = "Fabric-first architecture", ["taskTypeId"] = "T601-BootstrapFromDocuments" },
        new Dictionary<string, object?> { ["patternId"] = "HB-002", ["patternType"] = "DESIGN_REASONING", ["title"] = "Portability gates are evidence-based", ["taskTypeId"] = "T602-ArchPhilosophyRetriever" },
        new Dictionary<string, object?> { ["patternId"] = "HB-003", ["patternType"] = "ARCH_PATTERN", ["title"] = "Outbox before queue", ["taskTypeId"] = "T603-PhilosophyPatternSummarizer" },
        new Dictionary<string, object?> { ["patternId"] = "HB-004", ["patternType"] = "RAG_PATTERN", ["title"] = "History RAG seed for later flows", ["taskTypeId"] = "T604-HistoryBootstrapOrchestrator" }
    ];

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadRecords(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<IReadOnlyDictionary<string, object?>[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];

    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "")
    {
        if (!source.TryGetValue(key, out var value) || value is null) return fallback;
        return value is JsonElement element ? element.GetString() ?? fallback : Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
    }
}
