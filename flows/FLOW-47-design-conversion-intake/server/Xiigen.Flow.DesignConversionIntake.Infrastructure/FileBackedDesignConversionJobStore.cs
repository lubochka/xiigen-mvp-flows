using System.Text.Json;
using Xiigen.Flow.DesignConversionIntake.Application;

namespace Xiigen.Flow.DesignConversionIntake.Infrastructure;

public sealed class FileBackedDesignConversionJobStore : IDesignConversionJobStore
{
    private const string DefaultFileName = "design-conversion-jobs.json";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public FileBackedDesignConversionJobStore(DesignConversionJobStoreOptions? options = null)
    {
        options ??= new DesignConversionJobStoreOptions();
        var directoryPath = ResolveDirectoryPath(options.DirectoryPath);
        var fileName = string.IsNullOrWhiteSpace(options.FileName) ? DefaultFileName : options.FileName.Trim();
        _filePath = Path.Combine(directoryPath, fileName);
    }

    public async Task<DesignConversionJobRecord?> GetAsync(string tenantId, string jobId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
            return state.Jobs
                .FirstOrDefault(job => string.Equals(job.TenantId, tenantId, StringComparison.Ordinal) &&
                                       string.Equals(job.JobId, jobId, StringComparison.Ordinal))
                ?.ToRecord();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DesignConversionJobRecord?> FindByIdempotencyKeyAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
            return state.Jobs
                .FirstOrDefault(job => string.Equals(job.TenantId, tenantId, StringComparison.Ordinal) &&
                                       string.Equals(job.IdempotencyKey, idempotencyKey, StringComparison.Ordinal))
                ?.ToRecord();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertAsync(DesignConversionJobRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
            state.Jobs.RemoveAll(job =>
                string.Equals(job.TenantId, record.TenantId, StringComparison.Ordinal) &&
                (string.Equals(job.JobId, record.JobId, StringComparison.Ordinal) ||
                 (!string.IsNullOrWhiteSpace(record.IdempotencyKey) &&
                  string.Equals(job.IdempotencyKey, record.IdempotencyKey, StringComparison.Ordinal))));
            state.Jobs.Add(PersistedDesignConversionJobRecord.FromRecord(record));
            await WriteStateAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<DesignConversionJobStoreState> ReadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new DesignConversionJobStoreState();
        }

        await using var stream = File.OpenRead(_filePath);
        var state = await JsonSerializer.DeserializeAsync<DesignConversionJobStoreState>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
        return state ?? new DesignConversionJobStoreState();
    }

    private async Task WriteStateAsync(DesignConversionJobStoreState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, _jsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }

    private static string ResolveDirectoryPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "data", "flow-47-design-conversion-intake");
        }

        var trimmed = configuredPath.Trim();
        return Path.IsPathRooted(trimmed) ? trimmed : Path.Combine(AppContext.BaseDirectory, trimmed);
    }

    private sealed class DesignConversionJobStoreState
    {
        public List<PersistedDesignConversionJobRecord> Jobs { get; set; } = [];
    }

    private sealed record PersistedDesignConversionJobRecord(
        string JobId,
        string TenantId,
        string Status,
        string? IdempotencyKey,
        string SourceType,
        string SourceUri,
        string[] OutputTargets,
        string? CoreProviderJobId,
        DesignConversionEvaluationResultResponse? Evaluation,
        DesignConversionErrorResponse? Error,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        public static PersistedDesignConversionJobRecord FromRecord(DesignConversionJobRecord record) =>
            new(
                record.JobId,
                record.TenantId,
                record.Status,
                record.IdempotencyKey,
                record.SourceType,
                record.SourceUri,
                record.OutputTargets.ToArray(),
                record.CoreProviderJobId,
                record.Evaluation,
                record.Error,
                record.CreatedAt,
                record.UpdatedAt);

        public DesignConversionJobRecord ToRecord() =>
            new(
                JobId,
                TenantId,
                Status,
                IdempotencyKey,
                SourceType,
                SourceUri,
                OutputTargets,
                CoreProviderJobId,
                Evaluation,
                Error,
                CreatedAt,
                UpdatedAt);
    }
}
