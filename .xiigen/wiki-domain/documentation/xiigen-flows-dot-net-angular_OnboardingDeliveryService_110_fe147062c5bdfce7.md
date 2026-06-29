# xiigen-flows-dot-net-angular::OnboardingDeliveryService.StoreDeliveryAsync

## Meaning
xiigen-flows-dot-net-angular::OnboardingDeliveryService.StoreDeliveryAsync implements local project behavior with return type Task<DataProcessResult<OnboardingOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:38::xiigen-flows-dot-net-angular::OnboardingDeliveryService.DeliverInvitationAsync.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:110

## Parameters
- string userId
- string materialType
- IReadOnlyDictionary<string
- object?>? socialParams
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<OnboardingOutcome>> returned by xiigen-flows-dot-net-angular::OnboardingDeliveryService.StoreDeliveryAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:38::xiigen-flows-dot-net-angular::OnboardingDeliveryService.DeliverInvitationAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:110::xiigen-flows-dot-net-angular::OnboardingDeliveryService.StoreDeliveryAsync