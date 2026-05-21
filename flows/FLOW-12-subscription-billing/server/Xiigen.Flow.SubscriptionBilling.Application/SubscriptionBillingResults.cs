namespace Xiigen.Flow.SubscriptionBilling.Application;

public sealed record BfaRule(
    string FlowId,
    string RuleId,
    string RuleType,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record SubscriptionBillingOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record SubscriptionBillingDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Plans,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Subscriptions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Invoices,
    IReadOnlyDictionary<string, object?> Mrr);

public sealed record PaymentValidationResult(
    bool Valid,
    string PaymentMethodRef,
    string Status,
    IReadOnlyDictionary<string, object?> Metadata);

public sealed record PaymentChargeResult(
    bool Charged,
    string PaymentReference,
    string Status,
    IReadOnlyDictionary<string, object?> Metadata);

public interface IPaymentFabricService
{
    Task<Xiigen.Kernel.DataProcessResult<PaymentValidationResult>> ValidateAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default);

    Task<Xiigen.Kernel.DataProcessResult<PaymentChargeResult>> ChargeAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default);
}
