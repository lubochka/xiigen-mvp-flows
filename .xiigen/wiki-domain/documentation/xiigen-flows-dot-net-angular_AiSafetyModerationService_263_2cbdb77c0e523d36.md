# xiigen-flows-dot-net-angular::AiSafetyModerationService.TrackProgressAsync

## Meaning
xiigen-flows-dot-net-angular::AiSafetyModerationService.TrackProgressAsync implements local project behavior with return type Task<DataProcessResult<AiSafetyModerationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired. It is called by xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:189::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_2_ProgressRequiresSafetyTokenAndSkipNotFailWhenDisabled, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:147::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.ProgressPlateauRecommendsPreSeedingOnlyAndNeverAutoActs.

## Location
flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:263

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<AiSafetyModerationOutcome>> returned by xiigen-flows-dot-net-angular::AiSafetyModerationService.TrackProgressAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:189::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_2_ProgressRequiresSafetyTokenAndSkipNotFailWhenDisabled
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:147::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.ProgressPlateauRecommendsPreSeedingOnlyAndNeverAutoActs

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:263::xiigen-flows-dot-net-angular::AiSafetyModerationService.TrackProgressAsync