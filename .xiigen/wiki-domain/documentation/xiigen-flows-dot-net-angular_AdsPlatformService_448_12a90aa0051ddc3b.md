# xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateConsentAsync

## Meaning
xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateConsentAsync implements local project behavior with return type Task<ConsentGateResult>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:65::xiigen-flows-dot-net-angular::AdsPlatformService.ProcessAdRequestAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:42::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_1_ConsentGateWritesAllOutcomesAndBlocksDenied, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:74::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_2_ComposeSafetyPublishOrderIssuesTokenAndOutboxAfterState, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:94::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF470_UnsafeLessonIsRejectedAndNeverPublished, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:204::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.DashboardIsTenantIsolated, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:219::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope.

## Location
flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:448

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string userId
- CancellationToken cancellationToken

## Return
Task<ConsentGateResult> returned by xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateConsentAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:65::xiigen-flows-dot-net-angular::AdsPlatformService.ProcessAdRequestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:42::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_1_ConsentGateWritesAllOutcomesAndBlocksDenied
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:74::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_2_ComposeSafetyPublishOrderIssuesTokenAndOutboxAfterState
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:94::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF470_UnsafeLessonIsRejectedAndNeverPublished
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:204::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.DashboardIsTenantIsolated
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:219::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:448::xiigen-flows-dot-net-angular::AdsPlatformService.EvaluateConsentAsync