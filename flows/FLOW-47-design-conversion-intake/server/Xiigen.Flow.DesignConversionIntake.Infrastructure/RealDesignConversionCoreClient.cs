using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DesignConversionDomain;
using Xiigen.Flow.DesignConversionIntake.Application;

namespace Xiigen.Flow.DesignConversionIntake.Infrastructure;

public sealed class RealDesignConversionCoreClient : IDesignConversionCoreClient
{
    private readonly DesignConversionInternalService _core;

    public RealDesignConversionCoreClient(DesignConversionInternalService core)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
    }

    public async Task<DesignConversionCoreDispatchResult> DispatchAsync(
        DesignConversionCoreDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var internalRequest = MapToInternalRequest(request);
        var result = await _core.EvaluateAsync(internalRequest, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? DesignConversionCoreDispatchResult.Completed($"core-{result.Value!.JobId}", MapEvaluationResult(result.Value))
            : DesignConversionCoreDispatchResult.Failed(result.ErrorCode, result.ErrorMessage);
    }

    public static DesignConversionInternalEvaluateRequest MapToInternalRequest(DesignConversionCoreDispatchRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var options = request.Options ?? new Dictionary<string, object?>();
        var sourceKind = MapSourceKind(request.SourceType);
        var sourceTreeEntries = BuildSourceTreeEntries(options);
        var requestId = FirstString(options, "requestId", "sourceRequestId", "idempotencyKey")
            ?? $"{request.TenantId}:{request.JobId}";
        var html = FirstString(options, "html", "sourceHtml", "markup")
            ?? FirstSourceFileContent(sourceTreeEntries, "html")
            ?? BuildFallbackHtml(request);
        var css = FirstString(options, "css", "sourceCss", "styles")
            ?? FirstSourceFileContent(sourceTreeEntries, "css")
            ?? string.Empty;
        var designTreeContent = FirstString(options, "designTreePayload", "designTree", "sourcePayload")
            ?? FirstSourceFileContent(sourceTreeEntries, "json")
            ?? SourceTreeJson(options)
            ?? request.SourceUri;
        var originalTreeContent = FirstString(options, "originalTreePayload", "originalDesignTree", "originalSourcePayload")
            ?? designTreeContent;
        var metadata = BuildMetadata(request, options);
        var resolution = BuildResolution(options);
        var screenshots = BuildScreenshotArtifacts(request, resolution, options, metadata);
        var designRenders = BuildDesignRenderArtifacts(request, resolution, options, metadata, screenshots);

        return new DesignConversionInternalEvaluateRequest(
            JobId: request.JobId,
            RequestId: requestId,
            SourceKind: sourceKind,
            Html: html,
            Css: css,
            DesignTreePayload: BuildTreePayload(request, "design-tree", designTreeContent, metadata),
            OriginalTreePayload: BuildTreePayload(request, "original-design-tree", originalTreeContent, metadata),
            Screenshots: screenshots,
            DesignRenders: designRenders,
            Budget: BuildBudget(options, metadata),
            RenderResolutions: [resolution],
            AiTeacherEvidenceRefs: BuildStringList(options, "aiTeacherEvidenceRefs"),
            DeterministicEvidenceRefs: BuildStringDictionary(options, "deterministicEvidenceRefs"),
            Provenance: metadata);
    }

    private static DesignConversionSourceKind MapSourceKind(string sourceType)
    {
        if (sourceType.Contains("figma", StringComparison.OrdinalIgnoreCase))
        {
            return DesignConversionSourceKind.Figma;
        }

        if (sourceType.Contains("canva", StringComparison.OrdinalIgnoreCase))
        {
            return DesignConversionSourceKind.Canva;
        }

        return DesignConversionSourceKind.Other;
    }

    private static RenderQaResolution BuildResolution(IReadOnlyDictionary<string, object?> options)
    {
        var width = FirstInt(options, "viewportWidth", "width") ?? 1440;
        var height = FirstInt(options, "viewportHeight", "height") ?? 900;
        var scale = FirstDouble(options, "deviceScaleFactor", "scale") ?? 1.0;
        var id = FirstString(options, "resolutionId", "viewportId") ?? $"{width}x{height}@{scale.ToString("0.###", CultureInfo.InvariantCulture)}";

        return new RenderQaResolution(id, width, height, scale);
    }

    private static DesignConversionBudgetMetadata BuildBudget(
        IReadOnlyDictionary<string, object?> options,
        IReadOnlyDictionary<string, string> metadata)
        => new(
            UserId: FirstString(options, "userId", "submittedBy") ?? "flow-47-intake",
            Tier: FirstString(options, "designTier", "tier") ?? "standard",
            MaxImprovementCandidates: FirstInt(options, "maxImprovementCandidates", "candidateBudget") ?? 1,
            MaxRenderQaRuns: FirstInt(options, "maxRenderQaRuns", "loopBudget", "renderQaBudget") ?? 1,
            BudgetUsd: FirstDecimal(options, "budgetUsd", "budget") ?? 0m,
            Metadata: metadata);

    private static DesignTreePayload BuildTreePayload(
        DesignConversionCoreDispatchRequest request,
        string role,
        string content,
        IReadOnlyDictionary<string, string> metadata)
        => new(
            PayloadId: $"{request.JobId}:{role}",
            Format: request.SourceType,
            Content: content,
            Sha256: $"sha256:{Sha256Text(content)}",
            Metadata: metadata
                .Concat(new Dictionary<string, string> { ["payload_role"] = role })
                .GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal));

    private static IReadOnlyList<DesignRenderArtifact> BuildScreenshotArtifacts(
        DesignConversionCoreDispatchRequest request,
        RenderQaResolution resolution,
        IReadOnlyDictionary<string, object?> options,
        IReadOnlyDictionary<string, string> metadata)
    {
        var specs = new List<RenderArtifactSpec>();
        foreach (var key in new[] { "screenshots", "screenshot", "designScreenshots", "referenceImages", "designReferenceImages", "referenceImage" })
        {
            if (options.TryGetValue(key, out var value))
            {
                specs.AddRange(ExtractRenderArtifactSpecs(value, key));
            }
        }

        AddScalarArtifactSpec(specs, options, "screenshotUri", "screenshotSha256", "screenshot");
        AddScalarArtifactSpec(specs, options, "referenceImageUri", "referenceImageSha256", "reference-image");

        return specs
            .Where(spec => !string.IsNullOrWhiteSpace(spec.Uri))
            .Select((spec, index) => BuildRenderArtifact(request, resolution, metadata, spec, index, "screenshot"))
            .ToArray();
    }

    private static IReadOnlyList<DesignRenderArtifact> BuildDesignRenderArtifacts(
        DesignConversionCoreDispatchRequest request,
        RenderQaResolution resolution,
        IReadOnlyDictionary<string, object?> options,
        IReadOnlyDictionary<string, string> metadata,
        IReadOnlyList<DesignRenderArtifact> screenshots)
    {
        var specs = new List<RenderArtifactSpec>();
        foreach (var key in new[] { "designRenders", "designRender", "expectedRenders", "expectedRender" })
        {
            if (options.TryGetValue(key, out var value))
            {
                specs.AddRange(ExtractRenderArtifactSpecs(value, key));
            }
        }

        AddScalarArtifactSpec(specs, options, "designRenderUri", "designRenderSha256", "expected-design-render");
        AddScalarArtifactSpec(specs, options, "expectedRenderUri", "expectedRenderSha256", "expected-design-render");

        if (specs.Count == 0 && screenshots.Count > 0)
        {
            return [];
        }

        if (specs.Count == 0)
        {
            specs.Add(new RenderArtifactSpec(null, "expected-design-render", request.SourceUri, $"sha256:{Sha256Text(request.SourceUri)}", null, null));
        }

        return specs
            .Where(spec => !string.IsNullOrWhiteSpace(spec.Uri))
            .Select((spec, index) => BuildRenderArtifact(request, resolution, metadata, spec, index, "expected-design-render"))
            .ToArray();
    }

    private static DesignRenderArtifact BuildRenderArtifact(
        DesignConversionCoreDispatchRequest request,
        RenderQaResolution resolution,
        IReadOnlyDictionary<string, string> metadata,
        RenderArtifactSpec spec,
        int index,
        string fallbackKind)
    {
        var kind = string.IsNullOrWhiteSpace(spec.Kind) ? fallbackKind : spec.Kind!;
        var uri = spec.Uri ?? request.SourceUri;
        var sha = string.IsNullOrWhiteSpace(spec.Sha256) ? $"sha256:{Sha256Text(uri)}" : spec.Sha256!;
        var width = spec.Width ?? resolution.Width;
        var height = spec.Height ?? resolution.Height;
        var artifactId = string.IsNullOrWhiteSpace(spec.ArtifactId)
            ? $"{request.JobId}:{fallbackKind}:{index + 1}:{resolution.ResolutionId}"
            : spec.ArtifactId!;

        return new DesignRenderArtifact(
            ArtifactId: artifactId,
            Kind: kind,
            Width: width,
            Height: height,
            Uri: uri,
            Sha256: sha,
            Metadata: metadata
                .Concat(new Dictionary<string, string>
                {
                    ["artifact_role"] = fallbackKind,
                    ["artifact_kind"] = kind,
                    ["artifact_index"] = (index + 1).ToString(CultureInfo.InvariantCulture)
                })
                .GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal));
    }

    private static DesignRenderArtifact BuildExpectedRenderArtifact(
        DesignConversionCoreDispatchRequest request,
        RenderQaResolution resolution,
        IReadOnlyDictionary<string, object?> options,
        IReadOnlyDictionary<string, string> metadata)
    {
        var uri = FirstString(options, "designRenderUri", "screenshotUri", "expectedRenderUri") ?? request.SourceUri;
        var sha = FirstString(options, "designRenderSha256", "screenshotSha256", "expectedRenderSha256") ?? $"sha256:{Sha256Text(uri)}";

        return new DesignRenderArtifact(
            ArtifactId: $"{request.JobId}:expected-render:{resolution.ResolutionId}",
            Kind: "expected-design-render",
            Width: resolution.Width,
            Height: resolution.Height,
            Uri: uri,
            Sha256: sha,
            Metadata: metadata
                .Concat(new Dictionary<string, string> { ["artifact_role"] = "expected_design_render" })
                .GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal));
    }

    private static DesignConversionEvaluationResultResponse MapEvaluationResult(DesignConversionInternalEvaluationResult result)
        => new(
            VerdictId: result.Verdict.VerdictId,
            Score: result.Verdict.Score,
            Passed: result.Verdict.Passed,
            Defects: result.Verdict.Defects
                .Select(defect => new DesignConversionDefectResponse(
                    defect.DefectId,
                    defect.Category,
                    defect.Severity,
                    defect.Description,
                    defect.X,
                    defect.Y,
                    defect.Width,
                    defect.Height,
                    defect.EvidenceImageIds.ToArray()))
                .ToArray(),
            ComparisonArtifacts: result.ComparisonArtifacts
                .Select(artifact => new DesignConversionComparisonArtifactResponse(
                    artifact.ArtifactId,
                    artifact.Expected.Uri,
                    artifact.Actual.Uri,
                    artifact.DiffImageUri,
                    artifact.Verdict.Score,
                    artifact.Metrics,
                    artifact.EvidenceRefs.ToArray()))
                .ToArray(),
            EvidenceRefs: result.EvidenceRefs.ToArray(),
            Warnings: result.Warnings.ToArray(),
            CurrentHtml: result.ImprovedHtml,
            CurrentCss: result.ImprovedCss);

    private static IReadOnlyList<SourceTreeEntry> BuildSourceTreeEntries(IReadOnlyDictionary<string, object?> options)
    {
        var entries = new List<SourceTreeEntry>();
        foreach (var key in new[] { "sourceTree", "sourceFiles", "files", "entries" })
        {
            if (options.TryGetValue(key, out var value))
            {
                entries.AddRange(ExtractSourceTreeEntries(value, key));
            }
        }

        return entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Path) && entry.Content is not null)
            .GroupBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();
    }

    private static string? FirstSourceFileContent(IReadOnlyList<SourceTreeEntry> entries, string fileKind)
    {
        if (entries.Count == 0)
        {
            return null;
        }

        var candidates = fileKind switch
        {
            "html" => entries.Where(entry => IsHtmlPath(entry.Path)),
            "css" => entries.Where(entry => IsCssPath(entry.Path)),
            "json" => entries.Where(entry => IsDesignTreePath(entry.Path)),
            _ => []
        };

        return candidates
            .OrderBy(entry => SourceTreeRank(entry.Path, fileKind))
            .Select(entry => entry.Content)
            .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content));
    }

    private static string? SourceTreeJson(IReadOnlyDictionary<string, object?> options)
    {
        foreach (var key in new[] { "sourceTree", "sourceFiles", "files", "entries" })
        {
            if (options.TryGetValue(key, out var value) && value is not null)
            {
                return value is JsonElement element
                    ? element.GetRawText()
                    : JsonSerializer.Serialize(value);
            }
        }

        return null;
    }

    private static IReadOnlyList<SourceTreeEntry> ExtractSourceTreeEntries(object? value, string? defaultPath = null)
    {
        if (value is null)
        {
            return [];
        }

        if (value is JsonElement element)
        {
            return ExtractSourceTreeEntries(element, defaultPath);
        }

        if (value is IReadOnlyDictionary<string, object?> dictionary)
        {
            var fileEntry = SourceTreeEntryFromDictionary(dictionary, defaultPath);
            if (fileEntry is not null)
            {
                return [fileEntry];
            }

            var entries = new List<SourceTreeEntry>();
            foreach (var pair in dictionary)
            {
                if (IsSourceTreeContainerKey(pair.Key))
                {
                    entries.AddRange(ExtractSourceTreeEntries(pair.Value, pair.Key));
                    continue;
                }

                if (IsLikelySourceFilePath(pair.Key))
                {
                    entries.Add(new SourceTreeEntry(pair.Key, ConvertToString(pair.Value)));
                    continue;
                }

                entries.AddRange(ExtractSourceTreeEntries(pair.Value, pair.Key));
            }

            return entries;
        }

        if (value is IEnumerable<object?> objects && value is not string)
        {
            return objects.SelectMany(item => ExtractSourceTreeEntries(item, defaultPath)).ToArray();
        }

        return IsLikelySourceFilePath(defaultPath) ? [new SourceTreeEntry(defaultPath!, ConvertToString(value))] : [];
    }

    private static IReadOnlyList<SourceTreeEntry> ExtractSourceTreeEntries(JsonElement element, string? defaultPath)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var fileEntry = SourceTreeEntryFromJsonObject(element, defaultPath);
            if (fileEntry is not null)
            {
                return [fileEntry];
            }

            var entries = new List<SourceTreeEntry>();
            foreach (var property in element.EnumerateObject())
            {
                if (IsSourceTreeContainerKey(property.Name))
                {
                    entries.AddRange(ExtractSourceTreeEntries(property.Value, property.Name));
                    continue;
                }

                if (IsLikelySourceFilePath(property.Name))
                {
                    entries.Add(new SourceTreeEntry(property.Name, JsonElementToString(property.Value)));
                    continue;
                }

                entries.AddRange(ExtractSourceTreeEntries(property.Value, property.Name));
            }

            return entries;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .SelectMany(item => ExtractSourceTreeEntries(item, defaultPath))
                .ToArray();
        }

        return IsLikelySourceFilePath(defaultPath) ? [new SourceTreeEntry(defaultPath!, JsonElementToString(element))] : [];
    }

    private static SourceTreeEntry? SourceTreeEntryFromJsonObject(JsonElement element, string? defaultPath)
    {
        var path = FirstJsonPropertyString(element, "path", "name", "fileName", "filename", "uri", "key") ?? defaultPath;
        var content = FirstJsonPropertyString(element, "content", "text", "source", "body", "value", "raw");
        return IsLikelySourceFilePath(path) && content is not null ? new SourceTreeEntry(path!, content) : null;
    }

    private static SourceTreeEntry? SourceTreeEntryFromDictionary(IReadOnlyDictionary<string, object?> dictionary, string? defaultPath)
    {
        var path = FirstDictionaryString(dictionary, "path", "name", "fileName", "filename", "uri", "key") ?? defaultPath;
        var content = FirstDictionaryString(dictionary, "content", "text", "source", "body", "value", "raw");
        return IsLikelySourceFilePath(path) && content is not null ? new SourceTreeEntry(path!, content) : null;
    }

    private static string? FirstJsonPropertyString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property))
            {
                var value = JsonElementToString(property);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string? FirstDictionaryString(IReadOnlyDictionary<string, object?> dictionary, params string[] names)
    {
        foreach (var name in names)
        {
            if (dictionary.TryGetValue(name, out var value))
            {
                var text = ConvertToString(value);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static bool IsSourceTreeContainerKey(string key)
        => key.Equals("files", StringComparison.OrdinalIgnoreCase) ||
           key.Equals("entries", StringComparison.OrdinalIgnoreCase) ||
           key.Equals("children", StringComparison.OrdinalIgnoreCase) ||
           key.Equals("sourceTree", StringComparison.OrdinalIgnoreCase) ||
           key.Equals("sourceFiles", StringComparison.OrdinalIgnoreCase);

    private static bool IsLikelySourceFilePath(string? path)
        => !string.IsNullOrWhiteSpace(path) &&
           (IsHtmlPath(path) || IsCssPath(path) || IsDesignTreePath(path));

    private static bool IsHtmlPath(string path)
        => path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
           path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("html", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("generatedHtml", StringComparison.OrdinalIgnoreCase);

    private static bool IsCssPath(string path)
        => path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("css", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("styles", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("generatedCss", StringComparison.OrdinalIgnoreCase);

    private static bool IsDesignTreePath(string path)
        => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
           path.Contains("canva-selection", StringComparison.OrdinalIgnoreCase) ||
           path.Contains("design-tree", StringComparison.OrdinalIgnoreCase) ||
           path.Contains("source-tree", StringComparison.OrdinalIgnoreCase);

    private static int SourceTreeRank(string path, string fileKind)
    {
        if (fileKind == "html" && path.Contains("generated", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (fileKind == "css" && path.Contains("generated", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (fileKind == "json" && path.Contains("canva-selection", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return path.Contains("generated", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
    }

    private static IReadOnlyList<RenderArtifactSpec> ExtractRenderArtifactSpecs(object? value, string? defaultKind = null)
    {
        if (value is null)
        {
            return [];
        }

        if (value is JsonElement element)
        {
            return ExtractRenderArtifactSpecs(element, defaultKind);
        }

        if (value is IReadOnlyDictionary<string, object?> dictionary)
        {
            var uri = FirstDictionaryString(dictionary, "uri", "url", "path", "src", "dataUri");
            if (!string.IsNullOrWhiteSpace(uri))
            {
                return
                [
                    new RenderArtifactSpec(
                        FirstDictionaryString(dictionary, "artifactId", "id", "imageId"),
                        FirstDictionaryString(dictionary, "kind", "type", "role") ?? defaultKind,
                        uri,
                        FirstDictionaryString(dictionary, "sha256", "sha", "hash"),
                        FirstDictionaryInt(dictionary, "width"),
                        FirstDictionaryInt(dictionary, "height"))
                ];
            }

            return dictionary.Values.SelectMany(item => ExtractRenderArtifactSpecs(item, defaultKind)).ToArray();
        }

        if (value is IEnumerable<object?> objects && value is not string)
        {
            return objects.SelectMany(item => ExtractRenderArtifactSpecs(item, defaultKind)).ToArray();
        }

        var text = ConvertToString(value);
        return string.IsNullOrWhiteSpace(text) ? [] : [new RenderArtifactSpec(null, defaultKind, text, null, null, null)];
    }

    private static IReadOnlyList<RenderArtifactSpec> ExtractRenderArtifactSpecs(JsonElement element, string? defaultKind)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var uri = FirstJsonPropertyString(element, "uri", "url", "path", "src", "dataUri");
            if (!string.IsNullOrWhiteSpace(uri))
            {
                return
                [
                    new RenderArtifactSpec(
                        FirstJsonPropertyString(element, "artifactId", "id", "imageId"),
                        FirstJsonPropertyString(element, "kind", "type", "role") ?? defaultKind,
                        uri,
                        FirstJsonPropertyString(element, "sha256", "sha", "hash"),
                        FirstJsonPropertyInt(element, "width"),
                        FirstJsonPropertyInt(element, "height"))
                ];
            }

            return element.EnumerateObject()
                .SelectMany(property => ExtractRenderArtifactSpecs(property.Value, property.Name))
                .ToArray();
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .SelectMany(item => ExtractRenderArtifactSpecs(item, defaultKind))
                .ToArray();
        }

        var text = JsonElementToString(element);
        return string.IsNullOrWhiteSpace(text) ? [] : [new RenderArtifactSpec(null, defaultKind, text, null, null, null)];
    }

    private static void AddScalarArtifactSpec(List<RenderArtifactSpec> specs, IReadOnlyDictionary<string, object?> options, string uriKey, string shaKey, string kind)
    {
        var uri = FirstString(options, uriKey);
        if (!string.IsNullOrWhiteSpace(uri))
        {
            specs.Add(new RenderArtifactSpec(null, kind, uri, FirstString(options, shaKey), null, null));
        }
    }

    private static int? FirstDictionaryInt(IReadOnlyDictionary<string, object?> dictionary, params string[] names)
        => names.Select(name => dictionary.TryGetValue(name, out var value) ? ConvertToInt(value) : null)
            .FirstOrDefault(value => value.HasValue);

    private static int? FirstJsonPropertyInt(JsonElement element, params string[] names)
        => names.Select(name => element.TryGetProperty(name, out var value) ? ConvertToInt(value) : null)
            .FirstOrDefault(value => value.HasValue);

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        DesignConversionCoreDispatchRequest request,
        IReadOnlyDictionary<string, object?> options)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["flow_id"] = "FLOW-47",
            ["job_id"] = request.JobId,
            ["tenant_id"] = request.TenantId,
            ["source_type"] = request.SourceType,
            ["source_uri"] = request.SourceUri,
            ["output_targets"] = string.Join(",", request.OutputTargets)
        };

        foreach (var pair in BuildStringDictionary(options, "metadata"))
        {
            metadata[pair.Key] = pair.Value;
        }

        return metadata;
    }

    private static IReadOnlyList<string> BuildStringList(IReadOnlyDictionary<string, object?> options, string key)
    {
        if (!options.TryGetValue(key, out var value))
        {
            return [];
        }

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item!)
                    .ToArray();
            }

            var single = JsonElementToString(element);
            return string.IsNullOrWhiteSpace(single) ? [] : [single];
        }

        if (value is IEnumerable<string> strings)
        {
            return strings.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        }

        return value is null ? [] : [Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty];
    }

    private static IReadOnlyDictionary<string, string> BuildStringDictionary(IReadOnlyDictionary<string, object?> options, string key)
    {
        if (!options.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, string>();
        }

        if (value is IReadOnlyDictionary<string, string> typed)
        {
            return typed;
        }

        if (value is IReadOnlyDictionary<string, object?> objects)
        {
            return objects.ToDictionary(pair => pair.Key, pair => ConvertToString(pair.Value), StringComparer.Ordinal);
        }

        if (value is JsonElement { ValueKind: JsonValueKind.Object } element)
        {
            return element.EnumerateObject()
                .ToDictionary(property => property.Name, property => JsonElementToString(property.Value), StringComparer.Ordinal);
        }

        return new Dictionary<string, string>();
    }

    private static string? FirstString(IReadOnlyDictionary<string, object?> options, params string[] keys)
        => keys.Select(key => options.TryGetValue(key, out var value) ? ConvertToString(value) : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static int? FirstInt(IReadOnlyDictionary<string, object?> options, params string[] keys)
        => keys.Select(key => options.TryGetValue(key, out var value) ? ConvertToInt(value) : null)
            .FirstOrDefault(value => value.HasValue);

    private static double? FirstDouble(IReadOnlyDictionary<string, object?> options, params string[] keys)
        => keys.Select(key => options.TryGetValue(key, out var value) ? ConvertToDouble(value) : null)
            .FirstOrDefault(value => value.HasValue);

    private static decimal? FirstDecimal(IReadOnlyDictionary<string, object?> options, params string[] keys)
        => keys.Select(key => options.TryGetValue(key, out var value) ? ConvertToDecimal(value) : null)
            .FirstOrDefault(value => value.HasValue);

    private static string ConvertToString(object? value)
        => value switch
        {
            null => string.Empty,
            JsonElement element => JsonElementToString(element),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

    private static string JsonElementToString(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };

    private static int? ConvertToInt(object? value)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var jsonNumber))
            {
                return jsonNumber;
            }

            value = JsonElementToString(element);
        }

        return value switch
        {
            int number => number,
            long number => checked((int)number),
            double number => Convert.ToInt32(number, CultureInfo.InvariantCulture),
            decimal number => Convert.ToInt32(number, CultureInfo.InvariantCulture),
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }

    private static double? ConvertToDouble(object? value)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var jsonNumber))
            {
                return jsonNumber;
            }

            value = JsonElementToString(element);
        }

        return value switch
        {
            double number => number,
            float number => number,
            int number => number,
            long number => number,
            decimal number => Convert.ToDouble(number, CultureInfo.InvariantCulture),
            string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }

    private static decimal? ConvertToDecimal(object? value)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var jsonNumber))
            {
                return jsonNumber;
            }

            value = JsonElementToString(element);
        }

        return value switch
        {
            decimal number => number,
            double number => Convert.ToDecimal(number, CultureInfo.InvariantCulture),
            float number => Convert.ToDecimal(number, CultureInfo.InvariantCulture),
            int number => number,
            long number => number,
            string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }

    private static string BuildFallbackHtml(DesignConversionCoreDispatchRequest request)
        => $"<main data-flow=\"FLOW-47\" data-job-id=\"{EscapeHtml(request.JobId)}\" data-source-type=\"{EscapeHtml(request.SourceType)}\"></main>";

    private static string EscapeHtml(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private static string Sha256Text(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record SourceTreeEntry(string Path, string Content);

    private sealed record RenderArtifactSpec(
        string? ArtifactId,
        string? Kind,
        string? Uri,
        string? Sha256,
        int? Width,
        int? Height);
}
