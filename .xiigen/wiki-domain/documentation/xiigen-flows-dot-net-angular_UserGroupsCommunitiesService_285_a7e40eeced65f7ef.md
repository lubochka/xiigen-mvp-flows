# xiigen-flows-dot-net-angular::in.UpdateTierAsync

## Meaning
xiigen-flows-dot-net-angular::in.UpdateTierAsync implements local project behavior with return type Task<DataProcessResult<UserGroupsCommunitiesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:485::xiigen-flows-dot-net-angular::in.StoreMembershipAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:656::xiigen-flows-dot-net-angular::in.FindMembershipAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:714::xiigen-flows-dot-net-angular::in.NormalizeTier, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:214::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF883_ApprovalTierAndLeaveOperations_AreIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:249::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_TierManagement_BlocksSelfPromotionAndRequiresAdminForAdminTier.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:285

## Parameters
- string groupId
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<UserGroupsCommunitiesOutcome>> returned by xiigen-flows-dot-net-angular::in.UpdateTierAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:214::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF883_ApprovalTierAndLeaveOperations_AreIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:249::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_TierManagement_BlocksSelfPromotionAndRequiresAdminForAdminTier

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:485::xiigen-flows-dot-net-angular::in.StoreMembershipAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:656::xiigen-flows-dot-net-angular::in.FindMembershipAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:714::xiigen-flows-dot-net-angular::in.NormalizeTier
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:285::xiigen-flows-dot-net-angular::in.UpdateTierAsync