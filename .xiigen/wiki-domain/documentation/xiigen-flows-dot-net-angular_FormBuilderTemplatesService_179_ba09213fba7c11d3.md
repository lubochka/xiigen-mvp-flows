# xiigen-flows-dot-net-angular::FormBuilderTemplatesService.RecordUsageAsync

## Meaning
xiigen-flows-dot-net-angular::FormBuilderTemplatesService.RecordUsageAsync implements local project behavior with return type Task<DataProcessResult<FormBuilderTemplatesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired. It is called by xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Tests\FormBuilderTemplatesServiceTests.cs:166::xiigen-flows-dot-net-angular::FormBuilderTemplatesServiceTests.CF23_4_UsageMetricsAreAppendOnlyAndRejectPii.

## Location
flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:179

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FormBuilderTemplatesOutcome>> returned by xiigen-flows-dot-net-angular::FormBuilderTemplatesService.RecordUsageAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Tests\FormBuilderTemplatesServiceTests.cs:166::xiigen-flows-dot-net-angular::FormBuilderTemplatesServiceTests.CF23_4_UsageMetricsAreAppendOnlyAndRejectPii

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:179::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.RecordUsageAsync