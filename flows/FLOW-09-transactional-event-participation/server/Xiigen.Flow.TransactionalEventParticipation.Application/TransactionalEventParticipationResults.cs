namespace Xiigen.Flow.TransactionalEventParticipation.Application;

public sealed record TransactionalEventParticipationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record TicketSnapshot(
    string TicketId,
    string EventId,
    string UserId,
    string Status,
    string QrToken,
    string Version);

public sealed record ParticipationSummary(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Tickets,
    int Count,
    IReadOnlyDictionary<string, int> StatusCounts);

public sealed record ParticipationAnalytics(
    int TicketCount,
    int PaymentCount,
    int RefundCount,
    int WaitlistCount,
    int RedeemedCount,
    double PaymentCompletionRate,
    bool ParticipantIdsStoredInAggregate);

public sealed record FeeBreakdown(decimal TicketPrice, decimal ServiceFee, decimal ProcessingFee, decimal Tax, decimal Total, string Currency);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
