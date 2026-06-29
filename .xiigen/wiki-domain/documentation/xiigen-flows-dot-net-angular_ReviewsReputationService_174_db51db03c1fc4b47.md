# xiigen-flows-dot-net-angular::ReviewsReputationService.ModerateReviewAsync

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationService.ModerateReviewAsync implements local project behavior with return type Task<DataProcessResult<ReviewsReputationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:539::xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:596::xiigen-flows-dot-net-angular::ReviewsReputationService.ResolveDoubleAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:71::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1002_ModerationHasPassRejectAndUncertainHumanQueuePaths, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:241::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF918_ReputationRecomputeReplayReturnsCachedAggregate.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:174

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<ReviewsReputationOutcome>> returned by xiigen-flows-dot-net-angular::ReviewsReputationService.ModerateReviewAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:71::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1002_ModerationHasPassRejectAndUncertainHumanQueuePaths
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:241::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF918_ReputationRecomputeReplayReturnsCachedAggregate

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:539::xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:596::xiigen-flows-dot-net-angular::ReviewsReputationService.ResolveDoubleAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:174::xiigen-flows-dot-net-angular::ReviewsReputationService.ModerateReviewAsync