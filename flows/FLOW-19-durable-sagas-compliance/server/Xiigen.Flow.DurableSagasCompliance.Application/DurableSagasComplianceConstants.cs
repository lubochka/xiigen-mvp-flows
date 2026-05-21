namespace Xiigen.Flow.DurableSagasCompliance.Application;

public static class DurableSagasComplianceConstants
{
    public const string FlowId = "FLOW-19";
    public const string FlowSlug = "durable-sagas-compliance";
    public const string DynamicEndpoint = "/api/dynamic/durable-sagas-compliance";

    public const string SagaStates = "xiigen-flow19-saga-states";
    public const string SagaStepLocks = "xiigen-flow19-saga-step-locks";
    public const string CompensationLocks = "xiigen-flow19-compensation-locks";
    public const string CompensationRecords = "xiigen-flow19-compensation-records";
    public const string ComplianceRecords = "xiigen-flow19-compliance-records";
    public const string RetentionArchives = "xiigen-flow19-retention-archive";
    public const string SagaAudits = "xiigen-flow19-saga-audits";
    public const string ExportJobs = "xiigen-flow19-export-jobs";

    public const string SagaStartedEvent = "xiigen.flow19.saga.started";
    public const string SagaStepExecutedEvent = "xiigen.flow19.saga.step.executed";
    public const string SagaAlreadyRunningEvent = "xiigen.flow19.saga.already-running";
    public const string SagaTimeoutRetainedEvent = "xiigen.flow19.saga.timeout.retained";
    public const string CompensationStepExecutedEvent = "xiigen.flow19.compensation.step.executed";
    public const string CompensationFailedEvent = "xiigen.flow19.compensation.failed";
    public const string CompensationCompletedEvent = "xiigen.flow19.compensation.completed";
    public const string ComplianceRecordWrittenEvent = "xiigen.flow19.compliance.record-written";
    public const string ComplianceWriteFailedEvent = "xiigen.flow19.compliance.write-failed";
    public const string RetentionHoldActiveEvent = "xiigen.flow19.retention.hold-active";
    public const string PurgeCompletedEvent = "xiigen.flow19.retention.purge-completed";
    public const string PurgeSkippedEvent = "xiigen.flow19.retention.purge-skipped";
    public const string ExportQueuedEvent = "xiigen.flow19.compliance.export-queued";

    public static readonly string[] RuleIds =
    [
        "CF-19-1",
        "CF-19-2",
        "CF-19-3",
        "CF-19-4",
        "CF-961",
        "CF-962",
        "CF-963",
        "CF-964"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T621",
        "T622",
        "T623",
        "T624"
    ];

    public static readonly string[] PrimaryRoles =
    [
        "anonymous",
        "tenant_user",
        "tenant_admin",
        "tenant_consumer",
        "tenant_ops",
        "saga_operator",
        "compliance_officer",
        "platform_ops",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "tenant_consumer",
        "tenant_ops",
        "saga_operator",
        "compliance_officer",
        "platform_ops",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] TenantOpsRoles =
    [
        "tenant_admin",
        "tenant_ops",
        "saga_operator",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ConsumerRoles =
    [
        "tenant_user",
        "tenant_consumer",
        "tenant_admin",
        "tenant_ops",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] PlatformOpsRoles =
    [
        "compliance_officer",
        "platform_ops",
        "platform_admin",
        "platform_support"
    ];
}
