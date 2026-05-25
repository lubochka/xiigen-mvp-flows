using Xiigen.Kernel;

namespace Xiigen.Flow.FramerAdapter.Application;

public sealed record FramerNode(
    string Id,
    string Type,
    string? Name,
    string? Text,
    string? Characters,
    double? Width,
    double? Height,
    string? FontFamily,
    object? FontSize,
    object? FontWeight,
    object? LineHeight,
    object? LetterSpacing,
    object? Color,
    IReadOnlyList<object?>? Fills,
    double? Opacity);

public sealed record FramerSolidPaint(string Type, string Color, double Opacity);
public sealed record FramerTextElement(string Id, string Name, string Text, double Width, double Height);
public sealed record FramerTextStyle(string FontFamily, double FontSize, int FontWeight, string FontStyleName, object LineHeight, object LetterSpacing, string Color, double Opacity, FramerSolidPaint Paint);
public sealed record FramerTextReadResult(IReadOnlyList<FramerTextElement> Elements, IReadOnlyList<FramerTextStyle> Styles, IReadOnlyList<FramerNode> SourceNodes);
public sealed record FramerTextAttributes(IReadOnlyDictionary<string, object?> Values);
public sealed record FramerTextUpdate(string NodeId, FramerTextAttributes Attributes);
public sealed record FramerTextOutput(FramerTextElement Element, FramerTextStyle Style, string? NextText);
public sealed record FramerTextWriteResult(int Written, int Failed, IReadOnlyList<FramerTextUpdate> Updates);
public sealed record FramerStoredState(string Key, IReadOnlyDictionary<string, object?> Value);
public sealed record FramerAdapterOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);
public sealed record FramerAdapterDashboard(IReadOnlyList<IReadOnlyDictionary<string, object?>> AdapterRuns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Publications, IReadOnlyDictionary<string, object?> Kpis);
public sealed record FramerAdapterBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record FramerAdapterRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record FramerAdapterPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
