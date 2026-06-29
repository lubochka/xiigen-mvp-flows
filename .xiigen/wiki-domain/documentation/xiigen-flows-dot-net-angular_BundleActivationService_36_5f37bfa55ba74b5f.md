# xiigen-flows-dot-net-angular::BundleActivationService.ActivateAsync

## Meaning
xiigen-flows-dot-net-angular::BundleActivationService.ActivateAsync implements local project behavior with return type Task<DataProcessResult<BundleActivationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleActivationService.cs:211::xiigen-flows-dot-net-angular::BundleActivationService.PopulateFreedomDefaultsAsync, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleValidatorService.cs:27::xiigen-flows-dot-net-angular::BundleValidatorService.ValidateBundleAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:24::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_StoresBeforeOutboxAndRunsDryRunBeforeFull, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:50::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TreatsEmptyIfMatchAsCurrentVersion, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:65::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReplaysCachedResponseForSameIdempotencyKey, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:88::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReturnsConcurrentUpdateForStaleVersion, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:108::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_RejectsBoundaryValidationWithoutPartialWrite, xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:125::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TimeoutRetainsOutboxRecord.

## Location
flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleActivationService.cs:36

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? ifMatch
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BundleActivationOutcome>> returned by xiigen-flows-dot-net-angular::BundleActivationService.ActivateAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:24::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_StoresBeforeOutboxAndRunsDryRunBeforeFull
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:50::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TreatsEmptyIfMatchAsCurrentVersion
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:65::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReplaysCachedResponseForSameIdempotencyKey
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:88::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_ReturnsConcurrentUpdateForStaleVersion
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:108::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_RejectsBoundaryValidationWithoutPartialWrite
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:125::xiigen-flows-dot-net-angular::BundleActivationServiceTests.ActivateAsync_TimeoutRetainsOutboxRecord

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleActivationService.cs:211::xiigen-flows-dot-net-angular::BundleActivationService.PopulateFreedomDefaultsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleValidatorService.cs:27::xiigen-flows-dot-net-angular::BundleValidatorService.ValidateBundleAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleActivationService.cs:36::xiigen-flows-dot-net-angular::BundleActivationService.ActivateAsync