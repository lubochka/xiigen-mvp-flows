namespace Xiigen.Flow.SchemaRegistryDag.Application;

public sealed record SchemaRegistryDagOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record SchemaVersionSummary(
    string SchemaType,
    string Version,
    string Status,
    string ChangeType,
    string VersionPin);

public sealed record SchemaCollection(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Schemas,
    int Count);

public sealed record DagRenderSnapshot(
    string Format,
    string Content,
    int NodeCount,
    int EdgeCount,
    bool FromCache);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
