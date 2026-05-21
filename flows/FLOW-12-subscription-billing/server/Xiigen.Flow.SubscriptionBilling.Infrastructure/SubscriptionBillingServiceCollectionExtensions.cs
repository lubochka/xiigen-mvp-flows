using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SubscriptionBilling.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.SubscriptionBilling.Infrastructure;

public static class SubscriptionBillingServiceCollectionExtensions
{
    public static IServiceCollection AddSubscriptionBillingFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<IPaymentFabricService, InMemorySubscriptionPaymentFabric>();
        services.AddSingleton<SubscriptionBillingService>();
        return services;
    }
}

public sealed class InMemorySubscriptionPaymentFabric : IPaymentFabricService
{
    public Task<DataProcessResult<PaymentValidationResult>> ValidateAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = ReadString(request, "paymentMethodId");
        var declined = ReadBool(request, "paymentDeclined") ||
            paymentMethod?.StartsWith("pm_declined", StringComparison.OrdinalIgnoreCase) == true;

        if (string.IsNullOrWhiteSpace(paymentMethod) || declined)
        {
            return Task.FromResult(DataProcessResult<PaymentValidationResult>.Success(new PaymentValidationResult(
                false,
                string.Empty,
                "DECLINED",
                new Dictionary<string, object?> { ["provider"] = "in-memory" })));
        }

        return Task.FromResult(DataProcessResult<PaymentValidationResult>.Success(new PaymentValidationResult(
            true,
            $"vault://payment-methods/{paymentMethod}",
            "VALIDATED",
            new Dictionary<string, object?> { ["provider"] = "in-memory" })));
    }

    public Task<DataProcessResult<PaymentChargeResult>> ChargeAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = ReadString(request, "paymentMethodRef");
        var declined = ReadBool(request, "paymentDeclined") ||
            paymentMethod?.Contains("pm_declined", StringComparison.OrdinalIgnoreCase) == true;

        if (string.IsNullOrWhiteSpace(paymentMethod) || declined)
        {
            return Task.FromResult(DataProcessResult<PaymentChargeResult>.Success(new PaymentChargeResult(
                false,
                string.Empty,
                "DECLINED",
                new Dictionary<string, object?> { ["provider"] = "in-memory" })));
        }

        var invoiceId = ReadString(request, "invoiceId") ?? Guid.NewGuid().ToString("n");
        return Task.FromResult(DataProcessResult<PaymentChargeResult>.Success(new PaymentChargeResult(
            true,
            $"payref-{invoiceId}",
            "CHARGED",
            new Dictionary<string, object?>
            {
                ["provider"] = "in-memory",
                ["priceCents"] = ReadInt(request, "priceCents")
            })));
    }

    private static string? ReadString(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && value is bool boolValue && boolValue;

    private static int ReadInt(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && value is int intValue ? intValue : 0;
}
