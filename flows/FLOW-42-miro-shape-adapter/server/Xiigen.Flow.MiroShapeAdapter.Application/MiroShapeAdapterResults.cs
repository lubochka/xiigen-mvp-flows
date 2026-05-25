using Xiigen.Kernel;

namespace Xiigen.Flow.MiroShapeAdapter.Application;

public sealed record MiroBoardItem(
    string Id,
    string Type,
    string? Title,
    string? Content,
    string? Shape,
    MiroBoardStyle Style,
    MiroPosition Position,
    MiroGeometry Geometry,
    MiroConnectorEndpoint? StartItem = null,
    MiroConnectorEndpoint? EndItem = null);

public sealed record MiroBoardStyle(string FillColor, string BorderColor, string TextColor, decimal? FontSize = null, decimal? BorderRadius = null);
public sealed record MiroPosition(decimal X, decimal Y);
public sealed record MiroGeometry(decimal Width, decimal Height);
public sealed record MiroConnectorEndpoint(string Id);

public sealed record SharedArchitectElement(
    string Type,
    string Label,
    string Color,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string? FromId = null,
    string? ToId = null);

public sealed record SharedArchitectStyle(string FillColor, string BorderColor, string TextColor, string ElementType);
public sealed record MiroShapeClassification(string RuleId, string Label, string SharedType, decimal Confidence);
public sealed record MiroParsedContent(string Text, IReadOnlyList<string> Links);
public sealed record MiroAdapterReadResult(IReadOnlyList<SharedArchitectElement> Elements, IReadOnlyList<SharedArchitectStyle> Styles, IReadOnlyList<MiroBoardItem> SourceItems);
public sealed record ArchitectEnhancedOutput(SharedArchitectElement Element, SharedArchitectStyle Style);
public sealed record MiroWritePayload(string Type, string Title, SharedArchitectStyle Style, MiroPosition Position, MiroGeometry Geometry);
public sealed record MiroAdapterWriteResult(int Written, int Failed, IReadOnlyList<MiroWritePayload> Payloads);
public sealed record MiroLayoutNode(MiroBoardItem Item, IReadOnlyList<MiroLayoutNode> Children, string LayoutMode, decimal Gap, MiroPadding Padding);
public sealed record MiroPadding(decimal Top, decimal Right, decimal Bottom, decimal Left);
public sealed record MiroSpatialAnalysis(IReadOnlyList<MiroLayoutNode> Roots, int NodeCount, decimal Tolerance);
public sealed record MiroPackageReadback(string PackageName, string Version, bool IncludesStep1Invariants, bool IncludesFunctionalSpec, bool LocalRegistryReadback);
public sealed record MiroShapeAdapterOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);
public sealed record MiroShapeAdapterDashboard(IReadOnlyList<IReadOnlyDictionary<string, object?>> AdapterRuns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Publications, IReadOnlyDictionary<string, object?> Kpis);
public sealed record MiroShapeAdapterBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record MiroShapeAdapterRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record MiroShapeAdapterPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);

public interface IMiroWriter
{
    Task<DataProcessResult<bool>> WriteAsync(MiroWritePayload payload, CancellationToken cancellationToken = default);
}
