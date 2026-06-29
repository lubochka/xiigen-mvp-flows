# xiigen-flows-dot-net-angular::SubscriptionBillingService.CancelSubscriptionAsync

## Meaning
xiigen-flows-dot-net-angular::SubscriptionBillingService.CancelSubscriptionAsync implements local project behavior with return type Task<DataProcessResult<SubscriptionBillingOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:181::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF12_1_CancelledSubscriptionVoidsInvoiceWithoutChargeAttemptOrAudit.

## Location
flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:390

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SubscriptionBillingOutcome>> returned by xiigen-flows-dot-net-angular::SubscriptionBillingService.CancelSubscriptionAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:181::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF12_1_CancelledSubscriptionVoidsInvoiceWithoutChargeAttemptOrAudit

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:390::xiigen-flows-dot-net-angular::SubscriptionBillingService.CancelSubscriptionAsync