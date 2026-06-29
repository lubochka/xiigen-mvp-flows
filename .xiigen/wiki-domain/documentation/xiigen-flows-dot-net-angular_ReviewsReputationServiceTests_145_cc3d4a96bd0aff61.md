# xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:515::xiigen-flows-dot-net-angular::ReviewsReputationService.GetReviewsAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:264::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review. No internal caller was detected.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:145

## Parameters
- none recorded

## Return
Task returned by xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Tests\BundleActivationServiceTests.cs:195::xiigen-flows-dot-net-angular::FlowFixture.PushContext
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:338::xiigen-flows-dot-net-angular::FlowFixture.Create
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:515::xiigen-flows-dot-net-angular::ReviewsReputationService.GetReviewsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:264::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:145::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak