namespace Xiigen.Flow.EventAttendance.Application;

public sealed record EventAttendanceOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record EventAttendanceDashboard(
    string EventId,
    string EventTitle,
    int? Capacity,
    int ConfirmedCount,
    int WaitlistCount,
    int CheckedInCount,
    int FeedbackCount,
    string CapacityLabel,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Attendees);

public sealed record EventAttendanceStatus(
    string EventId,
    string AttendeeId,
    string Status,
    string Routed,
    string? RsvpId,
    string? CheckInId,
    string? FeedbackWindowId,
    bool Cancellable);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
