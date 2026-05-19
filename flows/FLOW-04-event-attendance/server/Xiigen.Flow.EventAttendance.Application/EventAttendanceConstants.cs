namespace Xiigen.Flow.EventAttendance.Application;

public static class EventAttendanceConstants
{
    public const string FlowId = "FLOW-04";
    public const string FlowSlug = "event-attendance";
    public const string DynamicEndpoint = "/api/dynamic/event-attendance";

    public const string EventSource = "xiigen-dotnet-events";
    public const string Rsvps = "xiigen-event-rsvps";
    public const string CheckIns = "xiigen-event-checkins";
    public const string FeedbackWindows = "xiigen-feedback-windows";
    public const string Feedback = "xiigen-event-feedback";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-04-1",
        "CF-04-2",
        "CF-04-3",
        "CF-04-4",
        "CF-863",
        "CF-864",
        "CF-865",
        "CF-866"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "referral_user",
        "tenant_admin",
        "business_partner",
        "event_organiser",
        "platform_admin"
    ];

    public const string RsvpConfirmedEvent = "rsvp.confirmed";
    public const string RsvpWaitlistedEvent = "rsvp.waitlisted";
    public const string RsvpCancelledEvent = "RsvpCancelled";
    public const string WaitlistPromotionCompletedEvent = "WaitlistPromotionCompleted";
    public const string AttendeeCheckedInEvent = "AttendeeCheckedIn";
    public const string FeedbackWindowOpenedEvent = "FeedbackWindowOpened";
    public const string FeedbackSubmittedEvent = "FeedbackSubmitted";
    public const string TimeoutRetainedEvent = "xiigen.event-attendance.timeout-retained.v1";
}
