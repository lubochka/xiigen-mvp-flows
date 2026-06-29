# xiigen-flows-dot-net-angular::TenantLifecycleManagerService.RecordHealthAsync

## Meaning
xiigen-flows-dot-net-angular::TenantLifecycleManagerService.RecordHealthAsync implements local project behavior with return type Task<DataProcessResult<TenantLifecycleManagerOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Tests\TenantLifecycleManagerServiceTests.cs:121::xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.HealthAndOffboardingAreTenantScopedAndIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Tests\TenantLifecycleManagerServiceTests.cs:144::xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.DashboardSummarizesLifecycleHealthAndOffboarding.

## Location
flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Application\TenantLifecycleManagerService.cs:93

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<TenantLifecycleManagerOutcome>> returned by xiigen-flows-dot-net-angular::TenantLifecycleManagerService.RecordHealthAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Tests\TenantLifecycleManagerServiceTests.cs:121::xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.HealthAndOffboardingAreTenantScopedAndIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Tests\TenantLifecycleManagerServiceTests.cs:144::xiigen-flows-dot-net-angular::TenantLifecycleManagerServiceTests.DashboardSummarizesLifecycleHealthAndOffboarding

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-30-tenant-lifecycle-manager\server\Xiigen.Flow.TenantLifecycleManager.Application\TenantLifecycleManagerService.cs:93::xiigen-flows-dot-net-angular::TenantLifecycleManagerService.RecordHealthAsync