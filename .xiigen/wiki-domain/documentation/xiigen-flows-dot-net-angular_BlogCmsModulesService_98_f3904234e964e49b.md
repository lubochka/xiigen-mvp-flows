# xiigen-flows-dot-net-angular::BlogCmsModulesService.AutosaveDraftAsync

## Meaning
xiigen-flows-dot-net-angular::BlogCmsModulesService.AutosaveDraftAsync implements local project behavior with return type Task<DataProcessResult<BlogCmsModulesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:135::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.AutosaveUsesBlogSessionPrefixAndRejectsUnsafeHtml, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:214::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.DashboardIsTenantIsolated.

## Location
flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Application\BlogCmsModulesService.cs:98

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BlogCmsModulesOutcome>> returned by xiigen-flows-dot-net-angular::BlogCmsModulesService.AutosaveDraftAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:135::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.AutosaveUsesBlogSessionPrefixAndRejectsUnsafeHtml
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:214::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.DashboardIsTenantIsolated

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Application\BlogCmsModulesService.cs:98::xiigen-flows-dot-net-angular::BlogCmsModulesService.AutosaveDraftAsync