# xiigen-flows-dot-net-angular::OnboardingDeliveryService.DeliverInvitationAsync

## Meaning
xiigen-flows-dot-net-angular::OnboardingDeliveryService.DeliverInvitationAsync implements local project behavior with return type Task<DataProcessResult<OnboardingOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:110::xiigen-flows-dot-net-angular::OnboardingDeliveryService.StoreDeliveryAsync, xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:150::xiigen-flows-dot-net-angular::OnboardingDeliveryService.GetStringAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:240::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_OnboardingRequiresAllMaterialsBeforeCompletionEvent.

## Location
flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:38

## Parameters
- string userId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<OnboardingOutcome>> returned by xiigen-flows-dot-net-angular::OnboardingDeliveryService.DeliverInvitationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Tests\UserRegistrationServiceTests.cs:240::xiigen-flows-dot-net-angular::UserRegistrationServiceTests.CF01_OnboardingRequiresAllMaterialsBeforeCompletionEvent

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:110::xiigen-flows-dot-net-angular::OnboardingDeliveryService.StoreDeliveryAsync
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:150::xiigen-flows-dot-net-angular::OnboardingDeliveryService.GetStringAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-01-user-registration\server\Xiigen.Flow.UserRegistration.Application\OnboardingDeliveryService.cs:38::xiigen-flows-dot-net-angular::OnboardingDeliveryService.DeliverInvitationAsync