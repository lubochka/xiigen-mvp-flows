# xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.MembershipRequest

## Meaning
xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.MembershipRequest implements local project behavior with return type object?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:29::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0601_InviteOnlyJoinRequests_DoNotSeedFeedBeforeApproval, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:51::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0602_MembershipTier_IsServerSideAndPayloadTierIsRejected, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:72::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0603_DuplicateMembershipWithDifferentKey_ReturnsExistingRecordNotFailure, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:88::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0604_MembershipApprovalFeedTierAndAccessLogs_AreTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:116::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF878_ConcurrentJoin_AllowsOneWinnerAndOneConflict, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:137::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF879_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:173::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF881_TimeoutReturnsRetryHintAndRetainsOutboxRecord, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:193::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF882_ApprovalConcurrency_UsesSameMembershipGuard, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:214::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF883_ApprovalTierAndLeaveOperations_AreIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:233::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_AdminApproval_ActivatesInviteOnlyMembershipAndThenSeedsFeed, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:249::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_TierManagement_BlocksSelfPromotionAndRequiresAdminForAdminTier, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:300::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_LeaveMembership_StoresStateBeforeOutbox.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:315

## Parameters
- string groupId
- string userId
- string action
- string groupType
- bool simulateSlowWrite
- bool simulateFabricTimeout

## Return
object?> returned by xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.MembershipRequest

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:29::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0601_InviteOnlyJoinRequests_DoNotSeedFeedBeforeApproval
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:51::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0602_MembershipTier_IsServerSideAndPayloadTierIsRejected
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:72::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0603_DuplicateMembershipWithDifferentKey_ReturnsExistingRecordNotFailure
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:88::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0604_MembershipApprovalFeedTierAndAccessLogs_AreTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:116::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF878_ConcurrentJoin_AllowsOneWinnerAndOneConflict
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:137::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF879_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:173::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF881_TimeoutReturnsRetryHintAndRetainsOutboxRecord
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:193::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF882_ApprovalConcurrency_UsesSameMembershipGuard
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:214::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF883_ApprovalTierAndLeaveOperations_AreIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:233::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_AdminApproval_ActivatesInviteOnlyMembershipAndThenSeedsFeed
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:249::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_TierManagement_BlocksSelfPromotionAndRequiresAdminForAdminTier
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:300::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_LeaveMembership_StoresStateBeforeOutbox

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:315::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.MembershipRequest