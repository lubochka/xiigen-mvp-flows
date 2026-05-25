using Xiigen.Kernel;

namespace Xiigen.Flow.CanvaTextAdapter.Application;

public sealed record CanvaTextElement(
    string Id,
    string Content,
    decimal FontSize,
    CanvaFontStyle FontStyle,
    string TextAlign,
    CanvaRgbColor Color,
    decimal Width,
    decimal Height);

public sealed record CanvaFontStyle(string Family, int Weight);
public sealed record CanvaRgbColor(int R, int G, int B);
public sealed record SharedElement(string Type, string Characters, decimal Width, decimal Height);
public sealed record SharedStyle(decimal FontSize, string FontFamily, string FontWeight, string TextAlignHorizontal, string Color);
public sealed record CanvaAdapterReadResult(IReadOnlyList<SharedElement> Elements, IReadOnlyList<SharedStyle> Styles, IReadOnlyList<CanvaTextElement> SourceElements);
public sealed record CanvaEnhancedOutput(string Content, SharedStyle Style);
public sealed record CanvaWriteChild(string Text, CanvaFontStyle FontStyle, string TextAlign, decimal FontSize);
public sealed record CanvaWritePayload(string Type, IReadOnlyList<CanvaWriteChild> Children);
public sealed record CanvaAdapterWriteResult(int Written, int Failed, IReadOnlyList<CanvaWritePayload> Payloads);
public sealed record CanvaPackageReadback(string PackageName, string Version, bool IncludesStep1Invariants, bool IncludesFunctionalSpec, bool LocalRegistryReadback);
public sealed record CanvaTextAdapterOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);
public sealed record CanvaTextAdapterDashboard(IReadOnlyList<IReadOnlyDictionary<string, object?>> AdapterRuns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Publications, IReadOnlyDictionary<string, object?> Kpis);
public sealed record CanvaTextAdapterBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record CanvaTextAdapterRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record CanvaTextAdapterPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);

public interface ICanvaWriter
{
    Task<DataProcessResult<bool>> WriteAsync(CanvaWritePayload payload, CancellationToken cancellationToken = default);
}
