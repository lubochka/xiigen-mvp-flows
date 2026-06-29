# xiigen-flows-dot-net-angular::BlogCmsModulesService.SubmitCommentAsync

## Meaning
xiigen-flows-dot-net-angular::BlogCmsModulesService.SubmitCommentAsync implements local project behavior with return type Task<DataProcessResult<BlogCmsModulesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired. It is called by xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:174::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CommentsRequireBudgetPrecheckAndRespectThreadDepth, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:214::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.DashboardIsTenantIsolated.

## Location
flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Application\BlogCmsModulesService.cs:136

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BlogCmsModulesOutcome>> returned by xiigen-flows-dot-net-angular::BlogCmsModulesService.SubmitCommentAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:174::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CommentsRequireBudgetPrecheckAndRespectThreadDepth
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:214::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.DashboardIsTenantIsolated

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Application\BlogCmsModulesService.cs:136::xiigen-flows-dot-net-angular::BlogCmsModulesService.SubmitCommentAsync